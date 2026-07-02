using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace DiplomacyOverview.UI.Widgets
{
    /// <summary>
    /// Technique B of the sacrificial tracer (issue #5, docs/research/04 §C technique 1):
    /// draws one hardcoded, tinted, angled line in OnRender via CreateSimpleMaterial +
    /// DrawSprite with a Rectangle2D whose LocalScale = (length, thickness) and
    /// LocalRotation = angle in degrees (decompiled TwoDimensionDrawContext/Rectangle2D,
    /// v1.3.15). All coordinates are widget-local design pixels, multiplied by the widget's
    /// UI scale (_scaleToUse) exactly like the vanilla Widget.OnRender does.
    ///
    /// Diagnostics baked in so ONE screenshot answers everything:
    ///   - green line from A=(40,60) to B=(360,240)  -> ~29.4 degrees; proves rotation+tint
    ///   - two gold 10x10 endpoint markers at A and B -> if the line connects them, angle
    ///     sign, pivot math and coordinate space are all correct; if the line mirrors away
    ///     from the markers, the rotation sign convention is inverted (fix: negate angle)
    ///   - thin gray border around the widget bounds  -> proves widget placement and size
    ///
    /// Class name is the XML element name (global namespace, hence the prefix, P-11).
    /// Auto-discovery contract (S1): public class, public (UIContext) constructor, assembly
    /// references TaleWorlds.GauntletUI.
    /// </summary>
    public class DiplomacyOverviewTracerLineWidget : Widget
    {
        private const string SpriteName = "BlankWhiteSquare_9";

        private static readonly Vector2 PointA = new Vector2(40f, 60f);
        private static readonly Vector2 PointB = new Vector2(360f, 240f);

        // Alliance green, tracer gold, subtle gray (docs/research/04 §F palette).
        private static readonly Color LineColor = Color.ConvertStringToColor("#4E9B47FF");
        private static readonly Color MarkerColor = Color.ConvertStringToColor("#D4A017FF");
        private static readonly Color BorderColor = Color.ConvertStringToColor("#FFFFFF66");

        public DiplomacyOverviewTracerLineWidget(UIContext context) : base(context)
        {
        }

        /// <summary>Line thickness in design pixels; settable from prefab XML.</summary>
        public float LineThickness { get; set; } = 6f;

        protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
        {
            base.OnRender(twoDimensionContext, drawContext);

            Sprite sprite = Context.SpriteData.GetSprite(SpriteName);
            if (sprite?.Texture is null)
            {
                // Finding-relevant failure mode: sprite category not loaded in this screen.
                return;
            }

            float s = _scaleToUse;

            // Border: widget bounds in already-scaled pixels (Size), drawn as four thin lines.
            var size = Size;
            DrawLine(drawContext, sprite, new Vector2(0f, 0f), new Vector2(size.X, 0f), 2f, BorderColor);
            DrawLine(drawContext, sprite, new Vector2(size.X, 0f), new Vector2(size.X, size.Y), 2f, BorderColor);
            DrawLine(drawContext, sprite, new Vector2(size.X, size.Y), new Vector2(0f, size.Y), 2f, BorderColor);
            DrawLine(drawContext, sprite, new Vector2(0f, size.Y), new Vector2(0f, 0f), 2f, BorderColor);

            // Endpoint markers first (under the line), then the line itself.
            DrawMarker(drawContext, sprite, PointA * s, 10f * s);
            DrawMarker(drawContext, sprite, PointB * s, 10f * s);
            DrawLine(drawContext, sprite, PointA * s, PointB * s, Math.Max(1f, LineThickness * s), LineColor);
        }

        /// <summary>
        /// One rotated stretched sprite per line: a Rectangle2D positioned so its rotation
        /// pivot sits exactly on <paramref name="from"/>, scaled to (length, thickness) and
        /// rotated by atan2 degrees. CalculateMatrixFrame(in AreaRect) parents the rect to
        /// this widget, converting widget-local coordinates to screen space.
        /// </summary>
        private void DrawLine(TwoDimensionDrawContext drawContext, Sprite sprite, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.Length();
            if (length < 1E-3f)
            {
                return;
            }

            float angleDegrees = (float)(Math.Atan2(delta.Y, delta.X) * (180.0 / Math.PI));

            Rectangle2D rect = Rectangle2D.Create();
            rect.LocalPosition = new Vector2(from.X, from.Y - thickness * 0.5f);
            rect.LocalPivot = new Vector2(0f, 0.5f); // pivot = (from.X, from.Y): rotate about the start point
            rect.LocalScale = new Vector2(length, thickness);
            rect.LocalRotation = angleDegrees;
            rect.CalculateMatrixFrame(in AreaRect);

            drawContext.DrawSprite(sprite, CreateTintedMaterial(drawContext, sprite, color), in rect, _scaleToUse);
        }

        private void DrawMarker(TwoDimensionDrawContext drawContext, Sprite sprite, Vector2 center, float side)
        {
            Rectangle2D rect = Rectangle2D.Create();
            rect.LocalPosition = new Vector2(center.X - side * 0.5f, center.Y - side * 0.5f);
            rect.LocalScale = new Vector2(side, side);
            rect.CalculateMatrixFrame(in AreaRect);

            drawContext.DrawSprite(sprite, CreateTintedMaterial(drawContext, sprite, MarkerColor), in rect, _scaleToUse);
        }

        /// <summary>Material setup mirrors vanilla Widget.OnRender (decompiled v1.3.15).</summary>
        private SimpleMaterial CreateTintedMaterial(TwoDimensionDrawContext drawContext, Sprite sprite, Color color)
        {
            SimpleMaterial material = drawContext.CreateSimpleMaterial();
            material.OverlayEnabled = false;
            material.CircularMaskingEnabled = false;
            material.Texture = sprite.Texture;
            material.NinePatchParameters = sprite.NinePatchParameters;
            material.Color = color;
            material.ColorFactor = 1f;
            material.AlphaFactor = 1f * Context.ContextAlpha;
            material.HueFactor = 0f;
            material.SaturationFactor = 0f;
            material.ValueFactor = 0f;
            return material;
        }
    }
}
