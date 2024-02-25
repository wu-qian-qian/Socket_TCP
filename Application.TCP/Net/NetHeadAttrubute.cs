using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Net
{
    /// <summary>
    /// 数据包定义主要用于类对象的解析
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NetHeadAttribute(byte id, byte version) : Attribute
    {
        /// <summary>
        /// 对象Id
        /// </summary>
        public byte Id { get; set; } = id;

        /// <summary>
        /// 对象版本号
        /// </summary>
        public byte Version { get; set; } = version;
    }
}
