using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutMenu : MonoBehaviour
{
    public void BackToMenu()
    {
        SceneManager.LoadScene("MenuScene"); // Nama scene Menu kamu
    }
}
