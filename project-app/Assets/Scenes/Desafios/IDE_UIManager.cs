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
 * Descripción: Gestor de la interfaz de usuario del IDE para el desafío.
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IDE_UIManager : MonoBehaviour
{
    [Header("UI del Desafío")] 
    public TextMeshProUGUI challengeTitleText;
    public TextMeshProUGUI challengeDescriptionText;

    [Header("Constructor de Comandos")]
    public TMP_InputField singleCommandInput;
    public Button addCommandButton;
    public RectTransform programContent; 
    public GameObject commandBlockPrefab; 

    [Header("Botones de Control")]
    public Button executeButton;
    public Button clearButton;
    public TextMeshProUGUI outputText;

    //Lista de instrucciones válidas
    private List<Instruction> programInstructions = new List<Instruction>();
    private ProgramBuilder programBuilder = new ProgramBuilder();

    void Start()
    {
      
        executeButton.onClick.AddListener(OnExecuteClicked);
        addCommandButton.onClick.AddListener(OnAddCommandClicked);
        clearButton.onClick.AddListener(OnClearClicked);
    }

    private void OnAddCommandClicked()
    {
        string commandText = singleCommandInput.text;
        if (string.IsNullOrWhiteSpace(commandText)) return;

        // Se añade la línea de texto al builder
        programBuilder.AddLine(commandText);

        //Se crea el bloque visual en la UI
        AddCommandBlockToUI(commandText);

        // SE limpia para el siguiente comando
        singleCommandInput.text = "";
        singleCommandInput.ActivateInputField();
        SetOutputText("", Color.white); // Limpiar mensajes de error
    }

    private void AddCommandBlockToUI(string commandText)
    {
        GameObject newBlock = Instantiate(commandBlockPrefab, programContent, false);

        newBlock.transform.localScale = Vector3.one;
        newBlock.transform.localPosition = Vector3.zero;
        TextMeshProUGUI blockText = newBlock.GetComponentInChildren<TextMeshProUGUI>();

        if (blockText != null)
        {
            blockText.text = commandText.Trim();
        }
    }
    public void OnExecuteClicked()
    {
        // El ProgramBuilder intenta parsear el código que ha acumulado.
        // out var program creará una nueva lista de instrucciones si el parseo es exitoso.
        if (programBuilder.TryParseProgram(out var program, out string errorMessage))
        {
            // Se comprueba si el programa resultante tiene algún comando.
            if (program.Count > 0)
            {
                //Si es correcto enviamos la lista de instrucciones ya parseada al GameManager.
                GameManager.Instance.ProcessAndRunProgram(program);
            }
            else
            {
                //Si el usuario no introdujo ningún comando válido.
                SetOutputText("No hay comandos en el programa para ejecutar.", Color.yellow);
            }
        }
        else
        {
            // Mostramos el mensaje de error que nos dio el parser.
            SetOutputText(errorMessage, Color.red);
        }
    }

    private void OnClearClicked()
    {
        // Limpiar el builder
        programBuilder.Clear();
        // Limpiar la UI
        foreach (Transform child in programContent)
        {
            Destroy(child.gameObject);
        }
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
