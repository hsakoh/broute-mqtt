namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 不揮発性メモリに保存されている仮想レジスタの内容をロードします。
    /// 何らかの要因でロードに失敗した場合、FAIL ER10 エラーになります。
    /// 内容が保存されていない状態でロードを実行すると L ER10 になります。
    /// </summary>
    internal class SKLoadCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKLoadCommand() : base("SKLOAD")
        {
        }
    }
}
