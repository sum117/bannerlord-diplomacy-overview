# 05 — Diplomacy data access (v1.3.15 APIs; 1.4.7 trade addendum)

Every signature below was decompiled from the installed assemblies. **[LOCAL]** throughout;
exceptions tagged. Namespace root: `TaleWorlds.CampaignSystem` unless stated.
2026-07-22: every API our code consumes compiles unchanged against **1.4.7.117484** (doc 11); the
"Trade Pact gap" section is superseded — v1.4.7 has native Trade Agreements (update inline below).

## Enumerating the graph's nodes

```csharp
MBReadOnlyList<Kingdom> kingdoms = Kingdom.All;          // => Campaign.Current.Kingdoms
MBReadOnlyList<Clan>    clans    = Clan.All;             // => Campaign.Current.Clans
```

Filters that matter:

- Kingdoms: skip `k.IsEliminated`.
- Clans: skip `c.IsEliminated`, `c.IsBanditFaction`; decide policy for `c.IsMinorFaction`
  (mercenaries/outlaws — they *do* have war stances; a toggle candidate). `c.Kingdom` is null for
  independents; `c.MapFaction` resolves to the kingdom when sworn, else the clan itself.
- Everything UI-relevant lives on **`IFaction`** (common to Kingdom & Clan — perfect for the scope
  dropdown): `Name`, `Banner`, `Color`/`Color2` (uint ARGB), `Leader`, `Culture`, `IsKingdomFaction`,
  `IsClan`, `IsMinorFaction`, `IsEliminated`, `MapFaction`, `EncyclopediaLink`.

## Wars

```csharp
// pairwise:
bool atWar = faction1.IsAtWarWith(faction2);                       // IFaction instance method
bool atWar2 = FactionManager.IsAtWarAgainstFaction(f1, f2);        // static equivalent
// enumerate an entity's wars directly (no pair scan needed):
MBReadOnlyList<IFaction> enemies = faction.FactionsAtWarWith;
// rich per-war data for tooltips:
StanceLink s = faction1.GetStanceWith(faction2);
```

`StanceLink` (war/peace only — `internal enum StanceType { Neutral, War }`; **no alliance stance**):
`IsAtWar`, `IsNeutral`, `Faction1/2`, `WarStartDate`, `PeaceDeclarationDate`, `TroopCasualties1/2`,
`ShipCasualties1/2` (War Sails), `SuccessfulSieges1/2`, `SuccessfulRaids1/2`,
`TotalTributePaidFrom1To2` / `2To1`, `GetDailyTributeToPay(IFaction)`, per-faction getters
(`GetCasualties(IFaction)` …). Tribute direction/amount = great edge tooltip content.

`FactionManager` full war surface: `DeclareWar`, `SetNeutral`, `IsAtWarAgainstFaction`,
`IsAtConstantWarAgainstFaction` (minor factions), `IsNeutralWithFaction`. We only ever *read*.

## Alliances — vanilla-native in v1.3 (kingdom-level)

```csharp
bool allied = kingdom1.IsAllyWith(kingdom2);              // Kingdom instance method
MBReadOnlyList<Kingdom> allies = kingdom.AlliedKingdoms;  // maintained collection
// richer queries via the behavior interface (public):
var ab = Campaign.Current.GetCampaignBehavior<CampaignBehaviors.IAllianceCampaignBehavior>();
// interface: IsAllyWithKingdom, GetAllianceEndDate(k1,k2), HasCalledToWar, StartAlliance/EndAlliance…
```

Semantics from `DefaultAllianceModel` + `AllianceCampaignBehavior`: alliances are **time-limited**
(default max 84 days, auto-expire), **max 2 per kingdom**, stored as an internal `List<Alliance>`
(`Kingdom1`, `Kingdom2`, `EndTime`) inside the behavior — *not* in `StanceLink`. There is also a
**call-to-war agreement** concept (`CallToWarAgreementStarted/Ended` events) — a candidate 4th line
type with real vanilla data. Alliance end date → "expires in X days" tooltip.

Clan-scope note: alliances are kingdom-level only. In clan view, render alliance edges between
clans' `MapFaction`s (i.e., inherited from their kingdoms) or restrict alliance lines to kingdom view.

## Non-aggression pacts — Diplomacy mod (optional integration)

From decompiled `Bannerlord.Diplomacy.1.3.13.dll` **[LOCAL]**:

- Storage: `Diplomacy.DiplomaticAction.DiplomaticAgreementManager` — **internal class**, but its
  members are public: `static DiplomaticAgreementManager Instance`,
  `static bool HasNonAggressionPact(Kingdom, Kingdom, out NonAggressionPactAgreement)` (expiry-aware),
  `Dictionary<FactionPair, List<DiplomaticAgreement>> Agreements`.
- `AgreementType` enum has exactly one value: `NonAggressionPact`. **No alliance store** (defers to
  vanilla) and **no trade concepts** (0 hits for trade/commerce/embargo in its type list).
- Agreements carry `StartDate`/`EndDate` → "NAP expires in X days" tooltips.

**Safe cross-mod read recipe** (type is internal → reflect the *type*, invoke its *public* method;
never reference the assembly at compile time):

```csharp
static class DiplomacyNapAdapter
{
    private static MethodInfo? _hasNap;   // resolve once, cache
    public static bool TryInit()
    {
        // Assembly file names are per-game-version (Bannerlord.Diplomacy.1.3.13.dll) — never
        // hardcode; scan loaded assemblies instead. Absence simply means "no NAP data".
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.StartsWith("Bannerlord.Diplomacy") == true
                                 && a.GetType("Diplomacy.DiplomaticAction.DiplomaticAgreementManager") != null);
        _hasNap = asm?.GetType("Diplomacy.DiplomaticAction.DiplomaticAgreementManager")
                     ?.GetMethod("HasNonAggressionPact", BindingFlags.Public | BindingFlags.Static);
        return _hasNap != null;
    }
    public static bool HasNap(Kingdom a, Kingdom b)
    {
        if (_hasNap is null) return false;
        var args = new object?[] { a, b, null };
        try { return (bool)_hasNap.Invoke(null, args)!; } catch { return false; }  // mod-version drift → degrade
    }
}
```

Initialize lazily at first campaign use (after all modules loaded), guarded by the optional
`DependedModuleMetadata` (doc 03) so Diplomacy, when present, loads before us. Wrap in try/catch —
a Diplomacy update must never crash us, only remove NAP lines. **[LOCAL mechanism; exact member
shape re-verify against future Diplomacy versions]**

## Clan-to-clan color (optional relation heat)

The client floated "affiliation" beyond war/alliance. Real primitives available:

```csharp
int rel = hero1.GetRelation(hero2);        // -100..100, leader-to-leader
// Hero.SetPersonalRelation / GetBaseHeroRelation also exist; CharacterRelationManager backs it
```

A "leader relation" edge type (green/red gradient above/below thresholds) is data-backed and cheap —
but O(n²) lookups; compute only when that toggle is on. Marked v2 candidate (doc 09).

## Refresh events (when to rebuild the graph)

Exact `CampaignEvents` members **[LOCAL]**:

| Event | Why we care |
|---|---|
| `WarDeclared` | red edges appear |
| `MakePeace` | red edges disappear |
| `OnTradeAgreementSignedEvent` *(1.4.7)* | trade edges appear — **no end event exists**; see §Trade update |
| `AllianceStartedEvent` / `AllianceEndedEvent` | green edges |
| `CallToWarAgreementStartedEvent` / `CallToWarAgreementEndedEvent` | if we render the 4th type |
| `ClanChangedKingdom` | clan nodes re-cluster; edges re-derive |
| `KingdomCreated` / `KingdomDestroyed` / `ClanDestroyed` | node set changes |

Pattern: a `CampaignBehaviorBase` with `RegisterEvents()` calling
`CampaignEvents.X.AddNonSerializedListener(this, handler)` and an **empty `SyncData`** (read-only mod
⇒ nothing persisted ⇒ save-safe, doc 06). Handlers should only set a `dirty` flag; rebuild lazily
when the Relations tab is opened/visible. NAP changes have no vanilla event — Diplomacy's expiry is
time-based — so also rebuild on tab open and optionally `DailyTickEvent` while visible.

## The "Trade Pact" gap — **SUPERSEDED 2026-07-22: v1.4.7 has native Trade Agreements**

Game v1.4.7 added kingdom⇄kingdom **Trade Agreements** to the base game. Full surface, mechanics,
and provider-design notes: [11-game-1.4.7-migration.md](11-game-1.4.7-migration.md). Essentials
**[LOCAL v1.4.7]**:

```csharp
var b = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();  // null-guard (P-08)
bool has = b.HasTradeAgreement(k1, k2, out TradeAgreementsCampaignBehavior.TradeAgreement a);
CampaignTime end = b.GetTradeAgreementEndDate(k1, k2);  // only when has == true
// a: Kingdom1, Kingdom2, EndTime + per-kingdom gold-gained counters (tooltip material).
// Refresh: CampaignEvents.OnTradeAgreementSignedEvent (IMbEvent<Kingdom, Kingdom>).
// Endings fire NO event: war declaration auto-breaks (behavior listens to WarDeclared itself),
// kingdom destruction clears, and EndTime expiry is silent (pruned lazily on next query)
// => a trade provider must also dirty on DailyTickEvent, or rebuild trade edges on tab open.
// Note: HasTradeAgreement prunes expired entries as a side effect — that is the game's own
// bookkeeping on the official read path (vanilla KingdomDiplomacyVM makes the identical call);
// our read-only rule is about *us* writing state, which we still never do.
```

The original 1.3.15 analysis is kept below for history:

- Vanilla: ~~no trade agreements~~ (closest: war *tribute*, which we can already show on war edges).
- Diplomacy mod: none (verified above).
- Ecosystem: two gameplay mods implement trade-agreement *mechanics* —
  [Art of the Trade](https://www.nexusmods.com/mountandblade2bannerlord/mods/10414) (diplomacy-gated
  trade rights) and [Bannerlord Living Economy](https://www.nexusmods.com/mountandblade2bannerlord/mods/10796)
  (kingdom trade-agreement corridors). Neither exposes a public API designed for consumers; interop
  would be another reflection adapter, per-mod. **[WEB]**

**Recommendation:** v1 line types = **War / Alliance / Non-Aggression Pact (/ Call-to-War?)** — all
data-backed. Show "Trade Pact" in the legend only when a data source exists (future adapter);
options for the client in doc 09.
