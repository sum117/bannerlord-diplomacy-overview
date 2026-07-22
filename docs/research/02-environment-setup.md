# 02 — Environment setup runbook

Goal: from clean machine to "edit code/XML → build → module auto-deployed → game runs it → debugger attached".
This machine already satisfies most prerequisites. **[LOCAL]** where noted.

## 1. Prerequisites

- **.NET SDK** — any modern SDK builds `net472` class libraries. Installed here: 10.0.x. **[LOCAL]**
  If a hand-rolled csproj fails with missing .NET Framework reference assemblies, add
  `<PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.*" PrivateAssets="all" />`
  **[UNVERIFIED — only needed if the BUTR packages don't already satisfy it; test at first build]**
- **IDE** — VS 2022 or Rider. Rider note: create projects from `dotnet new` CLI, then open in Rider
  (the template's own recommendation). **[WEB: template README]**
- **Game** — Steam install at
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord` (v1.3.15 + War Sails). **[LOCAL]**
- **BLSE** — already installed (`bin\Win64_Shipping_Client\Bannerlord.BLSE.*.exe`). **[LOCAL]**
- Optional: the **Modding Kit** (editor tools, `SpriteSheetGenerator`) is a separate Steam tool
  install — **not present here**. We deliberately avoid needing it (doc 04 §sprites). **[LOCAL]**

## 2. Set the game-folder convention

The BUTR ecosystem's convention is an environment variable consumed by csproj:

```powershell
[Environment]::SetEnvironmentVariable('BANNERLORD_GAME_DIR',
  'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord', 'User')
```

Projects then use `<GameFolder>$(BANNERLORD_GAME_DIR)</GameFolder>` — no hardcoded paths in the repo. **[WEB: template README]**

## 3. Project scaffolding — two viable paths

### Path A: BUTR template (reference/starting point)

```powershell
dotnet new install Bannerlord.Templates
dotnet new blmodfx --name "DiplomacyOverview" [options]   # framework-style, all options
# or: dotnet new blmodsdk  (SDK-style variant using Bannerlord.Module.Sdk)
```

The template wires: `net472` target, ReferenceAssemblies pinning, `Bannerlord.BuildResources`
(deploy + packaging), SubModule.xml token substitution (`$moduleid$`, `$version$`…), optional
Harmony/ButterLib/UIExtenderEx/MCM refs, `.vscode`/`launchSettings.json` debug profiles.
**[WEB: [Bannerlord.Module.Template](https://github.com/BUTR/Bannerlord.Module.Template); repo + generated files read during research]**

### Path B: hand-rolled csproj following the template's patterns (recommended)

We control every line and skip template options we don't need. Blueprint in doc 03 — it is
essentially the template's output with our choices baked in. Use the template once in a scratch
folder as a cross-check if anything misbehaves.

## 4. Reference assemblies (compile without shipping game DLLs)

Exact package IDs (nupkg inspected) **[LOCAL]** + [BUTR/Bannerlord.ReferenceAssemblies](https://github.com/BUTR/Bannerlord.ReferenceAssemblies) **[WEB]**:

- Family: `Bannerlord.ReferenceAssemblies.{Core, Native, SandBox, SandBoxCore*, StoryMode, CustomBattle, BirthAndDeath, Multiplayer, NavalDLC}` (+ `.EarlyAccess` variants for beta branches).
  \*SandBoxCore assemblies ride inside the module packages; the template references Core/Native/SandBox/StoryMode/CustomBattle/BirthAndDeath.
- **Pin exactly** to the installed build — currently `Bannerlord.ReferenceAssemblies.Core`
  **`1.4.7.117484`** (was `1.3.15.110062`; bumped 2026-07-22, doc 11). When `Version.xml` gives only
  `v<major>.<minor>.<rev>`, read the changeset from the decompiled
  `TaleWorlds.Library.ApplicationVersion.DefaultChangeSet` constant. Never float across minors.
- `Bannerlord.ReferenceAssemblies.NavalDLC` exists (latest observed `1.3.14.107738`) — **we don't
  reference it**; the Relations view uses no naval APIs.
- Packages contain `ref/net472` + `ref/netstandard2.0` reference-only assemblies, `PrivateAssets="All"`.

Alternative (offline / bleeding-edge): reference the game folder directly with `Private=False`
(never copy game DLLs to output) — the template shows the exact `<Reference>` globs. Reference
assemblies are preferred: reproducible CI builds without the game installed.

## 5. Build → deploy loop

`Bannerlord.BuildResources` (v1.1.0.124 in the template) gives MSBuild targets that, when
`<ModuleName>` and `<GameFolder>` are set, **copy the built module into `<game>\Modules\<ModuleName>`
after every build**. The `_Module\` folder in the project is the module-root template (SubModule.xml,
`GUI\`, `ModuleData\`) and is copied verbatim with token substitution. **[WEB: template README + BuildResources sources read]**

Enable the mod once in the launcher (BLSE LauncherEx) → thereafter build+launch is the whole loop.

## 6. Run & debug

- **Launchers on this machine** **[LOCAL]**: `Bannerlord.BLSE.LauncherEx.exe` (load-order UI),
  `Bannerlord.BLSE.Launcher.exe`, `Bannerlord.BLSE.Standalone.exe` (CLI, no launcher UI).
- **Direct launch with forced module list** (community-documented; skips launcher):

  ```
  bin\Win64_Shipping_Client\Bannerlord.exe /singleplayer
    _MODULES_*Bannerlord.Harmony*Bannerlord.ButterLib*Bannerlord.UIExtenderEx*Bannerlord.MBOptionScreen*Native*SandBoxCore*CustomBattle*SandBox*StoryMode*BirthAndDeath*NavalDLC*DiplomacyOverview*_MODULES_
  ```

  Working directory must be `bin\Win64_Shipping_Client`. Module IDs between `*`, order = load order.
  **[WEB: [docs.bannerlordmodding.com basic-csharp-mod](https://docs.bannerlordmodding.com/_tutorials/basic-csharp-mod.html); NavalDLC/BirthAndDeath added for 1.3.15 [LOCAL]]**
  Prefer `Bannerlord.BLSE.Standalone.exe` with the same args so BLSE features stay active. **[UNVERIFIED arg-forwarding — spike]**
- **VS/Rider**: launch profile = start external program (exe above) with args + workdir; or attach
  to the running `Bannerlord.exe`. Build with `<DebugType>portable</DebugType>` and copy the PDB
  (BuildResources does) for breakpoints.
- Crash diagnostics: ButterLib/BLSE produce structured crash-report HTML (module list + stacktrace
  attribution) — read those before guessing. **[WEB]**

## 7. Gauntlet UI iteration (hot reload)

- Prefab XML that lives **as loose files** in `<mod>\GUI\Prefabs\**` is re-read when the containing
  screen is closed and reopened — edit XML → reopen screen, **no game restart**. A restart is needed
  only when adding a *new* file. **[WEB: [docs.bannerlordmodding.lt/gauntletui/uiextenderex](https://docs.bannerlordmodding.lt/gauntletui/uiextenderex/)]**
- UIExtenderEx's own SubModule carries `<Tag key="DumpXML" value="false">` — presumably dumps
  post-patch XML for debugging. **[UNVERIFIED — check UIExtenderEx source for "DumpXML" before relying on it]**
- Code-side (mixins/widgets) changes still need rebuild + restart. Keep layout/styling in XML to
  maximize hot-reloadable surface.

## 8. Decompile toolkit (API ground truth)

- `dotnet tool install --global ilspycmd`, then e.g.
  `ilspycmd -t TaleWorlds.CampaignSystem.FactionManager "<game>\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll"`.
  Use `--list c` + grep to find exact type names first. **[LOCAL — this is how every API claim in these docs was produced]**
- Cross-check signatures on [apidoc.bannerlord.com](https://apidoc.bannerlord.com) (pick the version in the URL).
- **Never commit decompiled TaleWorlds/mod sources to this repo** — keep them in a local scratch
  folder; commit only distilled notes (like these docs).
