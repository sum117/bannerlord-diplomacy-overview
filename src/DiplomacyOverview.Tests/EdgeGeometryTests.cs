using System;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class EdgeGeometryTests
    {
        [Fact]
        public void Compute_Horizontal_ZeroAngle_LengthAndCenterCorrect()
        {
            var s = EdgeGeometry.Compute(100, 200, 300, 200);

            Assert.Equal(200.0, s.Length, 6);
            Assert.Equal(0.0, s.AngleRadians, 6);
            Assert.Equal(200.0, s.CenterX, 6);
            Assert.Equal(200.0, s.CenterY, 6);
        }

        [Fact]
        public void Compute_Vertical_QuarterTurn()
        {
            var s = EdgeGeometry.Compute(50, 0, 50, 80);

            Assert.Equal(80.0, s.Length, 6);
            Assert.Equal(Math.PI / 2.0, s.AngleRadians, 6);
        }

        [Fact]
        public void Compute_Diagonal_45Degrees()
        {
            var s = EdgeGeometry.Compute(0, 0, 10, 10);

            Assert.Equal(Math.Sqrt(200.0), s.Length, 6);
            Assert.Equal(Math.PI / 4.0, s.AngleRadians, 6);
        }

        [Fact]
        public void Compute_CenterIsMidpoint_RegardlessOfDirection()
        {
            var forward = EdgeGeometry.Compute(10, 20, 90, 120);
            var reversed = EdgeGeometry.Compute(90, 120, 10, 20);

            Assert.Equal(forward.CenterX, reversed.CenterX, 6);
            Assert.Equal(forward.CenterY, reversed.CenterY, 6);
            Assert.Equal(50.0, forward.CenterX, 6);
            Assert.Equal(70.0, forward.CenterY, 6);
        }

        [Fact]
        public void TopLeft_CentersStripOnSegmentMidpoint()
        {
            var s = EdgeGeometry.Compute(100, 100, 300, 100);
            const double thickness = 20.0;

            // Unrotated strip: top-left + half-extents must land on the midpoint.
            Assert.Equal(s.CenterX, s.TopLeftX(thickness) + s.Length / 2.0, 6);
            Assert.Equal(s.CenterY, s.TopLeftY(thickness) + thickness / 2.0, 6);
        }
    }
}
