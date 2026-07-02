using System;
using System.IO;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.Mixins
{
    /// <summary>
    /// TRACER RUN 2 shape: exact mirror of the one mixin PROVEN to bind on this machine —
    /// Diplomacy 1.3.13's KingdomManagementVMMixin ([ViewModelMixin] with NO refresh argument,
    /// text as a plain settable property assigned in the constructor). Run 1 showed our prefab
    /// nodes applied (UIExtenderEx DumpXML evidence) but the tab button rendered without its
    /// mixin-bound text; the run-1 mixin differed from Diplomacy's in exactly three ways
    /// (refresh hook "OnFrameTick", getter-only expression property, no {..} re-scope in XML),
    /// so run 2 eliminates all three at once and instruments the seams.
    ///
    /// Deliberate regression, shared with Diplomacy/BannerKings: without a refresh hook there is
    /// no deselect-on-vanilla-tab-click callback; a stale selected state is masked by panel
    /// z-order (our panel renders under the vanilla panels). The production design will solve
    /// deselection properly; the tracer only needs the binding proven.
    /// </summary>
    [ViewModelMixin]
    internal sealed class KingdomManagementMixin : BaseViewModelMixin<KingdomManagementVM>
    {
        private string _relationsText;
        private bool _relationsSelected;
        private bool _readLogged;

        public KingdomManagementMixin(KingdomManagementVM vm) : base(vm)
        {
            _relationsText = new TextObject("{=DipOvTab01}Relations").ToString();
            TracerDiag.Log("mixin attached; vm runtime type = " + vm.GetType().FullName);
        }

        [DataSourceProperty]
        public string RelationsText
        {
            get
            {
                if (!_readLogged)
                {
                    _readLogged = true;
                    TracerDiag.Log("RelationsText read by binder");
                }
                return _relationsText;
            }
            set => _relationsText = value;
        }

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
            TracerDiag.Log("SelectRelations invoked");
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            vm.Clan.Show = false;
            vm.Settlement.Show = false;
            vm.Policy.Show = false;
            vm.Army.Show = false;
            vm.Diplomacy.Show = false;
            RelationsSelected = true;
        }
    }

    /// <summary>
    /// Throwaway tracer instrumentation: appends to a plain-text log next to the other mod logs
    /// so one game run answers "did the mixin attach / did the binder read the property / did the
    /// click reach us" even when pixels are ambiguous. Never throws.
    /// </summary>
    internal static class TracerDiag
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "DiplomacyOverview-tracer.log");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + message + "\r\n");
            }
            catch
            {
                // diagnostics must never hurt the game
            }
        }
    }
}
