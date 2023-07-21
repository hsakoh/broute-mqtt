using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Enums
{
    public enum EHD1 : byte
    {
        //図 ３-２ EHD1 詳細規定
        //プロトコル種別
        //1* * * :従来のECHONET規格
        //0001:ECHONET Lite規格
        ECHONETLite = 0x10,
        //0000:使用不可
        //その他:future reserved
    }
}
