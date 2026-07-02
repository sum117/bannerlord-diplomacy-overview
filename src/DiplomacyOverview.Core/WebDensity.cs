using System;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// Adaptive sizing for the relation web: given how many nodes share the circle, decides the
    /// circle radius, a uniform node scale, whether name labels fit, and the derived node-box /
    /// banner / trim / edge dimensions. Pure and deterministic; all values are canvas design px.
    ///
    /// Motivation (docs/research/10 run 7): real modded campaigns legitimately hold 80+ living
    /// kingdoms (a Separatism shattered world), where vanilla-sized medallions fuse into a solid
    /// ring and the medallion-radius edge trim annihilates every short-range war line (P-24
    /// corollary: never size UI to vanilla counts). Above the label-crowding threshold the web
    /// degrades the way the client's atWar reference does: small banner chips, no names.
    /// </summary>
    public readonly struct WebDensity
    {
        // Base (scale = 1) geometry — mirrored by DiplomacyOverviewRelationsPanel.xml's layout.
        public const double BaseCircleRadius = 310.0;
        public const double BaseNodeBoxWidth = 170.0;
        public const double BaseNodeBoxHeight = 128.0;
        public const double BaseBannerWidth = 75.0;
        public const double BaseBannerHeight = 86.0;
        public const double BaseEdgeThickness = 4.0;

        private const double CanvasMargin = 16.0;
        private const double BoxGap = 8.0;
        private const double BannerGap = 3.0;
        private const double DenseBoxPad = 6.0;
        private const double TrimPad = 3.0;
        private const double MinScale = 0.22;
        private const double MinEdgeThickness = 2.5;

        /// <summary>
        /// Labels stay on while the arc per node is at least this fraction of a full node box —
        /// below it adjacent name labels collide faster than they inform.
        /// </summary>
        private const double LabelCrowdingTolerance = 0.55;

        public double CircleRadius { get; }
        public double NodeScale { get; }
        public bool ShowLabels { get; }
        public double NodeBoxWidth { get; }
        public double NodeBoxHeight { get; }
        public double BannerWidth { get; }
        public double BannerHeight { get; }

        /// <summary>Banner-medallion center inside the node box (banner is top-centered).</summary>
        public double NodeCenterOffsetX { get; }
        public double NodeCenterOffsetY { get; }

        /// <summary>Edge trim radius: half the larger banner dimension plus a small pad — at
        /// scale 1 this reproduces the original hand-tuned 46 px exactly.</summary>
        public double TrimRadius { get; }

        public double EdgeThickness { get; }

        private WebDensity(double circleRadius, double nodeScale, bool showLabels)
        {
            CircleRadius = circleRadius;
            NodeScale = nodeScale;
            ShowLabels = showLabels;

            BannerWidth = BaseBannerWidth * nodeScale;
            BannerHeight = BaseBannerHeight * nodeScale;

            // With labels the box carries the name underneath; without, it hugs the banner.
            NodeBoxWidth = showLabels ? BaseNodeBoxWidth * nodeScale : BannerWidth + DenseBoxPad;
            NodeBoxHeight = showLabels ? BaseNodeBoxHeight * nodeScale : BannerHeight + DenseBoxPad;

            // Banner is horizontally centered and top-aligned in the box in both modes.
            NodeCenterOffsetX = NodeBoxWidth / 2.0;
            NodeCenterOffsetY = BannerHeight / 2.0;

            TrimRadius = Math.Max(BannerWidth, BannerHeight) / 2.0 + TrimPad;
            EdgeThickness = Math.Max(MinEdgeThickness, BaseEdgeThickness * nodeScale);
        }

        public static WebDensity Compute(int nodeCount, double canvasWidth, double canvasHeight)
        {
            if (canvasWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(canvasWidth), canvasWidth, "Canvas width must be positive.");
            }

            if (canvasHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(canvasHeight), canvasHeight, "Canvas height must be positive.");
            }

            var halfW = canvasWidth / 2.0;
            var halfH = canvasHeight / 2.0;

            // Largest radius a full-size labeled node box allows (box center rides the circle).
            var maxLabeledRadius = Math.Min(halfH - BaseNodeBoxHeight / 2.0, halfW - BaseNodeBoxWidth / 2.0) - CanvasMargin;
            if (maxLabeledRadius <= 0)
            {
                // Canvas smaller than one node — degenerate but defined.
                return new WebDensity(0.0, MinScale, showLabels: false);
            }

            if (nodeCount <= 1)
            {
                return new WebDensity(Math.Min(BaseCircleRadius, maxLabeledRadius), 1.0, showLabels: true);
            }

            var fullArc = BaseNodeBoxWidth + BoxGap;
            var labeledArc = 2.0 * Math.PI * maxLabeledRadius / nodeCount;
            if (labeledArc >= fullArc * LabelCrowdingTolerance)
            {
                // Labeled mode: grow the radius toward the cap as the strip crowds.
                var neededRadius = nodeCount * fullArc / (2.0 * Math.PI);
                var radius = Math.Min(maxLabeledRadius, Math.Max(BaseCircleRadius, neededRadius));
                return new WebDensity(radius, 1.0, showLabels: true);
            }

            // Dense mode: solve the scale at which n banner chips (plus gap) exactly fill the
            // largest circle that still fits them: n(bW·s + g) = 2π(halfDim − m − bH·s/2), taking
            // the tighter of the height/width budgets.
            var scaleByHeight = (2.0 * Math.PI * (halfH - CanvasMargin) - nodeCount * BannerGap)
                                / (nodeCount * BaseBannerWidth + Math.PI * BaseBannerHeight);
            var scaleByWidth = (2.0 * Math.PI * (halfW - CanvasMargin) - nodeCount * BannerGap)
                               / (nodeCount * BaseBannerWidth + Math.PI * BaseBannerWidth);
            var scale = Math.Min(scaleByHeight, scaleByWidth);
            scale = Math.Min(1.0, Math.Max(MinScale, scale));

            var denseRadius = Math.Min(
                halfH - BaseBannerHeight * scale / 2.0,
                halfW - BaseBannerWidth * scale / 2.0) - CanvasMargin;
            return new WebDensity(Math.Max(0.0, denseRadius), scale, showLabels: false);
        }
    }
}
