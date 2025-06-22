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
 * Descripción: 
 */


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    [Header("Configuración")]
    public List<ChallengeData> availableChallenges; // La lista de todos los desafíos
    public GameObject challengeCardPrefab;
    public Transform cardContainer; // El panel 'ChallengeSelectionPanel'

    void Start()
    {
        PopulateChallengeCards();
    }

    private void PopulateChallengeCards()
    {
        // Limpiar tarjetas antiguas si las hubiera
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear una tarjeta por cada desafío disponible
        foreach (var challenge in availableChallenges)
        {
            GameObject cardInstance = Instantiate(challengeCardPrefab, cardContainer);

            // Buscar los textos dentro de la tarjeta
            var texts = cardInstance.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = challenge.challengeTitle;
                texts[1].text = challenge.shortDescription; // Descripción es corta
            }

            // Configurar el botón de la tarjeta
            Button cardButton = cardInstance.GetComponent<Button>();
            if (cardButton != null)
            {
                // AddListener puede capturar una variable.  Cada botón recordará su propio desafío.
                cardButton.onClick.AddListener(() => OnChallengeSelected(challenge));
            }
        }
    }

    private void OnChallengeSelected(ChallengeData selectedChallenge)
    {
        Debug.Log($"Desafío seleccionado: {selectedChallenge.name}");
        // Le decimos al gestor de transición que cargue este desafío
        ChallengeLoader.Instance.LoadChallenge(selectedChallenge);
    }

    public void GoToMainMenu()
    {
    
        Debug.Log("Volviendo a la escena del menú principal (mainMenu)...");
        SceneManager.LoadScene("mainMenu");
    }
}
