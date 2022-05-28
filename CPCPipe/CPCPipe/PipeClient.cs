using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace CPCPipe
{
    public class PipeClient
    {
        private Socket _sock;
        private byte[] _buffer;
        private ConcurrentDictionary<string, Delegate> _actionList;
        private ConcurrentDictionary<string, Type> _objTypes;

        public event Action<string> ErrMessage;
        public event Action<bool> ConnectionStateChanged;

        public PipeClient()
        {
            _actionList = new ConcurrentDictionary<string, Delegate>();
            _objTypes = new ConcurrentDictionary<string, Type>();
        }

        /// <summary>
        /// 连接客户端
        /// </summary>
        /// <param name="connectPort">端口</param>
        /// <returns>连接状态</returns>
        public async Task Connect(IPEndPoint serverEndPoint)
        {
            try
            {
                _sock ??= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                await _sock.ConnectAsync(serverEndPoint);
                ConnectionStateChanged?.Invoke(true);
                await Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        SendMessage<string>("@@HeartBeats$$##@@", "@@HeartBeats$$##@@");
                        await Task.Delay(1000);
                    }
                });

                _buffer = new byte[655360];

                _sock.BeginReceive(_buffer, 0, 65536, SocketFlags.None, callback, _sock);

                void callback(IAsyncResult e)
                {
                    var sock = e.AsyncState as Socket;
                    var readLen = sock.EndReceive(e);
                    if (readLen > 0)
                    {
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
                    sock.BeginReceive(_buffer, 0, 65536, SocketFlags.None, callback, sock);
                };
            }
            catch (Exception ex)
            {
                ErrMessage?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// 断开客户端
        /// </summary>
        /// <returns>断开状态</returns>
        public bool Disconnect()
        {
            ConnectionStateChanged?.Invoke(false);
            return true;
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

        public bool SendMessage<T>(T message, string messageTitle)
        {
            if (_sock == null || !_sock.Connected) return false;
            var pipmsg = PipeMessage.CreateMessage(messageTitle, message);
            var json = JsonConvert.SerializeObject(pipmsg);
            var jsonBytes = Encoding.Default.GetBytes(json);
            _sock.Send(jsonBytes);
            return true;
        }
    }
}
