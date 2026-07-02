using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace DiplomacyOverview.Tests
{
    /// <summary>
    /// Enforces AGENTS.md hard rules against the built module assembly. Reads raw PE metadata
    /// instead of loading the assembly, so the absent TaleWorlds runtime is irrelevant and the
    /// net472 module can be inspected from the net8 test host.
    /// </summary>
    public class ArchitectureInvariantTests
    {
        [Fact]
        public void Module_assembly_never_references_SaveableTypeDefiner()
        {
            // Save-safety guarantee (AGENTS.md rule 1, P-09): the mod writes nothing into save
            // files. Any reference to SaveableTypeDefiner — deriving from it is impossible
            // without referencing it — breaks the "safe to add/remove mid-campaign" promise.
            using var stream = File.OpenRead(FindModuleAssembly());
            using var pe = new PEReader(stream);
            var md = pe.GetMetadataReader();

            var offenders = md.TypeReferences
                .Select(md.GetTypeReference)
                .Where(r => md.GetString(r.Name) == "SaveableTypeDefiner")
                .Select(r => $"{md.GetString(r.Namespace)}.{md.GetString(r.Name)}")
                .ToArray();

            Assert.True(offenders.Length == 0,
                $"DiplomacyOverview.dll references {string.Join(", ", offenders)} — " +
                "the mod must never define save data (AGENTS.md rule 1, P-09).");
        }

        [Fact]
        public void Module_build_output_contains_only_our_assembly()
        {
            // Packaging rule (AGENTS.md rule 2, P-03): game/framework DLLs must never ship in the
            // module bin. The build output folder feeds both the BuildResources deploy and the CI
            // artifact staging, so a stray DLL here would reach players.
            var dir = Path.GetDirectoryName(FindModuleAssembly())!;
            var foreign = Directory.GetFiles(dir, "*.dll")
                .Select(Path.GetFileName)
                .Where(name => !name!.StartsWith("DiplomacyOverview", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.True(foreign.Length == 0,
                $"Foreign DLLs would ship with the module (P-03): {string.Join(", ", foreign)}");
        }

        private static string FindModuleAssembly()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DiplomacyOverview.sln")))
            {
                dir = dir.Parent;
            }

            Assert.True(dir is not null,
                "Could not locate the repo root (folder containing DiplomacyOverview.sln) above the test bin.");

            var binRoot = Path.Combine(dir!.FullName, "src", "DiplomacyOverview", "bin");
            var dll = Directory.Exists(binRoot)
                ? Directory.GetFiles(binRoot, "DiplomacyOverview.dll", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault()
                : null;

            Assert.True(dll is not null,
                "DiplomacyOverview.dll not found under src/DiplomacyOverview/bin — " +
                "build the full solution before running tests (dotnet build DiplomacyOverview.sln).");
            return dll!;
        }
    }
}
