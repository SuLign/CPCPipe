using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using CommDef;
using CPCPipe;

namespace DemoServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //if(args == null ||
            //    args.Length != 1)
            //MessageBox.Show($"Server Application is Running! Para {args[0]} {args[1]}");
            PipeClient client = new PipeClient();
            client.ErrMessage += Client_ErrMessage;
            await client.Connect(new System.Net.IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])));
            LibComDef[] comDef = new LibComDef[] { new LibComDef { key = 110021, value = 4567 } };
            client.RegistFunc<LibComDef>("Reply", (e) =>
            {
                MessageBox.Show("Replied");
                var rep = e as LibComDef;
                MessageBox.Show($"{rep.key}, {rep.value}");
            });
            client.SendMessage(comDef, "ComDef");
            Application app = new Application();
            app.Run();
        }

        private static void Client_ErrMessage(string obj)
        {
            MessageBox.Show(obj);
        }
    }
}
