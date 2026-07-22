using System;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// The kinds of diplomatic relation an edge can represent. Flags-based so a caller can build
    /// filter masks (e.g. War | Alliance) even though any single <see cref="RelationEdge"/> always
    /// carries exactly one flag.
    /// </summary>
    [Flags]
    public enum RelationKind
    {
        None = 0,
        War = 1,
        Alliance = 2,
        NonAggressionPact = 4,
        CallToWar = 8,
        TradeAgreement = 16,
    }
}
