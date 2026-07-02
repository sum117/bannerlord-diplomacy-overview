using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using DiplomacyOverview.UI.Mixins;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.MountAndBlade;

namespace DiplomacyOverview
{
    /// <summary>
    /// Module entry point. Registers this assembly with UIExtenderEx, plus TRACER-ONLY probes
    /// (issue #5, runs 1-2 showed prefab patches applying but the mixin never instantiating):
    /// a reflection dump of UIExtenderEx's registries after Enable, and a diagnostic Harmony
    /// postfix on KingdomManagementVM's constructors logging whether mixins reached the instance
    /// cache. Both go to Configs\ModLogs\DiplomacyOverview-tracer.log and disappear with the tracer.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private UIExtender? _extender;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _extender = UIExtender.Create("DiplomacyOverview");
            _extender.Register(typeof(SubModule).Assembly);
            _extender.Enable();
            TracerDiag.Log("SubModule: UIExtender registered + enabled");
            DumpUieState();
            InstallCtorProbe();
        }

        /// <summary>Dump both runtimes' mixin registries + the set of ctor-patched VM types.</summary>
        private static void DumpUieState()
        {
            try
            {
                var uieAsm = typeof(UIExtender).Assembly;
                var instances = (IDictionary)typeof(UIExtender)
                    .GetField("Instances", BindingFlags.NonPublic | BindingFlags.Static)!
                    .GetValue(null)!;
                TracerDiag.Log("UIE Instances: " + string.Join(", ", instances.Keys.Cast<object>()));

                var runtimeField = typeof(UIExtender).GetField("_runtime", BindingFlags.NonPublic | BindingFlags.Instance)!;
                foreach (DictionaryEntry kv in instances)
                {
                    var runtime = runtimeField.GetValue(kv.Value);
                    if (runtime is null)
                    {
                        TracerDiag.Log("  [" + kv.Key + "] runtime = NULL (Register failed silently!)");
                        continue;
                    }

                    var vmc = runtime.GetType().GetField("ViewModelComponent")!.GetValue(runtime)!;
                    var mixins = (IDictionary)vmc.GetType().GetField("Mixins")!.GetValue(vmc)!;
                    if (mixins.Count == 0)
                    {
                        TracerDiag.Log("  [" + kv.Key + "] Mixins dict EMPTY");
                    }
                    foreach (DictionaryEntry m in mixins)
                    {
                        var list = ((IEnumerable)m.Value!).Cast<Type>().Select(t => t.FullName);
                        TracerDiag.Log("  [" + kv.Key + "] VM " + ((Type)m.Key!).FullName + " <- " + string.Join("; ", list));
                    }

                    var enabled = (IDictionary)vmc.GetType()
                        .GetField("_mixinTypeEnabled", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .GetValue(vmc)!;
                    foreach (DictionaryEntry e in enabled)
                    {
                        TracerDiag.Log("  [" + kv.Key + "] enabled[" + ((Type)e.Key!).Name + "] = " + e.Value);
                    }
                }

                var vwmp = uieAsm.GetType("Bannerlord.UIExtenderEx.Patches.ViewModelWithMixinPatch");
                if (vwmp?.GetProperty("ViewModelInitializations", BindingFlags.NonPublic | BindingFlags.Static)
                        ?.GetValue(null) is IDictionary inits)
                {
                    TracerDiag.Log("ctor-patched VM types: " +
                        string.Join(", ", inits.Keys.Cast<Type>().Select(t => t.Name)));
                }
            }
            catch (Exception ex)
            {
                TracerDiag.Log("DumpUieState FAILED: " + ex);
            }
        }

        /// <summary>Diagnostic postfix on every declared KingdomManagementVM ctor.</summary>
        private static void InstallCtorProbe()
        {
            try
            {
                var harmony = new Harmony("DiplomacyOverview.TracerProbe");
                foreach (var ctor in AccessTools.GetDeclaredConstructors(typeof(KingdomManagementVM), searchForStatic: false))
                {
                    harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(SubModule), nameof(CtorProbe)));
                }
                TracerDiag.Log("ctor probe installed");
            }
            catch (Exception ex)
            {
                TracerDiag.Log("ctor probe install FAILED: " + ex);
            }
        }

        private static void CtorProbe(object __instance)
        {
            try
            {
                TracerDiag.Log("KingdomManagementVM constructed; runtime type = " + __instance.GetType().FullName);
                var instances = (IDictionary)typeof(UIExtender)
                    .GetField("Instances", BindingFlags.NonPublic | BindingFlags.Static)!
                    .GetValue(null)!;
                var runtimeField = typeof(UIExtender).GetField("_runtime", BindingFlags.NonPublic | BindingFlags.Instance)!;
                foreach (DictionaryEntry kv in instances)
                {
                    var runtime = runtimeField.GetValue(kv.Value);
                    if (runtime is null) continue;
                    var vmc = runtime.GetType().GetField("ViewModelComponent")!.GetValue(runtime)!;
                    var cache = vmc.GetType()
                        .GetField("MixinInstanceCache", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!
                        .GetValue(vmc)!;
                    var args = new object?[] { __instance, null };
                    var hit = (bool)cache.GetType().GetMethod("TryGetValue")!.Invoke(cache, args)!;
                    var count = hit && args[1] is ICollection c ? c.Count : 0;
                    TracerDiag.Log("  mixin cache [" + kv.Key + "]: hit=" + hit + " count=" + count);
                }
            }
            catch (Exception ex)
            {
                TracerDiag.Log("CtorProbe FAILED: " + ex);
            }
        }
    }
}
