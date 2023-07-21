namespace SkstackIpDotNet
{
    internal enum ScanMode
    {
        /// <summary>
        /// 2:アクティブスキャン（IE あり）
        /// </summary>
        ActiveScanWithIE = '2',
        /// <summary>
        /// 3:アクティブスキャン（IE なし）
        /// </summary>
        ActiveScanWithoutIE = '3',
        /// <summary>
        /// 0:ED スキャン
        /// </summary>
        EdScan = '0',
    }

    internal enum SKTableMode
    {
        /// <summary>
        /// 1:端末で利用可能な IP アドレス一覧
        /// EADDR イベントが発生します。
        /// </summary>
        EAddr = '1',
        /// <summary>
        /// 2:ネイバーキャッシュ
        /// ENEIGHBOR イベントが発生します。
        /// </summary>
        ENeighbor = '2',
        /// <summary>
        /// 9:ネイバーテーブル一覧
        /// ENBR イベントが発生します。
        /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
        /// </summary>
        ENbr = '9',
        /// <summary>
        /// A:MAC セキィリティのキー設定表示
        /// ESEC イベントが発生します。
        /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
        /// </summary>
        ESec = 'A',
        /// <summary>
        /// F:TCP ハンドル状態一覧
        /// EHANDLE イベントが発生します。
        /// </summary>
        EHandle = 'F',
    }

    /// <summary>
    /// 暗号化オプション
    /// </summary>
    public enum SKSendToSec
    {
        /// <summary>
        /// 0: 必ず平文で送信
        /// </summary>
        Plain = '0',
        /// <summary>
        /// 1: SKSECENABLE コマンドで送信先がセキュリティ有効で登録されている場合、暗号化して送ります。
        /// 登録されてない場合、または、暗号化無しで登録されている場合、データは送信されません。
        /// </summary>
        SecOrNotTransfer = '1',
        /// <summary>
        /// 2: SKSECENABLE コマンドで送信先がセキュリティ有効で登録されている場合、暗号化して送ります。
        /// 登録されてない場合、または、暗号化無しで登録されている場合、データは平文で送信されます。
        /// </summary>
        SecOrPlain = '2',
    }
}
