using TaleWorlds.CampaignSystem;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// Decides which kingdoms participate in the relations web.
    ///
    /// <c>Kingdom.All</c> is NOT "the active kingdoms": it is
    /// <c>CampaignObjectManager.Kingdoms</c>, mods create Kingdom objects freely, and only a proper
    /// <c>DestroyKingdomAction</c> flags <c>IsEliminated</c>. Verified on the reference 60-mod
    /// campaign (docs/research/10 run 6): Separatism creates a per-clan "personal kingdom" on every
    /// secession and, with its KeepEmptyKingdoms setting, deliberately keeps them alive after the
    /// clan leaves or dies — a long campaign accumulates dozens of live, clanless zombie kingdoms
    /// that flooded the #6 node circle.
    ///
    /// A kingdom with no living clan cannot meaningfully relate to anything, so it is not a node;
    /// RelationGraph then drops any stale zombie stances automatically (edges referencing a
    /// non-node endpoint are discarded by design).
    /// </summary>
    internal static class KingdomFilter
    {
        public static bool IsParticipant(Kingdom? kingdom)
        {
            if (kingdom is null || kingdom.IsEliminated)
            {
                return false;
            }

            try
            {
                var clans = kingdom.Clans;
                if (clans is null)
                {
                    return false;
                }

                for (var i = 0; i < clans.Count; i++)
                {
                    var clan = clans[i];
                    if (clan is not null && !clan.IsEliminated && clan.Leader?.IsAlive == true)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                // A kingdom whose clan list cannot even be read is not worth a node — skip it,
                // never crash (rule 6). WarProvider/RelationsVM note skips at their level.
                return false;
            }
        }
    }
}
