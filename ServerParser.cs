using System;
using System.Net.Sockets;

namespace E705Proxy
{
    abstract class ServerParser
    {
        static public TcpClient Parse(string reqHeader)
        {
            string Method, Path;
            string Host = null;
            int Port = 80;
            string[] arrHeaders = reqHeader.Split(new char[] { '\r', '\n' }, 20, StringSplitOptions.RemoveEmptyEntries);
            if (arrHeaders.Length < 1)
                return null;

            Method = arrHeaders[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
            Path = arrHeaders[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
            // Get Host from "host: xxx.com"
            for (int i = 1; i < arrHeaders.Length; i++)
            {
                string strTemp = arrHeaders[i].Trim();
                if (strTemp.StartsWith(":"))
                    strTemp = strTemp.Substring(1).Trim();
                if (strTemp.StartsWith("host", true, null))
                {
                    Host = strTemp.Substring(5).Trim();
                    break;
                }
            }
            // The case starting an SSL tunnel
            if (Method.StartsWith("CONNECT", true, null)) Port = 443;
            // Handle the header without "host: xxx.com"
            if (string.IsNullOrEmpty(Host) && Path.Length >= 10)
            {
                if (Path.IndexOf(@"://") > -1) // has protocol
                {
                    string protocol = Path.Split(':')[0];
                    Host = Path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    //if (protocol.StartsWith("https", true, null)) Port = 443;
                    //if (protocol.StartsWith("ftp", true, null)) Port = 21;
                }
                else
                    Host = Path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            // Handle the case with specified port number
            int iTemp = Host.LastIndexOf(":");
            if (iTemp > -1)
            {
                Port = int.Parse(Host.Substring(iTemp + 1));
                Host = Host.Substring(0, iTemp);
            }
            // Host & Port are ready, return TcpClient to server
            TcpClient server = null;
            try
            { server = new TcpClient(Host, Port); }
            //{ server = new TcpClient("127.0.0.1", 8087); }
            catch { }
            return server;
        }
    }
}
