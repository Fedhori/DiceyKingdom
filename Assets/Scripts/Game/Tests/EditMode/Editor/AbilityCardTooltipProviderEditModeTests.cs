using Game.Presentation.Duel;
using Game.UI.Tooltip;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class AbilityCardTooltipProviderEditModeTests
    {
        [Test]
        public void TryBuildTooltipModel_WithoutContent_ReturnsFalse()
        {
            GameObject go = new("AbilityCardTooltipProviderTest");
            try
            {
                AbilityCardTooltipProvider provider = go.AddComponent<AbilityCardTooltipProvider>();
                bool ok = provider.TryBuildTooltipModel(out TooltipModel model);

                Assert.IsFalse(ok);
                Assert.AreEqual(default(TooltipModel), model);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryBuildTooltipModel_WithContent_ReturnsSimpleModel()
        {
            GameObject go = new("AbilityCardTooltipProviderTest");
            try
            {
                AbilityCardTooltipProvider provider = go.AddComponent<AbilityCardTooltipProvider>();
                provider.SetContent("재생력", "턴 종료 시 체력 +1");

                bool ok = provider.TryBuildTooltipModel(out TooltipModel model);

                Assert.IsTrue(ok);
                Assert.AreEqual("재생력", model.title);
                Assert.AreEqual("턴 종료 시 체력 +1", model.body);
                Assert.AreEqual(TooltipKind.Simple, model.kind);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
