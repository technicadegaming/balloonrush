using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Core
{
    public sealed class SceneBootstrapGuard : MonoBehaviour
    {
        private void Awake()
        {
            if (GameBootstrap.Instance != null || SceneManager.GetActiveScene().name == GameBootstrap.BootSceneName)
            {
                return;
            }

            SceneManager.LoadScene(GameBootstrap.BootSceneName);
        }
    }
}
