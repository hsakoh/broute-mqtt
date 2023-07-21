using System;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// プロパティクラス
    /// </summary>
    public class EchoPropertyInstance
    {
        public EchoPropertyInstance(Specifications.EchoProperty spec)
        {
            Spec = spec;
        }
        public EchoPropertyInstance(byte classGroupCode, byte classCode, byte epc)
        {
            Spec = SpecificationUtil.FindProperty(classGroupCode, classCode, epc);
            Spec ??= new Specifications.EchoProperty()
            {
                Code = epc,
            };
            Get = false;
            Set = false;
            Anno = false;
        }
        /// <summary>
        /// EPC
        /// ECHONET機器オブジェクト詳細規定がある場合、詳細仕様
        /// </summary>
        public Specifications.EchoProperty Spec { get; set; }
        /// <summary>
        /// プロパティ値の読み出し・通知要求のサービスを処理する。
        /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
        /// </summary>
        public bool Get { get; set; }
        /// <summary>
        /// プロパティ値の書き込み要求のサービスを処理する。
        /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
        /// </summary>
        public bool Set { get; set; }
        /// <summary>
        /// プロパティ値の通知要求のサービスを処理する。
        /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
        /// </summary>
        public bool Anno { get; set; }

        private byte[] _Value;
        /// <summary>
        /// プロパティ値
        /// </summary>
        public byte[] Value
        {
            get => _Value;
            set
            {
                //TODO とりあえず変更がなくてもイベントを起こす
                ValueChanged?.Invoke(this, value);
                if (value == _Value)
                    return;
                _Value = value;
            }
        }
        /// <summary>
        /// プロパティ値変更イベント
        /// </summary>
        public event EventHandler<byte[]> ValueChanged;
    }
}
