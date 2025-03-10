﻿using System.Text;

namespace SkstackIpDotNet.Commands.BP35C0
{
    /// <summary>
    /// 指定されたIDから各 Route-B ID を生成して設定します。
    /// Pairing ID (SA レジスタ)としてIDの下位 8 文字が設定されます。
    /// ＊）IDは ASCII 32 文字必要で、足りない場合、不足分が不定値になります。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKSetRBIDCommand(SKSetRBIDCommand.Input input) : SimpleCommand("SKSETRBID")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Id}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 32 桁の ASCII 文字
            /// </summary>
            public string Id { get; set; }
        }
    }
}
