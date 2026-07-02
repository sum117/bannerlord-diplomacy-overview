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
    /// the whole VM.
    /// </summary>
    internal sealed class RelationNodeVM : ViewModel
    {
        private readonly string _name;
        private readonly ImageIdentifierVM? _banner;
        private readonly float _x;
        private readonly float _y;
        private readonly float _boxWidth;
        private readonly float _boxHeight;
        private readonly float _bannerWidth;
        private readonly float _bannerHeight;
        private readonly bool _showLabel;

        public RelationNodeVM(
            string nodeId,
            string name,
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

        public override void OnFinalize()
        {
            base.OnFinalize();
            _banner?.OnFinalize();
        }
    }
}
