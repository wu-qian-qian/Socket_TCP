using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Application.Net
{
    public static class SerializeHelper
    {
        private static readonly MessagePackSerializerOptions Options =
       MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public const int PacketHeadLen = 14; //数据包包头大小 
        public static Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// 网络对象的序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="systemId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] Serialize<T>(this T data, long systemId) where T : INetBase
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            //获得该对象的标记
            var netObjectInfo = GetNetObjectHead(data.GetType());
            dynamic netObject = data;
            var bodyBuffer = MessagePackSerializer.Serialize(netObject, Options);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, DefaultEncoding);
            //将数据包长度写入到buffer
            writer.Write(PacketHeadLen + bodyBuffer.Length);
            //将进程值写入
            writer.Write(systemId);
            //将对象
            writer.Write(netObjectInfo.Id);
            //将版本值
            writer.Write(netObjectInfo.Version);
            writer.Write(bodyBuffer);

            return stream.ToArray();
        }

        /// <summary>
        /// 将接收的数据包反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this byte[] buffer) where T : new()
        {
            var bodyBufferLen = buffer.Length - PacketHeadLen;
            using var stream = new MemoryStream(buffer, PacketHeadLen, bodyBufferLen);
            var data = MessagePackSerializer.Deserialize<T>(stream, Options);
            return data;
        }

        public static NetHeadAttribute GetNetObjectHead(this Type netObjectType)
        {
            var attribute = netObjectType.GetCustomAttribute<NetHeadAttribute>();
            return attribute ?? throw new Exception(
                $"{netObjectType.Name} has not been marked with the attribute {nameof(NetHeadAttribute)}");
        }


        /// <summary>
        /// 判断类型是否一致
        /// 通过对网络数据包的NetHead来判断是否为同一对象，
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="netObjectHeadInfo"></param>
        /// <returns></returns>
        public static bool IsNetObject<T>(this NetObjectInfo netObjectHeadInfo)
        {
            var netObjectAttribute = GetNetObjectHead(typeof(T));
            return netObjectAttribute.Id == netObjectHeadInfo.ObjectId &&
                   netObjectAttribute.Version == netObjectHeadInfo.ObjectVersion;
        }
    }
}
