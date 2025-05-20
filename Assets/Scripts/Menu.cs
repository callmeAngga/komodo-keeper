using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Main VR Scene"); // Ganti sesuai scene utama kamu
    }

    public void OpenAbout()
    {
        SceneManager.LoadScene("About");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Keluar dari game");
    }
}
