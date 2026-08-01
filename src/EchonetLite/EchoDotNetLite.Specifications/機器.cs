using System.Collections.Generic;

namespace EchoDotNetLite.Specifications
{

    /// <summary>
    /// ECHONET Lite クラスグループ定義
    /// 機器
    /// </summary>
    public static class 機器
    {
        /// <summary>
        /// 一覧
        /// </summary>
        public static IEnumerable<IEchonetObject> クラス一覧 =
        [
            住宅設備関連機器.低圧スマート電力量メータ,
            管理操作関連機器.コントローラ,
            プロファイル.ノードプロファイル,
        ];
        /// <summary>
        /// ECHONET Lite クラスグループ定義
        /// 住宅設備関連機器クラスグループ
        /// </summary>
        public static class 住宅設備関連機器
        {
            /// <summary>
            /// 0x88 低圧スマート電力量メータ
            /// </summary>
            public static IEchonetObject 低圧スマート電力量メータ = new EchonetObject(0x02, 0x88);
        }
        /// <summary>
        /// ECHONET Lite クラスグループ定義
        /// 管理操作関連機器 クラスグループ
        /// </summary>
        public static class 管理操作関連機器
        {
            /// <summary>
            /// 0xFF コントローラ
            /// </summary>
            public static IEchonetObject コントローラ = new EchonetObject(0x05, 0xFF);
        }
    }
}