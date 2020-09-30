using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    public void Replay()
    {
        Invoke("DelayReplay", 0.7f);
    }

    private void DelayReplay()
    {
        // SceneManager.GetActiveScene().name 取得目前啟動場景的名稱
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Invoke("DelayBackToMenu", 0.7f);
    }

    private void DelayBackToMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Invoke("DelayQuit", 0.7f);
    }

    private void DelayQuit()
    {
        Application.Quit();
    }
}
