using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("可选：直接在这里填场景名")]
    [SerializeField] string sceneName;

    // 原有按钮调用
    public void LoadScene()
    {
        LoadByName(sceneName);
    }

    // 通用：在 OnClick 里也可以直接传参（UnityEvent 支持）
    public void LoadByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("SceneLoader: sceneName 未设置");
            return;
        }
        SceneManager.LoadScene(name);
    }

    // 便捷：用于“Level1”与“StartScene”切换
    public void LoadStartScene() => LoadByName("StartScene");
    public void LoadLevel1() => LoadByName("Level1");

    // 退出游戏按钮（Editor 中不会退出，Build 后生效）
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

