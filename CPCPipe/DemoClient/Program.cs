using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

using CPCPipe.Interfaces;

namespace DemoClient
{
    class Program
    {
        delegate bool ConsoleCtrlDelegate(int dwCtrlType);

        const int CTRL_CLOSE_EVENT = 2;

        [DllImport("Kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handle, bool Add);

        static void Main(string[] args)
        {
           
            if (!File.Exists("DemoServer.exe"))
            {
                throw new FileNotFoundException("程序启动失败，服务端程序未找到！");
            }
            PipeServer server = new PipeServer();
            if (server.StartListenning(out var endp))
            {
                var addr = $"{endp.Address.ToString()} {endp.Port}";
                var proc = new Process();
                proc.StartInfo = new ProcessStartInfo("DemoServer.exe", addr);
                proc.Start();
                ConsoleCtrlDelegate ctrlDelegate = new ConsoleCtrlDelegate((e) => {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    return true;
                });
                SetConsoleCtrlHandler(ctrlDelegate, true);
            }

            Application application = new Application();
            application.Run();

        }
    }
}
