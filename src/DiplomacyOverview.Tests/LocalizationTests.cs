using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace DiplomacyOverview.Tests
{
    /// <summary>
    /// Keeps the shipped translations (<c>_Module/ModuleData/Languages</c>) in lock-step with the
    /// keyed <c>TextObject</c> literals in the module source (AGENTS.md rule 5, P-12). Works on the
    /// files alone — no game types — so a string added in code without its translations, or a
    /// translation that drops a <c>{VARIABLE}</c>, fails here instead of showing up in-game as
    /// English text or a raw placeholder.
    /// </summary>
    public class LocalizationTests
    {
        // {=Id}default text, up to the closing quote of the C# literal.
        private static readonly Regex KeyedLiteral = new Regex(@"\{=(\w+)\}((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

        // Plain variables only ({N}, {F}); conditionals ({?N > 1}, {?}, {\?}) start with ? or \.
        private static readonly Regex Variable = new Regex(@"\{([A-Z][A-Z0-9_]*)\}", RegexOptions.Compiled);

        [Fact]
        public void English_template_matches_the_defaults_in_code()
        {
            // The game prefers a loaded language file over the inline default, so a stale English
            // file would silently override the text in code.
            var code = CodeStrings();
            var english = LoadStrings(Path.Combine(LanguagesDir(), "DiplomacyOverview_strings.xml"));

            Assert.Equal(code.OrderBy(p => p.Key), english.OrderBy(p => p.Key));
        }

        [Fact]
        public void Every_language_translates_every_string_and_keeps_its_variables()
        {
            var code = CodeStrings();
            var languageDirs = Directory.GetDirectories(LanguagesDir());
            Assert.NotEmpty(languageDirs);

            var problems = new List<string>();
            foreach (var dir in languageDirs)
            {
                var name = Path.GetFileName(dir);
                var strings = LoadStrings(Path.Combine(dir, "DiplomacyOverview_strings.xml"));

                problems.AddRange(code.Keys.Except(strings.Keys).Select(id => $"{name}: missing {id}"));
                problems.AddRange(strings.Keys.Except(code.Keys).Select(id => $"{name}: unknown {id}"));

                foreach (var pair in strings.Where(p => code.ContainsKey(p.Key)))
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                    {
                        problems.Add($"{name}: {pair.Key} is empty");
                    }

                    if (!Variables(pair.Value).SetEquals(Variables(code[pair.Key])))
                    {
                        problems.Add($"{name}: {pair.Key} variables differ from the default \"{code[pair.Key]}\"");
                    }
                }
            }

            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void Every_language_data_file_points_at_an_existing_strings_file_for_its_own_language()
        {
            var root = LanguagesDir();
            var manifests = Directory.GetFiles(root, "language_data.xml", SearchOption.AllDirectories);
            Assert.NotEmpty(manifests);

            foreach (var manifest in manifests)
            {
                var data = XDocument.Load(manifest).Root!;
                var language = data.Attribute("id")!.Value;

                foreach (var file in data.Elements("LanguageFile"))
                {
                    // xml_path is relative to the Languages folder, not to the manifest.
                    var path = Path.Combine(root, file.Attribute("xml_path")!.Value);
                    Assert.True(File.Exists(path), $"{manifest} references missing {path}");

                    var tag = XDocument.Load(path).Root!.Element("tags")!.Element("tag")!.Attribute("language")!.Value;
                    Assert.Equal(language, tag);
                }
            }
        }

        private static HashSet<string> Variables(string text) =>
            new HashSet<string>(Variable.Matches(text).Select(m => m.Groups[1].Value));

        private static Dictionary<string, string> LoadStrings(string path) =>
            XDocument.Load(path).Root!.Element("strings")!.Elements("string")
                .ToDictionary(s => s.Attribute("id")!.Value, s => s.Attribute("text")!.Value);

        private static Dictionary<string, string> CodeStrings()
        {
            var source = Path.Combine(RepoRoot(), "src", "DiplomacyOverview");
            var strings = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file.Substring(source.Length + 1).Replace('\\', '/');
                if (relative.StartsWith("obj/") || relative.StartsWith("bin/"))
                {
                    continue;
                }

                foreach (Match match in KeyedLiteral.Matches(File.ReadAllText(file)))
                {
                    // Undo the C# escaping of the literal ("\\?" in source is "\?" at runtime).
                    strings[match.Groups[1].Value] = Regex.Unescape(match.Groups[2].Value);
                }
            }

            Assert.NotEmpty(strings);
            return strings;
        }

        private static string LanguagesDir() =>
            Path.Combine(RepoRoot(), "src", "DiplomacyOverview", "_Module", "ModuleData", "Languages");

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DiplomacyOverview.sln")))
            {
                dir = dir.Parent;
            }

            Assert.True(dir is not null, "Could not locate the repository root (DiplomacyOverview.sln).");
            return dir!.FullName;
        }
    }
}
