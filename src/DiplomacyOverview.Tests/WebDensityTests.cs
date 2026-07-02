using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class WebDensityTests
    {
        private const double CanvasW = 1400.0;
        private const double CanvasH = 800.0;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(10)]
        public void SmallWorlds_KeepBaseGeometryAndLabels(int nodeCount)
        {
            var d = WebDensity.Compute(nodeCount, CanvasW, CanvasH);

            Assert.True(d.ShowLabels);
            Assert.Equal(1.0, d.NodeScale, 6);
            Assert.Equal(WebDensity.BaseCircleRadius, d.CircleRadius, 6);
            Assert.Equal(WebDensity.BaseNodeBoxWidth, d.NodeBoxWidth, 6);
            Assert.Equal(WebDensity.BaseNodeBoxHeight, d.NodeBoxHeight, 6);
            // Scale-1 derived values reproduce the original hand-tuned #6 geometry.
            Assert.Equal(46.0, d.TrimRadius, 6);
            Assert.Equal(85.0, d.NodeCenterOffsetX, 6);
            Assert.Equal(43.0, d.NodeCenterOffsetY, 6);
            Assert.Equal(WebDensity.BaseEdgeThickness, d.EdgeThickness, 6);
        }

        [Fact]
        public void CrowdedLabeledWorld_GrowsRadiusButKeepsLabels()
        {
            // ~Realm of Thrones scale: more kingdoms than vanilla, labels still worth having.
            var d = WebDensity.Compute(16, CanvasW, CanvasH);

            Assert.True(d.ShowLabels);
            Assert.Equal(1.0, d.NodeScale, 6);
            Assert.True(d.CircleRadius >= WebDensity.BaseCircleRadius);
            // Never past the fit cap: a full box on the circle stays inside the canvas.
            Assert.True(d.CircleRadius + WebDensity.BaseNodeBoxHeight / 2.0 <= CanvasH / 2.0);
        }

        [Fact]
        public void DenseWorld_ShrinksNodesDropsLabelsAndFitsTheRing()
        {
            // The reference shattered campaign: 82 living kingdoms (docs/research/10 run 7).
            var d = WebDensity.Compute(82, CanvasW, CanvasH);

            Assert.False(d.ShowLabels);
            Assert.InRange(d.NodeScale, 0.25, 0.45);
            Assert.InRange(d.CircleRadius, 330.0, 390.0);

            // The chips actually fit around the circle (allowing sub-pixel slack).
            var arc = 2.0 * System.Math.PI * d.CircleRadius / 82.0;
            Assert.True(arc + 0.01 >= d.BannerWidth, $"arc {arc} vs banner {d.BannerWidth}");

            // Trim shrank with the medallions, so mid-range wars stop degenerating.
            Assert.True(d.TrimRadius < 20.0);
            Assert.True(d.EdgeThickness >= 2.5);

            // The ring itself stays on the canvas.
            Assert.True(d.CircleRadius + d.BannerHeight / 2.0 <= CanvasH / 2.0);
        }

        [Fact]
        public void ExtremeWorld_ClampsToMinimumScale()
        {
            var d = WebDensity.Compute(400, CanvasW, CanvasH);

            Assert.False(d.ShowLabels);
            Assert.Equal(0.22, d.NodeScale, 6);
            Assert.True(d.CircleRadius > 0);
        }

        [Fact]
        public void NodeScale_NeverGrowsWithNodeCount()
        {
            var previous = double.MaxValue;
            for (var n = 1; n <= 200; n += 7)
            {
                var d = WebDensity.Compute(n, CanvasW, CanvasH);
                Assert.True(d.NodeScale <= previous + 1e-9, $"scale grew at n={n}");
                previous = d.NodeScale;
            }
        }

        [Fact]
        public void DenseBox_HugsTheBanner()
        {
            var d = WebDensity.Compute(82, CanvasW, CanvasH);

            Assert.True(d.NodeBoxWidth < WebDensity.BaseNodeBoxWidth * d.NodeScale + 10.0);
            Assert.Equal(d.NodeBoxWidth / 2.0, d.NodeCenterOffsetX, 6);
            Assert.Equal(d.BannerHeight / 2.0, d.NodeCenterOffsetY, 6);
        }
    }
}
