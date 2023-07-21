using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// PWD で指定したパスワードから PSK を生成して登録します。
    /// SKSETPSK による設定よりも本コマンドが優先され、PSK は本コマンドの内容で上書きされます。
    /// ＊）PWDの文字数が指定したLENに足りない場合、不足分は不定値になります。
    /// </summary>
    internal class SKSetPwdCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKSetPwdCommand(Input input) : base("SKSETPWD")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


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
