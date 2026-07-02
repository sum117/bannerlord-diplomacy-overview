# 03 — Module anatomy & build pipeline

## Module folder anatomy (what ships to `<game>\Modules\DiplomacyOverview`)

Modeled on the installed Diplomacy/UIExtenderEx modules **[LOCAL]**:

```
DiplomacyOverview/
├── SubModule.xml                        # manifest (below)
├── bin/Win64_Shipping_Client/
│   ├── DiplomacyOverview.dll            # our assembly (+ .pdb during dev)
│   └── (no TaleWorlds*/0Harmony DLLs — ever; deps come from their own modules)
├── GUI/
│   ├── Prefabs/DiplomacyOverview/*.xml  # our movie/prefab XMLs (hot-reloadable)
│   └── Brushes/DiplomacyOverview.xml    # optional custom brushes (reusing native sprites)
└── ModuleData/
    └── Languages/std_module_strings_xml.xml (+ per-language subfolders later)
```

Localization layout precedent: Diplomacy ships `ModuleData\Languages\{BR,CNs,CNt,DE,FR,JP,KO,PL,RU,SP,TR}\...`
with `language_data.xml` per language. Start EN-only with `{=key}` strings from day one (doc 08 #12).

## SubModule.xml — annotated proposal

Assembled from the installed BUTR exemplars (Harmony/ButterLib/UIExtenderEx/MCM/Diplomacy manifests
read verbatim) and the BUTR template. **[LOCAL + template]**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Module xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BUTR/Bannerlord.XmlSchemas/master/SubModule.xsd">
  <Id value="DiplomacyOverview" />
  <Name value="Diplomacy Overview" />
  <Version value="v0.1.0" />
  <DefaultModule value="false" />
  <ModuleCategory value="Singleplayer" />
  <ModuleType value="Community" />
  <Url value="https://github.com/sum117/bannerlord-diplomacy-overview" />

  <!-- Hard deps: the game refuses to load us without these (vanilla launcher semantics) -->
  <DependedModules>
    <DependedModule Id="Bannerlord.Harmony" DependentVersion="v2.4.2" />
    <DependedModule Id="Bannerlord.UIExtenderEx" DependentVersion="v2.13.2" />
    <DependedModule Id="Native" DependentVersion="v1.3.15" />
    <DependedModule Id="SandBoxCore" DependentVersion="v1.3.15" />
    <DependedModule Id="Sandbox" DependentVersion="v1.3.15" />
    <DependedModule Id="StoryMode" DependentVersion="v1.3.15" />
  </DependedModules>

  <!-- BUTR community metadata: richer semantics, enforced by BLSE/LauncherEx -->
  <!-- https://github.com/BUTR/Bannerlord.BLSE#community-dependency-metadata -->
  <DependedModuleMetadatas>
    <DependedModuleMetadata id="Bannerlord.Harmony"      order="LoadBeforeThis" version="v2.4.2" />
    <DependedModuleMetadata id="Bannerlord.UIExtenderEx" order="LoadBeforeThis" version="v2.13.2" />
    <!-- optional integrations: load before us IF present; absence is fine -->
    <DependedModuleMetadata id="Bannerlord.MBOptionScreen" order="LoadBeforeThis" version="v5.11.4" optional="true" />
    <DependedModuleMetadata id="Bannerlord.Diplomacy"      order="LoadBeforeThis" optional="true" />
    <DependedModuleMetadata id="Native"      order="LoadBeforeThis" version="v1.3.15.*" />
    <DependedModuleMetadata id="SandBoxCore" order="LoadBeforeThis" version="v1.3.15.*" />
    <DependedModuleMetadata id="Sandbox"     order="LoadBeforeThis" version="v1.3.15.*" />
    <DependedModuleMetadata id="StoryMode"   order="LoadBeforeThis" version="v1.3.15.*" />
    <DependedModuleMetadata id="CustomBattle" order="LoadBeforeThis" version="v1.3.15.*" optional="true" />
    <DependedModuleMetadata id="NavalDLC"     order="LoadBeforeThis" version="v1.1.3.*"  optional="true" />
  </DependedModuleMetadatas>

  <SubModules>
    <SubModule>
      <Name value="Diplomacy Overview" />
      <DLLName value="DiplomacyOverview.dll" />
      <SubModuleClassType value="DiplomacyOverview.SubModule" />
      <Tags />
    </SubModule>
  </SubModules>
</Module>
```

### Semantics worth internalizing **[LOCAL exemplars + BLSE README]**

- `DependedModules` = vanilla-launcher hard requirements (`Optional="true"` exists but is weak).
- `DependedModuleMetadatas` = BUTR extension: `order="LoadBeforeThis|LoadAfterThis"`,
  `optional="true"`, `version` / `version="vX.Y.Z.*"` ranges, `incompatible="true"`.
  Both blocks should agree (hard deps appear in both; optional ones only in metadata).
- `ModulesToLoadAfterThis` is how *framework* mods (Harmony et al.) sort themselves **above** the
  official modules. A content mod like ours does the inverse: everything `LoadBeforeThis`.
- Officials use `<ModuleType value="Official|OfficialOptional">`; community mods `Community`.
- Real-world load order on this machine confirms: frameworks → officials → content mods → patches. **[LOCAL]**
- Gotcha from a shipped mod: Diplomacy's manifest contains `DependentVersion="vv1.3.4"` — double
  `v`, silently tolerated. Version strings are not validated; be exact (doc 08 #2).

## csproj blueprint

Distilled from the template (`BLNamespace.csproj`) with our choices. **[LOCAL template copy]**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <Platforms>x64</Platforms>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <Version>0.1.0</Version>
    <ModuleId>DiplomacyOverview</ModuleId>
    <ModuleName>DiplomacyOverview</ModuleName>
    <GameFolder>$(BANNERLORD_GAME_DIR)</GameFolder>
    <GameVersion>1.3.15.110062</GameVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- build/deploy plumbing: packs _Module, substitutes tokens, copies to <game>\Modules -->
    <PackageReference Include="Bannerlord.BuildResources" Version="1.1.0.124" PrivateAssets="all"
                      IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
    <!-- compile-time refs; never shipped (their modules provide them at runtime) -->
    <PackageReference Include="Lib.Harmony" Version="2.3.3" IncludeAssets="compile" />
    <PackageReference Include="Bannerlord.UIExtenderEx" Version="2.13.2" IncludeAssets="compile" />
    <!-- game reference assemblies, pinned to the installed build -->
    <PackageReference Include="Bannerlord.ReferenceAssemblies.Core"    Version="$(GameVersion)" PrivateAssets="All" />
    <PackageReference Include="Bannerlord.ReferenceAssemblies.Native"  Version="$(GameVersion)" PrivateAssets="All" />
    <PackageReference Include="Bannerlord.ReferenceAssemblies.SandBox" Version="$(GameVersion)" PrivateAssets="All" />
    <PackageReference Include="Bannerlord.ReferenceAssemblies.StoryMode" Version="$(GameVersion)" PrivateAssets="All" />
    <!-- nullable-annotation shims for net472 -->
    <PackageReference Include="Nullable" Version="1.3.1" PrivateAssets="all" IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
    <PackageReference Include="IsExternalInit" Version="1.0.3" PrivateAssets="all" IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
  </ItemGroup>
</Project>
```

Notes:
- Template pins may lag (e.g. `Lib.Harmony 2.3.3` vs installed module 2.4.2) — Harmony is
  backward-compatible at compile time; bump if an API is missing. **[template + INFERRED]**
- MCM: when we add settings, use the template's **soft-dependency** MCM pattern
  (`Bannerlord.MCM` package + `MCMv5` SubModule entries guarded in SubModule.xml) or plain hard
  optional metadata. Decision deferred (doc 07 §settings).
- If ReferenceAssemblies for `1.3.15.110062` are missing any assembly we need, fall back to direct
  game-folder `<Reference>` with `Private=False` (template shows the exact globs).
- Exact `Version` string format for the packages is `1.3.15.110062` (4-part, no `v`). **[LOCAL nupkg]**

## Per-version DLL pattern (know it, defer it)

BUTR's `Bannerlord.ModuleLoader` lets one module ship `MyMod.1.3.4.dll … MyMod.1.3.13.dll` and pick
at runtime (`LoaderFilter` tag; see Diplomacy/MCM manifests). That's how big mods span game
versions. **v1 of our mod targets 1.3.15 only with a single DLL**; adopt the loader only when a new
game branch actually demands it. **[LOCAL]**

## CI/CD (GitHub Actions)

BUTR publishes reusable workflows ([BUTR/workflows](https://github.com/BUTR/workflows), consumed by
[DiplomacyTeam/Bannerlord.Diplomacy](https://github.com/DiplomacyTeam/Bannerlord.Diplomacy):
`Publish.yml`, `TestBuild.yml`): **[WEB: [BUTR publishing docs](https://butr.github.io/documentation/advanced/publishing-on-github/)]**

- `BUTR/workflows/.github/workflows/release-nexusmods.yml@master` — secrets `NEXUSMODS_APIKEY`, `NEXUSMODS_COOKIES`
- `.../release-steam.yml@master` — secrets `STEAM_LOGIN`, `STEAM_PASSWORD`, `STEAM_AUTH_CODE`
- `.../release-github.yml@master`, `.../release-nuget.yml@master`

Recommended for us now: a plain **build + artifact** workflow on PR/push (`dotnet build -c Release`
+ upload the packed module folder). Wire the release-* workflows only when we're actually publishing.
Verify exact `with:` inputs against a current consumer (Diplomacy's `Publish.yml`) at adoption time —
inputs observed: `nexusmods_game_id: mountandblade2bannerlord`, `nexusmods_mod_id`, `mod_filename`,
`mod_version`, `mod_description`, `artifact_name`. **[WEB, medium confidence on arg names]**

Repo conventions worth copying from Diplomacy: `src/` layout, `build/common.props`,
`supported-game-versions.txt` (drives their multi-version CI matrix), CHANGELOG discipline.
