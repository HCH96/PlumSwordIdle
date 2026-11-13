using UnityEngine;

namespace PlumSword.Game
{
    /// <summary>
    /// 화면 조립의 뿌리. 각 기능은 Awake 에서 순수 C# 로직(PlumSword.Core)을 만들고,
    /// 자기 화면(오브젝트, UI)을 이 오브젝트의 자식으로 만들어 붙인다.
    /// 조립 순서와 규칙은 docs/dev/architecture.md 참고.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private void Awake()
        {
        }
    }
}
