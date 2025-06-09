using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gestiona la lógica de la escena de introducción, incluyendo el menú principal,
/// la selección de desafíos y la vista previa de videos.
/// </summary>
public class IntroMenuController : MonoBehaviour
{
    // --- REFERENCIAS DE UI (Arrastrar desde el Inspector de Unity) --- //

    [Header("Paneles Principales")]
    [Tooltip("El panel que contiene los botones 'Programación Libre', 'Seleccionar Desafío', etc.")]
    public GameObject mainOptionsPanel;
    [Tooltip("El panel/ScrollView que contiene la lista de tarjetas de desafío.")]
    public GameObject challengeSelectionPanel;
    [Tooltip("El panel modal que muestra la vista previa del video.")]
    public GameObject videoPreviewPanel;

    [Header("UI de Selección de Desafíos")]
    [Tooltip("El objeto 'Content' dentro del ScrollView donde se instanciarán las tarjetas.")]
    public RectTransform challengeListContentContainer;
    [Tooltip("El prefab de la 'Tarjeta de Desafío' que has creado.")]
    public GameObject challengeCardPrefab;
    [Tooltip("Referencia al botón 'Volver' en el panel de selección de desafíos.")]
    public Button backFromChallengesButton;


    [Header("UI de Vista Previa de Video")]
    [Tooltip("El componente RawImage donde se renderizará el video.")]
    public RawImage videoDisplayRawImage;
    [Tooltip("El componente VideoPlayer que controla la reproducción.")]
    public VideoPlayer videoPlayer;
    [Tooltip("El botón para cerrar la vista previa del video.")]
    public Button closeVideoButton;


    /// <summary>
    /// Define la estructura de datos para un solo desafío.
    /// [System.Serializable] permite que se muestre en el Inspector.
    /// </summary>
    [System.Serializable]
    public struct ChallengeInfo
    {
        [Tooltip("Identificador único para el desafío (e.g., 'desafio_01_mover').")]
        public string id;
        [Tooltip("Nombre que verá el usuario en la lista de desafíos.")]
        public string displayName;
        [Tooltip("Descripción detallada del objetivo del desafío.")]
        [TextArea(3, 5)]
        public string description;
        [Tooltip("El clip de video que se mostrará como vista previa.")]
        public VideoClip videoPreviewClip;
        [Tooltip("El nombre de la escena de programación a cargar para este desafío.")]
        public string targetSceneToLoad;
    }

    [Header("Datos de los Desafíos")]
    [Tooltip("Crea y arrastra aquí tus ScriptableObjects de 'ChallengeData' o rellena esta lista manualmente.")]
    public List<ChallengeInfo> availableChallenges = new List<ChallengeInfo>();

    void Start()
    {
        // 1. Asegurar el estado inicial correcto de los paneles.
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(true);
        if (challengeSelectionPanel != null) challengeSelectionPanel.SetActive(false);
        if (videoPreviewPanel != null) videoPreviewPanel.SetActive(false);

        // 2. Conectar los listeners de los botones principales a sus funciones
        SetupButtonListeners();

        // 3. Generar dinámicamente las tarjetas para cada desafío en la lista.
        PopulateChallengeList();
    }

    /// <summary>
    /// Configura los listeners para los botones principales de la UI.
    /// Se podría hacer también desde el inspector.
    /// </summary>
    private void SetupButtonListeners()
    {
        
        Button freeProgBtn = mainOptionsPanel.transform.Find("FreeProgrammingButton")?.GetComponent<Button>();
        if (freeProgBtn) freeProgBtn.onClick.AddListener(OnFreeProgrammingClicked);

        Button selectChallengeBtn = mainOptionsPanel.transform.Find("SelectChallengeButton")?.GetComponent<Button>();
        if (selectChallengeBtn) selectChallengeBtn.onClick.AddListener(ShowChallengeSelectionPanel);

        Button exitBtn = mainOptionsPanel.transform.Find("ExitButton")?.GetComponent<Button>();
        if (exitBtn) exitBtn.onClick.AddListener(OnExitClicked);

        if (backFromChallengesButton != null)
            backFromChallengesButton.onClick.AddListener(HideChallengeSelectionPanel);

        if (closeVideoButton != null)
            closeVideoButton.onClick.AddListener(HideVideoPreview);
    }

    /// <summary>
    /// Crea una tarjeta de UI para cada desafío disponible y la añade a la lista visual.
    /// </summary>
    void PopulateChallengeList()
    {
        if (challengeListContentContainer == null || challengeCardPrefab == null)
        {
            Debug.LogError("UI para la lista de desafíos no asignada. No se pueden crear las tarjetas.");
            return;
        }

        // Limpiar cualquier tarjeta que pudiera existir de antes
        foreach (Transform child in challengeListContentContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear una tarjeta por cada desafío en la lista `availableChallenges`
        foreach (var challenge in availableChallenges)
        {
            GameObject cardInstance = Instantiate(challengeCardPrefab, challengeListContentContainer);
            cardInstance.name = "ChallengeCard_" + challenge.id;

            // Obtener el script de la tarjeta y pasarle los datos
            ChallengeCardUI cardUI = cardInstance.GetComponent<ChallengeCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(challenge, this);
            }
            else
            {
                Debug.LogError($"El prefab 'ChallengeCard' no tiene el script ChallengeCardUI.cs asignado.", cardInstance);
            }
        }
    }

    #region --- Handlers de Botones ---

    public void OnFreeProgrammingClicked()
    {
        Debug.Log("Iniciando modo de Programación Libre...");
        ChallengeContext.SelectedChallengeId = null; // Marcar que no hay un desafío específico.
        SceneManager.LoadScene("ProgrammingScene"); // Asegúrar de que el nombre de la escena sea correcto
    }

    public void ShowChallengeSelectionPanel()
    {
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
        if (challengeSelectionPanel != null) challengeSelectionPanel.SetActive(true);
    }

    public void HideChallengeSelectionPanel()
    {
        if (challengeSelectionPanel != null) challengeSelectionPanel.SetActive(false);
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(true);
    }

    public void OnExitClicked()
    {
        Debug.Log("Saliendo de la aplicación.");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>
    /// Muestra el panel de video y reproduce el clip proporcionado. Llamado desde ChallengeCardUI.
    /// </summary>
    public void ShowVideoPreview(VideoClip clip)
    {
        if (videoPlayer == null || clip == null)
        {
            Debug.LogError("No se puede reproducir el video. El VideoPlayer o el VideoClip es nulo.");
            return;
        }

        videoPlayer.clip = clip;
        videoPreviewPanel.SetActive(true);
        videoPlayer.Play();
    }

    /// <summary>
    /// Oculta el panel de video y detiene la reproducción.
    /// </summary>
    public void HideVideoPreview()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        videoPreviewPanel.SetActive(false);
    }

    /// <summary>
    /// Guarda el ID del desafío y carga la escena de programación. Llamado desde ChallengeCardUI.
    /// </summary>
    public void StartChallenge(ChallengeInfo challenge)
    {
        Debug.Log($"Iniciando desafío: {challenge.displayName} (ID: {challenge.id})");
        ChallengeContext.SelectedChallengeId = challenge.id;
        SceneManager.LoadScene(challenge.targetSceneToLoad);
    }

    #endregion
}