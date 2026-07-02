using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DiplomacyOverview.UI.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.Mixins
{
    /// <summary>
    /// Adds the "Relations" tab state to the vanilla kingdom screen VM (docs/research/04 §A).
    ///
    /// Refresh hook rationale (docs/research/10, correction #1): clicking a vanilla tab runs
    /// ExecuteShowX -> SetSelectedCategory, which only flips the per-tab Show bools and never
    /// calls RefreshValues — a RefreshValues-hooked mixin would not fire on the exact interaction
    /// that must deselect us. KingdomManagementVM.OnFrameTick is invoked by
    /// GauntletKingdomScreen.OnFrameTick every frame while the screen is open [LOCAL decompile],
    /// so hooking it runs the deselect check per frame: five bool reads when our tab is selected,
    /// one bool read otherwise. The graph itself NEVER rebuilds here — rebuilds happen only in
    /// SelectRelations, gated by the dirty flag (issue #6 acceptance: no per-frame campaign work).
    /// </summary>
    // handleDerived: TRUE is load-bearing — War Sails constructs NavalKingdomManagementVM (a
    // KingdomManagementVM subclass) and UIExtenderEx mixin lookup is exact-runtime-type keyed;
    // without it the mixin silently never attaches on DLC installs (P-22, docs/research/10 run 3).
    [ViewModelMixin("OnFrameTick", true)]
    internal sealed class KingdomManagementMixin : BaseViewModelMixin<KingdomManagementVM>
    {
        private readonly RelationsVM _relations;
        private string _relationsText;
        private bool _relationsSelected;

        public KingdomManagementMixin(KingdomManagementVM vm) : base(vm)
        {
            // Plain settable property assigned in the constructor — the exact shape the tracer
            // proved binding on this modlist (mirrors Diplomacy's own kingdom mixin).
            _relationsText = new TextObject("{=DipOvTab01}Relations").ToString();

            // Created eagerly: the panel's DataSource="{Relations}" resolves at movie load,
            // before any tab click. Contents stay empty until the first SelectRelations.
            _relations = new RelationsVM();
        }

        [DataSourceProperty]
        public string RelationsText
        {
            get => _relationsText;
            set => _relationsText = value;
        }

        /// <summary>The whole tab's VM; the injected panel node re-scopes onto it.</summary>
        [DataSourceProperty]
        public RelationsVM Relations => _relations;

        [DataSourceProperty]
        public bool RelationsSelected
        {
            get => _relationsSelected;
            set
            {
                if (value != _relationsSelected)
                {
                    _relationsSelected = value;
                    _relations.IsSelected = value; // panel visibility follows the tab button
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

            // Lazy rebuild: only on tab open, and only when dirty or never built.
            _relations.RebuildIfNeeded();

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
            if (!_relationsSelected)
            {
                return;
            }

            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            if (vm.Clan.Show || vm.Settlement.Show || vm.Policy.Show || vm.Army.Show || vm.Diplomacy.Show)
            {
                RelationsSelected = false;
            }
        }

        public override void OnFinalize()
        {
            _relations.OnFinalize();
            base.OnFinalize();
        }
    }
}
