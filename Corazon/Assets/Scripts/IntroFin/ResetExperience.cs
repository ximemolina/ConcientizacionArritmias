using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetExperience : MonoBehaviour
{
    public void Reiniciar()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopAll();
            Destroy(GameAudioManager.Instance.gameObject);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}