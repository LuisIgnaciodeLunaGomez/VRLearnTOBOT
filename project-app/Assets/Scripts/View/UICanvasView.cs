/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 17/02/2025
 * 
 * Versión: 2.0.1
 * 
 * Descripción: Crear la estructura visual básica: El Canvas, el Panel Superior (con logo e iconos), y los contenedores (paneles vacíos) para las otras áreas (Categorías, Lista de Bloques/Toolbox, Área de Código).
 *
 * Exponer referencias a estos contenedores para que AppController pueda dárselos a las vistas y controladores correspondientes (BlockListView, WorkspaceView de UBlockly, CategoryController, etc.).
 *
 * Manejar eventos de UI propios: Como los clics en los iconos del panel superior (Play, Save, Load, Stop).
 */

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Collections;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]

public class UICanvasView : MonoBehaviour
{
    [Header("Configuración Visual añadir en el Inspector")]
    public string logoSpriteName; // Nombre del sprite para el logo - Cargar en Inspector
    public string[] topIconNames; // Nombres de sprites para iconos (GreenFlag, StopFlag, Save...) - Cargar en Inspector
    private WorkSpaceModel m_WorkspaceModel; // Modelo lógico de area de trabajo
    private WorkSpaceView m_WorkSpaceView; // Vista  del área de código
    private BlockListView m_Toolbox;  // Contenedor de bloques (Toolbox) en el panel izquierdo (MiddlePanel)
    private ToolboxConfig m_ToolboxConfiguration;
    [SerializeField] private GameObject categoryButtonPrefab; // Prefab  botón de categoría - Cargar en Inspector
    private RectTransform m_CategoryButtonContainerRect; //Contenedor de botones
    //private RectTransform BlockListAreaRect;
    //private RectTransform WorkSpaceAreaRect;
    private ScrollRect m_MiddlePanelScrollRect;
    private WorkspaceController m_WorkspaceController;
    public GameObject CategoryButtonPrefab => categoryButtonPrefab;
    private RectTransform m_RightPanelRect;
    public RectTransform CodingAreaPanelRect => m_RightPanelRect;
    private RectTransform m_MiddlePanelRect;
    public RectTransform BlockListPanelRect => m_MiddlePanelRect;
    public RectTransform CategoryButtonContainerRect => m_CategoryButtonContainerRect;
    public ScrollRect MiddlePanelScrollRect => m_MiddlePanelScrollRect;
    private RectTransform m_LeftToolBarContainerRect;
    public ToolboxConfig ToolboxConfig => m_ToolboxConfiguration;
    public WorkSpaceModel Workspace => m_WorkspaceModel;
    public WorkSpaceView WorkSpaceView => m_WorkSpaceView;
    private CategoryController m_CategoryController;
    public BlockListView Toolbox => m_Toolbox;
    private GameObject m_CanvasGO;
    private GameObject m_UiManagerView;
    private RectTransform m_DragLayerRect; //<-----Panel para el arrastre de los bloques en la escena.

 
    public RectTransform DragLayer => m_DragLayerRect;
    private Dictionary<string, Color> mCategoryColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

    private bool m_isCoreComponentsReady = false;//<-----Bandera para indicar que los componentes principales están listos.
    public bool IsCoreComponentsReady() => m_isCoreComponentsReady; //
    //Dimensiones estimadas de la pantalla
    private const int m_screenWidth = 1200;
    private const int m_screenHeight = 720;

    private GameObject m_CategoriesPanelGO; // GameObject del panel izquierdo de categorías
    private GameObject m_greenFlagIconGO;   // GameObject del icono de GreenFlag
    private GameObject m_stopFlagIconGO;    // GameObject del icono de StopFlag

    void Awake()
    {
        // Debug.Log("<color=cyan>UICanvasView: Awake starting UI Setup...</color>");
      //  Debug.LogError("---- UICanvasView Awake() START ---- Instance ID: " + this.GetInstanceID());
        m_isCoreComponentsReady = false;

        InitializeCore();
        InitializeUIManager();
        InitializeCanvas();
        CreateTopPanel();

        GameObject categoryContentPanelGO = SetupLeftPanel(m_CanvasGO); // Left Panel
        if (categoryContentPanelGO != null)
        {
            m_CategoryButtonContainerRect = categoryContentPanelGO.GetComponent<RectTransform>();
            if (m_CategoryButtonContainerRect == null)
                Debug.LogError("SetupLeftPanel returned GO missing RectTransform!");
        }
        else { Debug.LogError("SetupLeftPanel failed to return content panel GO!"); }

        CreateWorkspacePanels(); // Middle y Right Panels

        CreateDragLayer(); // Panel para el arrastre de bloques

        if (m_RightPanelRect != null && m_MiddlePanelRect != null)
        {
            try
            {
                //  Crear/Añadir componentes vista a los paneles 
                m_WorkSpaceView = m_RightPanelRect.gameObject.GetComponent<WorkSpaceView>();
                if (m_WorkSpaceView == null) m_WorkSpaceView = m_RightPanelRect.gameObject.AddComponent<WorkSpaceView>();

                m_Toolbox = m_MiddlePanelRect.gameObject.GetComponent<BlockListView>();
                if (m_Toolbox == null) m_Toolbox = m_MiddlePanelRect.gameObject.AddComponent<BlockListView>();

                m_MiddlePanelScrollRect = m_MiddlePanelRect.gameObject.GetComponent<ScrollRect>();
                if (m_MiddlePanelScrollRect == null) { }

                // Creamos Modelo
                WorkSpaceModel.WorkspaceOptions options = new WorkSpaceModel.WorkspaceOptions();
                m_WorkspaceModel = new WorkSpaceModel(options);

               // Debug.LogError($"HASHCODE_CHECK - WorkSpaceModel UICanvasView CONSTRUCTOR - ID: {m_WorkspaceModel.Id} - Instance HashCode: {m_WorkspaceModel.GetHashCode()}");

                // Debug.Log($"<color=green>UICanvasView: WorkSpaceModel created (ID: {m_WorkspaceModel.Id}).</color>");

                // Marcamos componentes listos 
                if (m_WorkspaceModel != null && m_WorkSpaceView != null && m_Toolbox != null)
                {
                    m_isCoreComponentsReady = true;
                 //   Debug.Log("<color=green>UICanvasView: Core components (Model, WorkspaceView, Toolbox) created/found.</color>");
                }
                else
                {
                    Debug.LogError("UICanvasView: Failed to create/find core Model/View components in Awake!");
                    enabled = false;
                    return;
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>UICanvasView: Error creating components in Awake: {e.Message}</color>");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("UICanvasView: Failed to create Middle or Right panels. Cannot setup UBlockly.");
            enabled = false;
            return;
        }

       
        // Debug.Log("<color=green>UICanvasView: Awake finished UI and  base setup.</color>");
    }

    IEnumerator Start()
    {
        //Debug.Log("<color=cyan>UICanvasView: Start - Waiting for AppController initialization...</color>");
        // Esperamos a que AppController esté listo y tenga los controladores
        yield return new WaitUntil(() => AppController.Instance != null && AppController.Instance.IsInitialized());

       // Debug.Log("<color=cyan>UICanvasView: AppController ready. Initializing Toolbox UI...</color>");

        m_WorkspaceController = AppController.Instance.GetComponent<WorkspaceController>();
        //Obtenemos el CategoryController 
        CategoryController categoryControllerInstance = AppController.Instance.GetCategoryController();

        if (categoryControllerInstance == null)
        {
            Debug.LogError("UICanvasView: Could not get CategoryController from AppController in Start!");
            yield break;
        }

        if (m_Toolbox != null && m_WorkspaceModel != null && m_ToolboxConfiguration != null && m_WorkSpaceView != null && m_CategoryButtonContainerRect != null && m_MiddlePanelScrollRect != null && m_CategoryController == null /* Asegurar que no se inicialice dos veces */)
        {
            m_CategoryController = categoryControllerInstance;

           // Debug.Log("<color=yellow>UICanvasView: Calling InitializeToolbox on BlockListView...</color>");

            m_Toolbox.InitializeToolbox(
                m_WorkspaceModel,
                m_ToolboxConfiguration,
                m_WorkSpaceView,
                m_CategoryButtonContainerRect,
                m_MiddlePanelScrollRect,
                categoryButtonPrefab,
                categoryControllerInstance
            );
           // Debug.Log("<color=green>UICanvasView: BlockListView Toolbox Initialized.</color>");

            if (m_WorkSpaceView != null)
            {
               // Debug.Log($"<color=yellow>UICanvasView: Binding WorkSpaceView ({m_WorkSpaceView.GetInstanceID()})...</color>");
                m_WorkSpaceView.BindModel(
                   m_WorkspaceModel,
                   m_Toolbox,
                   m_RightPanelRect,
                   null
                );
               // Debug.Log("<color=green>UICanvasView: WorkSpaceView bound.</color>");
            }

        }
        else
        {
            string errorMsg = "UICanvasView: Cannot initialize Toolbox in Start due to missing refs: ";
            if (m_Toolbox == null) errorMsg += "Toolbox ";
            if (m_WorkspaceModel == null) errorMsg += "WorkspaceModel ";
            if (m_ToolboxConfiguration == null) errorMsg += "ToolboxConfig ";
            if (m_WorkSpaceView == null) errorMsg += "WorkSpaceView ";
            if (m_CategoryButtonContainerRect == null) errorMsg += "CategoryContainer ";
            if (m_MiddlePanelScrollRect == null) errorMsg += "MiddleScrollRect ";
            if (categoryControllerInstance == null) errorMsg += "CategoryController(from App) ";
            Debug.LogError(errorMsg);
        }


        //Debug.Log("<color=green>UICanvasView: Start method finished.</color>");
    }
    /**
     * Descripción: Inicializa el núcleo y carga los recursos necesarios.
     */
    private void InitializeCore()
    {
       // Debug.Log("<color=yellow>UICanvasView: Initializing Core...</color>");
        try
        {
            BlockResMgr resMgr = BlockResMgr.Get();
            if (resMgr == null)
            {
                Debug.LogWarning("BlockResMgr not ready, attempting synchronous load...");

            }

           // Debug.Log("<color=teal>UICanvasView: Loading Toolbox Configuration...</color>");

            m_ToolboxConfiguration = LoadToolboxConfigFromXml("XML/DefaultToolBox");
            if (m_ToolboxConfiguration == null)
            {
                Debug.LogError("UICanvasView: FAILED to load Toolbox Configuration from XML! Using default empty config.");
                m_ToolboxConfiguration = new ToolboxConfig { BlockCategoryList = new List<ToolboxBlockCategory>() };
            }
            else
            {
                //Obtiene el color del CategoryLoader
                InitializeCategoryColors(m_ToolboxConfiguration);
              //  Debug.Log($"<color=green>UICanvasView: Toolbox Configuration loaded from XML. Categories: {m_ToolboxConfiguration.BlockCategoryList?.Count ?? 0}</color>");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>UICanvasView: CRITICAL ERROR during ScratchBlocks.Init(): {e.Message}\n{e.StackTrace}</color>");
            enabled = false;
        }
    }
    /**
     * Descripcion: Inicializa el UIManagerView
     */
    private void InitializeUIManager()
    {
      //  Debug.Log("<color=yellow>UICanvasView: Creating UIManagerView...</color>");
        m_UiManagerView = GameObject.Find("UIManagerView");
        if (m_UiManagerView == null)
        {
            m_UiManagerView = new GameObject("UIManagerView");
        }
    }

    /**
     * Descripción: Crea el canvas y lo configura como hijo del UIManagerView para usar los GO en el programa
     */
    private void InitializeCanvas()
    {
       // Debug.Log("<color=yellow>UICanvasView: Creating Canvas...</color>");

        m_CanvasGO = new GameObject("Canvas");
        m_CanvasGO.transform.SetParent(this.m_UiManagerView.transform);

        Canvas canvas = m_CanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = m_CanvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;// ScaleWithScreenSize;
        canvasScaler.scaleFactor = 1f;

        canvasScaler.referenceResolution = new Vector2(m_screenWidth, m_screenHeight);
        canvasScaler.matchWidthOrHeight = 1f;

        //Detección de eventos de UI en el canvas necesario para el Drag and Drop
        m_CanvasGO.AddComponent<GraphicRaycaster>();
    }

    /**
     * Descripción: Crea el panel izquierdo (Categorías) y lo configura. Incorporo un botón para depuración provisional.
     */
    private void CreateTopPanel()
    {
        //Debug.Log("<color=yellow>UICanvasView: Creating Top Panel...</color>");

        //Creación del panel de herramientas superior
        GameObject topPanel = CreatePanel("Tools Panel", this.m_CanvasGO.transform,
            new Vector2(0, 0.90f), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            new Vector2(0, 0), new Color(0.6f, 0.4f, 1f, 1f));

        // Asignación altura fija 
        topPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

        //Posicionamiento por encima de otros paneles del topPanel
        Canvas topCanvas = topPanel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 1000;

        // Creamos un contenedor para el logo y los elementos de la izquierda
        GameObject leftContainer = new GameObject("LeftToolBarContainer");
        leftContainer.transform.SetParent(topPanel.transform, false); 

        m_LeftToolBarContainerRect = leftContainer.AddComponent<RectTransform>(); 
        m_LeftToolBarContainerRect.anchorMin = new Vector2(0, 0.5f); // Anclado a la izquierda, centrado vertical
        m_LeftToolBarContainerRect.anchorMax = new Vector2(0, 0.5f);
        m_LeftToolBarContainerRect.pivot = new Vector2(0, 0.5f); // Pivote a la izquierda, centrado vertical
        m_LeftToolBarContainerRect.anchoredPosition = new Vector2(300, 0); // Pequeño margen a la izquierda


        HorizontalLayoutGroup leftLayoutGroup = leftContainer.AddComponent<HorizontalLayoutGroup>();
        leftLayoutGroup.childAlignment = TextAnchor.MiddleLeft; 
        leftLayoutGroup.spacing = 10f; 
        leftLayoutGroup.padding = new RectOffset(10, 0, 0, 0); 
        leftLayoutGroup.childControlWidth = false; 
        leftLayoutGroup.childControlHeight = false;
        leftLayoutGroup.childForceExpandWidth = false;
        leftLayoutGroup.childForceExpandHeight = false;

        ContentSizeFitter leftFitter = leftContainer.AddComponent<ContentSizeFitter>();
        leftFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; 
        leftFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        topPanel.AddComponent<GraphicRaycaster>();

        if (!string.IsNullOrEmpty(logoSpriteName))
        {
            this.AddLogoToPanel(topPanel, logoSpriteName);
        }

        AddExportDebugButtonToPanel(m_LeftToolBarContainerRect);


        if (topIconNames != null && topIconNames.Length > 0)
        {
            this.AddIconsToPanel(topPanel, topIconNames);
        }
    }

    /**
        * Crea un panel con los parámetros indicados sirve para el let y el middle panel
        * @param name Nombre del panel
        * @param parent Padre del panel
        * @param anchorMin Ancla mínimo
        * @param anchorMax Ancla máximo
        * @param offsetMin Offset mínimo
        * @param offsetMax Offset máximo
        * @param pivot Pivote
        * @param color Color del panel
        * @return GameObject creado
        */
    GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = pivot;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    public void CreateDragLayer()
    {

        if (m_CanvasGO == null) { Debug.LogError("Cannot create DragLayer, Canvas is null!"); return; }

     //   Debug.Log("<color=yellow>UICanvasView: Creating Drag Layer Panel...</color>");

        GameObject dragLayerGO = CreatePanel(
            "DragLayerPanel",             // Nombre
            m_CanvasGO.transform,         // Padre = Canvas principal
            new Vector2(0.15f, 0f),       // Anchor Min: Igual que WorkArea (derecha del Left Panel, abajo)
            new Vector2(1f, 0.90f),       // Anchor Max: Igual que WorkArea (derecha total, abajo del Top Panel)
            Vector2.zero, Vector2.zero,   // Offsets: Estirar completamente a los anchors
            new Vector2(0.5f, 0.5f),      // Pivot: Centro (estándar para capas)
            Color.clear                   // Color: Totalmente transparente
        );

        // Configuración específica de la Drag Layer:
        Image dragImage = dragLayerGO.GetComponent<Image>();
        if (dragImage != null)
        {
            dragImage.raycastTarget = false; // IMPORTANTE: No debe bloquear clics a paneles inferiores cuando NADA se arrastra
        }

        // Cachear la referencia
        m_DragLayerRect = dragLayerGO.GetComponent<RectTransform>();
        if (m_DragLayerRect == null)
        {
            Debug.LogError("Failed to get RectTransform for DragLayerPanel!", dragLayerGO);
        }

        //Debug.Log($"<color=green>UICanvasView: DragLayerPanel created. Rect: {m_DragLayerRect?.rect}</color>", dragLayerGO);

    }

    /** Añade un logo al panel indicado
   * @param panel GameObject al que se le añadirá el logo
   * @param spriteName Nombre del sprite a añadir
   */
    void AddLogoToPanel(GameObject panel, string spriteName)
    {
        Texture2D logoTexture = Resources.Load<Texture2D>("Icons/" + spriteName);

        if (logoTexture != null)
        {
            GameObject logo = new GameObject("TopPanelLogo");
            logo.transform.SetParent(panel.transform);
            RectTransform logoRect = logo.AddComponent<RectTransform>();
            Image logoImage = logo.AddComponent<Image>();

            // Convertir Texture2D a Sprite
            Sprite sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImage.sprite = sprite;

            // Alinear el logo a la izquierda con un margen 
            logoRect.anchorMin = new Vector2(0, 0.5f);
            logoRect.anchorMax = new Vector2(0, 0.5f);
            logoRect.pivot = new Vector2(0, 0.5f);
            logoRect.sizeDelta = new Vector2(panel.GetComponent<RectTransform>().rect.width * 0.05f, panel.GetComponent<RectTransform>().rect.height * 0.5f);
            logoRect.anchoredPosition = new Vector2(50, 0); // Desplazamiento a la izquierda
        }
        else
        {
            Debug.LogError("No se encontró el sprite en Resources: " + spriteName);
        }
    }

    /**
     * Añade iconos al panel indicado
     * @param panel GameObject al que se le añadirán los iconos
     * @param iconNames Nombres de los iconos a añadir
     */
    void AddIconsToPanel(GameObject panel, string[] iconNames)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        if (panelHeight <= 0) panelHeight = 30; // Fallback
        float iconSize = panelHeight * 0.7f;
        float padding = 5f;

        // Contenedor para layout automático a la derecha
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(panel.transform, false);
        RectTransform containerRect = iconContainer.AddComponent<RectTransform>();

        // Anclar a la derecha, centrado verticalmente
        containerRect.anchorMin = new Vector2(1, 0.5f);
        containerRect.anchorMax = new Vector2(1, 0.5f);
        containerRect.pivot = new Vector2(1, 0.5f); // Pivote Derecha-Centro
        containerRect.anchoredPosition = new Vector2(-10, 0); // Margen derecho 

        HorizontalLayoutGroup layoutGroup = iconContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
        layoutGroup.spacing = padding;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0); // Padding interno del grupo 
        layoutGroup.childControlWidth = true; // Cada icono tendrá su tamaño
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // El tamaño del container lo gestionará el LayoutGroup con un ContentSizeFitter
        ContentSizeFitter containerFitter = iconContainer.AddComponent<ContentSizeFitter>();
        containerFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Altura fija por los hijos

        for (int i = 0; i < iconNames.Length; i++)
        {
            string iconName = iconNames[i];
            Texture2D iconTexture = Resources.Load<Texture2D>("Icons/" + iconName);
          
            if (iconTexture != null)
            {
                GameObject iconGO = new GameObject("Icon_" + iconName);
                //Padre -> IconContainer con el LayoutGroup
                iconGO.transform.SetParent(iconContainer.transform, false);
                iconGO.transform.localScale = Vector3.one;

                Image iconImage = iconGO.AddComponent<Image>();
                Button iconButton = iconGO.AddComponent<Button>();

                Sprite sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                iconImage.sprite = sprite;
                iconImage.preserveAspect = true;

                //LayoutElement controla el tamaño que pide el icono al LayoutGroup
                LayoutElement layoutElement = iconGO.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = iconSize;
                layoutElement.preferredHeight = iconSize;

                // Asociar la acción al botón
                string currentIconName = iconName;
                iconButton.onClick.AddListener(() => OnIconButtonClick(currentIconName));


                //Capturo las referencias a los iconos

                if (currentIconName == "GreenFlag")
                {
                    m_greenFlagIconGO = iconGO;
                }
                else if (currentIconName == "stopFlag")
                {
                    m_stopFlagIconGO = iconGO;
                }

                //Desactivo inicialmente 
                if (currentIconName == "stopFlag")
                {
                    iconGO.SetActive(false);

                }
            }
        }
    }

    /**
     * Descripción: Crea Panel WorkArea, contiene Middle y Right (ocupa el espacio bajo Top y a la derecha de Left)
     */
    private void CreateWorkspacePanels()
    {

        GameObject workAreaPanel = CreatePanel("WorkArea",
             m_CanvasGO.transform,
             new Vector2(0.15f, 0), // Derecha del panel izq,
             new Vector2(1, 0.90f),//  debajo del panel top
             Vector2.zero, Vector2.zero,
             new Vector2(0.5f, 0.5f), Color.clear); // Fondo transparente 

        // Panel Izquierdo del WorkArea 
        GameObject middlePanel = CreatePanel(
            "BlockListPanel",
            workAreaPanel.transform,
            new Vector2(0.0f, 0),   // Anchor: Izquierda-Abajo
            new Vector2(0.3f, 1),   // Anchor: 30% del ancho, Arriba
            Vector2.zero,
            Vector2.zero,
            new Vector2(0.5f, 0.5f), // Pivot Centro
            new Color(0.9f, 0.9f, 0.9f, 1f)); // Gris claro
        m_MiddlePanelRect = middlePanel.GetComponent<RectTransform>();

        ScrollRect scrollRect = middlePanel.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = middlePanel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; // Solo scroll vertical
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            //Debug.Log("Added ScrollRect to MiddlePanel dynamically.", middlePanel);

            scrollRect.viewport = m_MiddlePanelRect;
        }


        if (scrollRect.viewport == null)
        {
            Debug.LogWarning($"ScrollRect en {middlePanel.name} no tenía Viewport asignado. Asignando al propio RectTransform.");
            scrollRect.viewport = m_MiddlePanelRect; // Asegura la asignación
        }

        // Panel Derecho del WorkArea 
        GameObject rightPanel = CreatePanel(
            "CodingAreaPanel",
            workAreaPanel.transform,
            new Vector2(0.3f, 0), // Anchor: Derecha del middle panel, Abajo
            new Vector2(1, 1),    // Anchor: Derecha del todo, Arriba
            Vector2.zero,
            Vector2.zero,
            new Vector2(0.5f, 0.5f), // Pivot Centro
            new Color(0.85f, 0.85f, 0.85f, 1f)); // Gris un poco más oscuro o blanco
        m_RightPanelRect = rightPanel.GetComponent<RectTransform>();

        // Añadir bordes visuales para los paneles
        AddBorder(middlePanel, new Vector2(1, 0), new Vector2(1, 1)); // Borde derecho
        AddBorder(rightPanel, new Vector2(0, 0), new Vector2(0, 1)); // Borde izquierdo 
    }

    /**
     *Descripcón: Configuración de componentes
     * @param middlePanelGO GameObject del panel izquierdo (MiddlePanel)
     * @param rightPanelGO GameObject del panel derecho (RightPanel)
     *//*
    private void SetUpComponents(GameObject middlePanelGO, GameObject rightPanelGO, CategoryController categoryControllerInstance)
    {
        Debug.Log("<color=yellow>UICanvasView: Setting up Components...</color>");

        try
        {
            WorkSpaceModel.WorkspaceOptions options = new WorkSpaceModel.WorkspaceOptions();
            m_WorkspaceModel = new WorkSpaceModel(options);
            Debug.Log($"<color=green>UICanvasView: WorkSpaceModel created (ID: {m_WorkspaceModel.Id}).</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>UICanvasView: Failed to create WorkSpaceModel: {e.Message}</color>");
            return;
        }

        m_WorkSpaceView = rightPanelGO.GetComponent<WorkSpaceView>();
        if (m_WorkSpaceView == null) m_WorkSpaceView = rightPanelGO.AddComponent<WorkSpaceView>();

        m_Toolbox = middlePanelGO.GetComponent<BlockListView>();
        if (m_Toolbox == null) m_Toolbox = middlePanelGO.AddComponent<BlockListView>();

        m_MiddlePanelScrollRect = middlePanelGO.GetComponent<ScrollRect>();
        if (m_MiddlePanelScrollRect == null)
        {
            Debug.LogWarning("Middle Panel (BlockListPanel) is missing ScrollRect component. Adding one.", middlePanelGO);
            m_MiddlePanelScrollRect = middlePanelGO.AddComponent<ScrollRect>();
            m_MiddlePanelScrollRect.horizontal = false;
            m_MiddlePanelScrollRect.vertical = true;
            m_MiddlePanelScrollRect.movementType = ScrollRect.MovementType.Clamped;
            m_MiddlePanelScrollRect.viewport = middlePanelGO.GetComponent<RectTransform>();

            if (middlePanelGO.GetComponent<Mask>() == null) middlePanelGO.AddComponent<Mask>().showMaskGraphic = false;
            Image panelImage = middlePanelGO.GetComponent<Image>();
            if (panelImage == null) panelImage = middlePanelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0);
            panelImage.raycastTarget = true;
        }

        Debug.Log("<color=yellow>UICanvasView: Setting up Components...</color>");
        m_CategoryController = categoryControllerInstance ?? throw new ArgumentNullException(nameof(categoryControllerInstance));
        // CategoryController categoryController = AppController.Instance?.GetComponentInChildren<CategoryController>();

        if (categoryControllerInstance == null)
        {
            Debug.LogError("UICanvasView: CategoryController not found in the scene. Cannot bind Toolbox.");
            return;
        }

        if (m_WorkspaceModel != null && m_Toolbox != null && m_RightPanelRect != null && m_WorkSpaceView != null)
        {
            Debug.Log($"<color=yellow>UICanvasView: Binding WorkSpaceView ({m_WorkSpaceView.GetInstanceID()}) to Model...</color>");
            m_WorkSpaceView.BindModel(
                m_WorkspaceModel,
                m_Toolbox,
                m_RightPanelRect,
                null
             );
            Debug.Log("<color=green>UICanvasView: WorkSpaceView bound to Model.</color>");

            RectTransform categoryButtonContainer = m_CategoryButtonContainerRect;

            ToolboxConfig toolboxConfig = m_ToolboxConfiguration;

            if (m_Toolbox != null && m_WorkspaceModel != null && toolboxConfig != null && m_WorkSpaceView != null && categoryButtonContainer != null && m_MiddlePanelScrollRect != null)
            {
                Debug.Log("<color=yellow>UICanvasView: Calling InitializeToolbox on BlockListView...</color>");
                m_Toolbox.InitializeToolbox(
                    m_WorkspaceModel,
                    toolboxConfig,
                    m_WorkSpaceView,
                    categoryButtonContainer,
                    m_MiddlePanelScrollRect,
                    categoryButtonPrefab,
                    m_CategoryController
                );
                Debug.Log("<color=green>UICanvasView: BlockListView Toolbox Initialized.</color>");
            }
            else
            {
                string errorMsg = "UICanvasView: Cannot bind WorkSpaceView due to missing refs: ";
                if (m_WorkspaceModel == null) errorMsg += "WorkspaceModel ";
                if (m_Toolbox == null) errorMsg += "Toolbox ";
                if (m_RightPanelRect == null) errorMsg += "RightPanelRect ";
                if (m_WorkSpaceView == null) errorMsg += "WorkspaceView ";
                Debug.LogError(errorMsg);
                enabled = false;

            }

        }
    }*/

    /**
     * Descripción: Añade un borde al panel indicado
     * @param panel GameObject al que se le añadirá el borde
     * @param anchorMin Ancla mínimo del borde
     * @param anchorMax Ancla máximo del borde
     */
    void AddBorder(GameObject panel, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject border = new GameObject("Border");
        border.transform.SetParent(panel.transform, false);
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        if (anchorMin.x == anchorMax.x) // Línea vertical
        {
            rect.offsetMin = new Vector2(-1, 0); // -1  a la izq/der del anchor
            rect.offsetMax = new Vector2(1, 0); // +1  grosor 2
            rect.sizeDelta = new Vector2(2, 0); // Grosor 2, altura stretch
        }
        else // Línea horizontal
        {
            rect.offsetMin = new Vector2(0, -1);
            rect.offsetMax = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(0, 2); // Grosor 2, anchura stretch
        }
        rect.localScale = Vector3.one;

        Image img = border.AddComponent<Image>();
        img.color = Color.gray;
        img.raycastTarget = false; // no necesita interactuar con él
    }

    /**
     * Método que se ejecuta al hacer clic en un icono de la interfaz
     * @param iconName Nombre del icono pulsado
     */
    private void OnIconButtonClick(string iconName)
    {
        // Actualizar el estado visual de la UI dentro de UICanvasView
        if (iconName == "GreenFlag")
        {
            SetUISimulationState(true); // Entrar en modo simulación
        }
        else if (iconName == "stopFlag")
        {
            SetUISimulationState(false); // Volver a modo edición
        }

        var actions = new System.Collections.Generic.Dictionary<string, System.Action>
        {
           { "GreenFlag", () => {
               AppController.Instance.TriggerExecution();
               return;
           }},
            { "load_icon", () => {
                AppController.Instance.TriggerLoad();
                return;
            }},
            { "save_icon", () => {

                AppController.Instance.TriggerSave();
                return;
            }},
            { "stopFlag", () => {

                AppController.Instance.TriggerStop();
                return;
            }}
        };

        if (actions.TryGetValue(iconName, out var action))
        {
            action.Invoke();
        }
        else
        {
            Debug.Log("Acción no definida para: " + iconName);
            return;
        }
    }

    /**
     * Método que crea el panel izquierdo (Categorías) y lo configura
     * @param parent GameObject padre del panel
     * @return GameObject del panel creado
     */
    GameObject SetupLeftPanel(GameObject parent)
    {
        GameObject leftPanel = CreatePanel("CategoriesPanel", parent.transform,
            new Vector2(0, 0), new Vector2(0.15f, 0.95f), // Abajo-Izquierda hasta 15% ancho, debajo TopPanel
            Vector2.zero, Vector2.zero,
            new Vector2(0, 1), // Pivot Arriba-Izquierda
            new Color(0.95f, 0.95f, 0.95f, 1f));

        m_CategoriesPanelGO = leftPanel; //Capturo la referencia del panel izquierdo

        // Añadir ScrollRect al LeftPanel directamente
        ScrollRect scrollRect = leftPanel.AddComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // El ContentPanel donde irán las categorías
        GameObject contentPanel = new GameObject("CategoryContent");
        contentPanel.transform.SetParent(leftPanel.transform, false);

        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); // Arriba-Izquierda
        contentRect.anchorMax = new Vector2(1, 1); // Arriba-Derecha (Stretch Horizontal)
        contentRect.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro

        // Layout Vertical para los botones de categoría
        VerticalLayoutGroup layoutGroup = contentPanel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(5, 5, 40, 5);
        layoutGroup.spacing = 40f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentPanel.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Asignar contenido y máscara 
        scrollRect.content = contentRect;

        Image panelImage = leftPanel.GetComponent<Image>() ?? leftPanel.AddComponent<Image>();
        panelImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        return contentPanel;
    }

    /**
     * Descripción: Método llamado por CategoryLoader cuando se hace clic en una categoría
     * @param: categoryName Nombre de la categoría seleccionada
     * @param: categoryColor Color de la categoría seleccionada
     */
    public void UpdateMiddlePanel(string categoryName, Color categoryColor)
    {
        if (m_Toolbox != null)
        {
            m_Toolbox.ShowBlockCategory(categoryName, categoryColor);

        }
        else
        {
            Debug.LogWarning($"UpdateMiddlePanel called for category '{categoryName}', but Toolbox (BlockScrollList) is not initialized.");
        }
    }

    /**
     * Descripción: Funcion de guardado del workspace
     */
    public void SaveWorkspace()
    {
        if (m_WorkspaceModel == null) return;

        Debug.Log("<color=blue>UICanvasView: Saving workspace...</color>");
        try
        {
            var workspaceXml = Xml.WorkspaceToDom(m_WorkspaceModel);
            string xmlText = Xml.DomToText(workspaceXml);
            PlayerPrefs.SetString("SavedWorkspace_UICanvasManagerView", xmlText);
            PlayerPrefs.Save();
            Debug.Log("<color=green>UICanvasView: Workspace saved to PlayerPrefs.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>UICanvasView: Error saving workspace: {e.Message}\n{e.StackTrace}</color>");
        }
    }

    /**
     * Descripción: Carga el workspace desde PlayerPrefs
     */
    public void LoadWorkspace()
    {
        Debug.LogError("!!!!!!!! UICanvasView.LoadWorkspace() CALLED !!!!!!!!");

        if (m_WorkspaceModel == null || m_WorkSpaceView == null) return;

        string savedXml = PlayerPrefs.GetString("SavedWorkspace_UBlockly", "");
        if (!string.IsNullOrEmpty(savedXml))
        {
            Debug.Log("<color=blue>UICanvasView: Loading workspace from PlayerPrefs...</color>");
            try
            {
                m_WorkspaceModel.Clear();
                m_WorkSpaceView.CleanViews();

                //Cargar XML al Modelo vacío
                var xmlDoc = Xml.TextToDom(savedXml);
                Xml.DomToWorkspace(xmlDoc, m_WorkspaceModel);
                m_WorkSpaceView.BuildViews();
                m_WorkspaceModel.UpdateProcedureDB();
                m_WorkspaceModel.UpdateVariableStore(true); //Limpia las variables no usadas en el XML

                Debug.Log("<color=green>UICanvasView: Workspace loaded successfully.</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>UICanvasView: Error loading workspace from XML: {e.Message}\n{e.StackTrace}</color>");
                m_WorkspaceModel?.Clear();
                m_WorkSpaceView?.CleanViews();
            }
        }
        else
        {
            Debug.Log("UICanvasView: No saved workspace found in PlayerPrefs.");
        }
    }

    /**
     * Descripción: Guardar al salir
     */
    void OnApplicationQuit()
    {
        SaveWorkspace();
    }

    /**
     * Descripción: Limpieza al destruir el objeto
     */
    void OnDestroy()
    {
        Debug.Log("<color=orange>UICanvasView: OnDestroy</color>");

        m_WorkSpaceView?.Dispose();
        m_WorkspaceModel?.Dispose();
        ScratchBlocks.Dispose();
    }


    private ToolboxConfig LoadToolboxConfigFromXml(string resourcePath)
    {
        TextAsset xmlAsset = Resources.Load<TextAsset>(resourcePath);
        if (xmlAsset == null)
        {
            Debug.LogError($"UICanvasView: Could not load toolbox XML at Resources/{resourcePath}");
            return null;
        }

        try
        {
            ToolboxConfig config = new ToolboxConfig();
            config.BlockCategoryList = new List<ToolboxBlockCategory>();

            XDocument xDoc = XDocument.Parse(xmlAsset.text);
            XElement toolboxElement = xDoc.Root;
            if (toolboxElement == null || toolboxElement.Name.LocalName != "toolbox")
            {
                Debug.LogError("Invalid toolbox XML root. Expected <toolbox>.");
                return null;
            }

            config.Style = toolboxElement.Attribute("style")?.Value;

            foreach (XElement categoryElement in toolboxElement.Elements("category"))
            {
                ToolboxBlockCategory category = new ToolboxBlockCategory();
                category.CategoryName = categoryElement.Attribute("name")?.Value;
                category.Custom = categoryElement.Attribute("custom")?.Value; // Para Variables/Procedimientos

                if (string.IsNullOrEmpty(category.CategoryName))
                {
                    Debug.LogWarning("Skipping category in toolbox.xml with missing 'name' attribute.");
                    continue;
                }

                category.BlockList = new List<string>();
                foreach (XElement blockElement in categoryElement.Elements("block"))
                {
                    string blockType = blockElement.Attribute("type")?.Value;
                    if (!string.IsNullOrEmpty(blockType))
                    {
                        category.BlockList.Add(blockType);
                    }
                }
                config.BlockCategoryList.Add(category);
            }
            return config;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing toolbox XML '{resourcePath}': {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /**
     * Descripción: Inicializa los colores de las categorías desde el XML
     * @param config Configuración de la Toolbox
     */
    private void InitializeCategoryColors(ToolboxConfig config)
    {
        // LLamada a la versión estática correcta
        var colorMap = CategoryLoader.LoadCategoryInfo();

       // Debug.Log($"UICanvasView: Category colors loaded. Found {colorMap.Count} colors.");

        if (config?.BlockCategoryList == null) return;

        foreach (var category in config.BlockCategoryList)
        {

            if (colorMap.TryGetValue(category.CategoryName, out Color color))
            {
                category.Init(color); // Asigna el color cargado a la categoría del toolbox
            }
            else
            {
                Debug.LogWarning($"Color for category '{category.CategoryName}' not found in Categories.xml. Using default grey.");
                category.Init(Color.grey);
            }
        }
    }

    //Debugging blockConnectionDB mediante un botón que obtiene la misma

    /// <summary>
    /// Exporta el estado actual de las bases de datos de conexión del workspace a un archivo JSON para depuración.
    /// Este método será llamado por un evento de UI (ej: Click de un botón).
    /// </summary>
    public void OnClickExportConnectionDBsButton()
    {
        if (m_WorkspaceController != null)
        {
            // Añadimos un timestamp al nombre para no sobreescribir exports viejos.
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"ConnectionDB_Debug_State_{timestamp}";
            m_WorkspaceController.ExportConnectionDBState(filename);
            Debug.Log($"Exporting DB state triggered. File: {filename}.json");
        }
        else
        {
            Debug.LogError("UICanvasView: WorkspaceController reference is null. Cannot export DB state.");
        }
    }

    /// <summary>
    /// Crea y añade el botón "Export DBs" al contenedor de elementos de la izquierda de la barra superior.
    /// Se llama desde CreateTopPanel.
    /// </summary>
    /// <param name="parentContainer">El RectTransform contenedor donde se añadirá el botón.</param>
    private GameObject AddExportDebugButtonToPanel(RectTransform parentContainer)
    {
        if (parentContainer == null)
        {
            Debug.LogError("UICanvasView: Cannot add debug button, parent container is null.");
            return null;
        }

        // Creo GO del botón
        GameObject debugButtonGO = new GameObject("ExportDebugDBButton");
        debugButtonGO.transform.SetParent(parentContainer.transform, false); 

        // Añado componentes UI: Image y Button
        Image debugButtonImage = debugButtonGO.AddComponent<Image>();
        Button debugButtonButton = debugButtonGO.AddComponent<Button>();

        debugButtonImage.color = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Color rojo para configurarlo

        // Añado Texto como hijo del botón
        GameObject debugButtonTextGO = new GameObject("Text");
        debugButtonTextGO.transform.SetParent(debugButtonGO.transform, false);
        Text debugButtonText = debugButtonTextGO.AddComponent<Text>();
        debugButtonText.text = "Export DBs";
        debugButtonText.font = Font.CreateDynamicFontFromOSFont("Arial", 18); // Fuente por defecto 
        debugButtonText.fontSize = 18;
        debugButtonText.fontStyle = FontStyle.Bold; // Negrita para que se vea
        debugButtonText.alignment = TextAnchor.MiddleCenter; // Texto centrado en el botón
        debugButtonText.color = Color.white; // Color del texto

        // Ajusto RectTransform del texto para que llene el área del botón
        RectTransform debugButtonTextRect = debugButtonTextGO.GetComponent<RectTransform>();
        debugButtonTextRect.anchorMin = new Vector2(0, 0);
        debugButtonTextRect.anchorMax = new Vector2(1, 1);
        debugButtonTextRect.offsetMin = new Vector2(5, 5); // Margen interno del texto respecto al borde del botón
        debugButtonTextRect.offsetMax = new Vector2(-5, -5);

        LayoutElement debugButtonLayout = debugButtonGO.AddComponent<LayoutElement>();
        debugButtonLayout.preferredWidth = 150f; 
        if (parentContainer.GetComponent<HorizontalLayoutGroup>() != null && parentContainer.GetComponent<ContentSizeFitter>()?.verticalFit == ContentSizeFitter.FitMode.PreferredSize)
        {
            debugButtonLayout.preferredHeight = 10f; 
        }
        else
        {
            
            debugButtonLayout.minHeight = 15f; 

        }
     
        debugButtonButton.onClick.AddListener(OnClickExportConnectionDBsButton);

        return debugButtonGO;
    }


    /// <summary>
    /// Establece el estado de la UI para el modo de simulación.
    /// Oculta paneles de codificación y cambia los iconos de bandera.
    /// </summary>
    /// <param name="isSimulating">True para activar el modo simulación; False para desactivarlo y volver al modo edición.</param>
    public void SetUISimulationState(bool isSimulating)
    {
        Debug.Log($"UICanvasView: Cambiando estado de UI a modo {(isSimulating ? "Simulación" : "Edición")}");

        // Alternar visibilidad de los iconos de bandera
        if (m_greenFlagIconGO != null)
        {
            m_greenFlagIconGO.SetActive(!isSimulating);
        }
        if (m_stopFlagIconGO != null)
        {
            m_stopFlagIconGO.SetActive(isSimulating);
        }

        // Ocultar/Mostrar paneles principales de la UI (excepto el Top Panel)
        // Panel de Categorías (Izquierda)
        if (m_CategoriesPanelGO != null)
        {
            m_CategoriesPanelGO.SetActive(!isSimulating);
            Debug.Log($"CategoriesPanel.SetActive: {m_CategoriesPanelGO.activeSelf}");
        }
        else
        {
            Debug.LogWarning("UICanvasView: Referencia a 'm_CategoriesPanelGO' es nula. No se puede alternar visibilidad.");
        }

        // Panel de Lista de Bloques (Medio)
        if (BlockListPanelRect != null)
        {
            BlockListPanelRect.gameObject.SetActive(!isSimulating);
            Debug.Log($"BlockListPanel.SetActive: {BlockListPanelRect.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("UICanvasView: Referencia a 'BlockListPanelRect' es nula. No se puede alternar visibilidad.");
        }

        // Panel de Área de Codificación (Derecho)
        if (CodingAreaPanelRect != null)
        {
            CodingAreaPanelRect.gameObject.SetActive(!isSimulating);
            Debug.Log($"CodingAreaPanel.SetActive: {CodingAreaPanelRect.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("UICanvasView: Referencia a 'CodingAreaPanelRect' es nula. No se puede alternar visibilidad.");
        }

        // Capa de Arrastre (DragLayer)
        
        if (m_DragLayerRect != null)
        {
            m_DragLayerRect.gameObject.SetActive(!isSimulating);
            Debug.Log($"DragLayerRect.SetActive: {m_DragLayerRect.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("UICanvasView: Referencia a 'm_DragLayerRect' es nula. No se puede alternar visibilidad.");
        }
    }

}//Fin clase UICanvasView