using System;
using System.Collections.Generic;
using System.Globalization;
using DiplomacyOverview.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// Trade-agreement edges between non-eliminated kingdoms, from the native 1.4.7
    /// <c>TradeAgreementsCampaignBehavior</c> (docs/research/11). The behavior exposes no "all
    /// agreements" enumerator, so unordered kingdom pairs are probed via
    /// <c>HasTradeAgreement</c> — the same read path the vanilla Kingdom→Diplomacy screen
    /// exercises (which also lazily prunes expired entries game-side; we write nothing).
    /// Presence-gated: no behavior (pre-1.4 save, total conversion) means no trade edges,
    /// never a crash (AGENTS.md rule 6). Kingdom scope only — the game has no clan agreements.
    ///
    /// Because <see cref="RelationEdge.Create"/> canonicalizes endpoint order, per-side detail
    /// keys embed the kingdom StringId instead of relying on A/B position (issue #6
    /// core-contract note).
    /// </summary>
    internal sealed class TradeAgreementProvider : IRelationProvider
    {
        /// <summary>Details key: campaign day the agreement ends (invariant-culture number).</summary>
        public const string EndDayKey = "trade.endDay";

        /// <summary>
        /// Details key prefix: lifetime gold a side gained from the agreement; the owning
        /// kingdom's StringId is appended (e.g. "trade.goldTotal.empire").
        /// </summary>
        public const string GoldTotalKeyPrefix = "trade.goldTotal.";

        public RelationKind Provides => RelationKind.TradeAgreement;

        public IReadOnlyList<RelationEdge> GetEdges()
        {
            try
            {
                return BuildEdges();
            }
            catch (Exception ex)
            {
                // AGENTS.md rule 6: a provider failure means missing lines, never a crash.
                Diagnostics.Note("trade provider failed outright: " + ex);
                return Array.Empty<RelationEdge>();
            }
        }

        private static IReadOnlyList<RelationEdge> BuildEdges()
        {
            if (Campaign.Current is null) // P-07
            {
                return Array.Empty<RelationEdge>();
            }

            var behavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (behavior is null)
            {
                // Presence gate: the trade layer simply stays empty. Logged so an empty trade web is
                // never ambiguous between "data source absent" (this line) and "present, no active
                // agreements" (the summary below) — the two look identical on screen.
                Diagnostics.Note("trade provider: ITradeAgreementsCampaignBehavior absent — no trade lines (pre-1.4 save / total conversion?)");
                return Array.Empty<RelationEdge>();
            }

            var participants = new List<Kingdom>();
            foreach (var kingdom in Kingdom.All)
            {
                if (KingdomFilter.IsParticipant(kingdom))
                {
                    participants.Add(kingdom);
                }
            }

            var edges = new List<RelationEdge>();

            // Pairwise probes: ≤ ~200 short list scans at real kingdom counts, only on rebuild.
            // HasTradeAgreement already answers false for expired pairs.
            for (var i = 0; i < participants.Count; i++)
            {
                for (var j = i + 1; j < participants.Count; j++)
                {
                    // Containment is PER PAIR: one malformed kingdom object costs its own edges,
                    // not the whole trade layer (mirrors WarProvider's per-kingdom containment).
                    try
                    {
                        var a = participants[i];
                        var b = participants[j];
                        if (!behavior.HasTradeAgreement(a, b, out var agreement))
                        {
                            continue;
                        }

                        edges.Add(RelationEdge.Create(
                            a.StringId, b.StringId, RelationKind.TradeAgreement, BuildDetails(agreement)));
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.Note(
                            "trade edge skipped for pair '" + (participants[i]?.StringId ?? "<null>") + "'/'"
                            + (participants[j]?.StringId ?? "<null>") + "': " + ex.Message);
                    }
                }
            }

            // Behavior present: distinguishes "no agreements exist" (edges == 0 here) from the
            // absent-behavior note above and from a provider crash ("failed outright").
            Diagnostics.Note(
                "trade provider: behavior present, " + participants.Count + " kingdoms, "
                + edges.Count + " active agreement(s)");
            return edges;
        }

        private static IReadOnlyDictionary<string, string>? BuildDetails(
            TradeAgreementsCampaignBehavior.TradeAgreement agreement)
        {
            try
            {
                var details = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [EndDayKey] = agreement.EndTime.ToDays.ToString("0.##", CultureInfo.InvariantCulture),
                };

                var id1 = agreement.Kingdom1?.StringId;
                if (!string.IsNullOrEmpty(id1))
                {
                    details[GoldTotalKeyPrefix + id1] =
                        agreement.Kingdom1GoldGainedTotal.ToString(CultureInfo.InvariantCulture);
                }

                var id2 = agreement.Kingdom2?.StringId;
                if (!string.IsNullOrEmpty(id2))
                {
                    details[GoldTotalKeyPrefix + id2] =
                        agreement.Kingdom2GoldGainedTotal.ToString(CultureInfo.InvariantCulture);
                }

                return details;
            }
            catch
            {
                // Tooltip payload is optional garnish — losing it must not lose the edge.
                return null;
            }
        }
    }
}
