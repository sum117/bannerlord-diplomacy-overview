using Bannerlord.UIExtenderEx;
using TaleWorlds.MountAndBlade;

namespace DiplomacyOverview
{
    /// <summary>
    /// Module entry point. Does exactly one thing: registers this assembly with UIExtenderEx so
    /// the [ViewModelMixin]/[PrefabExtension] types in UI/ are discovered and enabled.
    ///
    /// S3 verdict (decompiled Bannerlord.UIExtenderEx 2.13.2, installed module DLL):
    /// UIExtender.Create(string) -> Register(Assembly) -> Enable() is the current surface;
    /// Register(Assembly) scans for attributes deriving from BaseUIExtenderAttribute.
    /// OnSubModuleLoad is the hook the shipping Diplomacy mod uses for the same call.
    ///
    /// S1 verdict (decompiled TaleWorlds.GauntletUI/PrefabSystem v1.3.15): custom Widget
    /// subclasses are auto-discovered from every loaded assembly that references
    /// TaleWorlds.GauntletUI (WidgetInfo.CollectWidgetTypes scans AppDomain assemblies;
    /// WidgetFactory.Initialize maps them by class name). Module assemblies are loaded before
    /// any OnSubModuleLoad runs (Module.LoadSubModules), so no registration call is needed
    /// for DiplomacyOverviewTracerLineWidget.
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
        }
    }
}
