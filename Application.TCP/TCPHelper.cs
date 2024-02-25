using Application.Net;
using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Application.Client;
using System.Net.Http;

namespace Server_Tcp
{
    public class TCPHelper : ISocketBase
    {
        //链接用户 客户端发送过来的消息 服务端需要发向客户端的消息
        private readonly ConcurrentDictionary<string, TcpToken> _clients = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<INetBase>> _requests = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<INetBase>> _send = new();

        private Socket _server;
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9090;
        public bool IsStarted { get; set; }
        public bool IsRunning { get; set; }

        public TCPHelper(string ip,int port)
        {
            Ip = ip;
            Port = port;
        }

        public void Start()
        {
            if (IsStarted)
            {
                LogInfo.WriteLog("服务已经启动！！！！");
                return;
            }
            IsStarted = true;
            var ipEndPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
            Task.Run(async () => {
            while(IsStarted)
                    try
                    {
                        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _server.Bind(ipEndPoint);
                        _server.Listen(1000);
                        IsRunning = true;

                        ListenForClient();
                        ReciveRequests();
                        SendRequests();
                        UpdateHead();
                    }
                    catch (Exception ex)
                    {
                        IsRunning = false;
                        await LogInfo.WriteLogAsync($"运行TCP服务异常，3秒后将重新运行：{ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }
            });
        }
        /// <summary>
        /// 监听客户端链接
        /// </summary>
        private void ListenForClient()
        {
            Task.Run(async() => {
                while (IsRunning)
                {
                    try
                    {
                        var client = await _server!.AcceptAsync();
                        var token = new TcpToken(client,AddRequests, AddSend);
                        var key = client.RemoteEndPoint!.ToString();
                        _clients[key] = token;
                        await LogInfo.WriteLogAsync($"客户端链接：{key}");
                    }
                    catch (Exception ex)
                    {
                       await LogInfo.WriteLogAsync($"客户端异常链接：{ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 接收客户端消息
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ReciveRequests()
        {
            //优化使用多条线程进行管理提高性能
            Task.Run(async () => {
                while (IsRunning)
                {
                    try
                    {
                        foreach (var client in _clients.Values)
                        {
                            client.ReadPacket();
                        }
                    }
                    catch (SocketException ex)
                    {
                        await LogInfo.WriteLogAsync($"远程主机异常，将移除该客户端：{ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        await LogInfo.WriteLogAsync($"接收数据异常：{ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// 服务端向客户端发送信息
        /// </summary>
        private void SendRequests()
        {
            //优化使用多条线程进行管理提高性能
            Task.Run(async () => {
                while (IsRunning)
                {
                  foreach(var send in _send)
                    {
                      var cleint= _clients[send.Key];
                        var has = send.Value.TryDequeue(out var command);
                        if (has)
                        {
                            cleint.Client.Send(command.Serialize(1));
                            continue;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 心跳
        /// </summary>
        private void UpdateHead()
        {
            Task.Run(async () => {
                var needRemoveKeys = new List<string>();
                DateTime now = DateTime.Now;
                foreach (var request in _clients)
                {
                    var clientKey = request.Key;
                    if (!_clients.TryGetValue(clientKey, out var client))
                    {
                        needRemoveKeys.Add(clientKey);
                        continue;
                    }
                    TimeSpan secondSpan = new TimeSpan(now.Ticks - request.Value.Buffer.heat.Ticks);
                    if(secondSpan.TotalSeconds>=20)
                    {
                        needRemoveKeys.Add(clientKey);
                        continue;
                    }
                }

                if (needRemoveKeys.Count > 0) needRemoveKeys.ForEach(RemoveClient);

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            });
            }
        /// <summary>
        /// 停止服务
        /// </summary>
        public async void Stop()
        {
            if (!IsStarted)
            {
              await LogInfo.WriteLogAsync("Tcp服务已经关闭");
                return;
            }
            IsStarted = false;
            try
            {
                _server?.Close(0);
                _server = null;
                await LogInfo.WriteLogAsync("停止Tcp服务");
            }
            catch (Exception ex)
            {
                await LogInfo.WriteLogAsync($"停止TCP服务异常：{ex.Message}");
            }

            IsRunning = false;
        }

        private void AddRequests(string key,INetBase netBase)
        {
            if (!_requests.TryGetValue(key, out var value))
            {
                value = new ConcurrentQueue<INetBase>();
                _requests[key] = value;
            }

            value.Enqueue(netBase);
        }

        private void AddSend(string key, INetBase netBase)
        {
            if (!_send.TryGetValue(key, out var value))
            {
                value = new ConcurrentQueue<INetBase>();
                _requests[key] = value;
            }

            value.Enqueue(netBase);
        }

        private void RemoveClient(string key)
        {
            _clients.TryRemove(key, out _);
            _requests.TryRemove(key, out _);
            LogInfo.WriteLog($"已清除客户端信息{key}");
        }

    }
}
