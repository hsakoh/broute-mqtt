using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// PANA 認証に用いる PSK を登録します。
    /// すでに PSK が登録されている場合は新しい値で上書きされます。
    /// KEYのバイト列は ASCII２文字で１バイトの HEX 表現で指定します。そのためLENで指定する PSK 長の２倍の文字入力が必要です。
    /// PSK を変更するには、SKRESET でリセットした後、再度、SKSETPSK コマンドを発行する必要があります。
    /// ＊）PSK は 16 バイトの必要があります。そのため LEN は 0x10 以外で FAIL ER06 になります。
    /// またKEYが 32 文字（１６バイト）に足りない場合は、不足分が不定値になります。
    /// </summary>
    internal class SKSetPskCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKSetPskCommand(Input input) : base("SKSETPSK")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Len} {Arg.Key}");
            }
        }

        public class Input
        {
            /// <summary>
            /// PSK のバイト長
            /// </summary>
            public string Len { get; set; }
            /// <summary>
            /// PSK バイト列
            /// </summary>
            public string Key { get; set; }
        }
    }
}
