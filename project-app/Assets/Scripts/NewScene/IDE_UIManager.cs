/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 19/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IDE_UIManager : MonoBehaviour
{
    [Header("Referencias de la IDE")]
    public TMP_InputField codeInput;
    public Button executeButton;
    public Button clearButton;
    public TextMeshProUGUI outputText;

    [Header("UI del Desafío")] 
    public TextMeshProUGUI challengeTitleText;
    public TextMeshProUGUI challengeDescriptionText;

    void Start()
    {
        // Asignar funciones a los clics de los botones
        executeButton.onClick.AddListener(OnExecuteClicked);
        clearButton.onClick.AddListener(OnClearClicked);
    }

    private void OnExecuteClicked()
    {
        // Pedirle al GameManager que ejecute el código del InputField
        GameManager.Instance.ProcessAndRunCode(codeInput.text);
    }

    private void OnClearClicked()
    {
        codeInput.text = "";
        SetOutputText("Código borrado. Listo.", Color.white);
    }

    public void SetOutputText(string message, Color color)
    {
        outputText.text = message;
        outputText.color = color;
    }

    public void SetButtonsInteractable(bool interactable)
    {
        executeButton.interactable = interactable;
        clearButton.interactable = interactable;
    }

    
    public void DisplayChallenge(ChallengeData challenge)
    {
        if (challenge != null)
        {
            challengeTitleText.text = challenge.challengeTitle;
            challengeDescriptionText.text = challenge.challengeDescription;
        }
    }
}
