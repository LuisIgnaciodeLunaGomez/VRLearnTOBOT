// ChallengeLoader.cs
using UnityEngine;
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Cargador de desafios
 */


using UnityEngine.SceneManagement;

public class ChallengeLoader : MonoBehaviour
{
    public static ChallengeLoader Instance { get; private set; }

    // Aquí guardaremos el desafío que el usuario ha seleccionado.
    public ChallengeData SelectedChallenge { get; private set; }

    private void Awake()
    {
        // Patrón Singleton para que solo haya una instancia de este objeto.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // No destruimos este objeto al cambiar de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadChallenge(ChallengeData challengeToLoad)
    {
        if (challengeToLoad != null)
        {
            SelectedChallenge = challengeToLoad;
            // Cargamos la escena principal del juego.
            SceneManager.LoadScene("CodingScene");
        }
        else
        {
            Debug.LogError("Se intentó cargar un desafío nulo.");
        }
    }
}