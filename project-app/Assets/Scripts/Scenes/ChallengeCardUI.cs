using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la UI de una sola tarjeta de desafío.
/// Recibe datos de un desafío y configura sus elementos visuales y botones.
/// </summary>
public class ChallengeCardUI : MonoBehaviour
{
    [Header("Referencias de UI de la Tarjeta")]
    public TextMeshProUGUI titleText;
    // public Image iconImage; 
    // public TextMeshProUGUI descriptionText; 

    [Tooltip("El botón que inicia la vista previa del video.")]
    public Button previewButton;
    [Tooltip("El botón que carga el desafío en la escena de programación.")]
    public Button startButton;

    // Referencias internas para la lógica de los botones
    private IntroMenuController menuController;
    private IntroMenuController.ChallengeInfo currentChallenge;

    /// <summary>
    /// Este método es llamado por IntroMenuController cuando se instancia la tarjeta.
    /// </summary>
    /// <param name="challengeData">Los datos del desafío a mostrar.</param>
    /// <param name="controller">Referencia al controlador principal del menú.</param>
    public void Setup(IntroMenuController.ChallengeInfo challengeData, IntroMenuController controller)
    {
        currentChallenge = challengeData;
        menuController = controller;

        // Actualizar la UI de la tarjeta
        if (titleText != null)
            titleText.text = currentChallenge.displayName;

        // if (descriptionText != null) 
        //     descriptionText.text = currentChallenge.description;

        // Limpiar y añadir los listeners a los botones
        if (previewButton != null)
        {
            previewButton.onClick.RemoveAllListeners();
            previewButton.onClick.AddListener(OnPreviewClicked);
            // El video se puede ver
            previewButton.gameObject.SetActive(currentChallenge.videoPreviewClip != null);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }
    }

    /// <summary>
    /// Llamado cuando el usuario hace clic en "Ver Muestra".
    /// </summary>
    private void OnPreviewClicked()
    {
        if (menuController != null && currentChallenge.videoPreviewClip != null)
        {
            menuController.ShowVideoPreview(currentChallenge.videoPreviewClip);
        }
    }

    /// <summary>
    /// Llamado cuando el usuario hace clic en "Iniciar".
    /// </summary>
    private void OnStartClicked()
    {
        if (menuController != null)
        {
            menuController.StartChallenge(currentChallenge);
        }
    }
}