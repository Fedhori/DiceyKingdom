using Game.Presentation.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public sealed class UnityLocalizedTextResolverEditModeTests
    {
        const string missingNameKey = "ability.__test_missing_name__.name";
        const string missingDescKey = "ability.__test_missing_desc__.desc";

        [Test]
        public void ResolveRequired_MissingKey_LogsErrorAndReturnsMissingMarker()
        {
            var resolver = new UnityLocalizedTextResolver();

            LogAssert.Expect(
                LogType.Error,
                $"[Localization] Missing localized text. table='ability', key='{missingNameKey}'");

            string resolved = resolver.ResolveRequired("ability", missingNameKey);

            Assert.AreEqual($"[missing:{missingNameKey}]", resolved);
        }

        [Test]
        public void ResolveOptional_MissingKeyWithoutWarning_ReturnsEmptySilently()
        {
            var resolver = new UnityLocalizedTextResolver();

            string resolved = resolver.ResolveOptional("ability", missingDescKey, arguments: null, warnIfMissing: false);

            Assert.AreEqual(string.Empty, resolved);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ResolveOptional_MissingKeyWithWarning_LogsWarningAndReturnsEmpty()
        {
            var resolver = new UnityLocalizedTextResolver();

            LogAssert.Expect(
                LogType.Warning,
                $"[Localization] Optional localized text is missing. table='ability', key='{missingDescKey}'");

            string resolved = resolver.ResolveOptional("ability", missingDescKey, arguments: null, warnIfMissing: true);

            Assert.AreEqual(string.Empty, resolved);
        }
    }
}
