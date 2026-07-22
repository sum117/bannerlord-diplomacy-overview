using System;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// Pure geometry for an edge's hover hit-strip (issue #10): given the two endpoints in canvas
    /// design pixels, returns the placement of a thin rectangle centred on the segment and rotated
    /// to lie along it. A stretched-canvas edge draws its line in OnRender and can't be hovered
    /// per-line; a sibling widget positioned by this result gives it a line-shaped hover region.
    ///
    /// <see cref="HitStrip.AngleRadians"/> is radians — the unit of Gauntlet's
    /// <c>Widget.Rotation</c> (assigned straight to <c>AreaRect.LocalRotation</c>, which is what
    /// hit-testing uses). The strip is positioned top-left with a centre pivot (PivotX/Y = 0.5) so
    /// the rotation turns it about the segment midpoint.
    /// </summary>
    public static class EdgeGeometry
    {
        public readonly struct HitStrip
        {
            public HitStrip(double centerX, double centerY, double length, double angleRadians)
            {
                CenterX = centerX;
                CenterY = centerY;
                Length = length;
                AngleRadians = angleRadians;
            }

            /// <summary>Segment midpoint X (design px) — the strip's rotation centre.</summary>
            public double CenterX { get; }

            /// <summary>Segment midpoint Y (design px) — the strip's rotation centre.</summary>
            public double CenterY { get; }

            /// <summary>Segment length (design px) — the strip's width.</summary>
            public double Length { get; }

            /// <summary>Angle of the segment from the +X axis, radians in [-π, π].</summary>
            public double AngleRadians { get; }

            /// <summary>Top-left X for a strip of the given hover thickness, centred on the segment.</summary>
            public double TopLeftX(double thickness) => CenterX - Length / 2.0;

            /// <summary>Top-left Y for a strip of the given hover thickness, centred on the segment.</summary>
            public double TopLeftY(double thickness) => CenterY - thickness / 2.0;
        }

        public static HitStrip Compute(double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var length = Math.Sqrt(dx * dx + dy * dy);
            var angle = Math.Atan2(dy, dx);
            return new HitStrip((x1 + x2) / 2.0, (y1 + y2) / 2.0, length, angle);
        }
    }
}
