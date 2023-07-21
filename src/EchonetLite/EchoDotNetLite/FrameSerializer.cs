using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace EchoDotNetLite
{
    public static class FrameSerializer
    {
        public static byte[] Serialize(Frame frame)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte((byte)frame.EHD1);
                ms.WriteByte((byte)frame.EHD2);
                var tid = BitConverter.GetBytes(frame.TID);
                ms.WriteByte(tid[0]);
                ms.WriteByte(tid[1]);
                switch (frame.EHD2)
                {
                    case EHD2.Type1:
                        var edata1 = EDATA1ToBytes(frame.EDATA as EDATA1);
                        ms.Write(edata1, 0, edata1.Length);
                        break;
                    case EHD2.Type2:
                        var edata2 = (frame.EDATA as EDATA2).Message;
                        ms.Write(edata2, 0, edata2.Length);
                        break;
                }
                return ms.ToArray();
            }
        }
        public static Frame Deserialize(byte[] bytes)
        {
            //EHD1が0x1*(0001***)以外の場合、
            if ((bytes[0] & 0xF0) != (byte)EHD1.ECHONETLite)
            {
                //ECHONETLiteフレームではないため無視
                return null;
            }
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var flame = new Frame
                {
                    /// ECHONET Lite電文ヘッダー１(1B)
                    EHD1 = (EHD1)br.ReadByte(),
                    /// ECHONET Lite電文ヘッダー２(1B)
                    EHD2 = (EHD2)br.ReadByte(),
                    /// トランザクションID(2B)
                    TID = br.ReadUInt16()
                };
                /// ECHONET Liteデータ(残り全部)
                switch (flame.EHD2)
                {
                    case EHD2.Type1:
                        flame.EDATA = EDATA1FromBytes(br);
                        break;
                    case EHD2.Type2:
                        flame.EDATA = new EDATA2()
                        {
                            Message = br.ReadBytes((int)ms.Length - (int)ms.Position)
                        };
                        break;
                }
                return flame;
            }
        }
        private static EDATA1 EDATA1FromBytes(BinaryReader br)
        {
            var edata = new EDATA1
            {
                SEOJ = new EOJ()
                {
                    ClassGroupCode = br.ReadByte(),
                    ClassCode = br.ReadByte(),
                    InstanceCode = br.ReadByte()
                },
                DEOJ = new EOJ()
                {
                    ClassGroupCode = br.ReadByte(),
                    ClassCode = br.ReadByte(),
                    InstanceCode = br.ReadByte()
                },
                ESV = (ESV)br.ReadByte()
            };
            if (edata.ESV == ESV.SetGet
                || edata.ESV == ESV.SetGet_Res
                || edata.ESV == ESV.SetGet_SNA)
            {
                //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                // OPCSet 処理プロパティ数(1B)
                var opcSet = br.ReadByte();
                edata.OPCSetList = new List<PropertyRequest>();
                for (int i = 0; i < opcSet; i++)
                {
                    var prp = new PropertyRequest
                    {
                        // ECHONET Liteプロパティ(1B)
                        EPC = br.ReadByte(),
                        // EDTのバイト数(1B)
                        PDC = br.ReadByte()
                    };
                    if (prp.PDC != 0)
                    {
                        // プロパティ値データ(PDCで指定)
                        prp.EDT = br.ReadBytes(prp.PDC);
                    }
                    edata.OPCSetList.Add(prp);
                }
                // OPCGet 処理プロパティ数(1B)
                var opcGet = br.ReadByte();
                edata.OPCGetList = new List<PropertyRequest>();
                for (int i = 0; i < opcSet; i++)
                {
                    var prp = new PropertyRequest
                    {
                        // ECHONET Liteプロパティ(1B)
                        EPC = br.ReadByte(),
                        // EDTのバイト数(1B)
                        PDC = br.ReadByte()
                    };
                    if (prp.PDC != 0)
                    {
                        // プロパティ値データ(PDCで指定)
                        prp.EDT = br.ReadBytes(prp.PDC);
                    }
                    edata.OPCGetList.Add(prp);
                }
            }
            else
            {
                // OPC 処理プロパティ数(1B)
                var opc = br.ReadByte();
                edata.OPCList = new List<PropertyRequest>();
                for (int i = 0; i < opc; i++)
                {
                    var prp = new PropertyRequest
                    {
                        // ECHONET Liteプロパティ(1B)
                        EPC = br.ReadByte(),
                        // EDTのバイト数(1B)
                        PDC = br.ReadByte()
                    };
                    if (prp.PDC != 0)
                    {
                        // プロパティ値データ(PDCで指定)
                        prp.EDT = br.ReadBytes(prp.PDC);
                    }
                    edata.OPCList.Add(prp);
                }
            }
            return edata;
        }

        public static byte[] EDATA1ToBytes(EDATA1 edata)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(edata.SEOJ.ClassGroupCode);
                bw.Write(edata.SEOJ.ClassCode);
                bw.Write(edata.SEOJ.InstanceCode);
                bw.Write(edata.DEOJ.ClassGroupCode);
                bw.Write(edata.DEOJ.ClassCode);
                bw.Write(edata.DEOJ.InstanceCode);
                bw.Write((byte)edata.ESV);

                if (edata.ESV == ESV.SetGet
                    || edata.ESV == ESV.SetGet_Res
                    || edata.ESV == ESV.SetGet_SNA)
                {
                    //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                    // OPCSet 処理プロパティ数(1B)
                    bw.Write((byte)edata.OPCSetList.Count);
                    foreach (var prp in edata.OPCSetList)
                    {
                        // ECHONET Liteプロパティ(1B)
                        bw.Write(prp.EPC);
                        // EDTのバイト数(1B)
                        bw.Write(prp.PDC);
                        if (prp.PDC != 0)
                        {
                            // プロパティ値データ(PDCで指定)
                            bw.Write(prp.EDT);
                        }
                    }
                    // OPCGet 処理プロパティ数(1B)
                    bw.Write((byte)edata.OPCGetList.Count);
                    foreach (var prp in edata.OPCGetList)
                    {
                        // ECHONET Liteプロパティ(1B)
                        bw.Write(prp.EPC);
                        // EDTのバイト数(1B)
                        bw.Write(prp.PDC);
                        if (prp.PDC != 0)
                        {
                            // プロパティ値データ(PDCで指定)
                            ms.Write(prp.EDT);
                        }
                    }

                }
                else
                {
                    // OPC 処理プロパティ数(1B)
                    bw.Write((byte)edata.OPCList.Count);
                    foreach (var prp in edata.OPCList)
                    {
                        // ECHONET Liteプロパティ(1B)
                        bw.Write(prp.EPC);
                        // EDTのバイト数(1B)
                        bw.Write(prp.PDC);
                        if (prp.PDC != 0)
                        {
                            // プロパティ値データ(PDCで指定)
                            bw.Write(prp.EDT);
                        }
                    }
                }
                return ms.ToArray();
            }
        }
    }

}
