namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 現在確立している PANA セッションの終了を要請します。
    /// SKTERM は PAA、PaC どちらからでも実行できます。接続が確立していない状態でコマンドを発行すると ER10 になります。
    /// セッションの終了に成功すると暗号通信は解除されます。
    /// また PAA は他デバイスからの新しい接続を受け入れることができるようになります。
    /// セッションの終了要請に対して相手から応答がなく EVENT 28 が発生した場合、セッションは終了扱いとなります。
    /// </summary>
    internal class SKTermCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKTermCommand() : base("SKTERM")
        {
        }
    }
}
