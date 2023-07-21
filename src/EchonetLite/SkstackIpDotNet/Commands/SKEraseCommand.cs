namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// レジスタ保存用の不揮発性メモリエリアを初期化して、未保存状態に戻します。
    /// 本コマンドを実行後、SKLOAD コマンドを発行すると SKLOAD コマンドは ER10 エラーを返すようになります。
    /// </summary>
    internal class SKEraseCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKEraseCommand() : base("SKERASE")
        {
        }
    }
}
