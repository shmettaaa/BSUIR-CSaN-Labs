using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSaN_Lab2.Logic
{
    public static class FileTransfer
    {
        private const string ReceivedFolder = "ReceivedFiles";

        static FileTransfer()
        {
            Directory.CreateDirectory(ReceivedFolder);
        }

        public static async Task SendFileAsync(NetworkStream stream, string filePath, CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            string fileName = Path.GetFileName(filePath);
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath, ct);

            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] sizeBytes = BitConverter.GetBytes(fileBytes.Length);

            await stream.WriteAsync(new byte[] { 2 }, 0, 1, ct);
            await stream.WriteAsync(BitConverter.GetBytes(nameBytes.Length), 0, 4, ct);
            await stream.WriteAsync(nameBytes, 0, nameBytes.Length, ct);
            await stream.WriteAsync(sizeBytes, 0, sizeBytes.Length, ct);
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length, ct);
            await stream.FlushAsync(ct);
        }

        public static async Task<string> ReceiveFileAsync(NetworkStream stream, CancellationToken ct = default)
        {
            byte[] nameLenBytes = new byte[4];
            await ReadExactAsync(stream, nameLenBytes, ct);
            int nameLen = BitConverter.ToInt32(nameLenBytes);

            byte[] nameBytes = new byte[nameLen];
            await ReadExactAsync(stream, nameBytes, ct);
            string fileName = Encoding.UTF8.GetString(nameBytes);

            byte[] sizeBytes = new byte[4];
            await ReadExactAsync(stream, sizeBytes, ct);
            int fileSize = BitConverter.ToInt32(sizeBytes);

            byte[] fileBytes = new byte[fileSize];
            await ReadExactAsync(stream, fileBytes, ct);

            string savePath = Path.Combine(ReceivedFolder, fileName);
            if (File.Exists(savePath))
            {
                string ext = Path.GetExtension(fileName);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                savePath = Path.Combine(ReceivedFolder, $"{nameWithoutExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            }

            await File.WriteAllBytesAsync(savePath, fileBytes, ct);

            return savePath;
        }

        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, ct);
                if (read == 0)
                    throw new IOException("Ошибка во время чтения файла");

                totalRead += read;
            }
        }
    }
}