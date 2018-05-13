using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fisherman
{
    class Connector
    {
        NetworkStream engineStream;

        internal async void Start(string[] args)
        {
            Connect();

            var inputTask = Console.OpenStandardInput().CopyToAsync(engineStream);
            engineStream.CopyTo(Console.OpenStandardOutput());

            await inputTask;
        }

        private void Connect()
        {
            var hostname = "localhost";
            var port = 24377;

            var engine = new TcpClient();
            engine.Connect(hostname, port);

            engineStream = engine.GetStream();
        }
    }
}
