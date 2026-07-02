using Bannerlord.UIExtenderEx;
using TaleWorlds.MountAndBlade;

namespace DiplomacyOverview
{
    /// <summary>
    /// Module entry point. Does exactly one thing: registers this assembly with UIExtenderEx so
    /// the [ViewModelMixin]/[PrefabExtension] types in UI/ are discovered and enabled.
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
