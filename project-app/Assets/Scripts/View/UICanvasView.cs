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
 * Descripción: Clase que genera la vista de la interfaz de usuario de scratch transformación de la clases UIManager para adaptar el MVC
 */


using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]

public class UICanvasView : MonoBehaviour
{
    [Header("Configuración Visual añadir en el Inspector")]
    public string logoSpriteName; // Nombre del sprite para el logo 
    public string[] topIconNames; // Nombres de sprites para iconos (GreenFlag, StopFlag, Save...)


    private RectTransform m_topPanelRect;        //Panel Superior con iconos y logo
    private RectTransform m_categoriesPanelRect; // Panel izquierdo con ScrollRect
    private RectTransform m_blockListPanelRect;  // Panel central para BlockListView
    private RectTransform m_codingAreaPanelRect; // Panel derecho para WorkspaceView

    // Referencias públicas a los CONTENEDORES de contenido 
    public RectTransform categoriesContentRect { get; private set; }
    public RectTransform blockListContentRect { get; private set; }

    // Definiciones de Layout 
    private const float CategoryPanelWidthRatio = 0.18f;
    private const float BlockListPanelWidthRatio = 0.27f; 

    //CodingArea ocupa el resto: 1.0f - CategoryPanelWidthRatio - BlockListPanelWidthRatio
    private const float TopPanelHeightRatio = 0.10f; // Altura relativa del panel superior

    //Controladores / Vistas Hijas
    // Devuelve el RectTransform donde CategoryController/BlockListView colocarán los BOTONES
    public RectTransform CategoryButtonContainer => categoriesContentRect;
    public RectTransform BlockTemplateContainerParent => blockListContentRect; // El ScrollRect Content
    public RectTransform CodingAreaRect => m_codingAreaPanelRect;
   
    //Defino las medidas de la pantalla para que se ajuste a cualquier resolución
    private const int M_SCREENWIDTH =1200;
    private const int M_SCREENHEIGHT =720;

    void Awake()
    {
        
        Debug.Log("UICanvasView: Awake starting...");

        ConfigureMainCanvas(); // Asegura el canvas principal

        CreateBaseUILayout(this.transform); // objeto como padre de los paneles

        // Validación final después de crear todo
        Debug.Log($"FINAL Validation: TopPanel={m_topPanelRect != null}, CategoriesContent={categoriesContentRect != null}, BlockListContent={blockListContentRect != null}, CodingArea={m_codingAreaPanelRect != null}");
        if (m_topPanelRect == null || categoriesContentRect == null || blockListContentRect == null || m_codingAreaPanelRect == null)
        {
            Debug.LogError("UICanvasView: CRITICAL FAILURE during panel/content creation! Check preceding logs.");
            this.enabled = false; // Deshabilitar script si la UI base falla
            return;
        }

        ConfigureTopPanel(); // Configurar contenido del Top Panel
        Debug.Log("UICanvasView Awake Complete.");
    }

    //Configura los componentes existentes en este GO como Canvas, CanvasScaler y GraphicRaycaster
    private void ConfigureMainCanvas()
    {
        Canvas canvas = GetComponent<Canvas>(); // Obtiene el Canvas de este GO
        if (canvas == null) { Debug.LogError("Missing Canvas component!"); return; }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = GetComponent<CanvasScaler>(); // Obtiene el Scaler de este GO
        if (canvasScaler == null) { Debug.LogError("Missing CanvasScaler component!"); return; }
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(M_SCREENWIDTH, M_SCREENHEIGHT);
        canvasScaler.matchWidthOrHeight = 1f;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>(); // Obtiene el Raycaster de este GO
        if (raycaster == null) { Debug.LogError("Missing GraphicRaycaster component!"); return; }
        raycaster.enabled = true; 

        Debug.Log("Main Canvas configured.");
    }

    // Orquesta la creación de los 4 paneles principales como hijos del canvasTransform proporcionado
    private void CreateBaseUILayout(Transform parent) // Recibe el Transform padre
    {
        Debug.Log($"Creando panel '{name}' como hijo de '{parent.name}'");
        // --- Calcular límites ---
        float blockListStartX = CategoryPanelWidthRatio;
        float blockListEndX = blockListStartX + BlockListPanelWidthRatio;
        //float codingAreaStartX = blockListEndX;
        // Calcular inicio de Right Panel después de que Middle existe
        float codingAreaStartX = 0f;

        // Llama a los métodos de creación pasando el canvasTransform padre
        CreateTopPanel(parent);
        CreateLeftPanel(parent);
        CreateMiddlePanel(parent);
        // Calcular inicio de Right Panel después de que Middle existe
        if (m_blockListPanelRect != null)
        { 
            Rect middleRect = m_blockListPanelRect.rect; // Rect en su espacio local
            Vector2 middleAnchorMax = m_blockListPanelRect.anchorMax; // Su anchor X derecho 
             
            codingAreaStartX = middleAnchorMax.x;
        }
        else if (m_categoriesPanelRect != null)
        { // Fallback si Middle falla pero Left no
            codingAreaStartX = m_categoriesPanelRect.anchorMax.x;
            Debug.LogWarning("CreateBaseUILayout: Middle Panel (BlockList) failed to create, positioning Right Panel relative to Left Panel.");
        }
        else
        { // Fallback extremo
            codingAreaStartX = CategoryPanelWidthRatio + BlockListPanelWidthRatio; // Usar los ratios como última opción
            Debug.LogWarning("CreateBaseUILayout: Both Left and Middle panels failed, using estimated position for Right Panel.");
        }

        CreateRightPanel(parent, codingAreaStartX);

        // Validación  dentro de este método
        Debug.Log($"Validation Check within CreateBaseUILayout: top={m_topPanelRect != null}, catContent={categoriesContentRect != null}, blockListContent={blockListContentRect != null}, coding={m_codingAreaPanelRect != null}");
        if (m_topPanelRect == null || categoriesContentRect == null || blockListContentRect == null || m_codingAreaPanelRect == null)
        {
            Debug.LogError("UICanvasView: One or more panels/contents FAILED to create!");
            
            this.enabled = false;
        }
    }
    
    private void CreateTopPanel(Transform parent)
    {
      
        // Top Panel 
        m_topPanelRect = CreatePanelGO("TopPanel", parent,
                                    new Vector2(0, 1 - TopPanelHeightRatio), new Vector2(1, 1), // Anchors OK
                                    Vector2.zero, Vector2.zero,
                                    new Vector2(0, 0), new Color(0.6f, 0.4f, 1f, 1f)
                                    )
                                    // PIVOT ORIGINAL (0, 0)
                                    ; // Color original como fallback
        if (m_topPanelRect == null) { Debug.LogError("Failed TopPanel"); return; }

        ConfigureTopPanel(); // Configura el contenido del Top Panel
        // Añadir Canvas para Sorting 
         Canvas topCanvas = Utilidades.GetOrAddComponent<Canvas>(m_topPanelRect.gameObject);
         topCanvas.overrideSorting = true;
         topCanvas.sortingOrder = 0;
    }

    private void CreateLeftPanel(Transform parent) {

  

        //Panel Izquierdo -Categorías
        m_categoriesPanelRect = CreatePanelGO("CategoriesPanel", parent,
                                          new Vector2(0, 0), new Vector2(CategoryPanelWidthRatio, 1 - TopPanelHeightRatio), // Anchors OK
                                          Vector2.zero, Vector2.zero,
                                          new Vector2(0, 1), //  PIVOT ORIGINAL (0, 1)
                                           new Color(0.97f, 0.97f, 0.97f)) ; // Color original como fallback
       
        if (m_categoriesPanelRect == null) { Debug.LogError("Failed CategoriesPanel"); return; }
        
        ScrollRect catScrollRect = Utilidades.GetOrAddComponent<ScrollRect>(m_categoriesPanelRect.gameObject);
        Utilidades.GetOrAddComponent<Image>(m_categoriesPanelRect.gameObject).raycastTarget = true; // Asegurar raycast
        catScrollRect.vertical = true; 
        catScrollRect.horizontal = false;
        categoriesContentRect = SetupScrollableContent(m_categoriesPanelRect.gameObject, "CategoriesContent");
        if (categoriesContentRect == null) { Debug.LogError("Failed Categories Content"); return; }
        SetupVerticalLayout(categoriesContentRect.gameObject, 5, 5, 10, 5, 8f, TextAnchor.UpperCenter, true, false, false, false);
        Utilidades.GetOrAddComponent<ContentSizeFitter>(categoriesContentRect.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }


    private void CreateMiddlePanel(Transform parent)
    {
         
        // Panel Central Lista de Bloques
        float blockListStartX = CategoryPanelWidthRatio;
        float blockListEndX = blockListStartX + BlockListPanelWidthRatio;
        m_blockListPanelRect = CreatePanelGO("BlockListPanel", parent,
                                         new Vector2(blockListStartX, 0), new Vector2(blockListEndX, 1 - TopPanelHeightRatio), // Anchors OK
                                         Vector2.zero, Vector2.zero,
                                         new Vector2(0, 1), // <<< PIVOT NUEVO (0,1). 
                                         Color.white); // Fallback original
        
        if (m_blockListPanelRect == null) { Debug.LogError("Failed BlockListPanel"); return; }
       
        
        ScrollRect blScrollRect = Utilidades.GetOrAddComponent<ScrollRect>(m_blockListPanelRect.gameObject);
        blScrollRect.scrollSensitivity = 15f;
        blScrollRect.vertical = true; 
        blScrollRect.horizontal = false;

        GameObject viewportGO = GetOrCreateChild(m_blockListPanelRect.gameObject, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));

        Image blImage = m_blockListPanelRect.GetComponent<Image>(); 
        if (blImage != null)
        {
            blImage.raycastTarget = true; // Necesario para que ScrollRect funcione
                                        
        }
        else { Debug.LogError("BlockListPanel missing Image after CreatePanel!"); }

        if (viewportGO == null) { Debug.LogError("Failed to create BlockList Viewport"); return; }
        ConfigureViewport(viewportGO.GetComponent<RectTransform>(), blScrollRect);
        // Setup Content Y ASIGNAR
        Utilidades.GetOrAddComponent<Image>(m_categoriesPanelRect.gameObject).raycastTarget = true; // Asegurar raycast

        blockListContentRect = SetupScrollableContent(m_blockListPanelRect.gameObject, "BlockListsContent");
        if (blockListContentRect == null) { Debug.LogError("Failed BlockList Content Setup"); return; }
       
        if (blockListContentRect == null) { Debug.LogError("Failed BlockList Content"); return; }
        
        SetupVerticalLayout(blockListContentRect.gameObject, 5, 5, 8, 5, 6f, TextAnchor.UpperCenter, true, false, false, false); // ControlWidth=true?
        Utilidades.GetOrAddComponent<ContentSizeFitter>(blockListContentRect.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    }

    private void CreateRightPanel(Transform canvasTransform, float codingAreaStartX)
    {
       
        m_codingAreaPanelRect = CreatePanelGO("CodingAreaPanel", canvasTransform,
                                             new Vector2(codingAreaStartX, 0), new Vector2(1, 1 - TopPanelHeightRatio),
                                             Vector2.zero, Vector2.zero, // Añadir Offsets 
                                             new Vector2(0, 1), // Añadir Pivot
                                              Color.white);

        Image codingBg = Utilidades.GetOrAddComponent<Image>(m_codingAreaPanelRect.gameObject);
        codingBg.color = Color.white; // Color para depuración
        codingBg.raycastTarget = true;
    }


    // Configura el contenido del Top Panel
    private void ConfigureTopPanel()
    {
        if (m_topPanelRect == null) return;

        if (!string.IsNullOrEmpty(logoSpriteName))
            AddLogoToPanel(m_topPanelRect.gameObject, logoSpriteName);

        if (topIconNames != null && topIconNames.Length > 0)
            AddIconsToPanel(m_topPanelRect.gameObject, topIconNames);
    }

    private void ConfigureViewport(RectTransform vpRect, ScrollRect scrollRect)
    {
        Image img = Utilidades.GetOrAddComponent<Image>(vpRect.gameObject); 
        Mask mask = Utilidades.GetOrAddComponent<Mask>(vpRect.gameObject); 
                                                                
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero; vpRect.pivot = new Vector2(0, 1);
        img.color = new Color(0, 0, 0, 0); // Transparente
        mask.showMaskGraphic = false;
        scrollRect.viewport = vpRect; // Asignar al ScrollRect padre
    }

    private void ConfigureContent(RectTransform contentRect, ScrollRect scrollRect)
    {
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRect;
    }

    // Configura un panel para ser scrollable y devuelve su RectTransform de contenido
    private RectTransform SetupScrollableContent(RectTransform scrollPanelRect, string contentName)
    {
        // Añadir ScrollRect al panel principal
        ScrollRect scrollRect = scrollPanelRect.gameObject.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = scrollPanelRect.gameObject.AddComponent<ScrollRect>();

        // Crear Viewport hijo
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollPanelRect, false);
        RectTransform vpRect = viewportGO.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero; vpRect.pivot = new Vector2(0, 1); // Top-Left
        viewportGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // Casi invisible
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vpRect;

        // Crear Content hijo dentro de Viewport
        GameObject contentGO = new GameObject(contentName, typeof(RectTransform));
        contentGO.transform.SetParent(vpRect, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1); // Ancho completo, ancla arriba
        contentRect.pivot = new Vector2(0.5f, 1); // Pivote arriba centro
        contentRect.sizeDelta = new Vector2(0, 0); // Altura inicial 0
        contentRect.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRect;

        return contentRect; // Devuelve el Rect del Content
    }

    // Sobrecarga método anterior si ya existe el Viewport
    private RectTransform SetupScrollableContent(ScrollRect scrollRect, Transform viewportTransform, string contentName)
    {
        GameObject contentGO = new GameObject(contentName, typeof(RectTransform));
        contentGO.transform.SetParent(viewportTransform, false); // Hijo del viewport existente
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRect;
        return contentRect;
    }

    // Configuración de un VerticalLayoutGroup en un GameObject
    private void SetupVerticalLayout(GameObject targetGO, int padLeft, int padRight, int padTop, int padBottom, float spacing, TextAnchor alignment = TextAnchor.UpperLeft, bool controlWidth = false, bool controlHeight = false, bool forceExpandW = false, bool forceExpandH = false)
    {
        VerticalLayoutGroup vlg = targetGO.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = targetGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(padLeft, padRight, padTop, padBottom);
        vlg.spacing = spacing;
        vlg.childAlignment = alignment;
        vlg.childControlWidth = controlWidth;
        vlg.childControlHeight = controlHeight;
        vlg.childForceExpandWidth = forceExpandW;
        vlg.childForceExpandHeight = forceExpandH;
    }

    // Encuentra Content Rects si usa Prefabs
    private void FindContentRects()
    {
        if (m_categoriesPanelRect != null)
        {
            //estructura Prefab -> ScrollRect -> Viewport -> Content
            ScrollRect catSR = m_categoriesPanelRect.GetComponentInChildren<ScrollRect>();
            if (catSR != null) categoriesContentRect = catSR.content;
            if (categoriesContentRect == null) Debug.LogError("Categories Content Rect not found!");
        }
        if (m_blockListPanelRect != null)
        {
            ScrollRect blSR = m_blockListPanelRect.GetComponentInChildren<ScrollRect>();
            if (blSR != null) blockListContentRect = blSR.content;
            if (blockListContentRect == null) Debug.LogError("BlockList Content Rect not found!");
        }
    }

    //Al pulsar un botón respuesta del icono pulsado
    private void OnIconButtonClick(string iconName)
    {
   
        Debug.Log($"UI Action: {iconName}");
        switch (iconName)
        {
            case "GreenFlag": ExecutionController.Instance?.StartExecution(); break;
            case "stopFlag": ExecutionController.Instance?.StopExecution(); break;
            case "save_icon": WorkspaceController.Instance?.RequestSaveWorkspace(); break; // TODO: Implementar Save
            case "load_icon": WorkspaceController.Instance?.RequestLoadWorkspace(); break; // TODO: Implementar Load
            default: Debug.LogWarning($"Action not defined for icon: {iconName}"); break;
        }
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

            // Alinear el logo a la izquierda
            logoRect.anchorMin = new Vector2(0, 0.5f);
            logoRect.anchorMax = new Vector2(0, 0.5f);
            logoRect.pivot = new Vector2(0, 0.5f);
            logoRect.sizeDelta = new Vector2(panel.GetComponent<RectTransform>().rect.width * 0.05f, panel.GetComponent<RectTransform>().rect.height * 0.5f);
            logoRect.anchoredPosition = new Vector2(50, 0); 

           
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
        float iconSize = panel.GetComponent<RectTransform>().rect.height*0.05f;
        float padding = 10f;
       // float startX = panel.GetComponent<RectTransform>().rect.width - (iconSize + padding) * iconNames.Length;

        // Contenedor de los iconos 
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(panel.transform, false);

        RectTransform containerRect = iconContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1, 0.5f);  // Anclar a la derecha, centrado verticalmente
        containerRect.anchorMax = new Vector2(1, 0.5f);
        containerRect.pivot = new Vector2(1, 0.5f);// Pivote a la derecha para facilitar posicionamiento
        containerRect.anchoredPosition = new Vector2(-400, 0); // Ajuste de margen derecho
       // containerRect.sizeDelta = new Vector2(iconSize * iconNames.Length + (padding * (iconNames.Length - 1)), iconSize);

        //HorizontalLayoutGroup para distribuir los iconos automáticamente
        HorizontalLayoutGroup layoutGroup = iconContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
        layoutGroup.spacing = padding;
        
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        for (int i = 0; i < iconNames.Length; i++)
        {
            string iconName = iconNames[i];
            Texture2D iconTexture = Resources.Load<Texture2D>("Icons/" + iconName);
            if (iconTexture != null)
            {
                GameObject iconGO = new GameObject("Icon_" + iconName);

                iconGO.transform.SetParent(iconContainer.transform, false);
             
                Image iconImage = iconGO.AddComponent<Image>();
                Button iconButton = iconGO.AddComponent<Button>();
                iconButton.targetGraphic = iconImage;
                iconButton.onClick.AddListener(() => OnIconButtonClick(iconName));

                if (iconName == "stopFlag")
                {
                    iconGO.SetActive(true);
                }

                Sprite sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                iconImage.sprite = sprite;

                LayoutElement layoutElement = iconContainer.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = iconSize/2;
                layoutElement.preferredHeight = iconSize/2;
                layoutElement.minWidth = iconSize/2;
                layoutElement.minHeight = iconSize/2;

            }
            else
            {
                Debug.LogError("No se encontró la Texture2D para el icono: " + iconName);
                return;
            }
        }
    }

    /**
     * Descripcíon: Método que crea un panel con un nombre y un color
     * @param: name: nombre del panel
     * @param: parent: Transform del padre
     * @param: anchorMin: Vector2 con la posición mínima del ancla
     * @param: anchorMax: Vector2 con la posición máxima del ancla
     * @param: offsetMin: Vector2 con el desplazamiento mínimo
     * @param: offsetMax: Vector2 con el desplazamiento máximo
     * @param: pivot: Vector2 con el pivote
     * @param: color: Color del panel
     * @return: RectTransform del panel creado
     */
    private RectTransform CreatePanelGO(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot, Color color)                       
    {
        GameObject panelGO = new GameObject(name);
        panelGO.transform.SetParent(parent, false);
        RectTransform rect = panelGO.GetComponent<RectTransform>();

        if(rect==null) rect = panelGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = pivot;         
        rect.localScale = Vector3.one;
       
        Image img = panelGO.AddComponent<Image>();
        img.color = color;
        return rect;
    }

    /**
     * Descripición: Método que crea un panel scrollable
     * @param: scrollPanelGO: GameObject del panel scrollable
     * @param: contentName: Nombre del contenido
     * @return: contentRect: RectTransform del contenido
     */
    private RectTransform SetupScrollableContent(GameObject scrollPanelGO, string contentName)
    {
        ScrollRect scrollRect = scrollPanelGO.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = scrollPanelGO.AddComponent<ScrollRect>();
        scrollPanelGO.GetComponent<Image>().raycastTarget = true; // Panel necesita interceptar scroll

        Transform viewportTransform = scrollPanelGO.transform.Find("Viewport"); // Buscar existente primero
        GameObject viewportGO;
        if (viewportTransform == null)
        {
            viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollPanelGO.transform, false);
            //viewportGO.GetComponent<Image>().color = new Color(0, 0, 0, 0); // Transparente por defecto
            //viewportGO.GetComponent<Mask>().showMaskGraphic = false;
        }
        else { viewportGO = viewportTransform.gameObject; }
        RectTransform vpRect = viewportGO.GetComponent<RectTransform>();
        SetupChildRectFill(vpRect); // Asegura que llene el ScrollPanel
        scrollRect.viewport = vpRect;

        GameObject contentGO = new GameObject(contentName, typeof(RectTransform));
        contentGO.transform.SetParent(vpRect, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRect;

        return contentRect;
    }

    /**
     * Descripción:
     * @param: rect: RectTransform a configurar
     */
    private void SetupChildRectFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero; // Reset size delta
        rect.anchoredPosition = Vector2.zero; // Reset position
        rect.pivot = new Vector2(0.5f, 0.5f); // Pivot central para fill
    }


    private GameObject GetOrCreateChild(GameObject parent, string childName, params System.Type[] components) 
    {
        if (parent == null)
        {
            Debug.LogError($"GetOrCreateChild: Parent GameObject is null when trying to find/create '{childName}'.");
            return null;
        }

        Transform childTransform = parent.transform.Find(childName);
        if (childTransform != null)
        {
            // Asegurar que tenga los componentes si ya existe
            // foreach (var componentType in components) {
            //      if (childTransform.GetComponent(componentType) == null) {
            //           childTransform.gameObject.AddComponent(componentType);
            //            Debug.LogWarning($"Added missing component {componentType.Name} to existing child '{childName}'.");
            //       }
            // }
            return childTransform.gameObject;
        }

        // Crear el nuevo GameObject CON los componentes especificados
        GameObject childGO = new GameObject(childName, components); 
        childGO.transform.SetParent(parent.transform, false);
        return childGO;
    }
    /*
    public void SetupDependentViews(BlockListView blv, WorkSpaceView wsv, CategoryController cc, BlockDragController bdc, WorkspaceModel model)
    {
        Debug.Log("UICanvasView: Setting up dependent views...");
        if (blv != null && cc != null)
        {
            blv.InitializeView(cc); // Inicializar BLV
            cc.InitializeController(blv); // Inicializar CC (esto llama a blv.DisplayCategories)
        }
        else Debug.LogError("UICanvasView: BlockListView or CategoryController refs are null for setup!");

        if (wsv != null && bdc != null && model != null && codingAreaPanelRect != null)
        {
            wsv.InitializeView(model, bdc, codingAreaPanelRect); // Pasa modelo, BDC y el panel Rect
        }
        else Debug.LogError("UICanvasView: WorkspaceView, BlockDragController, Model or CodingArea ref is null for setup!");*/
    

}//Fin clase UICanvasView

