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

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]

public class UICanvasView : MonoBehaviour
{
    [Header("Configuración Visual añadir en el Inspector")]
    public string logoSpriteName; // Nombre del sprite para el logo 
    public string[] topIconNames; // Nombres de sprites para iconos (GreenFlag, StopFlag, Save...)

    private WorkSpaceModel m_WorkspaceModel; // Modelo lógico de area de trabajo
    private WorkSpaceView m_WorkSpaceView; // Vista  del área de código
    private BlockListView m_Toolbox;  // Contenedor de bloques (Toolbox) en el panel izquierdo (MiddlePanel)
    private ToolboxConfig m_ToolboxConfiguration;
    [SerializeField] private GameObject categoryButtonPrefab; // Prefab  botón de categoría
    [SerializeField] private GameObject BlockViewPrefab; // Prefab base
    private RectTransform m_CategoryButtonContainerRect; //Contenedor de botones
    private ScrollRect m_MiddlePanelScrollRect;
    public WorkSpaceModel Workspace => m_WorkspaceModel;
    public WorkSpaceView WorkSpaceView => m_WorkSpaceView;
    public BlockListView Toolbox => m_Toolbox;

    private GameObject m_CanvasGO; 
    private GameObject m_UiManagerView; 
    private RectTransform m_RightPanelRect;  
    private RectTransform m_MiddlePanelRect; 
    //private Transform m_blockContainerInToolbox;  
    private Dictionary<string, Color> mCategoryColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase); 

    //Dimensiones estimadas de la pantalla
    private const int m_screenWidth = 1200;
    private const int m_screenHeight = 720;

    void Awake()
    {
        Debug.Log("<color=cyan>UICanvasView: Awake starting UI Setup...</color>");

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

        if (m_RightPanelRect != null && m_MiddlePanelRect != null)
        {
            SetUpComponents(m_MiddlePanelRect.gameObject, m_RightPanelRect.gameObject); 
        }
        else
        {
            Debug.LogError("UICanvasView: Failed to create Middle or Right panels. Cannot setup UBlockly.");
            enabled = false;
            return;
        }

        Debug.Log("<color=green>UICanvasView: Awake finished UI and  base setup.</color>");
    }

    /**
     * Descripción: Inicializa el núcleo y carga los recursos necesarios.
     */
    private void InitializeCore()
    {
        Debug.Log("<color=yellow>UICanvasView: Initializing Core...</color>");
        try
        {
            BlockResMgr resMgr = BlockResMgr.Get();
            if (resMgr==null)
            {
                Debug.LogWarning("BlockResMgr not ready, attempting synchronous load...");
                
            }

            ScratchBlocks.Init();

            Debug.Log("<color=teal>UICanvasView: Calling BlockDataLoader.LoadAllDefinitions()...</color>");
            BlockDataLoader.LoadAllDefinitions(); 
            Debug.Log("<color=teal>UICanvasView: BlockDataLoader.LoadAllDefinitions() finished.</color>");
            Debug.Log("<color=green>UICanvasView: ScratchBlocks.Init() successful.</color>");
            
            Debug.Log("<color=teal>UICanvasView: Loading Toolbox Configuration...</color>");

            m_ToolboxConfiguration = ToolboxConfig.Load();
            if (m_ToolboxConfiguration == null)
            {
                Debug.LogError("UICanvasView: FAILED to load Toolbox Configuration!");
                m_ToolboxConfiguration = new ToolboxConfig { BlockCategoryList = new List<ToolboxBlockCategory>() }; 
            }
            else
            {
                Debug.Log($"<color=green>UICanvasView: Toolbox Configuration loaded successfully. Style: {m_ToolboxConfiguration.Style}, Categories: {m_ToolboxConfiguration.BlockCategoryList?.Count ?? 0}</color>");
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
        Debug.Log("<color=yellow>UICanvasView: Creating UIManagerView...</color>");
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
        Debug.Log("<color=yellow>UICanvasView: Creating Canvas...</color>");
        
        m_CanvasGO = new GameObject("Canvas");
        m_CanvasGO.transform.SetParent(this.m_UiManagerView.transform);

        Canvas canvas = m_CanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = m_CanvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(m_screenWidth, m_screenHeight);
        canvasScaler.matchWidthOrHeight = 1f;

        //Detección de eventos de UI en el canvas necesario para el Drag and Drop
        m_CanvasGO.AddComponent<GraphicRaycaster>();
    }

    /**
     * Descripción: Crea el panel izquierdo (Categorías) y lo configura
     */
    private void CreateTopPanel()
    {
        Debug.Log("<color=yellow>UICanvasView: Creating Top Panel...</color>");

        //Creación del panel de herramientas superior
        GameObject topPanel = CreatePanel("Tools Panel", this.m_CanvasGO.transform,
            new Vector2(0, 0.90f), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            new Vector2(0, 0), new Color(0.6f, 0.4f, 1f, 1f));

        // Asignación altura fija 
        topPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

        Canvas topCanvas = topPanel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 1000;

        if (!string.IsNullOrEmpty(logoSpriteName))
        {
            this.AddLogoToPanel(topPanel, logoSpriteName);
        }

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

    /** Añade un logo al panel indicado
   * @param panel GameObject al que se le añadirá el logo
   * @param spriteName Nombre del sprite a añadir
   */
    void AddLogoToPanel(GameObject panel, string spriteName)
    {
        //Debug.Log("Cargando sprite desde Resources: " + spriteName);
        Texture2D logoTexture = Resources.Load<Texture2D>("Icons/" + spriteName);

        if (logoTexture != null)
        {
            // Debug.Log("Texture2D encontrada, creando objeto Image en TopPanel.");
            GameObject logo = new GameObject("TopPanelLogo");
            logo.transform.SetParent(panel.transform);
            RectTransform logoRect = logo.AddComponent<RectTransform>();
            Image logoImage = logo.AddComponent<Image>();

            // Convertir Texture2D a Sprite
            Sprite sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImage.sprite = sprite;

            // Alinear el logo a la izquierda con un margen de 10px
            logoRect.anchorMin = new Vector2(0, 0.5f);
            logoRect.anchorMax = new Vector2(0, 0.5f);
            logoRect.pivot = new Vector2(0, 0.5f);
            logoRect.sizeDelta = new Vector2(panel.GetComponent<RectTransform>().rect.width * 0.05f, panel.GetComponent<RectTransform>().rect.height * 0.5f);
            logoRect.anchoredPosition = new Vector2(50, 0); // Desplazamiento de 10px a la izquierda

            // Debug.Log("Logo agregado correctamente en el Panel");
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
        float iconSize = panelHeight * 0.7f; // Iconos un poco más pequeños que la barra REVISAR no se muestran bien
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
        layoutGroup.childControlWidth = false; // Cada icono tendrá su tamaño
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // El tamaño del container lo gestionará el LayoutGroup con un ContentSizeFitter
        ContentSizeFitter containerFitter = iconContainer.AddComponent<ContentSizeFitter>();
        containerFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        containerFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // Altura fija por los hijos

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

                //Desactivo inicialmente 
                if (currentIconName == "stopFlag")
                {
                    iconGO.SetActive(false);

                }
               /* else
                {
                    Debug.LogError("UICanvasView: Icon texture not found in Resources/Icons/: " + iconName);
                }*/
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
            Debug.Log("Added ScrollRect to MiddlePanel dynamically.", middlePanel);
        }

        //Da problemas de render
        /*Mask mask = middlePanel.GetComponent<Mask>();
        if (mask == null)
        {
            mask = middlePanel.AddComponent<Mask>();
            mask.showMaskGraphic = false; // No mostrar el fondo del panel como máscara
            Debug.Log("Added Mask to MiddlePanel dynamically.", middlePanel);
        }*/

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
        AddBorder(rightPanel, new Vector2(0,0), new Vector2(0,1)); // Borde izquierdo 
    }

    /**
     *Descripcón: Configuración de componentes
     * @param middlePanelGO GameObject del panel izquierdo (MiddlePanel)
     * @param rightPanelGO GameObject del panel derecho (RightPanel)
     */
    private void SetUpComponents(GameObject middlePanelGO, GameObject rightPanelGO)
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
                    m_MiddlePanelScrollRect
                );
                Debug.Log("<color=green>UICanvasView: BlockListView Toolbox Initialized.</color>");
            }
            else
            {
                //  Debug.LogError("UICanvasView: Cannot bind WorkSpaceView, WorkSpaceModel is null!");

                string errorMsg = "UICanvasView: Cannot bind WorkSpaceView due to missing refs: ";
                if (m_WorkspaceModel == null) errorMsg += "WorkspaceModel ";
                if (m_Toolbox == null) errorMsg += "Toolbox ";
                if (m_RightPanelRect == null) errorMsg += "RightPanelRect ";
                if (m_WorkSpaceView == null) errorMsg += "WorkspaceView ";
                Debug.LogError(errorMsg);
                enabled = false;

            }

        }
    }

    /**
     * Descripción: Nos devuelve el Transform creado
     * @param: panel GameObject del panel donde se añadirá el contenedor
     * @param: blockScrollListRef Referencia al BlockScrollListView
     * @return: Transform del contenedor creado
     */
    public Transform CreateBlockContainer(GameObject panel, BlockListView blockScrollListRef)
    {
        GameObject blockContainer = new GameObject("BlockContainer");
        blockContainer.transform.SetParent(panel.transform, false);

        //Imagen para que ScrollRect detecte el contenido
        Image bgImage = blockContainer.AddComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0); 
        bgImage.raycastTarget = true; 

        RectTransform contentRect = blockContainer.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); // Ancla Arriba-Izquierda
        contentRect.anchorMax = new Vector2(1, 1); // Ancla Arriba-Derecha 
        contentRect.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro
        contentRect.sizeDelta = new Vector2(0, 300); // Altura inicial

        VerticalLayoutGroup layoutGroup = blockContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(5, 5, 5, 5); 
        layoutGroup.spacing = 5f;                      // Espacio entre bloques
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;          
        layoutGroup.childControlHeight = false;         
        layoutGroup.childForceExpandWidth = false;      
        layoutGroup.childForceExpandHeight = false;     

        ContentSizeFitter fitter = blockContainer.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Ajusta altura al contenido
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Ancho controlado por el padre/anchors

        //ScrollRect en el Panel Padre (MiddlePanel)
        ScrollRect scrollRect = panel.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;       // Asigna el contenedor como contenido
        scrollRect.horizontal = false;          // Scroll solo vertical
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        //Máscara para que el contenido no se salga del panel
        Mask mask = panel.GetComponent<Mask>();
        if (mask == null)
            mask = panel.AddComponent<Mask>();
        mask.showMaskGraphic = false; // No se muestra la imagen del panel como máscara visual

        Image panelImage = panel.GetComponent<Image>(); 
        if (panelImage == null) panelImage = panel.AddComponent<Image>();
        
        //panelImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
      
      
        Debug.Log($"BlockContainer created inside {panel.name}");
        return blockContainer.transform;
    }

    /**
     * Añade un borde al panel indicado
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
        img.raycastTarget = false; // no necesita interactuar
    }

    /**
     * Método que se ejecuta al hacer clic en un icono de la interfaz
     * @param iconName Nombre del icono pulsado
     */
    private void OnIconButtonClick(string iconName) //TODO REVISAR EL  PlayControlView m_PlayControlView 
    {
        var actions = new System.Collections.Generic.Dictionary<string, System.Action>
        {
           { "GreenFlag", () => {
                Debug.Log("Ejecutar acción de inicio");
               return;
           }},
            { "load_icon", () => {
                Debug.Log("Cargar datos");
                return;
            }},
            { "save_icon", () => {
                Debug.Log("Guardar datos");
                return;
            }},
            { "stopFlag", () => {
                Debug.Log("Detener ejecución");
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
       // contentRect.sizeDelta = new Vector2(0, 0);

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

      /*  Mask mask = leftPanel.GetComponent<Mask>() ?? leftPanel.AddComponent<Mask>();
        mask.showMaskGraphic = false;*/
        Image panelImage = leftPanel.GetComponent<Image>() ?? leftPanel.AddComponent<Image>();
        panelImage.color = new Color(0.95f, 0.95f, 0.95f, 1f); 
      
        return contentPanel; 
    }

    /**
     * Descripción: Método que carga las categorías desde un XML y las añade al panel indicado
     * @param contentPanel GameObject del panel donde se cargarán las categorías
     */
    void LoadCategories(GameObject contentPanel)
    {
        if (contentPanel == null)
        {
            Debug.LogError("UICanvasView: Category ContentPanel is null, cannot load categories.");
            return;
        }

        CategoryLoader categoryLoader = GetComponent<CategoryLoader>();
        if (categoryLoader == null)
        {
            categoryLoader = gameObject.AddComponent<CategoryLoader>();
        }

        categoryLoader.contentPanel = contentPanel; // Panel con VerticalLayoutGroup
        categoryLoader.xmlFileName = "XML/Categories"; 
        categoryLoader.categoryPrefab = Resources.Load<GameObject>("Prefabs/CategoryPrefab");
        categoryLoader.uiCanvasManageViewr = this; // Pasa la referencia a este manager

        if (categoryLoader.categoryPrefab == null)
        {
            Debug.LogError("UICanvasView: CategoryPrefab not found at 'Prefabs/CategoryPrefab'");
            return;
        }

        categoryLoader.LoadCategoriesFromXML();
        Debug.Log("<color=green>UICanvasView: Initiated category loading.</color>");

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
        //ScratchBlocks.Dispose(); //REVISAR 
    }

    /**
     * Descripción: Crea y configura el contenedor de bloques 
     * @param scrollRectPanel GameObject del panel que tendrá el ScrollRect
     * @return Transform del contenedor creado
     */
    public Transform CreateAndConfigureBlockContainer(GameObject scrollRectPanel) 
    {
        GameObject blockContainer = new GameObject("BlockContainer");
        blockContainer.transform.SetParent(scrollRectPanel.transform, false);

        Image bgImage = blockContainer.AddComponent<Image>();
        bgImage.color = Color.clear; 
        bgImage.raycastTarget = false; 

        RectTransform contentRect = blockContainer.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1); // Arriba, Stretch Horizontal
        contentRect.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro
        contentRect.sizeDelta = new Vector2(0, 0); // Altura y Ancho controlados por Layout/Fitter

        // Layout Vertical para los bloques plantilla 
        VerticalLayoutGroup layoutGroup = blockContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);
        layoutGroup.spacing = 5f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true; // Ancho de hijos controlado
        layoutGroup.childControlHeight = false; // Altura de hijos propia
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // ContentSizeFitter para ajustar la altura del contenedor
        ContentSizeFitter fitter = blockContainer.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        //Configuración el ScrollRect en el Panel Padre
        ScrollRect scrollRect = scrollRectPanel.GetComponent<ScrollRect>(); 
        if (scrollRect == null)
        {
            //Debug.LogError($"CreateAndConfigureBlockContainer: ScrollRect not found on parent panel '{scrollRectPanel.name}'. Adding one, but check setup.");
            scrollRect = scrollRectPanel.AddComponent<ScrollRect>();
           
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        scrollRect.content = contentRect; //Asignación del contenedor creado como contenido
        Debug.Log($"BlockContainer created and assigned to ScrollRect in {scrollRectPanel.name}");
        return blockContainer.transform; 
    }

}//Fin clase UICanvasView

