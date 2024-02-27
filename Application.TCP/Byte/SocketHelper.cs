using Application.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tcp
{
    public static class SocketHelper
    {
        public const int PacketHeadLen = 14; //数据包包头大小 
        #region

        /// <summary>
        /// 弃用
        /// 对服务端的消息进行解析，获得消息头与消息体(消息头使用一个对象来存储)
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="netObject"></param>
        /// <returns></returns>
        public static bool ReadPacket(this Socket socket, out byte[] buffer, out NetObjectInfo? netObject)
        {
            // 1、接收数据包  第一次接收只接收了包的长度只取了4个字节
            var lenBuffer = ReceiveBuffer1(socket, 4);

            //整个数据包的长度
            var bufferLen = BitConverter.ToInt32(lenBuffer, 0);

            //实际数据   
            var exceptLenBuffer = ReceiveBuffer1(socket, bufferLen - 4);

            buffer = new byte[bufferLen];
            //组装成一个完整的buffer包
            Array.Copy(lenBuffer, buffer, 4);
            Buffer.BlockCopy(exceptLenBuffer, 0, buffer, 4, bufferLen - 4);

            // 2、解析数据包
            var readIndex = 0;
            return ReadHead(buffer, ref readIndex, out netObject);
        }

        private static byte[] ReceiveBuffer1(Socket client, int count)
        {
            var buffer = new byte[count];
            var bytesReadAllCount = 0;
            bytesReadAllCount +=
                     client.Receive(buffer, bytesReadAllCount, count - bytesReadAllCount, SocketFlags.None);
            return buffer;
        }


        #endregion
        public static bool ReadPacket(this Socket socket, TcpPastePack buffer, NetObjectInfo? netObject)
        {
            if (buffer.Start)
                return StartReadPacket(socket, buffer, netObject);
            else
                return ContinueReadPacket(socket, buffer, netObject);
        }
        private static bool StartReadPacket(this Socket socket, TcpPastePack buffer,NetObjectInfo? netObject)
        {
            (var lenBuffer, var len) = ReceiveBuffer(socket, 4);
            if (len < 4)
            {
                buffer.Set(len, 0, lenBuffer);
                buffer.ChangeType(false);
                return false;
            }
            //整个数据包的长度
            var bufferLen = BitConverter.ToInt32(lenBuffer, 0);
            //实际数据，包-长度字节=实际资源字节包
            (var exceptLenBuffer, var exceptLen) = ReceiveBuffer(socket, bufferLen - 4);
            //前4位加 读取实际资源字节包
            int exceptCount = lenBuffer.Length + exceptLen;
            byte[] exceptPack = new byte[exceptCount];
            //组装成一个完整的buffer包 0--4,4--以读取
            Array.Copy(lenBuffer, exceptPack, 4);
            Buffer.BlockCopy(exceptLenBuffer, 0, exceptPack, 4, exceptCount - 4);
            if (exceptCount != bufferLen)
            {
                buffer.Set(exceptCount, bufferLen, exceptPack);
                buffer.ChangeType(false);
                return false;
            }
            // 2、解析数据包
            var readIndex = 0;
            var result = ReadHead(exceptPack, ref readIndex, out netObject);
            buffer.Rest();
            return result;
        }
        private static  bool ContinueReadPacket(this Socket socket, TcpPastePack buffer, NetObjectInfo? netObject)
        {
            if (buffer.CurrentCount < 4)
            {
                (var bufferLen, var len) = ReceiveBuffer(socket, 4 - buffer.CurrentCount);
                if (len < 4)
                {
                    Buffer.BlockCopy(bufferLen, 0, buffer.Buffer, buffer.CurrentCount, len);
                    buffer.Set(buffer.CurrentCount + len, 0, buffer.Buffer);
                    buffer.ChangeType(false);
                    return false;
                }
                //组成长度字节
                Buffer. BlockCopy(bufferLen, 0, buffer. Buffer, buffer.CurrentCount, len);
                var packLen = BitConverter.ToInt32(buffer.Buffer, 0);
                //实际数据
                (var exceptBuffer, var exceptLen) = ReceiveBuffer(socket, packLen - 4);
                //当前所读取到的缓冲区长度
                int exceptCount = buffer.Buffer.Length + exceptLen;
                byte[] exceptPack = new byte[exceptCount];
                //组装成一个完整的buffer包  0--4,4--以读取
                Array.Copy(buffer.Buffer, exceptPack, 4);
                Buffer.BlockCopy(exceptBuffer, 0, exceptPack, buffer.Buffer.Length, exceptCount - 4);
                if (exceptCount != packLen)
                {
                    buffer.Set(exceptPack.Length, packLen, exceptPack);
                      buffer.ChangeType(false);
                    return false;
                }
                // 2、解析数据包
                var readIndex = 0;
                var result = ReadHead(exceptPack, ref readIndex, out netObject);
                buffer.Rest();
                return result;
            }
            else
            {
                //读取剩余
                (var exceptBuffer, var exceptLen) = ReceiveBuffer(socket, buffer.PackLenght - buffer.CurrentCount);
                //当前长度
                int exceptCount = buffer.CurrentCount + exceptLen;
                byte[] exceptPack = new byte[exceptCount];
                //组装成一个完整的buffer包  0--4,4--以读取
                Array.Copy(buffer.Buffer, exceptPack, 4);
                Buffer.BlockCopy(exceptBuffer, 0, exceptPack, buffer.Buffer.Length, exceptCount - 4);
                if (exceptCount!=buffer.PackLenght)
                {
                    buffer.Set(exceptPack.Length, buffer.PackLenght, exceptPack);
                    buffer.ChangeType(false);
                    return false;
                }
                // 2、解析数据包
                var readIndex = 0;
                var result = ReadHead(exceptPack, ref readIndex, out netObject);
                buffer.Rest();
                return result;
            }
        }


        private static(byte[] buffer, int index) ReceiveBuffer(Socket client, int count)
        {
            var buffer = new byte[count];
            var bytesReadAllCount = 0;
            //将读取到的byte，和读取到的字节数返回出去return (buffer, bytesReadAllCount):
            bytesReadAllCount += client.Receive(buffer, bytesReadAllCount, count - bytesReadAllCount, SocketFlags.None);
            return (buffer, bytesReadAllCount);
        }



        public static bool ReadHead(byte[] buffer, ref int readIndex, out NetObjectInfo? netObjectHeadInfo)
        {
            netObjectHeadInfo = null;
            if (buffer.Length < (readIndex + PacketHeadLen))
            {
                return false;
            }

            netObjectHeadInfo = new NetObjectInfo();

            netObjectHeadInfo.BufferLen = BitConverter.ToInt32(buffer, readIndex);
            readIndex += sizeof(int);

            netObjectHeadInfo.SystemId = BitConverter.ToInt64(buffer, readIndex);
            readIndex += sizeof(long);

            netObjectHeadInfo.ObjectId = buffer[readIndex];
            readIndex += sizeof(byte);

            netObjectHeadInfo.ObjectVersion = buffer[readIndex];
            readIndex += sizeof(byte);

            return true;
        }
    }
}
