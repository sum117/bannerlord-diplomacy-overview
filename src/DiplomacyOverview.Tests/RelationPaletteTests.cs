using System.Text.RegularExpressions;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class RelationPaletteTests
    {
        [Theory]
        [InlineData(RelationKind.War, "#C0392BFF")]
        [InlineData(RelationKind.Alliance, "#4E9B47FF")]
        [InlineData(RelationKind.TradeAgreement, "#D4A017FF")] // the mockup orange, reclaimed from NAP (doc 09)
        [InlineData(RelationKind.NonAggressionPact, "#8E44ADFF")]
        [InlineData(RelationKind.CallToWar, "#2E86ABFF")]
        public void ColorOf_SingleKinds_MapToDocPalette(RelationKind kind, string expected)
        {
            Assert.Equal(expected, RelationPalette.ColorOf(kind));
        }

        [Theory]
        [InlineData(RelationKind.None)]
        [InlineData(RelationKind.War | RelationKind.Alliance)] // masks are for filtering, not edges
        public void ColorOf_NoneOrCombinedFlags_FallBackToNeutral(RelationKind kind)
        {
            Assert.Equal(RelationPalette.FallbackColor, RelationPalette.ColorOf(kind));
        }

        [Theory]
        [InlineData(RelationKind.None)]
        [InlineData(RelationKind.War)]
        [InlineData(RelationKind.Alliance)]
        [InlineData(RelationKind.NonAggressionPact)]
        [InlineData(RelationKind.TradeAgreement)]
        [InlineData(RelationKind.CallToWar)]
        public void ColorOf_AlwaysReturnsGauntletParseableRgbaHex(RelationKind kind)
        {
            // The widget hands this string to Color.ConvertStringToColor, which expects #RRGGBBAA.
            Assert.Matches(new Regex("^#[0-9A-F]{8}$"), RelationPalette.ColorOf(kind));
        }
    }
}
