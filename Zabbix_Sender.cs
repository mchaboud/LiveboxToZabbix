using System.Net.Sockets;
using System.Text;
//based on https://github.com/yanngg33/Zabbix_Sender  
namespace Zabbix_Sender
{
    public class ZS_Data
    {
        public string host { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public ZS_Data(string Zbxhost, string Zbxkey, string Zbxval)
        {
            host = Zbxhost;
            key = Zbxkey;
            value = Zbxval;
        }
    }

    public class ZS_Response
    {
        public string response { get; set; }
        public string info { get; set; }
    }
    public class ZS_Request
    {
        public string request { get; set; }
        public ZS_Data[] data { get; set; }
        public ZS_Request()
        {
            request = "sender data";
        }
        public ZS_Request(string ZbxHost, string ZbxKey, string ZbxVal) : base()
        {
            data = new ZS_Data[] { new ZS_Data(ZbxHost, ZbxKey, ZbxVal) };
        }

        public ZS_Response Send(string ZbxServer, int ZbxPort = 10051, int ZbxTimeOut = 750)
        {
            string jr = System.Text.Json.JsonSerializer.Serialize(this);
            using (TcpClient lTCPc = new TcpClient(ZbxServer, ZbxPort))
            using (NetworkStream lStream = lTCPc.GetStream())
            {
                byte[] Header = Encoding.UTF8.GetBytes("ZBXD\x01");
                byte[] DataLen = BitConverter.GetBytes((long)jr.Length);
                byte[] Content = Encoding.UTF8.GetBytes(jr);
                byte[] Message = new byte[Header.Length + DataLen.Length + Content.Length];
                Buffer.BlockCopy(Header, 0, Message, 0, Header.Length);
                Buffer.BlockCopy(DataLen, 0, Message, Header.Length, DataLen.Length);
                Buffer.BlockCopy(Content, 0, Message, Header.Length + DataLen.Length, Content.Length);

                lStream.Write(Message, 0, Message.Length);
                lStream.Flush();
                int counter = 0;
                while (!lStream.DataAvailable)
                {
                    if (counter < ZbxTimeOut / 50)
                    {
                        counter++;
                        Task.Delay(50).Wait();
                    }
                    else
                        throw new TimeoutException();
                }

                byte[] resbytes = new Byte[1024];
                int i = lStream.Read(resbytes, 0, resbytes.Length);
                string s = Encoding.UTF8.GetString(resbytes).Remove(i);
                string jsonRes = s.Substring(s.IndexOf('{'));
                return System.Text.Json.JsonSerializer.Deserialize<ZS_Response>(jsonRes);
            }
        }
    }
}