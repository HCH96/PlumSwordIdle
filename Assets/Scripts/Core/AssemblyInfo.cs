using System.Runtime.CompilerServices;

// 게임 로직 어셈블리. UnityEngine 을 참조하지 않는다 (asmdef 의 noEngineReferences).
// 테스트에서 internal 멤버를 검사할 수 있게 한다.
[assembly: InternalsVisibleTo("PlumSword.Core.Tests")]
