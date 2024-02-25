using Application.Net;
using Server_Tcp;
using System;
using System.Net.Sockets;

namespace Application.Client
{
    public class TcpToken : IToken
    {
     
        public TcpPastePack Buffer { get;private  set; }

        public Socket Client { get;private set; }

        private Action<string,INetBase> _callBackRe;
        private Action<string, INetBase> _callBackSend;
        public TcpToken(Socket client, Action<string, INetBase> callBack, Action<string, INetBase> callBackSend)
        {
            Client = client;
            _callBackRe = callBack;
            _callBackSend = callBackSend;
            Buffer = new TcpPastePack();
        }
        public void ReadPacket()
        {
            NetObjectInfo netObject=null;
            bool isPack= Client.ReadPacket(Buffer, netObject);
            if (isPack)
                ReadCommand(netObject);
            }

        public void ReadCommand( NetObjectInfo? netObjectHeadInfo)
        {
            INetBase net=null;
            if(netObjectHeadInfo.IsNetObject<object>())
            {
              //  net = Buffer.Buffer.Deserialize<object>();
            }
            _callBackRe.Invoke(Client.RemoteEndPoint!.ToString(), net);
        }
        public void SendCommand(INetBase netBase)
        {
            _callBackSend(Client.RemoteEndPoint!.ToString(), netBase);
        }
    }
}
