using UnityEngine;
using UnityEngine.SceneManagement;

public class TimelineSceneLoader : MonoBehaviour
{
    [Tooltip("Panggil fungsi ini lewat Signal Track di Timeline")]
    public void LoadScene(string sceneName)
    {
        Debug.Log("Pindah ke scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
