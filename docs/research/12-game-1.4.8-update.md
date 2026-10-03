# 12 — Game 1.4.8 update record (2026-10-03)

Development moved to a Linux machine (Steam/Proton) whose install is **v1.4.8.119303**, the current
public-branch build (beta branch: 1.5.x). The changeset was read from
`TaleWorlds.Library.dll` (`v1.4.8.119303`; `Version.xml` only carries `v1.4.8`) and matches the only
1.4.8 `Bannerlord.ReferenceAssemblies.*` build on NuGet. Everything below is **[LOCAL v1.4.8]**.

## Executed changes

| Where | Was | Now |
|---|---|---|
| `DiplomacyOverview.csproj` `<GameVersion>` | `1.4.7.117484` | **`1.4.8.119303`** |
| `DiplomacyOverview.csproj` `<Version>` / `SubModule.xml` `<Version>` | 1.0.0 | **1.0.1** |
| `SubModule.xml` Native/SandBoxCore/Sandbox/StoryMode/CustomBattle | `v1.4.7` / `v1.4.7.*` | `v1.4.8` / `v1.4.8.*` |
| `SubModule.xml` NavalDLC metadata | `v1.2.7.*` | `v1.2.8.*` |
| Toolchain | SDK installed system-wide (Windows) | `mise.toml` pins .NET SDK 10.0.401 |

No source changes were needed. Framework package pins (UIExtenderEx 2.13.3, Lib.Harmony 2.3.3,
BuildResources 1.1.0.124) are unchanged — UIExtenderEx 2.13.3 is still the newest on NuGet.

## Verification performed

- `dotnet build` against `1.4.8.119303` reference assemblies: **0 errors** — every game API we
  consume (`ITradeAgreementsCampaignBehavior`, `IAllianceCampaignBehavior`, `Kingdom.AlliedKingdoms`,
  `ClanManagementVM`, …) compiles unchanged. The net472 build works on Linux as-is.
- Unit tests: **105/105 pass** on net10.0.
- Injection anchors in the 1.4.8 `Modules/SandBox/GUI/Prefabs/Clan/ClanScreen.xml`: exactly one
  `ButtonWidget[@Brush='Header.Tab.Right']` ✔, exactly one `<ClanIncome>` ✔, vanilla tabs still
  bind `Command.Click="SetSelectedCategory"` ✔ and `ClanManagementVM.SetSelectedCategory` still
  exists (the `[ViewModelMixin]` refresh hook) ✔.
- Brush/sprite deps (`Clan.Leader.Text`, `Clan.TabControl.Text`, `Flat.Tuple.Banner.Small`,
  `Header.Tab.Center`, `BlankWhiteSquare_9`): all present in the 1.4.8 GUI assets ✔.
- `System.Numerics.Vectors.dll` still ships in the game bin (P-23 reference stays valid) ✔.
- BuildResources deploy to `<game>/Modules/DiplomacyOverview` works with a Linux
  `BANNERLORD_GAME_DIR` ✔.
- **In-game smoke test: NOT yet run.** This game copy has no Harmony/UIExtenderEx modules, so the
  mod cannot load until they are installed. Runtime behaviour on 1.4.8 is therefore unverified.

## Release automation

`release.yml` now (1) fails a tag whose name differs from the csproj/`SubModule.xml` version and
(2) uploads the release zip to Nexus Mods through the official `Nexus-Mods/upload-action` (v3 upload
API, pinned by commit), archiving the previous version and updating the mod-page version. It needs
the `NEXUSMODS_API_KEY` secret and `NEXUSMODS_FILE_ID` variable (`NEXUSMODS_MOD_ID` optional, adds a
changelog line) — all three are set on the repo as of 2026-10-03. The action can only add versions
to a file that already exists (ours: the v1.0.0 main file on mod page 12242).

The v3 API ids are **not** the numbers in the site URLs — verified with read-only calls:
`GET /mod-files/7696464/versions` → 200 (file *group* id; the URL file id `63480` → 404) and
`GET /mods/13632226209746/files` → 200 (mod uid; the page number `12242` → 404). Both come from the
Files tab → "Advanced" dialog (`file-group-id` / `mod-uid` attributes). The upload step itself has
not run yet — its first real execution is the next tag push.

## Localization (added with 1.0.1)

`_Module/ModuleData/Languages/` now ships an English template and the 12 languages the game
itself ships, in vanilla's layout (`<code>/language_data.xml` whose `id` is the language's native
name, `xml_path` relative to `Languages/`). Terms with a vanilla equivalent ("At War", "Trade
Agreement", tribute, call to war, …) copy the official string for that language; e.g. Español (LA)
says "contribución" for diplomatic tribute, and 简体中文 uses 贡金 (the bare "Tribute" id maps to an
unrelated perk name there). Faction names arrive as plain strings, so the article/case functions
vanilla uses (`{.L}`, `{.d}`) are unavailable — labels are phrased as `Losses: {F}` in inflected
languages instead. `{N} days` became `{N} {?N > 1}days{?}day{\?}` (vanilla's own pattern; RU uses
the "дн." abbreviation vanilla uses). `LocalizationTests` pins ids, variables and the English
template to the code. **Not verified in-game and not reviewed by native speakers.**

## Open compat signals (Nexus comments, 2026-10-03)

- "Does it work for BL 1.4.8?" and "does not work with 1.5.2" — v1.0.0 pins `v1.4.7` /
  `v1.4.7.*` in `SubModule.xml`, which BLSE/LauncherEx enforce, so every game patch gates the mod
  off until a re-release. The source compiles unchanged against `1.5.3.122374-beta` (0 errors), so
  the 1.5.x failure is the version gate and/or a runtime/prefab change — untested, no beta install.
- Requests: translation folder (CHS/Spanish) — done above; a 1.3.x build; larger banners in the
  medallions.
