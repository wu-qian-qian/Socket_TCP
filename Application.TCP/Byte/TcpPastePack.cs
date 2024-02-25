using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tcp
{
    /// <summary>
    /// TCP粘包辅助类
    /// </summary>
    public class TcpPastePack
    {
      public int CurrentCount { get;private set; }

      public int PackLenght { get; private set; }

      public byte[] Buffer { get; private set; }

      public bool Start { get; private set; }

        public DateTime heat { get; private set; }

        public void Set(int currCount,int packLenght, byte[] buffer)
        {
            CurrentCount = currCount;
            PackLenght = packLenght;
            Buffer = buffer;
        }
         public void Rest()
        {
            CurrentCount = 0;
            PackLenght = 0;
            Buffer = null;
            ChangeType(true);
            heat = DateTime.Now;
        }

    public void ChangeType(bool isStart)
        {
            Start = isStart;
        }
    }
}
