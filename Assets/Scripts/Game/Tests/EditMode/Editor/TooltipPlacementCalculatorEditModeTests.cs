using Game.UI.Tooltip;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class TooltipPlacementCalculatorEditModeTests
    {
        [Test]
        public void ComputeScreenTopLeft_WhenRightSideFits_PlacesOnRight()
        {
            TooltipAnchor anchor = TooltipAnchor.FromScreen(
                screenRightTop: new Vector2(100f, 300f),
                screenLeftTop: new Vector2(50f, 300f));

            Vector2 result = TooltipPlacementCalculator.ComputeScreenTopLeft(
                anchor,
                tooltipSize: new Vector2(80f, 40f),
                screenSize: new Vector2(500f, 400f),
                offset: new Vector2(16f, 0f),
                edgePadding: 8f);

            Assert.AreEqual(new Vector2(116f, 300f), result);
        }

        [Test]
        public void ComputeScreenTopLeft_WhenRightSideOverflows_FlipsToLeft()
        {
            TooltipAnchor anchor = TooltipAnchor.FromScreen(
                screenRightTop: new Vector2(470f, 200f),
                screenLeftTop: new Vector2(420f, 200f));

            Vector2 result = TooltipPlacementCalculator.ComputeScreenTopLeft(
                anchor,
                tooltipSize: new Vector2(80f, 40f),
                screenSize: new Vector2(500f, 400f),
                offset: new Vector2(16f, 0f),
                edgePadding: 8f);

            Assert.AreEqual(new Vector2(324f, 200f), result);
        }

        [Test]
        public void ComputeScreenTopLeft_WhenBottomOverflows_AdjustsY()
        {
            TooltipAnchor anchor = TooltipAnchor.FromScreen(
                screenRightTop: new Vector2(100f, 10f),
                screenLeftTop: new Vector2(50f, 10f));

            Vector2 result = TooltipPlacementCalculator.ComputeScreenTopLeft(
                anchor,
                tooltipSize: new Vector2(80f, 40f),
                screenSize: new Vector2(500f, 400f),
                offset: new Vector2(16f, 0f),
                edgePadding: 8f);

            Assert.AreEqual(new Vector2(116f, 48f), result);
        }
    }
}
