using SkstackIpDotNet.Events;
using SkstackIpDotNet.Responses;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace SkstackIpDotNet
{
    public interface ISKDevice
    {
        event EventHandler<ERXTCP> OnERXTCPReceived;
        event EventHandler<ERXUDP> OnERXUDPReceived;
        event EventHandler<ETCP> OnETCPReceived;
        event EventHandler<EVENT> OnEVENTReceived;

        void Close();
        void Dispose();
        void Open(string port, int baud, int data, Parity parity, StopBits stopbits);
        //Task<OKorFAIL> SKAddNbrAsync(string ipaddr, string macaddr);
        //Task<OKorFAIL> SKCloseAsync(string handle);
        //Task<OKorFAIL> SKConnect(string ipAddr, string rPort, string lPort);
        //Task<OKorFAIL> SKErase();
        Task<EINFO> SKInfoAsync();
        Task<OKorFAIL> SKJoinAsync(string ipaddr);
        Task<SKLL64> SKLl64Async(string addr64);
        //Task<OKorFAIL> SKLoadAsync();
        //Task<EPONG> SKPingAsync(string ipaddr);
        //Task<OKorFAIL> SKRegDevAsync(string ipaddr);
        //Task<OKorFAIL> SKRejoinAsync();
        //Task<OKorFAIL> SKResetAsync();
        //Task<OKorFAIL> SKRmDevAsync(string target);
        //Task<OKorFAIL> SKSaveAsync();
        //Task<IEnumerable<EPANDESC>> SKScanActiveAsync(uint channelMask, byte duration);
        Task<IEnumerable<EPANDESC>> SKScanActiveExAsync(uint channelMask, byte duration);
        //Task<EEDSCAN> SKScanEdAsync(uint channelMask, byte duration);
        //Task<OKorFAIL> SKSecAsync(string mode, string ipaddr, string macaddr);
        //Task<OKorFAIL> SKSendAsync(string handle, byte[] data);
        Task<OKorFAIL> SKSendToAsync(string handle, string ipaddr, string port, SKSendToSec sec, byte[] data);
        //Task<OKorFAIL> SKSetKey(string index, string key);
        //Task<OKorFAIL> SKSetPskAsync(string len, string key);
        Task<OKorFAIL> SKSetPwdAsync(string len, string pwd);
        Task<OKorFAIL> SKSetRBIDAsync(string id);
        Task<ESREG> SKSRegAsync(string sreg, string val);
        //Task<OKorFAIL> SKStartAsync();
        //Task<EADDR> SKTableEAddrAsync();
        //Task<EHANDLE> SKTableEHandleAsync();
        //Task<ENBR> SKTableENbrAsync();
        //Task<ENEIGHBOR> SKTableENeighborAsync();
        //Task<ESEC> SKTableESecAsync();
        //Task<OKorFAIL> SKTcpPortAsync(string index, string port);
        //Task<OKorFAIL> SKTermAsync();
        //Task<OKorFAIL> SKUdpPortAsync(string handle, string port);
        //Task<EVER> SKVerAsync();
    }
}