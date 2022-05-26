using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

using CPCPipe;

namespace DemoServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //if(args == null ||
            //    args.Length != 1)
            MessageBox.Show($"Server Application is Running! Para {args[0]} {args[1]}");
            PipeClient client = new PipeClient();
            client.ErrMessage += Client_ErrMessage;
            await client.Connect(new System.Net.IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])));


        }

        private static void Client_ErrMessage(string obj)
        {
            MessageBox.Show(obj);
        }
    }
}
