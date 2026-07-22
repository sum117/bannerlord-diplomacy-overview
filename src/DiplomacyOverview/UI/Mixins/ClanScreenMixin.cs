using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DiplomacyOverview.UI.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.Mixins
{
    /// <summary>
    /// Adds the "Relations" tab state to the vanilla Clan management screen VM. Home chosen over the
    /// Kingdom screen (supersedes the P-25 overflow): the clan screen has room for a 5th tab without
    /// pushing onto the top-right portrait, and it is reachable for landless / kingdomless /
    /// mercenary clans too — so the relations web is available to every player, not only kingdom
    /// members.
    ///
    /// Deselect hook: the clan tab buttons bind Command.Click="SetSelectedCategory". Hooking that
    /// method means OnRefresh fires exactly when the player picks a vanilla tab, at which point we
    /// clear our selection — cleaner than the kingdom screen's per-frame OnFrameTick (doc 10).
    /// handleDerived TRUE guards against a DLC subclassing ClanManagementVM (P-22).
    /// </summary>
    [ViewModelMixin("SetSelectedCategory", true)]
    internal sealed class ClanScreenMixin : BaseViewModelMixin<ClanManagementVM>
    {
        private readonly RelationsVM _relations;
        private string _relationsText;
        private bool _relationsSelected;

        public ClanScreenMixin(ClanManagementVM vm) : base(vm)
        {
            _relationsText = new TextObject("{=DipOvTab01}Relations").ToString();

            // Created eagerly: the panel's DataSource="{Relations}" resolves at movie load, before
            // any tab click. Contents stay empty until the first SelectRelations.
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

            // Hide the four vanilla panels (each panel root binds IsVisible="@IsSelected" against its
            // sub-VM) and deselect their tab buttons (which bind IsSelected to the IsXSelected flags).
            vm.ClanMembers.IsSelected = false;
            vm.ClanParties.IsSelected = false;
            vm.ClanFiefs.IsSelected = false;
            vm.ClanIncome.IsSelected = false;
            vm.IsMembersSelected = false;
            vm.IsPartiesSelected = false;
            vm.IsFiefsSelected = false;
            vm.IsIncomeSelected = false;
            RelationsSelected = true;
        }

        /// <summary>
        /// Runs after every hooked SetSelectedCategory. That method only ever selects a vanilla
        /// category (0-3), so if it ran the player picked a vanilla tab — our tab loses selection
        /// and the panel hides.
        /// </summary>
        public override void OnRefresh()
        {
            if (_relationsSelected)
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
