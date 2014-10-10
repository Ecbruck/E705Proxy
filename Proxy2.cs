using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E705Proxy
{
    class Proxy2 : IProxy
    {
        public int Port { get; protected set; }
        public List<ProxyConnection> Connections { get { return proxyConnections; } }
        private object connectionsLock;
        private List<ProxyConnection> proxyConnections;
        public long BytesTrans { get; private set; }
        private object bytestransLock;
        public Proxy2(int Port, bool autoStart = true)
        {
            proxyConnections = new List<ProxyConnection>();
            BytesTrans = 0;
            connectionsLock = new object();
            bytestransLock = new object();
            this.Port = Port;
            if (autoStart)
                Start();
        }

        TcpListener listener;
        public void Start()
        {
            if (listener != null)
                return;
            new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        listener = new TcpListener(IPAddress.Any, Port);
                        listener.Start();

                        while (true)
                        {
                            TcpClient browserConnection = listener.AcceptTcpClient();
                            var conn = new ProxyConnection(browserConnection);
                            conn.Closed += (o, e) =>
                            {
                                lock (connectionsLock)
                                    Connections.Remove(conn);
                            };
                            conn.Transferred += inc =>
                            {
                                lock (bytestransLock)
                                    BytesTrans += inc;
                            };
                            lock (connectionsLock)
                                Connections.Add(conn);
                        }
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10004)
                            break;
                    }
                    catch { }
                    Thread.Sleep(500);
                }
            })).Start();
        }
        public void Stop()
        {
            if (listener == null)
                return;
            listener.Stop();
            listener = null;
            Task.Factory.StartNew(() =>
            {
                foreach (var c in Connections.ToArray())
                    try { c.Close(); }
                    catch { }
            });
        }
    }
    class ProxyConnection
    {
        public TcpClient Browser { get; private set; }
        public TcpClient Web { get; private set; }
        public long BytesTrans { get; private set; }
        public event Action<long> Transferred;
        public void Close()
        {
            if (closing)
                return;
            closing = true;
            try { Browser.Close(); }
            catch { }
            try { Web.Close(); }
            catch { }
            try { Closed(null, null); }
            catch { }
        }
        private bool closing = false;
        public event EventHandler Closed;
        public ProxyConnection(TcpClient browser)
        {
            Browser = browser;
            this.Transferred += inc =>
            {
                lock (Transferred)
                    BytesTrans += inc;
            };
            beginRead(Browser, null, firstRequestCallback);
        }


        void proxyCallback(IAsyncResult result)
        {
            ProxyParams param = result.AsyncState as ProxyParams;
            int readCount;
            try
            {
                readCount = param.ClientFrom.GetStream().EndRead(result);
            }
            catch
            {
                Close();
                return;
            }
            if (readCount < 1)
            {
                Close();
                return;
            }
            try
            {
                param.ClientTo.GetStream().Write(param.buffer, 0, readCount);
            }
            catch
            {
                Close();
                return;
            }
            Transferred(readCount);
            beginRead(param.ClientFrom, param.ClientTo, proxyCallback);
        }

        void firstRequestCallback(IAsyncResult result)
        {
            ProxyParams param = result.AsyncState as ProxyParams;
            int readCount;
            try
            {
                readCount = param.ClientFrom.GetStream().EndRead(result);
            }
            catch
            {
                Close();
                return;
            }

            try
            {
                Web = ServerParser.Parse(Encoding.Default.GetString(param.buffer, 0, readCount));
            }
            catch { System.Diagnostics.Debug.WriteLine("\n =-= Server Parse Error: =-=\n{0}\n", Encoding.Default.GetString(param.buffer, 0, readCount)); }

            if (Web == null)
            {
                Close();
                return;
            }

            if (((IPEndPoint)Web.Client.RemoteEndPoint).Port == 443)
            {
                param.ClientFrom.GetStream().Write(connectEstBuf, 0, connectEstBuf.Length);
            }
            else
            {
                try
                {
                    Web.GetStream().Write(param.buffer, 0, readCount);
                }
                catch
                {
                    param.ClientFrom.Close();
                    Console.WriteLine("--- Removed client pair ---");
                    return;
                }
                Transferred(readCount);
            }
            beginRead(Web, param.ClientFrom, proxyCallback);
            beginRead(param.ClientFrom, Web, proxyCallback);
        }

        void beginRead(TcpClient clientFrom, TcpClient clientTo, AsyncCallback callback)
        {
            var buffer = new byte[20480];
            try
            {
                clientFrom.GetStream().BeginRead(
                    buffer, 0, buffer.Length,
                    callback,
                    new ProxyParams
                    {
                        ClientFrom = clientFrom,
                        ClientTo = clientTo,
                        buffer = buffer
                    });
            }
            catch
            {
                if (clientTo != null)
                    clientTo.Close();
            }
        }

        readonly byte[] connectEstBuf = Encoding.Default.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
        class ProxyParams
        {
            public TcpClient ClientFrom { get; set; }
            public TcpClient ClientTo { get; set; }
            public byte[] buffer { get; set; }
        }
    }
}
