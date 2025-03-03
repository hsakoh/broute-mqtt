using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// PWD で指定したパスワードから PSK を生成して登録します。
    /// SKSETPSK による設定よりも本コマンドが優先され、PSK は本コマンドの内容で上書きされます。
    /// ＊）PWDの文字数が指定したLENに足りない場合、不足分は不定値になります。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKSetPwdCommand(SKSetPwdCommand.Input input) : SimpleCommand("SKSETPWD")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Len} {Arg.Pwd}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 1-32
            /// </summary>
            public string Len { get; set; }
            /// <summary>
            /// ASCII 文字
            /// </summary>
            public string Pwd { get; set; }
        }
    }
}
