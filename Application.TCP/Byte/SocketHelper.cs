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
            var lenBuffer = ReceiveBuffer(socket, 4);

            //整个数据包的长度
            var bufferLen = BitConverter.ToInt32(lenBuffer, 0);

            //实际数据   
            var exceptLenBuffer = ReceiveBuffer(socket, bufferLen - 4);

            buffer = new byte[bufferLen];
            //组装成一个完整的buffer包
            Array.Copy(lenBuffer, buffer, 4);
            Buffer.BlockCopy(exceptLenBuffer, 0, buffer, 4, bufferLen - 4);

            // 2、解析数据包
            var readIndex = 0;
            return ReadHead(buffer, ref readIndex, out netObject);
        }


        public static bool ReadPacket(this Socket socket, TcpPastePack buffer, NetObjectInfo? netObject)
        {
            if (buffer.Start)
                return StartReadPacket(socket, buffer, netObject);
            else
                return ContinueReadPacket(socket, buffer, netObject);
        }
        private static bool StartReadPacket(this Socket socket, TcpPastePack buffer,NetObjectInfo? netObject)
        {
            var lenBuffer = ReceiveBuffer(socket, 4);
            if (lenBuffer.Length < 4)
            {
                buffer.Set(lenBuffer.Length, 0, lenBuffer);
                buffer.ChangeType(false);
                return false;
            }
            //整个数据包的长度
            var bufferLen = BitConverter.ToInt32(lenBuffer, 0);
            //实际数据   
            var exceptLenBuffer = ReceiveBuffer(socket, bufferLen - 4);
            //前4位加后续的长度
            int exceptCount = lenBuffer.Length + exceptLenBuffer.Length;
            byte[] exceptPack = new byte[exceptCount];
            //组装成一个完整的buffer包  0--4,4--以读取
            Array.Copy(lenBuffer, exceptPack, 4);
            Buffer.BlockCopy(exceptLenBuffer, 0, exceptPack, 4, exceptCount - 4);
            if (exceptCount != bufferLen)
            {
                buffer.Set(exceptPack.Length, bufferLen, exceptPack);
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
                var lenBuffer = ReceiveBuffer(socket, 4-buffer.CurrentCount);
                if (lenBuffer.Length < 4)
                {
                    buffer.Set(buffer.CurrentCount+lenBuffer.Length, 0, lenBuffer);
                    buffer.ChangeType(false);
                    return false;
                }
                //组成长度字节
                Buffer.BlockCopy(lenBuffer, 0, buffer.Buffer, buffer.CurrentCount,lenBuffer.Length);
                var bufferLen = BitConverter.ToInt32(buffer.Buffer, 0);
                //实际数据   
                var exceptLenBuffer = ReceiveBuffer(socket, bufferLen - 4);
                //当前长度
                int exceptCount = lenBuffer.Length + exceptLenBuffer.Length;
                byte[] exceptPack = new byte[exceptCount];
                //组装成一个完整的buffer包  0--4,4--以读取
                Array.Copy(lenBuffer, exceptPack, 4);
                Buffer.BlockCopy(exceptLenBuffer, 0, exceptPack, 4, exceptCount - 4);
                if (exceptCount != bufferLen)
                {
                    buffer.Set(exceptPack.Length, bufferLen, exceptPack);
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
                var exceptLenBuffer = ReceiveBuffer(socket, buffer.PackLenght - buffer.CurrentCount);
                //当前长度
                int exceptCount = buffer.CurrentCount + exceptLenBuffer.Length;
                byte[] exceptPack = new byte[exceptCount];
                //组装成一个完整的buffer包  0--4,4--以读取
                Array.Copy(buffer.Buffer, exceptPack, buffer.CurrentCount);
                Buffer.BlockCopy(exceptLenBuffer, 0, exceptPack, 4, exceptCount - buffer.CurrentCount);
                if (exceptCount != buffer.PackLenght)
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

        /// <summary>
        /// Socket接受数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static byte[] ReceiveBuffer(Socket client, int count)
        {
            var buffer = new byte[count];
            var bytesReadAllCount = 0;
            bytesReadAllCount +=
                     client.Receive(buffer, bytesReadAllCount, count - bytesReadAllCount, SocketFlags.None);
            return buffer;
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
