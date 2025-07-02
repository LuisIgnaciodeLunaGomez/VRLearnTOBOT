using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void GoToMainMenu()
    {

        Debug.Log("Volviendo a la escena del menú principal (mainMenu)...");
        SceneManager.LoadScene("mainMenu");
    }
}
