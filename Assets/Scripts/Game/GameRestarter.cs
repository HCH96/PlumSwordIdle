using UnityEngine;

namespace PlumSword.Game
{
    internal sealed class GameRestarter : MonoBehaviour
    {
        private int createdFrame;

        private void Awake() { createdFrame = Time.frameCount; }

        private void Update()
        {
            if (Time.frameCount == createdFrame) return;
            var root = new GameObject(GameBootstrap.RootName);
            DontDestroyOnLoad(root);
            root.AddComponent<GameRoot>();
            Destroy(gameObject);
        }
    }
}
