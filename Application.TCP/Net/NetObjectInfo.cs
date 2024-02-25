using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Net
{
    /// <summary>
    /// 网络数据包的信息，
    /// </summary>
    public class NetObjectInfo
    {
        /// <summary>
        /// 数据包大小
        /// </summary>
        public int BufferLen { get; set; }

        /// <summary>
        /// 系统标识
        /// </summary>
        public long SystemId { get; set; }

        /// <summary>
        /// 对象Id
        /// </summary>
        public byte ObjectId { get; set; }

        /// <summary>
        /// 对象版本号
        /// </summary>
        public byte ObjectVersion { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(BufferLen)}: {BufferLen}, {nameof(SystemId)}: {SystemId}，{nameof(ObjectId)}: {ObjectId}，{nameof(ObjectVersion)}: {ObjectVersion}";
        }
 
    }
}
