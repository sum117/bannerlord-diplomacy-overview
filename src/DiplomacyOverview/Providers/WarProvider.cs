using System;
using System.Collections.Generic;
using System.Globalization;
using DiplomacyOverview.Core;
using TaleWorlds.CampaignSystem;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// War edges between non-eliminated kingdoms, straight from the vanilla stance system
    /// (docs/research/05: <c>IFaction.FactionsAtWarWith</c> per node — no O(n²) pair scan; the
    /// per-war <c>StanceLink</c> payload is captured into <see cref="RelationEdge.Details"/> for
    /// later tooltips).
    ///
    /// Because <see cref="RelationEdge.Create"/> canonicalizes endpoint order, every
    /// direction-sensitive detail is encoded in Details VALUES, never inferred from A/B order
    /// (issue #6 core-contract note): the tribute payer is stored as a node id under
    /// "tribute.payer".
    /// </summary>
    internal sealed class WarProvider : IRelationProvider
    {
        /// <summary>Details key: campaign day the war started (invariant-culture number).</summary>
        public const string WarStartDayKey = "war.startDay";

        /// <summary>Details key: StringId of the tribute-paying side. Absent when no tribute flows.</summary>
        public const string TributePayerKey = "tribute.payer";

        /// <summary>Details key: daily tribute amount paid by "tribute.payer" (positive integer).</summary>
        public const string TributeDailyAmountKey = "tribute.dailyAmount";

        /// <summary>Details key prefix: a faction's war casualties; append the faction StringId
        /// (e.g. "war.casualties.vlandia"). Survives endpoint canonicalization (id-keyed, not A/B).</summary>
        public const string CasualtiesKeyPrefix = "war.casualties.";

        public RelationKind Provides => RelationKind.War;

        public IReadOnlyList<RelationEdge> GetEdges()
        {
            try
            {
                return BuildEdges();
            }
            catch (Exception ex)
            {
                // AGENTS.md rule 6: a provider failure means missing lines, never a crash.
                Diagnostics.Note("war provider failed outright: " + ex);
                return Array.Empty<RelationEdge>();
            }
        }

        private static IReadOnlyList<RelationEdge> BuildEdges()
        {
            if (Campaign.Current is null) // P-07
            {
                return Array.Empty<RelationEdge>();
            }

            var edges = new List<RelationEdge>();

            foreach (var kingdom in Kingdom.All)
            {
                // Containment is PER KINGDOM on purpose: on a heavy mod list one malformed
                // kingdom object must cost its own edges, not the whole web (the #6 in-game
                // pass ran against ~70 mod-created kingdoms — docs/research/10 run 6).
                try
                {
                    if (!KingdomFilter.IsParticipant(kingdom))
                    {
                        continue;
                    }

                    var enemies = kingdom.FactionsAtWarWith;
                    if (enemies is null)
                    {
                        continue;
                    }

                    foreach (var enemy in enemies)
                    {
                        // Kingdom scope only: wars against clans/minors are out (issue #6), and
                        // zombie-kingdom endpoints are dropped by the same participant test.
                        if (!(enemy is Kingdom other) || !KingdomFilter.IsParticipant(other))
                        {
                            continue;
                        }

                        // FactionsAtWarWith yields each war from both sides; keep the pair once.
                        // (Also guards the self-loop RelationEdge.Create rejects.)
                        if (string.CompareOrdinal(kingdom.StringId, other.StringId) >= 0)
                        {
                            continue;
                        }

                        edges.Add(RelationEdge.Create(
                            kingdom.StringId, other.StringId, RelationKind.War, BuildDetails(kingdom, other)));
                    }
                }
                catch (Exception ex)
                {
                    Diagnostics.Note(
                        "war edges skipped for kingdom '" + (kingdom?.StringId ?? "<null>") + "': " + ex.Message);
                }
            }

            return edges;
        }

        private static IReadOnlyDictionary<string, string>? BuildDetails(Kingdom kingdom, Kingdom other)
        {
            try
            {
                var stance = kingdom.GetStanceWith(other);
                if (stance is null)
                {
                    return null;
                }

                var details = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [WarStartDayKey] = stance.WarStartDate.ToDays.ToString("0.##", CultureInfo.InvariantCulture),
                };

                // GetDailyTributeToPay(f) is what f pays per day: positive = f pays, negative =
                // f receives (decompiled v1.3.15 StanceLink). Direction goes into the VALUE.
                var dailyTribute = stance.GetDailyTributeToPay(kingdom);
                if (dailyTribute > 0)
                {
                    details[TributePayerKey] = kingdom.StringId;
                    details[TributeDailyAmountKey] = dailyTribute.ToString(CultureInfo.InvariantCulture);
                }
                else if (dailyTribute < 0)
                {
                    details[TributePayerKey] = other.StringId;
                    details[TributeDailyAmountKey] = (-dailyTribute).ToString(CultureInfo.InvariantCulture);
                }

                details[CasualtiesKeyPrefix + kingdom.StringId] =
                    stance.GetCasualties(kingdom).ToString(CultureInfo.InvariantCulture);
                details[CasualtiesKeyPrefix + other.StringId] =
                    stance.GetCasualties(other).ToString(CultureInfo.InvariantCulture);

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
