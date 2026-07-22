# 11 — Game 1.4.7 migration record (2026-07-22)

The dev machine was reformatted and the Steam install updated **v1.3.15.110062 → v1.4.7.117484**.
The changeset was read from the decompiled `TaleWorlds.Library.ApplicationVersion.DefaultChangeSet`
constant (`Version.xml` in the game bin only carries `v1.4.7`); it matches the newest
`Bannerlord.ReferenceAssemblies.*` build on NuGet (`1.4.7.117484`; the only other 1.4.7 build is
`117131`). Everything below is **[LOCAL v1.4.7]** unless tagged.

## Executed changes

| Where | Was | Now |
|---|---|---|
| `DiplomacyOverview.csproj` `<GameVersion>` | `1.3.15.110062` | **`1.4.7.117484`** |
| `DiplomacyOverview.csproj` UIExtenderEx package | 2.13.2 | **2.13.3** (matches installed module) |
| `SubModule.xml` Native/SandBoxCore/Sandbox/StoryMode deps | `v1.3.15` / `v1.3.15.*` | `v1.4.7` / `v1.4.7.*` |
| `SubModule.xml` MCM metadata | v5.11.4 | v5.12.2 |
| `SubModule.xml` NavalDLC metadata | `v1.1.3.*` | `v1.2.7.*` |
| Tests TFM (`DiplomacyOverview.Tests.csproj`) | net8.0 | **net10.0** |

Tests TFM rationale: the formatted machine has only .NET 9/10 runtimes, and CI pins SDK `10.0.x` —
the old net8.0 target only ran on CI because GitHub's runner image happens to preinstall the 8.0
runtime. net10.0 removes that hidden dependency on both sides.

## Verification performed

- `dotnet build` against `1.4.7.117484` reference assemblies: **0 errors** — every game API our
  providers/behaviors/UI/widgets consume still exists with identical signatures (compile-level).
- Unit tests: **96/96 pass** on net10.0.
- BuildResources auto-deploy to `<game>\Modules\DiplomacyOverview`: verified
  (`bin\Win64_Shipping_Client\{DiplomacyOverview,DiplomacyOverview.Core}.{dll,pdb}` + `GUI` +
  `SubModule.xml`).
- Injection anchors in the 1.4.7 `Modules\SandBox\GUI\Prefabs\KingdomManagement\KingdomManagement.xml`:
  `ButtonWidget[@Id='DiplomacyTabButton']` ✔, `!Header.Tab.Center.*` constants ✔,
  `KingdomTabControlListPanel` ✔, and the five vanilla panels keep the exact idiom we mirror
  (`<XPanel DataSource="{X}" MarginTop="188" MarginBottom="75" />`) ✔.
- Brush/sprite deps of our prefab (`Clan.Leader.Text`, `Flat.Tuple.Banner.Small`,
  `Kingdom.NameTitle.Text`, `BlankWhiteSquare_9`): all present in 1.4.7 GUI assets ✔.
- `System.Numerics.Vectors.dll` still ships in the game bin — the P-23 dev-path reference and its
  CI fallback both stay valid ✔.
- **In-game smoke test (hitl): PASSED 2026-07-22** — user-confirmed on v1.4.7: tab renders, the
  mixin attaches (War Sails kingdom-VM subclass), war edges draw. (Only a live run can prove the
  `handleDerived` attach — compile checks cannot.)

## Environment deltas (dev machine, post-reformat)

- Framework mods: Harmony 2.4.2.248, ButterLib 2.11.1, UIExtenderEx 2.13.3, MCM 5.12.2, BLSE ✔
  (Standalone + LauncherEx).
- **Bannerlord.Diplomacy: not installed.** The NAP reflection adapter (issue #9) has no local test
  target until it is reinstalled. Our optional-dependency metadata is unaffected — absence is legal.
- NavalDLC 1.2.7 now declares `DefaultModule="true"` (was `ModuleType=OfficialOptional` in 1.1.3).
- The modlist is now a heavy gameplay/visual set (RBM + RBM_WS, ImprovedGarrisons,
  DismembermentPlus, RealisticWeather, RTSCameraUniversal, AIInfluence, etc.) — none obviously
  patch the kingdom screen structurally [module-list surface scan only; not decompiled].
- Dev tooling restored this session: `BANNERLORD_GAME_DIR` env var (User scope), `ilspycmd` 10.1.1
  (dotnet global tool). .NET SDK 10.0.302 + VS Community 2026 were already present.

## Native Trade Agreements (new in 1.4.x) — decompiled surface

User-visible: Kingdom screen → **Diplomacy** tab lists Trade Agreement status/proposals per kingdom
(`KingdomDiplomacyVM` + `DiplomacyItemType.TradeAgreement`); signing goes through a kingdom
election (`TaleWorlds.CampaignSystem.Election.TradeAgreementDecision`).

Data model — all in `TaleWorlds.CampaignSystem.CampaignBehaviors`:

```csharp
public interface ITradeAgreementsCampaignBehavior {
    void MakeTradeAgreement(Kingdom k1, Kingdom k2, CampaignTime duration);       // (mutator — never ours)
    bool HasTradeAgreement(Kingdom k, Kingdom other,
                           out TradeAgreementsCampaignBehavior.TradeAgreement a); // the read API
    void EndTradeAgreement(Kingdom k, Kingdom other);                             // (mutator — never ours)
    CampaignTime GetTradeAgreementEndDate(Kingdom k, Kingdom other);              // asserts if absent — gate on Has first
    void OnTradeAgreementOfferedToPlayer(Kingdom fromKingdom);
    void OnTradeGoldDistributedInKingdom(Kingdom k1, Kingdom k2, Clan clan, int share);
}

public struct TradeAgreement {   // nested in TradeAgreementsCampaignBehavior
    public readonly Kingdom Kingdom1, Kingdom2;
    public readonly CampaignTime EndTime;
    public int Kingdom1GoldGained,      Kingdom2GoldGained;       // current (undistributed) share
    public int Kingdom1GoldGainedTotal, Kingdom2GoldGainedTotal;  // lifetime — tooltip material
}
```

Storage: private `List<TradeAgreement>` inside `TradeAgreementsCampaignBehavior`, save-persisted by
the **game's own** `SaveableTypeDefiner` (id 312260) — nothing for us to define; hard rule 1 intact.
There is no public "all agreements" enumerator: query pairwise via `HasTradeAgreement` (kingdom
pairs only — no clan-level agreements; ~20 kingdoms → ≤~200 pairs, trivially cheap).

Lifecycle / events:

- **Signed** → `CampaignEvents.OnTradeAgreementSignedEvent` (`IMbEvent<Kingdom, Kingdom>`).
- **Broken by war**: the behavior itself listens to `WarDeclared`, ends the agreement, and applies a
  **−50 leader relation** penalty (plus a trait hit when player-caused).
- **Kingdom destroyed**: agreements cleared via `KingdomDestroyedEvent`.
- **Expiry**: `EndTime` passes silently; the entry is pruned lazily on the next `HasTradeAgreement`
  call. **No ended/expired/broken event exists for any of these paths.**

Rules (`DefaultTradeAgreementModel`, overridable — NavalDLC ships `NavalTradeAgreementModel`):
duration **1 campaign year**; max **2** concurrent agreements per kingdom; proposing costs **200
influence**; gold accrues per caravan town-visit (`GetProfitPerCaravanVisit`; NavalDLC: flat 1000
for naval-capable parties) and is distributed within the kingdom.

### Provider-design implications (future `TradeAgreementProvider`)

- Presence-gate on `Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>() !=
  null` (P-08 posture — old saves or total conversions may lack the behavior). Same
  presence-gated-legend pattern doc 09 chose for NAPs.
- Pairwise-enumerate non-eliminated kingdoms with `HasTradeAgreement`; never reflect into the
  private list.
- Dirty-flag events: `OnTradeAgreementSignedEvent` + `WarDeclared` + `KingdomDestroyedEvent` cover
  the evented paths; **expiry is eventless → also set dirty on `DailyTickEvent`** (flag-set only,
  P-09-style cheap) or rebuild trade edges on every tab open.
- Read-safety: `HasTradeAgreement` prunes expired entries as a side effect. That is the game's own
  bookkeeping on its official read path — vanilla `KingdomDiplomacyVM` issues the identical call
  every time the Diplomacy tab opens. Our "never mutate campaign state" rule is about *us* writing
  state (SyncData, actions, setters); it is not violated by the game's lazy cleanup. Exception-wrap
  the calls all the same (hard rule 6).
- Tooltips: `GetTradeAgreementEndDate` countdown (only call after `HasTradeAgreement` returned
  true — it `Debug.FailedAssert`s on a missing pair) + gold gained current/total per side.

## Roadmap impact

- **#9 (Diplomacy NAP adapter)**: doubly blocked — decision-gated *and* the Diplomacy mod is no
  longer installed locally. Consider re-scoping M4 to lead with the native
  `TradeAgreementProvider` (the client's original "trade agreement" ask) and NAP second.
- **Proposed new issue**: `TradeAgreementProvider` (native 1.4.7) — orange edges + legend entry +
  expiry/gold tooltips; presence-gated. Supersedes the "trade pact has no data source" premise of
  issue #4 (doc 09 decision reopened).
- **#12 (compat matrix)**: add "does RoT have a 1.4.x release?" — v8.0 targeted 1.3.x (doc 01).
