using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace PlumSword.Core.Tests
{
    /// <summary>
    /// 구조 규칙 검사: 게임 로직 어셈블리는 유니티 엔진·에디터에 의존하지 않는다.
    /// </summary>
    public class CoreAssemblyTests
    {
        [Test]
        public void Core_DoesNotReferenceUnity()
        {
            var core = Assembly.Load("PlumSword.Core");

            var unityReferences = core.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name.StartsWith("UnityEngine") || name.StartsWith("UnityEditor") || name.StartsWith("Unity."))
                .ToArray();

            Assert.That(unityReferences, Is.Empty, "PlumSword.Core 는 순수 C# 이어야 한다");
        }
    }
}
