using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// バイト配列-16進数ユーティリティ
    /// </summary>
    public static class BytesConvert
    {
        /// <summary>
        /// バイト配列から16進数の文字列を生成します。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <returns>16進数文字列</returns>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                if (b < 16) sb.Append('0'); // 二桁になるよう0を追加
                sb.Append(Convert.ToString(b, 16));
            }
            return sb.ToString().ToUpper();
        }

        /// <summary>
        /// 16進数の文字列からバイト配列を生成します。
        /// </summary>
        /// <param name="str">16進数文字列</param>
        /// <returns>バイト配列</returns>
        public static byte[] FromHexString(string str)
        {
            int length = str.Length / 2;
            byte[] bytes = new byte[length];
            int j = 0;
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Convert.ToByte(str.Substring(j, 2), 16);
                j += 2;
            }
            return bytes;
        }
    }
}
