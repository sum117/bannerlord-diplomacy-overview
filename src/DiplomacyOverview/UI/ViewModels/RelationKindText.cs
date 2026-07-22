using DiplomacyOverview.Core;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// Localized display label for a <see cref="RelationKind"/> — shared by the legend rows
    /// (issue #7) and the edge tooltips (issue #10) so a kind reads identically in both. Every
    /// string is a keyed <see cref="TextObject"/> (P-12).
    /// </summary>
    internal static class RelationKindText
    {
        public static string Label(RelationKind kind)
        {
            switch (kind)
            {
                case RelationKind.War:
                    return new TextObject("{=DipOvKindWar}At War").ToString();
                case RelationKind.Alliance:
                    return new TextObject("{=DipOvKindAlliance}Alliance").ToString();
                case RelationKind.TradeAgreement:
                    return new TextObject("{=DipOvKindTrade}Trade Agreement").ToString();
                case RelationKind.NonAggressionPact:
                    return new TextObject("{=DipOvKindNap}Non-Aggression Pact").ToString();
                case RelationKind.CallToWar:
                    return new TextObject("{=DipOvKindCallToWar}Call to War").ToString();
                default:
                    return kind.ToString();
            }
        }
    }
}
