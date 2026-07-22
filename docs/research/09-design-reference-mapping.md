# 09 — Design reference → implementation mapping

Reference: [docs/images/diplomacy-overview-design-reference.png](../images/diplomacy-overview-design-reference.png)
— a Realm-of-Thrones-themed mock ("Game of Thrones" title) of a kingdom-screen-style page with tabs
`Clans | Fiefs | Relations`, a legend (At War = red, Trade Pact = orange, Alliance = green), twelve
house sigils in circular medallions arranged on a circle, colored lines between them, close (X) top-right.

## Element-by-element mapping

| Mockup element | Implementation | Source docs |
|---|---|---|
| Screen + tab strip (`Clans/Fiefs/Relations`) | Vanilla Kingdom screen; our injected **Relations** tab (mockup omits Policies/Armies/Diplomacy — vanilla keeps all five + ours) | 04 §A |
| Title ("Game of Thrones") | Vanilla kingdom screen already shows the kingdom name banner — free | 04 §A |
| Close X (top-right) | Vanilla screen chrome (`Standard.DialogCloseButtons` / kingdom screen's own close) — free | 04 §F |
| Legend, top-left, 3 colored entries | `MBBindingList<RelationLegendItemVM>`: color swatch (tinted `BlankWhiteSquare_9`) + label + **click-to-toggle** (`ButtonType="Toggle"`) — the client's toggle ask lives here | 04 §E, 07 |
| Circular sigil medallions | `MaskedTextureWidget` + `BannerImageIdentifierVM(faction.Banner)`; circular frame brush or `SetCircualMask` | 04 §D |
| Node labels ("House Stark") | `TextWidget`, anchor outward from circle (layout math places label on the node's radial) | 07 CircleLayout |
| Circle arrangement | `CircleLayout` pure math; deterministic node order (e.g., alphabetical or by strength) so positions are stable across refreshes | 07 |
| Red lines (At War) | `WarProvider` → vanilla `IFaction.FactionsAtWarWith` | 05 |
| Green lines (Alliance) | `AllianceProvider` → vanilla v1.3 native alliances (`Kingdom.IsAllyWith`) | 05 |
| **Orange lines (Trade Pact)** | **No data source exists** — see decision below | 05 |
| "No affiliation ⇒ no line" | Edges only materialize from provider hits — inherently satisfied | 07 |
| Kingdom/Clan dropdown (client ask, not drawn in mock) | `DropdownWidget` + `SelectorVM<SelectorItemVM>`: `Kingdoms` / `Clans` scopes | 04 §E, 05 |

## Client request traceability

Raw asks → status:

1. *"every kingdom/clan having a line … allied, trade agreement, or at war"* → War ✅ vanilla;
   Alliance ✅ vanilla (v1.3 native); Trade ✅ vanilla (**v1.4.7 native** — see reopened decision below).
2. *"toggle on/off whatever the player wants … there could be a ton of lines"* → legend toggles per
   relation kind ✅ + minor-clan filter and lazy rebuild for line volume (RoT ≈ 93 clans) ✅.
3. *"no affiliation → no line"* → ✅ by construction.
4. *"work with Realm of Thrones"* → same game branch, additive UI injection, defensive
   banner/faction handling; verification pass is milestone M5 ✅ (doc 06).
5. *"dropdown to select kingdom or clans … more detailed view (houses under the same kingdom)"* →
   scope dropdown ✅; in clan scope, cluster clans by kingdom on the circle (RoT houses group
   naturally) ✅ (doc 07 M3).

## The Trade Pact decision — **DECIDED 2026-07-02: option 1** · **REOPENED 2026-07-22**

> **UPDATE 2026-07-22:** the premise dissolved — game **v1.4.7 ships native kingdom Trade
> Agreements** (API: doc 05 §Trade + doc 11), and the player can see them in the vanilla Kingdom →
> Diplomacy screen. The mock's orange "Trade Pact" line is now implementable on pure vanilla via a
> `TradeAgreementProvider` (presence-gated on the behavior, P-08 posture). Meanwhile the Diplomacy
> mod — the NAP data source this decision substituted with — is currently not installed on the dev
> machine. Recommendation: promote **Trade Agreement to a first-class relation kind** (the client's
> original ask) and keep NAP as the Diplomacy-mod extra. Needs client sign-off + a roadmap issue;
> the original decision below stands as history.

Decision (recorded in issue #4): substitute with **Non-Aggression Pact** lines, strictly
presence-gated — when the Diplomacy mod is installed the NAP legend entry and dashed lines appear;
when it isn't, the feature is simply absent (no greyed-out stub). Rationale: NAPs are real,
data-backed, and visually equivalent to the mock's third line type, and realistic mod lists include
Diplomacy anyway. Option 3 (economy-mod adapters) stays open as a possible v2, uncommitted.

Original analysis: no trade-agreement concept exists in vanilla or the Diplomacy mod; two gameplay
mods (Art of the Trade, Living Economy) implement their own — reflection adapters are possible later
(doc 05). Options considered, in recommended order:

1. **Substitute with real data**: legend = War (red) / Alliance (green) / **Non-Aggression Pact**
   (orange or dashed) via the Diplomacy mod — visually equivalent to the mock, fully data-backed.
   Optional 4th: Call-to-War agreements (vanilla).
2. Add a Trade Pact adapter for Art of the Trade / Living Economy users later (v2 feature).
3. Show Trade Pact greyed out with "no data source installed" hint — sets expectations, invites the
   mod-page conversation.

## Extra polish the data makes free (v1.5+ candidates)

- Edge tooltips: war duration + casualties (incl. naval), tribute direction/amount, alliance/NAP
  expiry countdown (`GetAllianceEndDate`, NAP `EndTime`) — doc 05.
- Line styles for accessibility: dashed NAP, dotted call-to-war (stamped-sprite technique, doc 04 §C).
- Node click → encyclopedia page (`IFaction.EncyclopediaLink`); hover → highlight that node's edges only.
- Leader-relation "heat" edges (clan scope, `Hero.GetRelation`) — the client's "affiliation" idea,
  behind an off-by-default toggle.

## v1 scope statement (proposed)

Kingdom + clan scopes; War/Alliance/(NAP when Diplomacy present) edges; legend toggles; circle
layout with banner medallions and labels; edge tooltips (basic); vanilla + RoT verified; no
persistence, no settings UI (MCM later); trade pact deferred per decision above.
