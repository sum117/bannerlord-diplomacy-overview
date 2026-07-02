extern alias game;

using System;
using Vector2 = game::System.Numerics.Vector2;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace DiplomacyOverview.UI.Widgets
{
    /// <summary>
    /// Draws one relation line: a single rotated, tinted sprite between (X1,Y1) and (X2,Y2) —
    /// the exact OnRender + Rectangle2D.LocalRotation + DrawSprite technique the issue-#5 tracer
    /// proved in-game (docs/research/10, S2 run 5). One widget instance per edge, instantiated by
    /// the edges container's ItemTemplate and stretched over the whole canvas, so the bound
    /// coordinates are canvas design pixels; UI scale is applied at render time via _scaleToUse,
    /// exactly like vanilla Widget.OnRender.
    ///
    /// Auto-discovery contract (S1): public class, public (UIContext) constructor, assembly
    /// references TaleWorlds.GauntletUI. Class name is the XML element name — globally namespaced,
    /// hence the DiplomacyOverview prefix (P-11).
    ///
    /// Vector2 note (P-23): game UI signatures use System.Numerics.Vectors 4.1.3.0; the extern
    /// alias binds to the game's identity, never the framework facade.
    /// </summary>
    public class DiplomacyOverviewEdgeWidget : Widget
    {
        private const string SpriteName = "BlankWhiteSquare_9";

        private string _lineColorText = RelationPaletteDefaultColorText;
        private Color _lineColor = new Color(1f, 1f, 1f);
        private bool _renderBroken;
        private bool _loggedFirstRender;

        // Neutral white; the VM always binds a real palette color over it.
        private const string RelationPaletteDefaultColorText = "#FFFFFFFF";

        public DiplomacyOverviewEdgeWidget(UIContext context) : base(context)
        {
        }

        /// <summary>Segment start X, canvas design px (bound from RelationEdgeVM).</summary>
        public float X1 { get; set; }

        /// <summary>Segment start Y, canvas design px.</summary>
        public float Y1 { get; set; }

        /// <summary>Segment end X, canvas design px.</summary>
        public float X2 { get; set; }

        /// <summary>Segment end Y, canvas design px.</summary>
        public float Y2 { get; set; }

        /// <summary>Line thickness in design px.</summary>
        public float LineThickness { get; set; } = 4f;

        /// <summary>"#RRGGBBAA". Unparseable values fall back to the previous color.</summary>
        public string LineColor
        {
            get => _lineColorText;
            set
            {
                if (string.Equals(value, _lineColorText, StringComparison.Ordinal))
                {
                    return;
                }

                _lineColorText = value;
                try
                {
                    _lineColor = Color.ConvertStringToColor(value);
                }
                catch
                {
                    // Keep drawing with the last good color; a bad string must not break rendering.
                }
            }
        }

        protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
        {
            base.OnRender(twoDimensionContext, drawContext);
            if (_renderBroken)
            {
                return;
            }

            // AGENTS.md rule 6: worst failure mode is "lines missing", never a crash. The drawing
            // body lives in a SEPARATE method on purpose: JIT-time signature failures
            // (MissingMethodException, the P-23 crash class) are thrown when the method containing
            // the bad token is first compiled — keeping those tokens out of OnRender itself makes
            // them catchable here instead of detonating in the game's render loop.
            try
            {
                RenderEdge(drawContext);
            }
            catch (Exception ex)
            {
                _renderBroken = true; // self-disable: never retry a broken render path
                try
                {
                    Debug.Print("DiplomacyOverview: edge render failed, widget self-disabled: " + ex);
                }
                catch
                {
                    // Diagnostics must never hurt the game.
                }
            }
        }

        private void RenderEdge(TwoDimensionDrawContext drawContext)
        {
            // One breadcrumb per widget lifetime: proves the widget instantiated AND shows the
            // values the bindings delivered — an all-zero line here means "bindings never
            // applied", no log at all means "no edge items existed". Both render as the same
            // blank screen, which is exactly why this line exists.
            if (!_loggedFirstRender)
            {
                _loggedFirstRender = true;
                Diagnostics.Note(
                    "edge widget rendering: (" + X1 + "," + Y1 + ")->(" + X2 + "," + Y2
                    + ") thickness " + LineThickness + " color " + _lineColorText);
            }

            Sprite sprite = Context.SpriteData.GetSprite(SpriteName);
            if (sprite?.Texture is null)
            {
                Diagnostics.Note("edge sprite '" + SpriteName + "' unavailable — lines skipped.");
                return; // sprite category not loaded in this screen — nothing to draw
            }

            float s = _scaleToUse;
            DrawLine(
                drawContext,
                sprite,
                new Vector2(X1 * s, Y1 * s),
                new Vector2(X2 * s, Y2 * s),
                Math.Max(1f, LineThickness * s),
                _lineColor);
        }

        /// <summary>
        /// One rotated stretched sprite per line: a Rectangle2D positioned so its rotation
        /// pivot sits exactly on <paramref name="from"/>, scaled to (length, thickness) and
        /// rotated by atan2 degrees. CalculateMatrixFrame(in AreaRect) parents the rect to
        /// this widget, converting widget-local coordinates to screen space.
        /// (Verbatim from the tracer widget, in-game-proven — docs/research/10 run 5.)
        /// </summary>
        private void DrawLine(TwoDimensionDrawContext drawContext, Sprite sprite, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.Length();
            if (length < 1E-3f)
            {
                return; // degenerate segment (e.g. midpoint-clamped overlap) — nothing to draw
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

        /// <summary>Material setup mirrors vanilla Widget.OnRender (decompiled v1.3.15). Materials
        /// are pooled by the draw context — created per draw, never cached across frames.</summary>
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
