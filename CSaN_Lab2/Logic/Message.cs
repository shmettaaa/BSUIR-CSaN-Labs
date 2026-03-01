using System.Text;

namespace CSaN_Lab2.Logic
{
    public class Message
    {
        public string Text { get; set; }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(Text);
        }

        public static Message FromBytes(byte[] data)
        {
            return new Message { Text = Encoding.UTF8.GetString(data) };
        }
    }
}