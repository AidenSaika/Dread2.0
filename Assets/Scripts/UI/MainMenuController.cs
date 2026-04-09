using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // This function will be called when the Start button is clicked
    public void StartGame()
    {
        SceneManager.LoadScene("LevelOne");
    }
}