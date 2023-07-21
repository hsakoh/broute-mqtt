namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 現在の仮想レジスタの内容を不揮発性メモリに保存します。
    /// 何らかの要因で保存に失敗した場合、FAIL ER10 エラーになります。
    /// </summary>
    internal class SKSaveCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKSaveCommand() : base("SKSAVE")
        {
        }
    }
}
