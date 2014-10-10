using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E705Proxy
{
    interface IProxy
    {
        int Port { get; }
        //int Connections { get; }
        long BytesTrans { get; }
        void Start();
        void Stop();
    }
}
