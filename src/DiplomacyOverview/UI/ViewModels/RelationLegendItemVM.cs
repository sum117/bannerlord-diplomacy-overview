using System;
using DiplomacyOverview.Core;
using TaleWorlds.Library;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// One legend row: a color swatch + localized label for a single <see cref="RelationKind"/>,
    /// clickable to toggle that kind's edges on/off. Clicking flips <see cref="IsSelected"/> and
    /// calls back into <see cref="RelationsVM"/>, which re-filters the already-built edge list
    /// live — no campaign re-query (issue #7). A toggled-off row dims (<see cref="RowAlpha"/>) so
    /// the affordance needs no separate checkbox sprite.
    /// </summary>
    internal sealed class RelationLegendItemVM : ViewModel
    {
        private const float OnAlpha = 1.0f;
        private const float OffAlpha = 0.4f;

        private readonly Action<RelationLegendItemVM> _onToggle;
        private readonly string _label;
        private readonly string _swatchColor;
        private bool _isSelected;

        public RelationLegendItemVM(
            RelationKind kind,
            string label,
            string swatchColor,
            bool isSelected,
            Action<RelationLegendItemVM> onToggle)
        {
            Kind = kind;
            _label = label;
            _swatchColor = swatchColor;
            _isSelected = isSelected;
            _onToggle = onToggle;
        }

        /// <summary>The relation kind this row governs — not bound to XML.</summary>
        public RelationKind Kind { get; }

        [DataSourceProperty]
        public string Label => _label;

        /// <summary>"#RRGGBBAA" swatch fill, parsed widget-side via Color.ConvertStringToColor.</summary>
        [DataSourceProperty]
        public string SwatchColor => _swatchColor;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value);
                    OnPropertyChanged(nameof(RowAlpha));
                }
            }
        }

        /// <summary>Row opacity: full when the kind is shown, dimmed when toggled off.</summary>
        [DataSourceProperty]
        public float RowAlpha => _isSelected ? OnAlpha : OffAlpha;

        /// <summary>Bound to the row button's Command.Click.</summary>
        public void ExecuteToggle()
        {
            IsSelected = !IsSelected;
            _onToggle?.Invoke(this);
        }
    }
}
