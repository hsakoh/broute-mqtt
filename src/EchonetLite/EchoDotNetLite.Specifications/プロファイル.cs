using System.Collections.Generic;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// ECHONET Lite クラスグループ定義
    /// プロファイルクラスグループ
    /// </summary>
    public static class プロファイル
    {
        /// <summary>
        /// 0xF0 ノードプロファイル
        /// </summary>
        public static IEchonetObject ノードプロファイル = new EchonetObject(0x0E, 0xF0);

        /// <summary>
        /// クラス一覧
        /// </summary>
        public static IEnumerable<IEchonetObject> クラス一覧 = new List<IEchonetObject>()
        {
            ノードプロファイル,
        };
    }
}
