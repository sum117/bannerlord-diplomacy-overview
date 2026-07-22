using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// One faction medallion on the relation web: banner image-identifier for the masked-texture
    /// widget, display name, the node box's top-left position, and the density-scaled box/banner
    /// dimensions (WebDensity) — all in canvas design pixels, bound onto the item widgets
    /// (PositionX/YOffset + SuggestedWidth/Height; nameplate-style free positioning). Labels
    /// switch off wholesale on dense worlds. Values are fixed at construction; rebuilds replace
    /// the whole VM. Clicking the node box opens the faction's encyclopedia page (#10).
    /// </summary>
    internal sealed class RelationNodeVM : ViewModel
    {
        private readonly string _name;
        private readonly string? _encyclopediaLink;
        private readonly ImageIdentifierVM? _banner;
        private readonly float _x;
        private readonly float _y;
        private readonly float _boxWidth;
        private readonly float _boxHeight;
        private readonly float _bannerWidth;
        private readonly float _bannerHeight;
        private readonly bool _showLabel;
        private bool _isHovered;

        public RelationNodeVM(
            string nodeId,
            string name,
            string? encyclopediaLink,
            ImageIdentifierVM? banner,
            float x,
            float y,
            float boxWidth,
            float boxHeight,
            float bannerWidth,
            float bannerHeight,
            bool showLabel)
        {
            NodeId = nodeId;
            _name = name;
            _encyclopediaLink = encyclopediaLink;
            _banner = banner;
            _x = x;
            _y = y;
            _boxWidth = boxWidth;
            _boxHeight = boxHeight;
            _bannerWidth = bannerWidth;
            _bannerHeight = bannerHeight;
            _showLabel = showLabel;
        }

        /// <summary>Faction StringId — graph identity, not bound to XML.</summary>
        public string NodeId { get; }

        [DataSourceProperty]
        public string Name => _name;

        [DataSourceProperty]
        public ImageIdentifierVM? Banner => _banner;

        /// <summary>Node box left edge, canvas design px.</summary>
        [DataSourceProperty]
        public float X => _x;

        /// <summary>Node box top edge, canvas design px.</summary>
        [DataSourceProperty]
        public float Y => _y;

        [DataSourceProperty]
        public float BoxWidth => _boxWidth;

        [DataSourceProperty]
        public float BoxHeight => _boxHeight;

        [DataSourceProperty]
        public float BannerWidth => _bannerWidth;

        [DataSourceProperty]
        public float BannerHeight => _bannerHeight;

        /// <summary>False on dense worlds — names collide faster than they inform (WebDensity).</summary>
        [DataSourceProperty]
        public bool ShowLabel => _showLabel;

        /// <summary>Hover state, driven by the node button's Command.HoverBegin/End. The hover-glow
        /// overlay binds its IsVisible to this — explicit, not brush-state propagation (#10).</summary>
        [DataSourceProperty]
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (value != _isHovered)
                {
                    _isHovered = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void ExecuteHoverBegin() => IsHovered = true;

        public void ExecuteHoverEnd() => IsHovered = false;

        /// <summary>
        /// Opens this faction's encyclopedia page over the kingdom screen — the vanilla link
        /// pattern (HeroVM.ExecuteLink, decompiled v1.4.7). Bound to the node box's Command.Click.
        /// </summary>
        public void ExecuteOpenEncyclopedia()
        {
            try
            {
                if (string.IsNullOrEmpty(_encyclopediaLink) || Campaign.Current is null) // P-07
                {
                    return;
                }

                Campaign.Current.EncyclopediaManager.GoToLink(_encyclopediaLink);
            }
            catch
            {
                // Rule 6: a dead link costs the click, never the screen.
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _banner?.OnFinalize();
        }
    }
}
