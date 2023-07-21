namespace SkstackIpDotNet.Events
{
    /// <summary>
    /// TCP の接続、切断処理が発生すると通知されます。
    /// </summary>
    public class ETCP : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ETCP(string response) : base(response)
        {
            var values = response.Split(' ');
            Status = values[1];
            Handle = values[2];
            if (Status == "01")
            {
                Ipaddr = values[3];
                RPort = values[4];
                LPort = values[5];
            }
        }

        /// <summary>
        /// TCP 処理ステータス
        /// 1：相手先との接続完了（成功）
        /// 3：切断成功、または相手先から切断された(対応するハンドル番号が通知されます)接続に失敗した(HANDLE=0 で通知されます)
        /// 4：指定された接続元ポート番号がすでに使われている
        /// 5：データ送信完了（成功）データ送信でタイムアウトが発生すると、ETCP 3 HANDLEで切断となり、そのハンドルは回収されます。
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// イベント対象となった TCP ハンドル番号
        /// </summary>
        public string Handle { get; set; }
        /// <summary>
        /// STATUS = 1 の場合のみ 接続先、または接続元の IP アドレスが通知されます
        /// </summary>
        public string Ipaddr { get; set; }
        /// <summary>
        /// STATUS = 1 の場合のみ 相手側の接続ポート番号が通知されます
        /// </summary>
        public string RPort { get; set; }
        /// <summary>
        /// STATUS = 1 の場合のみ 自端末の接続ポート番号が通知されます
        /// </summary>
        public string LPort { get; set; }

    }
}
