namespace DiplomacyOverview.Core
{
    /// <summary>
    /// Maps each <see cref="RelationKind"/> to its line color, as an "#RRGGBBAA" string (the format
    /// Gauntlet's <c>Color.ConvertStringToColor</c> parses). Palette source:
    /// docs/research/04-gauntlet-ui-playbook.md §F (mockup-derived). TradeAgreement owns the
    /// mockup's orange — NAP had only borrowed it while standing in for trade (doc 09, resolved
    /// 2026-07-22); kinds the mockup never colored (NAP, CallToWar) get distinct non-mockup hues.
    /// </summary>
    public static class RelationPalette
    {
        public const string WarColor = "#C0392BFF";
        public const string AllianceColor = "#4E9B47FF";
        public const string TradeAgreementColor = "#D4A017FF";
        public const string NonAggressionPactColor = "#8E44ADFF";
        public const string CallToWarColor = "#2E86ABFF";

        /// <summary>Neutral fallback for <see cref="RelationKind.None"/> or combined flags.</summary>
        public const string FallbackColor = "#FFFFFFFF";

        public static string ColorOf(RelationKind kind)
        {
            switch (kind)
            {
                case RelationKind.War:
                    return WarColor;
                case RelationKind.Alliance:
                    return AllianceColor;
                case RelationKind.TradeAgreement:
                    return TradeAgreementColor;
                case RelationKind.NonAggressionPact:
                    return NonAggressionPactColor;
                case RelationKind.CallToWar:
                    return CallToWarColor;
                default:
                    return FallbackColor;
            }
        }
    }
}
