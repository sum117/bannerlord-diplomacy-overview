using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// One faction medallion on the relation web: banner image-identifier for the masked-texture
    /// widget, display name, and the node box's top-left position in canvas design pixels
    /// (bound to PositionX/YOffset on the item widget — nameplate-style free positioning).
    /// Values are fixed at construction; rebuilds replace the whole VM.
    /// </summary>
    internal sealed class RelationNodeVM : ViewModel
    {
        private readonly string _name;
        private readonly ImageIdentifierVM? _banner;
        private readonly float _x;
        private readonly float _y;

        public RelationNodeVM(string nodeId, string name, ImageIdentifierVM? banner, float x, float y)
        {
            NodeId = nodeId;
            _name = name;
            _banner = banner;
            _x = x;
            _y = y;
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

        public override void OnFinalize()
        {
            base.OnFinalize();
            _banner?.OnFinalize();
        }
    }
}
