using Bannerlord.UIExtenderEx;
using DiplomacyOverview.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DiplomacyOverview
{
    /// <summary>
    /// Module entry point. Two responsibilities only: register this assembly with UIExtenderEx so
    /// the [ViewModelMixin]/[PrefabExtension] types in UI/ are discovered and enabled, and attach
    /// the dirty-flag campaign behavior when a campaign starts (same OnGameStart +
    /// CampaignGameStarter pattern the shipping Diplomacy mod uses).
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

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            try
            {
                if (gameStarterObject is CampaignGameStarter campaignStarter)
                {
                    campaignStarter.AddBehavior(new RelationsDirtyBehavior());
                }
            }
            catch
            {
                // AGENTS.md rule 6: failing to register the behavior only costs rebuild laziness
                // (the dirty flag stays permanently true) — never take the game down for it.
            }
        }
    }
}
