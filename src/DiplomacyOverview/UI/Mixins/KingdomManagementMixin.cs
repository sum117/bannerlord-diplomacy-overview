using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.Mixins
{
    /// <summary>
    /// Adds the "Relations" tab state to the vanilla kingdom screen VM (docs/research/04 §A,
    /// BannerKings KingdomManagementMixin exemplar).
    ///
    /// DELIBERATE DEVIATION from docs 04 §A: the mixin hooks <c>OnFrameTick</c>, not
    /// <c>RefreshValues</c>. Decompile evidence (KingdomManagementVM v1.3.15): clicking a vanilla
    /// tab runs ExecuteShowX -> SetSelectedCategory, which only flips the per-tab Show bools and
    /// never calls RefreshValues — so a RefreshValues-hooked OnRefresh would NOT fire on vanilla
    /// tab clicks and our tab/panel would stay selected (stale highlight + panel overlap).
    /// GauntletKingdomScreen.OnFrameTick calls DataSource.OnFrameTick() every frame while the
    /// screen is open, so hooking it makes the deselect check run each frame. The check is five
    /// bool reads — negligible — but UIExtenderEx resolves mixin attributes reflectively per
    /// invocation, so the final (non-tracer) design should revisit this (see
    /// docs/research/10-tracer-findings.md).
    /// </summary>
    [ViewModelMixin("OnFrameTick")]
    internal sealed class KingdomManagementMixin : BaseViewModelMixin<KingdomManagementVM>
    {
        private bool _relationsSelected;

        public KingdomManagementMixin(KingdomManagementVM vm) : base(vm)
        {
        }

        [DataSourceProperty]
        public string RelationsText => new TextObject("{=DipOvTab01}Relations").ToString();

        [DataSourceProperty]
        public bool RelationsSelected
        {
            get => _relationsSelected;
            set
            {
                if (value != _relationsSelected)
                {
                    _relationsSelected = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceMethod]
        public void SelectRelations()
        {
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            // Hide the five vanilla panels; their tab buttons follow automatically because
            // KingdomTabControlListPanel.OnLateUpdate mirrors panel visibility into button
            // IsSelected every frame (decompiled v1.3.15).
            vm.Clan.Show = false;
            vm.Settlement.Show = false;
            vm.Policy.Show = false;
            vm.Army.Show = false;
            vm.Diplomacy.Show = false;
            RelationsSelected = true;
        }

        /// <summary>
        /// Runs after every hooked KingdomManagementVM.OnFrameTick. If the player activated any
        /// vanilla tab (its Show flag is set), our tab loses selection and the panel hides.
        /// </summary>
        public override void OnRefresh()
        {
            var vm = ViewModel;
            if (vm is null || !_relationsSelected)
            {
                return;
            }

            if (vm.Clan.Show || vm.Settlement.Show || vm.Policy.Show || vm.Army.Show || vm.Diplomacy.Show)
            {
                RelationsSelected = false;
            }
        }
    }
}
