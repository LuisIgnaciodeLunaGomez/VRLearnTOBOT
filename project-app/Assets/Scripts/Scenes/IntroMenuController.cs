using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroMenuController : MonoBehaviour
{
    // ... (TODAS TUS VARIABLES PÚBLICAS Y STRUCTS) ...
    // Se quedan exactamente como las tienes.

    [Header("Paneles Principales")]
    public GameObject mainOptionsPanel;
    public GameObject challengeSelectionPanel;

    [Header("UI de Selección de Desafíos")]
    public RectTransform challengeListContentContainer;
    public GameObject challengeCardPrefab;
    public Button backFromChallengesButton;

    [Header("UI de Detalles del Desafío")]
    public GameObject challengeDetailsPanel;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailStepText;
    public RawImage detailVideoRawImage; // Usaremos esta para mostrar el video
    public VideoPlayer videoPlayer;
    public Button detailStartButton;
    public Button detailCloseButton;

    private ChallengeInfo m_SelectedChallenge;

    [System.Serializable]
    public struct ChallengeInfo
    {
        public string id;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public VideoClip videoPreviewClip;
        public string targetSceneToLoad;
        public string thumbnailSpriteName;
    }

    [Header("Datos de los Desafíos")]
    public List<ChallengeInfo> availableChallenges = new List<ChallengeInfo>();


    void Start()
    {
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(true);
        if (challengeSelectionPanel != null) challengeSelectionPanel.SetActive(false);

        // El panel de detalles también debe empezar oculto.
        if (challengeDetailsPanel != null) challengeDetailsPanel.SetActive(false);

        SetupButtonListeners();
        PopulateChallengeList();
    }

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
    }

    void PopulateChallengeList()
    {
        if (challengeListContentContainer == null || challengeCardPrefab == null)
        {
            Debug.LogError("UI para la lista de desafíos no asignada.");
            return;
        }

        foreach (Transform child in challengeListContentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var challenge in availableChallenges)
        {
            GameObject cardInstance = Instantiate(challengeCardPrefab, challengeListContentContainer);
            cardInstance.name = "ChallengeCard_" + challenge.id;

            ChallengeCardUI cardUI = cardInstance.GetComponent<ChallengeCardUI>();
            if (cardUI != null) cardUI.Setup(challenge, this);

            Button cardButton = cardInstance.GetComponent<Button>();
            if (cardButton != null)
            {
                var currentChallenge = challenge;
                cardButton.onClick.AddListener(() => ShowChallengeDetails(currentChallenge));
            }
        }
    }

    // ===============================================================
    // VERSIÓN CORREGIDA DE LAS FUNCIONES DE DETALLES
    // ===============================================================

    /// <summary>
    /// Punto de entrada. Lo único que hace es iniciar la corutina.
    /// </summary>
    public void ShowChallengeDetails(ChallengeInfo challenge)
    {
        // Detener cualquier rutina de mostrar detalles anterior para evitar conflictos
        StopAllCoroutines();

        // Iniciar la nueva rutina
        StartCoroutine(ShowChallengeDetailsRoutine(challenge));
    }

    /// <summary>
    /// La corutina que maneja el proceso de mostrar el panel paso a paso.
    /// </summary>
    private IEnumerator ShowChallengeDetailsRoutine(ChallengeInfo challenge)
    {
        Debug.Log($"<color=cyan>CORUTINA INICIADA para '{challenge.displayName}'</color>");

        m_SelectedChallenge = challenge;

        if (challengeDetailsPanel == null)
        {
            Debug.LogError("Panel 'challengeDetailsPanel' no asignado en Inspector.");
            yield break; // Termina la corutina
        }

        // 1. Activar el panel principal de detalles.
        challengeDetailsPanel.SetActive(true);

        // 2. ESPERA UN FRAME. ¡Esta es la parte más importante!
        yield return new WaitForEndOfFrame();

        Debug.Log("<color=cyan>Frame esperado. Ahora configurando la UI de detalles...</color>");

        // 3. Ahora que el panel y sus hijos están activos, configura todo.
        if (detailTitleText != null) detailTitleText.text = challenge.displayName;
        if (detailStepText != null) detailStepText.text = challenge.description;

        if (videoPlayer != null && detailVideoRawImage != null)
        {
            if (challenge.videoPreviewClip != null)
            {
                detailVideoRawImage.gameObject.SetActive(true);
                videoPlayer.clip = challenge.videoPreviewClip;

                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared)
                {
                    yield return null;
                }

                Debug.Log($"Antes de Prepare(). Estado de VideoPlayerObject: {videoPlayer.gameObject.activeSelf}. Estado del componente VideoPlayer: {videoPlayer.enabled}. ¿Está en una jerarquía activa?: {videoPlayer.gameObject.activeInHierarchy}", videoPlayer.gameObject);

                videoPlayer.Play();
            }
            else
            {
                detailVideoRawImage.gameObject.SetActive(false);
            }
        }

        // Configura los botones AHORA, cuando es seguro.
        if (detailStartButton != null)
        {
            detailStartButton.onClick.RemoveAllListeners();
            detailStartButton.onClick.AddListener(OnStartSelectedChallenge);
        }
        if (detailCloseButton != null)
        {
            detailCloseButton.onClick.RemoveAllListeners();
            detailCloseButton.onClick.AddListener(OnHideChallengeDetails);
        }
    }

    public void OnHideChallengeDetails()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (challengeDetailsPanel != null) challengeDetailsPanel.SetActive(false);
    }

    public void OnStartSelectedChallenge()
    {
        if (m_SelectedChallenge.id != null)
        {
            ChallengeContext.SelectedChallengeId = m_SelectedChallenge.id;
            SceneManager.LoadScene(m_SelectedChallenge.targetSceneToLoad);
        }
    }

    // El resto de tus funciones como OnFreeProgrammingClicked, Show/HideChallengeSelectionPanel...
    // Se quedan como están.

    public void OnFreeProgrammingClicked()
    {
        Debug.Log("Iniciando modo de Programación Libre...");
        ChallengeContext.SelectedChallengeId = null;
        SceneManager.LoadScene("UIGUIVRLearnToBot");
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
    /// Este método es llamado por el botón "Ver Muestra" de ChallengeCardUI.
    /// Reutiliza la lógica existente para mostrar el panel de detalles con el video.
    /// </summary>
    /// <param name="videoClip">El clip de video a reproducir.</param>
    public void ShowVideoPreview(VideoClip videoClip)
    {
        // En tu diseño, mostrar el video implica mostrar todo el panel de detalles.
        // Necesitamos encontrar el ChallengeInfo que corresponde a este video.
        foreach (var challenge in availableChallenges)
        {
            if (challenge.videoPreviewClip == videoClip)
            {
                // Encontramos el desafío, ahora mostramos sus detalles.
                ShowChallengeDetails(challenge);
                return; // Salimos del bucle una vez encontrado.
            }
        }
    }

    /// <summary>
    /// Este método es llamado por el botón "Iniciar" de ChallengeCardUI.
    /// Carga directamente la escena del desafío.
    /// </summary>
    /// <param name="challengeData">Los datos del desafío a iniciar.</param>
    public void StartChallenge(ChallengeInfo challengeData)
    {
        if (challengeData.id != null)
        {
            Debug.Log($"Iniciando desafío directo: {challengeData.displayName}");
            // ChallengeContext.SelectedChallengeId = challengeData.id;
            SceneManager.LoadScene(challengeData.targetSceneToLoad);
        }
    }
}
