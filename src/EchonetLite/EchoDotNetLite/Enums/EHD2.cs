using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Enums
{
    public enum EHD2 : byte
    {
        //図 ３-３ EHD2 詳細規定
        /// <summary>
        /// 形式1
        /// </summary>
        Type1 = 0x81,
        /// <summary>
        /// 形式2
        /// </summary>
        Type2 = 0x82,
        //その他:future reserved
        //ただし、b7=1固定
    }
}
