using System.IO;

namespace CSaN_Lab2.Logic
{
    public class FileTransfer
    {
        private const int BufferSize = 4096; 

        
        public void SendFile(IClientConnection connection, string filePath)
        {
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                byte[] sizeBytes = BitConverter.GetBytes(fileStream.Length);
                connection.Send(sizeBytes);

                byte[] buffer = new byte[BufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, BufferSize)) > 0)
                {
                    connection.Send(buffer.AsSpan(0, bytesRead).ToArray());
                }
            }
        }

        public void ReceiveFile(IClientConnection connection, string savePath)
        {
            using (FileStream fileStream = File.OpenWrite(savePath))
            {
               
                byte[] sizeBytes = connection.Receive();
                long fileSize = BitConverter.ToInt64(sizeBytes, 0);

                byte[] buffer = new byte[BufferSize];
                long remaining = fileSize;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(BufferSize, remaining);
                    byte[] received = connection.Receive();
                    fileStream.Write(received, 0, toRead);
                    remaining -= toRead;
                }
            }
        }
    }
}