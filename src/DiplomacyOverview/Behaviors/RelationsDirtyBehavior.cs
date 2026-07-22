using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace DiplomacyOverview.Behaviors
{
    /// <summary>
    /// The mod's only campaign presence: listens to the diplomacy-shape-changing events
    /// (docs/research/05 table) and raises a static dirty flag — nothing else. The graph itself is
    /// rebuilt lazily when the Relations tab is opened (never per-tick, never per-frame), so with
    /// the tab closed the mod's entire campaign cost is these flag writes.
    ///
    /// Save-safety: <see cref="SyncData"/> is intentionally empty and must stay empty — no
    /// SaveableTypeDefiner exists anywhere in this mod (AGENTS.md rule 1, P-09; enforced by
    /// ArchitectureInvariantTests). Listeners are non-serialized and exception-contained (P-08).
    /// </summary>
    internal sealed class RelationsDirtyBehavior : CampaignBehaviorBase
    {
        // Starts dirty so the first tab-open of a session always builds. Static on purpose: the
        // flag outlives screen-scoped VMs, and a stale-true worst case merely costs one rebuild.
        private static volatile bool _isDirty = true;

        /// <summary>True when campaign diplomacy changed since the graph was last rebuilt.</summary>
        public static bool IsDirty => _isDirty;

        public static void MarkClean() => _isDirty = false;

        private static void MarkDirty()
        {
            try
            {
                _isDirty = true;
            }
            catch
            {
                // P-08: a throwing listener disrupts the event broadcast for every other mod.
            }
        }

        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
                CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
                CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
                CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
                CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
                CampaignEvents.OnTradeAgreementSignedEvent.AddNonSerializedListener(this, OnTradeAgreementSigned);
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            }
            catch
            {
                // Degraded mode: the flag simply stays true and every tab-open rebuilds (AGENTS.md rule 6).
                _isDirty = true;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Intentionally empty: this mod never writes save data (P-09).
        }

        private static void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            => MarkDirty();

        private static void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
            => MarkDirty();

        private static void OnKingdomCreated(Kingdom kingdom) => MarkDirty();

        private static void OnKingdomDestroyed(Kingdom kingdom) => MarkDirty();

        private static void OnClanDestroyed(Clan clan) => MarkDirty();

        private static void OnTradeAgreementSigned(Kingdom kingdom1, Kingdom kingdom2) => MarkDirty();

        // Trade agreements END without any event — war auto-break arrives via OnWarDeclared, but
        // EndTime expiry is silent (docs/research/11) — so one flag write per game day bounds a
        // stale trade line's lifetime at one day past its agreement.
        private static void OnDailyTick() => MarkDirty();
    }
}
