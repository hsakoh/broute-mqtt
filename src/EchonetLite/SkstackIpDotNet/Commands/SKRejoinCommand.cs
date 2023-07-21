namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 現在接続中の相手に対して再認証シーケンスを開始します。
    /// 再認証シーケンスの前に SKJOIN による接続が成功している必要があり、接続していないとER10 になります。
    /// 再認証に成功すると、暗号キーと PANA セッション期限が更新されます。
    /// PaC は、PAA が指定したセッションのライフタイムの 80%が経過した時点で、自動的に再認証シーケンスを実行します。
    /// このため SKREJOIN コマンドは基本的に発行する必要がありませんが、任意のタイミングで再認証したい場合には本コマンドを使います。
    /// PAA は、セッションが更新されずにライフタイムが過ぎるとセッション終了要請を自動的に発行します。
    /// </summary>
    internal class SKRejoinCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKRejoinCommand() : base("SKREJOIN")
        {
        }
    }
}
