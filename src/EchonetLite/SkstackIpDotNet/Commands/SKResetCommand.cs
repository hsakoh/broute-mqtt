namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// プロトコル・スタックの内部状態を初期化します。
    /// すべての内部変数が初期値に戻ります。
    /// ただし 64bit アドレスだけは、S01 レジスタで設定した直近の値がそのまま再利用されます。
    /// </summary>
    internal class SKResetCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKResetCommand() : base("SKRESET")
        {
        }
    }
}
