using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 指定されたキーインデックスに対する暗号キー(128bit)を、MAC 層セキュリティコンポーネントに登録します。
    /// 本コマンドでキーを設定後、SKSECENABLE コマンドで対象デバイスのセキュリティ設定を有効にすることで、以後、そのデバイスに対するユニキャストが MAC 層で暗号化されます。
    /// 指定したキーの桁が 16 バイト（ASCII 32 文字）より多い場合、ER06 になります。
    /// 桁が 16 バイトより少ない場合、キーの内容が不定になり、OK または FAIL どちらになるか不定です。必ず 32 文字を指定してください。
    /// 暗号キーの登録容量を超えている場合、FAIL ER10 になります。指定したキーインデックスに既存の設定がある場合、新しい設定で上書き登録されます。
    /// 登録に成功するとカレントキーインデックスが指定したINDEXに切り替わります。
    /// </summary>
    internal class SKSetKeyCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKSetKeyCommand(Input input) : base("SKSETKEY")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Index} {Arg.Key}");
            }
        }

        public class Input
        {
            /// <summary>
            /// キーインデックス
            /// </summary>
            public string Index { get; set; }
            /// <summary>
            /// 128bit NWK 暗号キー
            /// </summary>
            public string Key { get; set; }
        }
    }
}
