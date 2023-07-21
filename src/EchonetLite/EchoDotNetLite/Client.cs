using EchoDotNetLite.Common;
using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoDotNetLite
{
    public class EchoClient
    {
        private readonly IPANAClient _panaClient;
        private readonly ILogger _logger;
        public EchoClient(ILogger<EchoClient> logger, IPANAClient panaClient)
        {
            _logger = logger;
            _panaClient = panaClient;
            _panaClient.OnEventReceived += ReceiveEvent;
            SelfNode = new EchoNode()
            {
                NodeProfile = new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01),
            };
            NodeList = new List<EchoNode>();
            //自己消費用
            OnFrameReceived += ReceiveFrame;
        }

        public void Initialize(string selfAddress)
        {
            _logger.LogInformation("自ノード IPアドレスを設定:{selfAddress}", selfAddress);
            SelfNode.Address = selfAddress;
        }

        public EchoNode SelfNode { get; set; }

        public List<EchoNode> NodeList { get; set; }

        public event EventHandler<(string, Frame)> OnFrameReceived;

        public event EventHandler<EchoNode> OnNodeJoined;

        private ushort tid = 0;
        public ushort GetNewTid()
        {
            return ++tid;
        }

        public async Task インスタンスリスト通知Async()
        {
            _logger.LogInformation("インスタンスリスト通知");
            //インスタンスリスト通知プロパティ
            var property = SelfNode.NodeProfile.ANNOProperties.Where(p => p.Spec.Code == 0xD5).First();
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                //1 ﾊﾞｲﾄ目：通報インスタンス数 
                bw.Write((byte)SelfNode.Devices.Count);
                //2～253 ﾊﾞｲﾄ目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
                foreach (var device in SelfNode.Devices)
                {
                    var eoj = device.GetEOJ();
                    bw.Write(eoj.ClassGroupCode);
                    bw.Write(eoj.ClassCode);
                    bw.Write(eoj.InstanceCode);
                }
                //インスタンスリスト通知
                property.Value = ms.ToArray();
            }
            await 自発プロパティ値通知(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ()
                {
                    ClassGroupCode = Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    ClassCode = Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    InstanceCode = 0x01,

                })
                , new List<EchoPropertyInstance>() { property }
                );
        }
        public async Task インスタンスリスト通知要求Async()
        {
            _logger.LogInformation("インスタンスリスト通知要求");
            var a = new List<EchoPropertyInstance>() { new EchoPropertyInstance(
                Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                0xD5//インスタンスリスト通知
                )
            };
            await プロパティ値通知要求(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ()
                {
                    ClassGroupCode = Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    ClassCode = Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    InstanceCode = 0x01,
                })
                , a);
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeout"></param>
        /// <returns>true:タイムアウトまでに不可応答なし,false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み要求応答不要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(string, Frame)>);
            handler += (object sender, (string address, Frame response) value) =>
            {
                if ((destinationNode != null && value.address != destinationNode.Address)
                    || value.response.EDATA is not EDATA1 edata
                    || edata.SEOJ != destinationObject.GetEOJ()
                    || edata.ESV != ESV.SetI_SNA)
                {
                    return;
                }
                foreach (var prop in edata.OPCList)
                {
                    //一部成功した書き込みを反映
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.EPC).First();
                    if (prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = properties.Where(p => p.Spec.Code == prop.EPC).First().Value;
                    }
                }
                responseTCS.SetResult((false, edata.OPCList));

                //TODO 一斉通知の不可応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.SetI,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = (byte)p.Value.Length,
                        EDT = p.Value,
                    }).ToList(),
                }
            });
            if (await Task.WhenAny(responseTCS.Task, Task.Delay(timeoutMilliseconds)) == responseTCS.Task)
            {
                return await responseTCS.Task;
            }
            else
            {
                foreach (var prop in properties)
                {
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.Spec.Code).First();
                    //成功した書き込みを反映(全部OK)
                    target.Value = prop.Value;
                }
                OnFrameReceived -= handler; ;
                return (true, null);
            }
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み応答要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(string, Frame)>);
            handler += (object sender, (string address, Frame response) value) =>
            {
                if ((destinationNode != null && value.address != destinationNode.Address)
                    || value.response.EDATA is not EDATA1 edata
                    || edata.SEOJ != destinationObject.GetEOJ()
                    || (edata.ESV != ESV.SetC_SNA && edata.ESV != ESV.Set_Res)
                    )
                {
                    return;
                }
                foreach (var prop in edata.OPCList)
                {
                    //成功した書き込みを反映
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.EPC).First();
                    if (prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = properties.Where(p => p.Spec.Code == prop.EPC).First().Value;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Set_Res, edata.OPCList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.SetC,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = (byte)p.Value.Length,
                        EDT = p.Value,
                    }).ToList(),
                }
            });
            if (await Task.WhenAny(responseTCS.Task, Task.Delay(timeoutMilliseconds)) == responseTCS.Task)
            {
                return await responseTCS.Task;
            }
            else
            {
                OnFrameReceived -= handler;
                throw new Exception("Time has expired");
            }
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値読み出し(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            _logger.LogInformation("{Node}のプロパティ値を読み出します({Properties})", destinationNode.NodeProfile.GetDebugString(),
                string.Join(',', properties.Select(s => s.GetDebugString())));
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(string, Frame)>);
            handler += (object sender, (string address, Frame response) value) =>
            {
                if ((destinationNode != null && value.address != destinationNode.Address)
                    || value.response.EDATA is not EDATA1 edata
                    || edata.SEOJ != destinationObject.GetEOJ()
                    || (edata.ESV != ESV.Get_Res && edata.ESV != ESV.Get_SNA)
                    )
                {
                    return;
                }
                foreach (var prop in edata.OPCList)
                {
                    //成功した読み込みを反映
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.EPC).First();
                    if (prop.PDC != 0x00)
                    {
                        //読み込み成功
                        target.Value = prop.EDT;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Get_Res, edata.OPCList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.Get,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = 0x00,
                        EDT = null
                    }).ToList(),
                }
            });
            if (await Task.WhenAny(responseTCS.Task, Task.Delay(timeoutMilliseconds)) == responseTCS.Task)
            {
                return await responseTCS.Task;
            }
            else
            {
                OnFrameReceived -= handler;
                throw new Exception("Time has expired");
            }
        }
        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="propertiesSet"></param>
        /// <param name="propertiesGet"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns></returns>
        public async Task<(bool, List<PropertyRequest>, List<PropertyRequest>)> プロパティ値書き込み読み出し(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> propertiesSet
            , IEnumerable<EchoPropertyInstance> propertiesGet
            , int timeoutMilliseconds = 1000)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>, List<PropertyRequest>)>();
            var handler = default(EventHandler<(string, Frame)>);
            handler += (object sender, (string address, Frame response) value) =>
            {
                if ((destinationNode != null && value.address != destinationNode.Address)
                    || value.response.EDATA is not EDATA1 edata
                    || edata.SEOJ != destinationObject.GetEOJ()
                    || (edata.ESV != ESV.SetGet_Res && edata.ESV != ESV.SetGet_SNA)
                    )
                {
                    return;
                }
                foreach (var prop in edata.OPCSetList)
                {
                    //成功した書き込みを反映
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.EPC).First();
                    if (prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = propertiesSet.Where(p => p.Spec.Code == prop.EPC).First().Value;
                    }
                }
                foreach (var prop in edata.OPCGetList)
                {
                    //成功した読み込みを反映
                    var target = destinationObject.Properties.Where(p => p.Spec.Code == prop.EPC).First();
                    if (prop.PDC != 0x00)
                    {
                        //読み込み成功
                        target.Value = prop.EDT;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.SetGet_Res, edata.OPCSetList, edata.OPCGetList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.SetGet,
                    OPCSetList = propertiesSet.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = (byte)p.Value.Length,
                        EDT = p.Value,
                    }).ToList(),
                    OPCGetList = propertiesGet.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = 0x00,
                        EDT = null,
                    }).ToList(),
                }
            });
            if (await Task.WhenAny(responseTCS.Task, Task.Delay(timeoutMilliseconds)) == responseTCS.Task)
            {
                return await responseTCS.Task;
            }
            else
            {
                OnFrameReceived -= handler;
                throw new Exception("Time has expired");
            }
        }


        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        public async Task プロパティ値通知要求(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties)
        {
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.INF_REQ,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = 0x00,
                        EDT = null,
                    }).ToList(),
                }
            });
        }


        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode">一斉通知の場合、NULL</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeout"></param>
        public async Task 自発プロパティ値通知(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties)
        {
            await RequestAsync(destinationNode?.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.INF,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = (byte)p.Value.Length,
                        EDT = p.Value,
                    }).ToList(),
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"></param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>成功の応答</returns>
        public async Task<List<PropertyRequest>> プロパティ値通知応答要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            var responseTCS = new TaskCompletionSource<List<PropertyRequest>>();
            var handler = default(EventHandler<(string, Frame)>);
            handler += (object sender, (string address, Frame response) value) =>
            {
                if (value.address != destinationNode.Address
                    || value.response.EDATA is not EDATA1 edata
                    || edata.SEOJ != destinationObject.GetEOJ()
                    || (edata.ESV != ESV.INFC_Res)
                    )
                {
                    return;
                }
                responseTCS.SetResult(edata.OPCList);
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;
            await RequestAsync(destinationNode.Address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = GetNewTid(),
                EDATA = new EDATA1()
                {
                    SEOJ = sourceObject.GetEOJ(),
                    DEOJ = destinationObject.GetEOJ(),
                    ESV = ESV.INFC,
                    OPCList = properties.Select(p => new PropertyRequest()
                    {
                        EPC = p.Spec.Code,
                        PDC = (byte)p.Value.Length,
                        EDT = p.Value,
                    }).ToList(),
                }
            });
            if (await Task.WhenAny(responseTCS.Task, Task.Delay(timeoutMilliseconds)) == responseTCS.Task)
            {
                return await responseTCS.Task;
            }
            else
            {
                OnFrameReceived -= handler;
                throw new Exception("Time has expired");
            }
        }

        private void ReceiveEvent(object sender, (string address, byte[] e) value)
        {
            var frame = FrameSerializer.Deserialize(value.e);
            if (frame != null)
            {
                _logger.LogTrace("Echonet Lite Frame受信: address:{address}\r\n,{frame}", value.address, JsonConvert.SerializeObject(frame));
                OnFrameReceived?.Invoke(this, (value.address, frame));
            }
        }

        private async Task RequestAsync(string address, Frame frame)
        {
            _logger.LogTrace("Echonet Lite Frame送信: address:{address}\r\n,{frame}", address, JsonConvert.SerializeObject(frame));
            await _panaClient.RequestAsync(address, FrameSerializer.Serialize(frame));
        }

        private void インスタンスリスト通知受信(EchoNode sourceNode, byte[] edt)
        {
            _logger.LogInformation("インスタンスリスト通知を受信しました");
            using (var ms = new MemoryStream(edt))
            using (var br = new BinaryReader(ms))
            {
                var count = br.ReadByte();
                for (int i = 0; i < count; i++)
                {
                    var classGroupCode = br.ReadByte();
                    var classCode = br.ReadByte();
                    var instanceCode = br.ReadByte();
                    var eoj = new EOJ()
                    {
                        ClassGroupCode = classGroupCode,
                        ClassCode = classCode,
                        InstanceCode = instanceCode,
                    };
                    var device = sourceNode.Devices.Where(d => d.GetEOJ() == eoj).FirstOrDefault();
                    if (device == null)
                    {
                        device = new EchoObjectInstance(eoj);
                        sourceNode.Devices.Add(device);
                    }
                    if (!device.IsPropertyMapGet)
                    {
                        _logger.LogInformation("{Device} プロパティマップを読み取ります", device.GetDebugString());
                        プロパティマップ読み取り(sourceNode, device);
                    }
                }
            }
            if (!sourceNode.NodeProfile.IsPropertyMapGet)
            {
                _logger.LogInformation("{Node} プロパティマップを読み取ります", sourceNode.NodeProfile.GetDebugString());
                プロパティマップ読み取り(sourceNode, sourceNode.NodeProfile);
            }
        }

        private void プロパティマップ読み取り(EchoNode sourceNode, EchoObjectInstance device)
        {
            プロパティ値読み出し(SelfNode.NodeProfile, sourceNode, device
                    , device.Properties.Where(p =>
                        p.Spec.Code == 0x9D //状変アナウンスプロパティマップ
                        || p.Spec.Code == 0x9E //Set プロパティマップ
                        || p.Spec.Code == 0x9F //Get プロパティマップ
                    ), 20000
            ).ContinueWith((result) =>
            {
                if (!result.IsCompletedSuccessfully)
                {
                    _logger.LogWarning("{Device} プロパティマップの読み取りがタイムアウトしました", device.GetDebugString());
                    return;
                }
                //不可応答は無視
                if (!result.Result.Item1)
                {
                    _logger.LogWarning("{Device} プロパティマップの読み取りで不可応答が返答されました", device.GetDebugString());
                    return;
                }
                _logger.LogInformation("{Device} プロパティマップの読み取りが成功しました", device.GetDebugString());
                device.Properties.Clear();
                foreach (var pr in result.Result.Item2)
                {
                    //状変アナウンスプロパティマップ
                    if (pr.EPC == 0x9D)
                    {
                        var propertyMap = ParsePropertyMap(pr.EDT);
                        foreach (var propertyCode in propertyMap)
                        {
                            var property = device.Properties.Where(p => p.Spec.Code == propertyCode).FirstOrDefault();
                            if (property == null)
                            {
                                property = new EchoPropertyInstance(device.Spec.ClassGroup.ClassGroupCode, device.Spec.Class.ClassCode, propertyCode);
                                device.Properties.Add(property);
                            }
                            property.Anno = true;
                        }
                    }
                    //Set プロパティマップ
                    if (pr.EPC == 0x9E)
                    {
                        var propertyMap = ParsePropertyMap(pr.EDT);
                        foreach (var propertyCode in propertyMap)
                        {
                            var property = device.Properties.Where(p => p.Spec.Code == propertyCode).FirstOrDefault();
                            if (property == null)
                            {
                                property = new EchoPropertyInstance(device.Spec.ClassGroup.ClassGroupCode, device.Spec.Class.ClassCode, propertyCode);
                                device.Properties.Add(property);
                            }
                            property.Set = true;
                        }
                    }
                    //Get プロパティマップ
                    if (pr.EPC == 0x9F)
                    {
                        var propertyMap = ParsePropertyMap(pr.EDT);
                        foreach (var propertyCode in propertyMap)
                        {
                            var property = device.Properties.Where(p => p.Spec.Code == propertyCode).FirstOrDefault();
                            if (property == null)
                            {
                                property = new EchoPropertyInstance(device.Spec.ClassGroup.ClassGroupCode, device.Spec.Class.ClassCode, propertyCode);
                                device.Properties.Add(property);
                            }
                            property.Get = true;
                        }
                    }
                }
                var sb = new StringBuilder();
                sb.AppendLine("------");
                foreach (var temp in device.Properties)
                {
                    sb.AppendFormat("\t{0}\r\n", temp.GetDebugString());
                }
                sb.AppendLine("------");
                _logger.LogTrace("{PropertyMap}", sb.ToString());
                device.IsPropertyMapGet = true;
            });
        }

        public static List<byte> ParsePropertyMap(byte[] value)
        {
            using var ms = new MemoryStream(value);
            using var br = new BinaryReader(ms);
            var epcList = new List<byte>();
            var count = br.ReadByte();
            if (count < 0x10)
            {
                //パターン1
                for (var i = 0; i < count; i++)
                {
                    epcList.Add(br.ReadByte());
                }
            }
            else
            {
                for (var i = 0; i < 16; i++)
                {
                    var flag = br.ReadByte();
                    if ((flag & 0b10000000) == 0b10000000)
                    {
                        epcList.Add((byte)(0xF0 | (byte)i));
                    }
                    if ((flag & 0b01000000) == 0b01000000)
                    {
                        epcList.Add((byte)(0xE0 | (byte)i));
                    }
                    if ((flag & 0b00100000) == 0b00100000)
                    {
                        epcList.Add((byte)(0xD0 | (byte)i));
                    }
                    if ((flag & 0b00010000) == 0b00010000)
                    {
                        epcList.Add((byte)(0xC0 | (byte)i));
                    }
                    if ((flag & 0b00001000) == 0b00001000)
                    {
                        epcList.Add((byte)(0xB0 | (byte)i));
                    }
                    if ((flag & 0b00000100) == 0b00000100)
                    {
                        epcList.Add((byte)(0xA0 | (byte)i));
                    }
                    if ((flag & 0b00000010) == 0b00000010)
                    {
                        epcList.Add((byte)(0x90 | (byte)i));
                    }
                    if ((flag & 0b00000001) == 0b00000001)
                    {
                        epcList.Add((byte)(0x80 | (byte)i));
                    }
                }
            }
            return epcList;
        }

        private void ReceiveFrame(object sendor, (string address, Frame frame) value)
        {
            if (value.frame.EHD1 == EHD1.ECHONETLite
                && value.frame.EHD2 == EHD2.Type1)
            {
                var edata = value.frame.EDATA as EDATA1;
                var sourceNode = NodeList.SingleOrDefault(n => n.Address == value.address);
                //未知のノードの場合
                if (sourceNode == null)
                {
                    //ノードを生成
                    sourceNode = new EchoNode()
                    {
                        Address = value.address,
                        NodeProfile = new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01),
                    };
                    NodeList.Add(sourceNode);
                    OnNodeJoined?.Invoke(this, sourceNode);
                }
                EchoObjectInstance destObject = null;
                //自ノードプロファイル宛てのリクエストの場合
                if (SelfNode.NodeProfile.GetEOJ() == edata.DEOJ)
                {
                    destObject = SelfNode.NodeProfile;
                }
                else
                {
                    var device = SelfNode.Devices.Where(d => d.GetEOJ() == edata.DEOJ).FirstOrDefault();
                    destObject = device;
                }
                Task task = null;

                switch (edata.ESV)
                {
                    case ESV.SetI://プロパティ値書き込み要求（応答不要）
                        //あれば、書き込んでおわり
                        //なければ、プロパティ値書き込み要求不可応答 SetI_SNA
                        task = Task.Run(() => プロパティ値書き込みサービス応答不要(value, edata, destObject));
                        break;
                    case ESV.SetC://プロパティ値書き込み要求（応答要）
                        //あれば、書き込んで プロパティ値書き込み応答 Set_Res
                        //なければ、プロパティ値書き込み要求不可応答 SetC_SNA
                        task = Task.Run(() => プロパティ値書き込みサービス応答要(value, edata, destObject));
                        break;
                    case ESV.Get://プロパティ値読み出し要求
                        //あれば、プロパティ値読み出し応答 Get_Res
                        //なければ、プロパティ値読み出し不可応答 Get_SNA
                        task = Task.Run(() => プロパティ値読み出しサービス(value, edata, destObject));
                        break;
                    case ESV.INF_REQ://プロパティ値通知要求
                        //あれば、プロパティ値通知 INF
                        //なければ、プロパティ値通知不可応答 INF_SNA
                        break;
                    case ESV.SetGet: //プロパティ値書き込み・読み出し要求
                        //あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
                        //なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
                        task = Task.Run(() => プロパティ値書き込み読み出しサービス(value, edata, destObject));
                        break;
                    case ESV.INF: //プロパティ値通知 
                        //プロパティ値通知要求 INF_REQのレスポンス
                        //または、自発的な通知のケースがある。
                        //なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
                        task = Task.Run(() => プロパティ値通知サービス(edata, sourceNode));
                        break;
                    case ESV.INFC: //プロパティ値通知（応答要）
                        //プロパティ値通知応答 INFC_Res
                        task = Task.Run(() => プロパティ値通知応答要サービス(value, edata, sourceNode, destObject));
                        break;

                    case ESV.SetI_SNA: //プロパティ値書き込み要求不可応答
                        //プロパティ値書き込み要求（応答不要）SetIのレスポンスなので、要求送信(SETI)のハンドラで対処
                        break;

                    case ESV.Set_Res: //プロパティ値書き込み応答
                                      //プロパティ値書き込み要求（応答要） SetC のレスポンスなので、要求送信(SETC)のハンドラで対処
                    case ESV.SetC_SNA: //プロパティ値書き込み要求不可応答
                        //プロパティ値書き込み要求（応答要） SetCのレスポンスなので、要求送信(SETC)のハンドラで対処
                        break;

                    case ESV.Get_Res: //プロパティ値読み出し応答 
                                      //プロパティ値読み出し要求 Getのレスポンスなので、要求送信(GET)のハンドラで対処
                    case ESV.Get_SNA: //プロパティ値読み出し不可応答
                        //プロパティ値読み出し要求 Getのレスポンスなので、要求送信(GET)のハンドラで対処
                        break;

                    case ESV.INFC_Res: //プロパティ値通知応答
                        //プロパティ値通知（応答要） INFCのレスポンスなので、要求送信(INFC)のハンドラで対処
                        break;

                    case ESV.INF_SNA: //プロパティ値通知不可応答
                        //プロパティ値通知要求 INF_REQ のレスポンスなので、要求送信(INF_REQ)のハンドラで対処
                        break;

                    case ESV.SetGet_Res://プロパティ値書き込み・読み出し応答
                                        //プロパティ値書き込み・読み出し要求 SetGetのレスポンスなので、要求送信(SETGET)のハンドラで対処
                    case ESV.SetGet_SNA: //プロパティ値書き込み・読み出し不可応答
                        //プロパティ値書き込み・読み出し要求 SetGet のレスポンスなので、要求送信(SETGET)のハンドラで対処
                        break;
                    default:
                        break;
                }
                task?.ContinueWith((t) =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogTrace(t.Exception, "Exception");
                    }
                });

            }
        }

        /// <summary>
        /// ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject"></param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値書き込みサービス応答不要((string address, Frame frame) request, EDATA1 edata, EchoObjectInstance destObject)
        {
            if (destObject == null)
            {
                //対象となるオブジェクト自体が存在しない場合には、「不可応答」も返さないものとする。
                return false;
            }
            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            foreach (var opc in edata.OPCList)
            {
                var property = destObject.SETProperties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                if (property == null
                        || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                        || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    hasError = true;
                    //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                    //要求された EDT を付け、要求を受理できなかったことを示す。
                    opcList.Add(opc);
                }
                else
                {
                    //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                    property.Value = opc.EDT;
                    opc.PDC = 0x00;
                    opc.EDT = null;
                    opcList.Add(opc);
                }
            }
            if (hasError)
            {
                await RequestAsync(request.address, new Frame()
                {
                    EHD1 = EHD1.ECHONETLite,
                    EHD2 = EHD2.Type1,
                    TID = request.frame.TID,
                    EDATA = new EDATA1()
                    {
                        SEOJ = edata.DEOJ,//入れ替え
                        DEOJ = edata.SEOJ,
                        ESV = ESV.SetI_SNA,//SetI_SNA(0x50)
                        OPCList = opcList,
                    }
                });
                return false;
            }
            return true;
        }

        /// <summary>
        /// ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
        /// </summary>
        /// <param name="value"></param>
        /// <param name="edata"></param>
        /// <param name="destObject"></param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値書き込みサービス応答要((string address, Frame frame) request, EDATA1 edata, EchoObjectInstance destObject)
        {
            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject != null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcList.AddRange(edata.OPCList);
            }
            else
            {
                foreach (var opc in edata.OPCList)
                {
                    var property = destObject.SETProperties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.Value = opc.EDT;
                        opc.PDC = 0x00;
                        opc.EDT = null;
                        opcList.Add(opc);
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync(request.address, new Frame()
                {
                    EHD1 = EHD1.ECHONETLite,
                    EHD2 = EHD2.Type1,
                    TID = request.frame.TID,
                    EDATA = new EDATA1()
                    {
                        SEOJ = edata.DEOJ,//入れ替え
                        DEOJ = edata.SEOJ,
                        ESV = ESV.SetC_SNA,//SetC_SNA(0x51)
                        OPCList = opcList,
                    }
                });
                return false;
            }
            await RequestAsync(request.address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = request.frame.TID,
                EDATA = new EDATA1()
                {
                    SEOJ = edata.DEOJ,//入れ替え
                    DEOJ = edata.SEOJ,
                    ESV = ESV.Set_Res,//Set_Res(0x71)
                    OPCList = opcList,
                }
            });
            return true;
        }

        /// <summary>
        /// ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject"></param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値読み出しサービス((string address, Frame frame) request, EDATA1 edata, EchoObjectInstance destObject)
        {
            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject != null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcList.AddRange(edata.OPCList);
            }
            else
            {
                foreach (var opc in edata.OPCList)
                {
                    var property = destObject.SETProperties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
                        //EDT はつけず、要求を受理できなかったことを示す。
                        //(そのままでよい)
                        opcList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
                        //EDT には読み出したプロパティ値を設定する
                        opc.PDC = (byte)property.Value.Length;
                        opc.EDT = property.Value;
                        opcList.Add(opc);
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync(request.address, new Frame()
                {
                    EHD1 = EHD1.ECHONETLite,
                    EHD2 = EHD2.Type1,
                    TID = request.frame.TID,
                    EDATA = new EDATA1()
                    {
                        SEOJ = edata.DEOJ,//入れ替え
                        DEOJ = edata.SEOJ,
                        ESV = ESV.Get_SNA,//Get_SNA(0x52)
                        OPCList = opcList,
                    }
                });
                return false;
            }
            await RequestAsync(request.address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = request.frame.TID,
                EDATA = new EDATA1()
                {
                    SEOJ = edata.DEOJ,//入れ替え
                    DEOJ = edata.SEOJ,
                    ESV = ESV.Get_Res,//Get_Res(0x72)
                    OPCList = opcList,
                }
            });
            return true;
        }

        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// 本実装は書き込み後、読み込む
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject"></param>
        private async Task<bool> プロパティ値書き込み読み出しサービス((string address, Frame frame) request, EDATA1 edata, EchoObjectInstance destObject)
        {
            bool hasError = false;
            var opcSetList = new List<PropertyRequest>();
            var opcGetList = new List<PropertyRequest>();
            if (destObject != null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcSetList.AddRange(edata.OPCSetList);
                opcGetList.AddRange(edata.OPCGetList);
            }
            else
            {
                foreach (var opc in edata.OPCSetList)
                {
                    var property = destObject.SETProperties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcSetList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.Value = opc.EDT;
                        opc.PDC = 0x00;
                        opc.EDT = null;
                        opcSetList.Add(opc);
                    }
                }
                foreach (var opc in edata.OPCGetList)
                {
                    var property = destObject.SETProperties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
                        //EDT はつけず、要求を受理できなかったことを示す。
                        //(そのままでよい)
                        opcGetList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
                        //EDT には読み出したプロパティ値を設定する
                        opc.PDC = (byte)property.Value.Length;
                        opc.EDT = property.Value;
                        opcGetList.Add(opc);
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync(request.address, new Frame()
                {
                    EHD1 = EHD1.ECHONETLite,
                    EHD2 = EHD2.Type1,
                    TID = request.frame.TID,
                    EDATA = new EDATA1()
                    {
                        SEOJ = edata.DEOJ,//入れ替え
                        DEOJ = edata.SEOJ,
                        ESV = ESV.SetGet_SNA,//SetGet_SNA(0x5E)
                        OPCSetList = opcSetList,
                        OPCGetList = opcGetList,
                    }
                });
                return false;
            }
            await RequestAsync(request.address, new Frame()
            {
                EHD1 = EHD1.ECHONETLite,
                EHD2 = EHD2.Type1,
                TID = request.frame.TID,
                EDATA = new EDATA1()
                {
                    SEOJ = edata.DEOJ,//入れ替え
                    DEOJ = edata.SEOJ,
                    ESV = ESV.SetGet_Res,//SetGet_Res(0x7E)
                    OPCSetList = opcSetList,
                    OPCGetList = opcGetList,
                }
            });
            return true;
        }

        /// <summary>
        /// ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
        /// 自発なので、0x73のみ。
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="sourceNode"></param>
        /// <returns></returns>
        private bool プロパティ値通知サービス(EDATA1 edata, EchoNode sourceNode)
        {
            bool hasError = false;
            var sourceObject = sourceNode.Devices.Where(d => d.GetEOJ() == edata.SEOJ).FirstOrDefault();
            if (sourceObject == null)
            {
                //ノードプロファイルからの通知の場合
                if (sourceNode.NodeProfile.GetEOJ() == edata.SEOJ)
                {
                    sourceObject = sourceNode.NodeProfile;
                }
                else
                {
                    //未知のオブジェクト
                    //新規作成(プロパティはない状態)
                    sourceObject = new EchoObjectInstance(edata.SEOJ);
                    sourceNode.Devices.Add(sourceObject);
                }
            }
            foreach (var opc in edata.OPCList)
            {
                var property = sourceObject.Properties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                if (property == null)
                {
                    //未知のプロパティ
                    //新規作成
                    property = new EchoPropertyInstance(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
                    sourceObject.Properties.Add(property);
                }
                if ((property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;
                }
                else
                {
                    property.Value = opc.EDT;
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        インスタンスリスト通知受信(sourceNode, opc.EDT);
                    }
                }
            }
            return !hasError;
        }

        /// <summary>
        /// ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="sourceNode"></param>
        /// <param name="destObject"></param>
        /// <returns></returns>
        private async Task<bool> プロパティ値通知応答要サービス((string address, Frame frame) request, EDATA1 edata, EchoNode sourceNode, EchoObjectInstance destObject)
        {
            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject == null)
            {
                //指定された DEOJ が存在しない場合には電文を廃棄する。
                //"けどこっそり保持する"
                hasError = true;
            }
            var sourceObject = sourceNode.Devices.Where(d => d.GetEOJ() == edata.SEOJ).FirstOrDefault();
            if (sourceObject == null)
            {
                //ノードプロファイルからの通知の場合
                if (sourceNode.NodeProfile.GetEOJ() == edata.SEOJ)
                {
                    sourceObject = sourceNode.NodeProfile;
                }
                else
                {
                    //未知のオブジェクト
                    //新規作成(プロパティはない状態)
                    sourceObject = new EchoObjectInstance(edata.SEOJ);
                    sourceNode.Devices.Add(sourceObject);
                }
            }
            foreach (var opc in edata.OPCList)
            {
                var property = sourceObject.Properties.Where(p => p.Spec.Code == opc.EPC).FirstOrDefault();
                if (property == null)
                {
                    //未知のプロパティ
                    //新規作成
                    property = new EchoPropertyInstance(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
                    sourceObject.Properties.Add(property);
                }
                if ((property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;

                }
                else
                {
                    property.Value = opc.EDT;
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        インスタンスリスト通知受信(sourceNode, opc.EDT);
                    }
                }
                //EPC には通知時と同じプロパティコードを設定するが、
                //通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
                opc.PDC = 0x00;
                opc.EDT = null;
                opcList.Add(opc);
            }
            if (destObject != null)
            {
                await RequestAsync(request.address, new Frame()
                {
                    EHD1 = EHD1.ECHONETLite,
                    EHD2 = EHD2.Type1,
                    TID = request.frame.TID,
                    EDATA = new EDATA1()
                    {
                        SEOJ = edata.DEOJ,//入れ替え
                        DEOJ = edata.SEOJ,
                        ESV = ESV.INFC_Res,//INFC_Res(0x74)
                        OPCList = opcList,
                    }
                });
            }
            return !hasError;

        }
    }
}
