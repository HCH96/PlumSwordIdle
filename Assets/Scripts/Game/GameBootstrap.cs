using UnityEngine;

namespace PlumSword.Game
{
    /// <summary>
    /// 게임 진입점. 씬에 오브젝트를 배치하지 않고, 실행 시점에 코드로 화면을 조립한다.
    /// 어떤 씬에서 시작해도 루트 오브젝트 하나를 만들고 <see cref="GameRoot"/> 를 붙인다.
    /// </summary>
    public static class GameBootstrap
    {
        public const string RootName = "[Game]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (GameObject.Find(RootName) != null)
            {
                return;
            }

            var root = new GameObject(RootName);
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameRoot>();
        }
    }
}
