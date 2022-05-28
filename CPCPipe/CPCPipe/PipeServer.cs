using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace CPCPipe
{
    public class PipeServer
    {
        private Socket _sock;
        private const int DEFAULT_PORT = 52164;
        private byte[] _buffer;
        private ConcurrentDictionary<string, Delegate> _actionList;
        private ConcurrentDictionary<string, Type> _objTypes;
        private long _lastTick = -1;
        private Task _heartBeatsMonitor;
        private IPEndPoint _remoteEnd;
        private bool _clientConnected = false;

        public PipeServer()
        {
            _actionList = new ConcurrentDictionary<string, Delegate>();
            _objTypes = new ConcurrentDictionary<string, Type>();
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

                    EndPoint remoteEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1);
                    _sock.BeginReceiveFrom(_buffer, 0, 65536, SocketFlags.None, ref remoteEnd, callback, remoteEnd);

                    void callback(IAsyncResult e)
                    {
                        _remoteEnd = e.AsyncState as IPEndPoint;
                        var readLen = _sock.EndReceiveFrom(e, ref remoteEnd);
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
                                    if (_objTypes.ContainsKey(pipemsg.MessageName))
                                    {
                                        _actionList[pipemsg.MessageName]?.DynamicInvoke(pipemsg.GetValue(_objTypes[pipemsg.MessageName]));
                                    }
                                    else
                                    {
                                        _actionList[pipemsg.MessageName]?.DynamicInvoke(pipemsg.Value);
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                        _sock.BeginReceiveFrom(_buffer, 0, 65536, SocketFlags.None, ref remoteEnd, callback, remoteEnd);
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
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public bool SendMessage<T>(T message, string messageTitle)
        {
            if (_sock == null || !_clientConnected) return false;
            var pipmsg = PipeMessage.CreateMessage(messageTitle, message);
            var json = JsonConvert.SerializeObject(pipmsg);
            var jsonBytes = Encoding.Default.GetBytes(json);
            if (_remoteEnd != null)
            {
                var sent = _sock.SendTo(jsonBytes, _remoteEnd);
                return sent > 0;
            }
            return false;
        }

        public bool RegistFunc<T>(string funcKey, Action<T> func)
        {
            if (_actionList.ContainsKey(funcKey))
            {
                return false;
            }
            if (_objTypes.ContainsKey(funcKey))
            {
                return false;
            }
            _objTypes.TryAdd(funcKey, typeof(T));
            _actionList.TryAdd(funcKey, func);
            return true;
        }
    }
}
