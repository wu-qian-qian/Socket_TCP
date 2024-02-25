using Application.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Client
{
    public  interface IToken
    {
       

        /// <summary>
        /// 读取包
        /// </summary>
        void ReadPacket();
        /// <summary>
        /// 解析成对像
        /// </summary>
        /// <param name="netObjectHeadInfo"></param>
        void ReadCommand( NetObjectInfo? netObjectHeadInfo);
    }
}
