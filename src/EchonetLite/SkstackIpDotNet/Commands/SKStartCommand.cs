namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 端末を PAA (PANA 認証サーバ)として動作開始します。
    /// 動作開始に先立って PSK, PWD, Route-B ID 等のセキュリティ設定を済ませておく必要があります。
    /// 本コマンドを発行すると自動的にコーディネータとして動作開始し S15 レジスタ値は１になります。
    /// またアクティブスキャンに対して自動的に応答するようになります。
    /// 本コマンド発行前に確立していた PANA セッションはクリアされます。
    /// </summary>
    internal class SKStartCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKStartCommand() : base("SKSTART")
        {
        }
    }
}
