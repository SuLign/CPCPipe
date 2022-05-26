using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace CPCPipe.Interfaces
{
    public class PipeServer
    {
        private Socket _sock;
        private const int DEFAULT_PORT = 52164;
        private byte[] _buffer;
        private int _bufferLength = 0;
        private ConcurrentDictionary<string, Action<object>> _actionList;
        private long _lastTick = -1;
        private Task _heartBeatsMonitor;

        private bool _clientConnected = false;

        public PipeServer()
        {
            _actionList = new ConcurrentDictionary<string, Action<object>>();
            _heartBeatsMonitor = new Task(() =>
            {
                while (true)
                {
                    if (_lastTick != -1)
                    {
                        if (DateTime.Now.Ticks - _lastTick > 10_000)
                        {
                            _clientConnected = false;
                        }
                        else
                        {
                            _clientConnected = true;
                        }
                        Task.Delay(1000);
                    }
                }
            });
        }

        public bool StartListenning(out IPEndPoint endPoint)
        {
            var port = DEFAULT_PORT;
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            endPoint = null;
            while (true)
            {
                try
                {
                    endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                    _sock.Bind(endPoint);
                    //_sock.Listen(10);
                    _buffer = new byte[655360];

                    _sock.BeginReceive(_buffer, 0, 65536, SocketFlags.None, callback, _sock);

                    void callback(IAsyncResult e)
                    {
                        var sock = e.AsyncState as Socket;
                        var readLen = sock.EndReceive(e);
                        if (readLen > 0)
                        {
                            _lastTick = DateTime.Now.Ticks;
                            _clientConnected = true;
                            var str = Encoding.Default.GetString(_buffer, 0, readLen);
                            try
                            {
                                var pipemsg = JsonConvert.DeserializeObject<PipeMessage>(str);
                                if (_actionList.ContainsKey(pipemsg.MessageName))
                                {
                                    _actionList[pipemsg.MessageName]?.Invoke(pipemsg.Value);
                                }
                            }
                            catch
                            {

                            }
                        }
                        sock.BeginReceive(_buffer, 0, 65536, SocketFlags.None, callback, sock);
                    };
                    break;
                }
                catch (SocketException socketexp)
                {
                    if (socketexp.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        port++;
                        if (port < 65535) continue;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return true;
        }

        public bool SendMessage<T>(T message, string messageTitle)
        {
            if (_sock == null || !_sock.Connected || !_clientConnected) return false;
            var pipmsg = PipeMessage.CreateMessage(messageTitle, message);
            var json = JsonConvert.SerializeObject(pipmsg);
            var jsonBytes = Encoding.Default.GetBytes(json);
            _sock.Send(jsonBytes);
            return true;
        }

        public bool RegistFunc(string funcKey, Action<object> func)
        {
            if (_actionList.ContainsKey(funcKey))
            {
                return false;
            }
            _actionList.TryAdd(funcKey, func);
            return true;
        }
    }
}
