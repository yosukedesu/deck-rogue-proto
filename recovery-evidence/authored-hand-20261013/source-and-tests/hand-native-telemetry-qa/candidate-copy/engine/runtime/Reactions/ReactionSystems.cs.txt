// Reactions/ReactionSystems.cs — src/engine/reactions/index.ts の手書き移植 (unity/PORTING.md)
// 3方式は IReactionSystem の実装として完全に差し替え可能 (CLAUDE.md「比較する3方式」)。
using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    /// <summary>TS の ReactionSystem (オブジェクト literal) に対応するインターフェース</summary>
    public interface IReactionSystem
    {
        string Mode { get; }
        bool CanHandle(GameState state, Command command);
        GameState HandleCommand(GameState state, Command command);
        GameState OnEvent(GameState state, GameEvent ev);
    }

    public static class ReactionSystems
    {
        public static readonly IReactionSystem SetAuto = new SetAutoSystem();
        public static readonly IReactionSystem HoldManual = new HoldManualSystem();
        public static readonly IReactionSystem SetConfirm = new SetConfirmSystem();

        public static IReactionSystem Get(string mode)
        {
            switch (mode)
            {
                case ReactionModes.SetAuto: return SetAuto;
                case ReactionModes.HoldManual: return HoldManual;
                case ReactionModes.SetConfirm: return SetConfirm;
                default: throw new InvalidOperationException($"未知のリアクション方式: {mode}");
            }
        }
    }
}
