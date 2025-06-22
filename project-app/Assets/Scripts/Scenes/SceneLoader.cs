using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombreDeLaEscena)
    {
       
        //Debug.Log("Cargando escena: " + nombreDeLaEscena);

        // Carga la escena que corresponde al nombre proporcionado.
        SceneManager.LoadScene(nombreDeLaEscena);
    }
}
