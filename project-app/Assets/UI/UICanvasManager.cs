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
 * Versión: 1.0.1
 * 
 * Descripción: Clase que genera la vista de la interfaz de usuario de scratch
 */


using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UICanvasManager : MonoBehaviour
{
    public string logo;
    public string[] iconNames;

    private WorkSpace m_workSpace; // Espacio de trabajo
    private BlockScrollList m_blockScrollList;
    private GameObject m_canvasGO; //Canvas principal que incluye a todos los paneles
    private GameObject m_uiManager; //UIManager que contiene el Canvas principal y todos los paneles de la interfaz
    private Transform m_rightPanelTransform; // Referencia al RightPanel

    //Defino las medidas de la pantalla para que se ajuste a cualquier resolución
    private const int m_screenWidth =1200;
    private const int m_screenHeight =720;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.InitializeUIManager();
        this.InitializeCanvas();
        this.CreateTopPanel();
        GameObject leftPanel = this.SetupLeftPanel(this.m_canvasGO);
        this.LoadCategories(leftPanel);
        this.CreateWorkspace();

    }

    public WorkSpace WorkSpace()
    {
        return this.m_workSpace;
    }

    private void InitializeUIManager()
    {
        this.m_uiManager = GameObject.Find("UIManager");
        if (this.m_uiManager == null)
        {
            this.m_uiManager = new GameObject("UIManager");
        }
    }

    private void InitializeCanvas()
    {
        //Creación del canvas donde se van a presentar todos los paneles
        this.m_canvasGO = new GameObject("Canvas");
        this.m_canvasGO.transform.SetParent(this.m_uiManager.transform);

        Canvas canvas = this.m_canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = this.m_canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(m_screenWidth, m_screenHeight);
        canvasScaler.matchWidthOrHeight = 1f;
        
        //Detección de eventos de UI en el canvas necesario para el Drag and Drop
        this.m_canvasGO.AddComponent<GraphicRaycaster>();

    }

    private void CreateTopPanel()
    {
        //Creación del panel de herramientas superior
        GameObject topPanel = CreatePanel("Tools Panel", this.m_canvasGO.transform,
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
        workSpaceGO.transform.SetParent(this.m_canvasGO.transform, false); //es hijo del CanvasGo principal
        workSpaceGO.AddComponent<CanvasRenderer>();

        //Creo el rectángulo contenedor del espacio de trabajo
        RectTransform workSpaceRect = workSpaceGO.AddComponent<RectTransform>();
        workSpaceRect.anchorMin = new Vector2(0.15f, 0);
        workSpaceRect.anchorMax = new Vector2(1, 0.95f);
        workSpaceRect.offsetMin = Vector2.zero;
        workSpaceRect.offsetMax = Vector2.zero;
        workSpaceRect.pivot = new Vector2(0.5f, 0.5f);

        Debug.Log(workSpaceGO.GetComponent<RectTransform>() != null ? "RectTransform presente" : "Falta RectTransform");

        this.m_workSpace = workSpaceGO.AddComponent<WorkSpace>(); //Añado el script

        if (this.m_workSpace == null)
        {
            Debug.Log(workSpaceGO.GetComponent<WorkSpace>() != null ? "WorkSpace encontrado": "Falta  WorkSpace");
            return;
            //Debug.LogError("No se pudo inicializar WorkSpace.");
        }
        WorkSpaceView workSpaceView = workSpaceGO.AddComponent<WorkSpaceView>(); //Añado el script

        if (workSpaceView == null)
        {
           
            Debug.Log(workSpaceGO.GetComponent<WorkSpaceView>() != null ? "WorkSpaceView encontrado" : "WorkSpaceView es null");
            return;
           // Debug.LogError(" No se pudo inicializar WorkSpaceView.");
        }


        BlockStatusView statusView = workSpaceGO.AddComponent<BlockStatusView>(); //Añado el script BlockStatusView al WorkSpace

        if (statusView == null)
        {
            Debug.Log(workSpaceGO.GetComponent<BlockStatusView>() != null ? "WorkSpaceView encontrado" : "WorkSpaceView es null");
            return;
            //Debug.LogError("No se pudo inicializar BlockStatusView.");
        }
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

        //RectTransform middlePanelRect = middlePanel.GetComponent<RectTransform>();

        //área de codificación donde se arrastran los bloques para su conexión y posterior ejecución
        GameObject rightPanel = CreatePanel("CodingArea", workSpaceGO.transform,
            new Vector2(0.4f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            new Vector2(0.5f, 0.5f), new Color(0.976f, 0.976f, 0.976f, 1f));

        /*Canvas topCanvas = rightPanel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 3;*/

        m_rightPanelTransform = rightPanel.transform; // Guardamos la referencia al Transform del RightPanel

        this.m_blockScrollList = middlePanel.AddComponent<BlockScrollList>();

        this.m_blockScrollList.Initialized(Resources.Load<GameObject>("Prefabs/BlockPrefab"), middlePanel.transform);
                this.m_blockScrollList.SetWorkspaceTransform(m_rightPanelTransform);

        //StartCoroutine(WaitForWorkSpace());

        if (m_workSpace == null)
        {
            Debug.LogError("m_workSpace es null al intentar configurar BlockScrollList.");
            return;
        }
        else
        {
            this.m_blockScrollList.SetWorkSpace(m_workSpace);
        }

        WorkSpace wsComponent = m_workSpace.GetComponent<WorkSpace>();
        //wsComponent.Initialized(middlePanel, rightPanel);

        //Verificación del WorkSpaceView
        workSpaceView = m_workSpace.GetComponent<WorkSpaceView>();

        if (workSpaceView != null)
        {
            workSpaceView.Initialized(middlePanel, rightPanel);

           // if (rightPanel == null)
           if (!rightPanel.TryGetComponent(out RectTransform rightPanelRect))
                {
                Debug.LogError("WorkSpaceView: rightPanel es NULL. No se puede obtener RectTransform.");
                return;
            }

            //RectTransform rightPanelRect = rightPanel.GetComponent<RectTransform>();
            if (rightPanelRect == null)
            {
                Debug.LogError("WorkSpaceView: No se encontró RectTransform en rightPanel.");
                return;
            }
            workSpaceView.BindModel(m_workSpace, rightPanelRect); // Vincula el modelo de workspace con el workSpaceView
            Debug.Log("UICanvasMangaer: CreateWorkSpace: WorkSpaceView inicializado correctamente.");

        }
        else
        {
            Debug.LogError("CreateWorkSpace: UICanvasMAnager:No se encontró WorkSpaceView en el GameObject WorkSpace.");
        }


        // Me aseguro que WorkSpace está inicializado
        if (m_workSpace != null)
        {
            m_workSpace.Initialized(middlePanel, rightPanel);
        }
        else
        {
            Debug.LogError("CreateWorkSpace: UICanvasMAnager: WorkSpace es null en CreateWorkspace.");
            return;
        }

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
                return;
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

        StartCoroutine(WaitForCategoryLoader(categoryLoader));
        //categoryLoader.LoadCategoriesFromXML();
    }

    GameObject SetupLeftPanel(GameObject parent)
    {
        //Creación del panel de categorías
        GameObject leftPanel = CreatePanel("CategoriesPanel", parent.transform, new Vector2(0, 0), new Vector2(0.15f, 0.95f), Vector2.zero, Vector2.zero, new Vector2(0, 1), Color.white);

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

    IEnumerator WaitForCategoryLoader(CategoryLoader loader)
    {
        yield return new WaitUntil(() => loader != null);
        loader.LoadCategoriesFromXML();
    }

    IEnumerator WaitForWorkSpace()
    {
        yield return new WaitUntil(() =>this.m_workSpace != null);

        this.m_blockScrollList.SetWorkSpace(m_workSpace);
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

