using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Enums
{
    public enum ESV : byte
    {
        //表 ３-９ 要求用 ESV コード一覧表
        ///<summary>0x60　プロパティ値書き込み要求（応答不要）　SetI　一斉同報可</summary>
        SetI = 0x60,
        ///<summary>0x61　プロパティ値書き込み要求（応答要）　SetC　</summary>
        SetC = 0x61,
        ///<summary>0x62　プロパティ値読み出し要求　Get　一斉同報可</summary>
        Get = 0x62,
        ///<summary>0x63　プロパティ値通知要求　INF_REQ　一斉同報可</summary>
        INF_REQ = 0x63,
        //0x64-0x6D　for future reserved,
        ///<summary>0x6E　プロパティ値書き込み・読み出し要求　SetGet　一斉同報可</summary>
        SetGet = 0x6E,
        //0x6F　for future reserved,

        //表 ３-１０ 応答・通知用 ESV コード一覧表
        ///<summary>0x71　プロパティ値書き込み応答　Set_Res　ESV=0x61 の応答、個別応答</summary>
        Set_Res = 0x71,
        ///<summary>0x72　プロパティ値読み出し応答　Get_Res　ESV=0x62 の応答、個別応答</summary>
        Get_Res = 0x72,
        ///<summary>0x73　プロパティ値通知　INF　*1個別通知、一斉同報通知共に可</summary>
        INF = 0x73,
        ///<summary>0x74　プロパティ値通知（応答要）　INFC　個別通知</summary>
        INFC = 0x74,
        //0x75-0x79 for future reserved,
        ///<summary>0x7A　プロパティ値通知応答　INFC_Res　ESV=0x74 の応答、個別応答</summary>
        INFC_Res = 0x7A,
        //0x7B-0x7D　for future reserved,
        ///<summary>0x7E　プロパティ値書き込み・読み出し応答　SetGet_Res　ESV=0x6E の応答、個別応答</summary>
        SetGet_Res = 0x7E,
        //0x7F　for future reserved,

        //表 ３-１１ 不可応答用 ESV コード一覧表
        ///<summary>0x50　プロパティ値書き込み要求不可応答　SetI_SNA　ESV=0x60 の不可応答、個別応答</summary>
        SetI_SNA = 0x50,
        ///<summary>0x51　プロパティ値書き込み要求不可応答　SetC_SNA　ESV=0x61 の不可応答、個別応答</summary>
        SetC_SNA = 0x51,
        ///<summary>0x52　プロパティ値読み出し不可応答　Get_SNA　ESV=0x62 の不可応答、個別応答</summary>
        Get_SNA = 0x52,
        ///<summary>0x53　プロパティ値通知不可応答　INF_SNA　ESV=0x63 の不可応答、個別応答</summary>
        INF_SNA = 0x53,
        //0x54-0x5D　for future reserved,
        ///<summary>0x5E　プロパティ値書き込み・読み出し不可応答　SetGet_SNA　ESV=0x6E の不可応答、個別応答</summary>
        SetGet_SNA = 0x5E,
        //0x5F　for future reserved
    }
}
