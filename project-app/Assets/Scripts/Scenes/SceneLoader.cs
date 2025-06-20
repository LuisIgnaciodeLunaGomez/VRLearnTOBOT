using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombreDeLaEscena)
    {
        // Imprime en la consola para saber que el botón funcionó (opcional, bueno para depurar).
        Debug.Log("Cargando escena: " + nombreDeLaEscena);

        // Carga la escena que corresponde al nombre proporcionado.
        SceneManager.LoadScene(nombreDeLaEscena);
    }
}
