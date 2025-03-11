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
 * Versión: 1.0.0
 * 
 * Descripción: Clase que genera la vista de la interfaz de usuario de scratch
 */


using UnityEngine;
using UnityEngine.UI;

public class UICanvasManager : MonoBehaviour
{
    public string logo;
    public string[] iconNames;
    private WorkSpace m_workSpace; // Espacio de trabajo
    private BlockScrollList m_blockScrollList;
    private GameObject m_CanvasGO; //Canvas principal que incluye a todos los paneles
    private GameObject m_UIManager; //UIManager que contiene el Canvas principal y todos los paneles de la interfaz
    private Transform m_rightPanelTransform; // Referencia al RightPanel
    private WorkSpaceView m_workSpaceView;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.InitializeUIManager();
        this.InitializeCanvas();
        this.CreateTopPanel();
        GameObject leftPanel = SetupLeftPanel(m_CanvasGO);
        this.LoadCategories(leftPanel);
        this.CreateWorkspace();


    }

    public WorkSpace WorkSpace()
    {
        return m_workSpace;
    }

    private void InitializeUIManager()
    {
        this.m_UIManager = GameObject.Find("UIManager");
        if (this.m_UIManager == null)
        {
            this.m_UIManager = new GameObject("UIManager");
        }
    }

    private void InitializeCanvas()
    {
        //Creación del canvas donde se van a presentar todos los paneles
        this.m_CanvasGO = new GameObject("Canvas");
        this.m_CanvasGO.transform.SetParent(m_UIManager.transform);

        Canvas canvas = this.m_CanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = this.m_CanvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720);
        canvasScaler.matchWidthOrHeight = 1f;
        this.m_CanvasGO.AddComponent<GraphicRaycaster>();
    }

    private void CreateTopPanel()
    {
        //Creación del panel de herramientas superior
        GameObject topPanel = CreatePanel("Tools Panel", m_CanvasGO.transform,
            new Vector2(0, 0.95f), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            new Vector2(0, 0), new Color(0.6f, 0.4f, 1f, 1f));

        Canvas topCanvas = topPanel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 1000;

        if (!string.IsNullOrEmpty(logo))
        {
            this.AddLogoToPanel(topPanel, logo);
        }

        if (iconNames != null && iconNames.Length > 0)
        {
            this.AddIconsToPanel(topPanel, iconNames);
        }
    }

    private void CreateWorkspace()
    {
        //Creación del panel de trabajo donde se van a mostrar el resto de paneles
        GameObject workSpaceGO = new GameObject("WorkSpace");
        workSpaceGO.transform.SetParent(m_CanvasGO.transform, false);
        workSpaceGO.AddComponent<CanvasRenderer>();

        RectTransform workSpaceRect = workSpaceGO.AddComponent<RectTransform>();
        workSpaceRect.anchorMin = new Vector2(0.15f, 0);
        workSpaceRect.anchorMax = new Vector2(1, 0.95f);
        workSpaceRect.offsetMin = Vector2.zero;
        workSpaceRect.offsetMax = Vector2.zero;
        workSpaceRect.pivot = new Vector2(0.5f, 0.5f);

        this.m_workSpace = workSpaceGO.AddComponent<WorkSpace>();
        this.m_workSpaceView = workSpaceGO.AddComponent<WorkSpaceView>();


        BlockStatusView statusView = workSpaceGO.AddComponent<BlockStatusView>(); //Añado el script BlockStatusView al WorkSpace
        //Panel para la lista de bloques y su representación en el espacio de trabajo al seleccionar una categoría
        GameObject middlePanel = CreatePanel(
            "BlockListPanel", 
            workSpaceGO.transform,
            new Vector2(0.0f, 0), 
            new Vector2(0.4f, 1), 
            Vector2.zero, 
            Vector2.zero,
            new Vector2(0.5f, 0.5f), 
            new Color(0.976f, 0.976f, 0.976f, 1f));

        RectTransform middlePanelRect = middlePanel.GetComponent<RectTransform>();

      //  Debug.Log($"MiddlePanel size: {middlePanelRect.sizeDelta}");
      /*
        if (middlePanel != null)
        {
            Debug.Log($"MiddlePanel tamaño: {middlePanelRect.sizeDelta}");
        }
        else
        {
            Debug.LogError("MiddlePanel no tiene un RectTransform asignado");
        }*/

        //área de codificación donde se arrastran los bloques para su conexión y posterior ejecución
        GameObject rightPanel = CreatePanel("CodingArea", workSpaceGO.transform,
            new Vector2(0.4f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            new Vector2(0.5f, 0.5f), new Color(0.976f, 0.976f, 0.976f, 1f));

        m_rightPanelTransform = rightPanel.transform; // Guardamos la referencia al Transform del RightPanel

        this.m_blockScrollList = middlePanel.AddComponent<BlockScrollList>();

       /* GameObject blockPrefab = Resources.Load<GameObject>("Prefabs/BlocksPrefab");
        if (blockPrefab == null)
        {
            Debug.LogError("No se pudo cargar el prefab en 'Prefabs/BlocksPrefab'. Verifica la ruta y existencia del archivo.");
            blockPrefab = new GameObject("FallbackBlockPrefab");
            blockPrefab.AddComponent<RectTransform>();
            blockPrefab.AddComponent<Image>();
        }
       */
        this.m_blockScrollList.Initialized(Resources.Load<GameObject>("Prefabs/BlockPrefab"), middlePanel.transform);

        this.m_blockScrollList.SetWorkspaceTransform(m_rightPanelTransform);
        this.m_blockScrollList.SetWorkSpace(m_workSpace);

        WorkSpace wsComponent = m_workSpace.GetComponent<WorkSpace>();
        wsComponent.Initialized(middlePanel, rightPanel);

        CreateBlockContainer(middlePanel, m_blockScrollList);
    }

    /**
     * Crea un panel con los parámetros indicados
     * @param name Nombre del panel
     * @param parent Transform del padre
     * @param anchorMin Posición mínima del ancla
     * @param anchorMax Posición máxima del ancla
     * @param offsetMin Offset mínimo
     * @param offsetMax Offset máximo
     * @param pivot Punto de pivote
     * @param color Color del panel
     * @return GameObject con el panel creado
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

    public void CreateBlockContainer(GameObject panel, BlockScrollList m_blockScrollList)
    {
        //Contenedor de bloques
        GameObject blockContainer = new GameObject("BlockContainer");
        blockContainer.transform.SetParent(panel.transform, false);
        blockContainer.AddComponent<CanvasRenderer>();

        RectTransform contentRect = blockContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.pivot = new Vector2(0, 1);

        VerticalLayoutGroup layoutGroup = blockContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.spacing = 0.0f; // Espacio entre bloques
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childScaleWidth = false;
        layoutGroup.childScaleHeight = false;
        layoutGroup.padding = new RectOffset(0, 0, 50, 0); // Ajuste de margen


        // ContentSizeFitter fitter = blockContainer.AddComponent<ContentSizeFitter>();
        //fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (m_blockScrollList != null)
        {
            m_blockScrollList.AssignBlockContainer(blockContainer.transform);
        }

       // Debug.Log($"BlockContainer size: {contentRect.sizeDelta}");

    }
    /**
     * Añade un borde al panel indicado
     * @param panel GameObject al que se le añadirá el borde
     * @param anchorMin Posición mínima del ancla
     * @param anchorMax Posición máxima del ancla
     */
    void AddBorder(GameObject panel, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject border = new GameObject("Border");
        border.transform.SetParent(panel.transform);
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = new Vector2(2, 0);

        Image img = border.AddComponent<Image>();
        img.color = Color.gray;
    }

    /** Añade un logo al panel indicado
     * @param panel GameObject al que se le añadirá el logo
     * @param spriteName Nombre del sprite a añadir
     */
    void AddLogoToPanel(GameObject panel, string spriteName)
    {
        //Debug.Log("Cargando sprite desde Resources: " + spriteName);
        //Sprite sprite = Resources.Load<Sprite>(spriteName);
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
        float iconSize = panel.GetComponent<RectTransform>().rect.height * 0.5f;
        float padding = 10f;
        float startX = panel.GetComponent<RectTransform>().rect.width - (iconSize + padding) * iconNames.Length;

        // Contenedor de los iconos (para distribuirlos correctamente)
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(panel.transform, false);

        RectTransform containerRect = iconContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1, 0.5f);  // Anclar a la derecha, centrado verticalmente
        containerRect.anchorMax = new Vector2(1, 0.5f);
        containerRect.pivot = new Vector2(1, 0.5f);
        containerRect.anchoredPosition = new Vector2(-20, 0); // Ajuste de margen derecho
        containerRect.sizeDelta = new Vector2(iconSize * iconNames.Length + (padding * (iconNames.Length - 1)), iconSize);

        // Agregar un `HorizontalLayoutGroup` para distribuir los iconos automáticamente
        HorizontalLayoutGroup layoutGroup = iconContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
        layoutGroup.spacing = padding;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        for (int i = 0; i < iconNames.Length; i++)
        {
            string iconName = iconNames[i];
            Texture2D iconTexture = Resources.Load<Texture2D>("Icons/" + iconName);
            if (iconTexture != null)
            {
                GameObject icon = new GameObject("Icon_" + iconName);
                icon.transform.SetParent(panel.transform);
                RectTransform iconRect = icon.AddComponent<RectTransform>();
                Image iconImage = icon.AddComponent<Image>();
                UnityEngine.UI.Button iconButton = icon.AddComponent<UnityEngine.UI.Button>();
                iconButton.onClick.AddListener(() => OnIconButtonClick(iconName));

                if (iconName == "stopFlag")
                {
                    icon.SetActive(false);
                }

                Sprite sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                iconImage.sprite = sprite;

                iconRect.anchorMin = new Vector2(1, 0.5f);
                iconRect.anchorMax = new Vector2(1, 0.5f);
                iconRect.pivot = new Vector2(1, 0.5f);
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                // iconRect.anchoredPosition = new Vector2(startX + (i * (iconSize + padding)), 0);
                iconRect.anchoredPosition = new Vector2(-((iconSize + padding) * (iconNames.Length - i)), 0);

            }
            else
            {
                Debug.LogError("No se encontró la Texture2D para el icono: " + iconName);
            }
        }
    }

    /**
     * Método que se ejecuta al hacer clic en un icono de la interfaz
     * @param iconName Nombre del icono pulsado
     */
    private void OnIconButtonClick(string iconName)
    {
        var actions = new System.Collections.Generic.Dictionary<string, System.Action>
        {
           { "GreenFlag", () => {
                Debug.Log("Ejecutar acción de inicio");
           }},
            { "load_icon", () => {
                Debug.Log("Cargar datos");
            }},
            { "save_icon", () => {
                Debug.Log("Guardar datos");
            }},
            { "stopFlag", () => {
                Debug.Log("Detener ejecución");

            }}
        };

        if (actions.TryGetValue(iconName, out var action))
        {
            action.Invoke();
        }
        else
        {
            Debug.Log("Acción no definida para: " + iconName);
        }
    }

    void LoadCategories(GameObject contentPanel)
    {
        if (contentPanel == null)
        {
            Debug.LogError("LeftPanel no encontrado, no se pueden cargar categorías.");
            return;
        }

        CategoryLoader categoryLoader = gameObject.AddComponent<CategoryLoader>();
        categoryLoader.contentPanel = contentPanel;
        categoryLoader.xmlFileName = "XML/Categories";
        categoryLoader.categoryPrefab = Resources.Load<GameObject>("Prefabs/CategoryPrefab"); //Prefab que muestra el circulo y texto de la categoría
        categoryLoader.uiCanvasManager = this;
        categoryLoader.LoadCategoriesFromXML();
    }

    GameObject SetupLeftPanel(GameObject parent)
    {
        //Creación del panel de categorías
        GameObject leftPanel = CreatePanel("CateogiesPanel", parent.transform, new Vector2(0, 0), new Vector2(0.15f, 0.95f), Vector2.zero, Vector2.zero, new Vector2(0, 1), Color.white);

        // Agregar ScrollRect al LeftPanel
        ScrollRect scrollRect = leftPanel.AddComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        //Creación del panel donde se muestran las categorías en el ScrollRect
        GameObject contentPanel = new GameObject("ContentPanel");
        //contentPanel.transform.SetParent(viewport.transform, false);
        contentPanel.transform.SetParent(leftPanel.transform, false);

        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layoutGroup = contentPanel.AddComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentPanel.AddComponent<VerticalLayoutGroup>();
        }
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 10f; //Espacio entre elementos
        layoutGroup.padding = new RectOffset(5, 5, 5, 5); // Ajusto espaciado superior

        ContentSizeFitter contentFitter = contentPanel.AddComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = contentPanel.AddComponent<ContentSizeFitter>();
        }
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect; // Asignao el contentPanel al ScrollRect

        return contentPanel;
    }

    /**
     * Actualiza el panel central con el nombre de la categoría seleccionada
     * @param categoryName Nombre de la categoría seleccionada
     */
    public void UpdateMiddlePanel(string categoryName, Color categoryColor)
    {
       
        if (this.m_blockScrollList != null)
        {
            this.m_blockScrollList.ShowBlockCategory(categoryName, categoryColor);
        }

     
    }

}

