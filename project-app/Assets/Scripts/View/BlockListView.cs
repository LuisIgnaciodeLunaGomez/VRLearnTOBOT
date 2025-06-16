/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/01/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Esta clase se encarga de generar las imagenes de los bloques para su representación correcta
 * 
 */

using UnityEngine;
using UnityEngine.UI;      
using TMPro;           
using System.Collections.Generic;
using System;            
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;

public class BlockListView : MonoBehaviour
{
    [Header("UI Assignments")]
    [Tooltip("El RectTransform del panel que contiene los botones de categoría (injectado)")]
    private RectTransform m_categoryButtonContainer;

    [Tooltip("El componente ScrollRect del panel donde se muestran las plantillas de bloques.")]
    [SerializeField] private ScrollRect m_BlockTemplateScrollRect;

    [Tooltip("El RectTransform que actúa como contenido dentro del ScrollRect de plantillas.")]
    private RectTransform m_BlockTemplateContainerRect;
    [SerializeField] private TextMeshProUGUI m_CategoryTitleText;
    [SerializeField] private GameObject m_BinArea;

    [Header("Prefabs")]
    [Tooltip("Texto que muestra el nombre de la categoría activa.")]
    [SerializeField] private GameObject m_CategoryButtonPrefab;

    [Header("Prefabs (Assigned Externally or Loaded)")]
    [Tooltip("Prefab para los botones de categoría que se crean.")]
    private RectTransform m_blockTemplateScrollAreaContent;

    // Referencias al Modelo y otras Vistas/Controladores (Injectadas)
    private WorkSpaceModel m_WorkspaceModel; //Modelo del WS
    protected ToolboxConfig m_ToolboxConfig; //Configuración de la Toolbox (cargada desde XML)
    protected WorkSpaceView m_WorkspaceView; //Vista área de codfificación para clonar e iniciar drag and drop
    private CategoryController m_CategoryController;

    //Estado interno
    private string m_ActiveCategoryName = null;
    private bool m_IsInitialized = false;

    private Dictionary<string, Toggle> m_CategoryToggles = new Dictionary<string, Toggle>(); // Cache de toggles de categoría creados

    // Caches para vistas plantilla de categorías especiales
    private Dictionary<string, BlockView> m_VariableGetterTemplateViews = new Dictionary<string, BlockView>();
    private List<BlockView> m_VariableHelperTemplateViews = new List<BlockView>();
    private Dictionary<string, BlockView> m_ProcedureCallerTemplateViews = new Dictionary<string, BlockView>();

    // Nombres de Categorías Especiales de m_Config
    private string m_CachedVariableCategoryName = null;
    private string m_CachedProcedureCategoryName = null;

    // Propiedades de Acceso
    public WorkSpaceView WorkspaceViewForFactory => m_WorkspaceView; 

    // Observadores
    private VariableObserver m_VariableObserver;
    private ProcedureObserver m_ProcedureObserver;

    /// <summary>
    /// Inicializa la BlockListView con las referencias necesarias desde el orquestador (AppController/UICanvasView).
    /// </summary>
    public void InitializeToolbox(WorkSpaceModel workspace, ToolboxConfig config, WorkSpaceView workspaceView, RectTransform categoryButtonContainer, 
                                  ScrollRect blockTemplateScrollRect, GameObject categoryButtonPrefab, CategoryController categoryController)
    {
        if (m_IsInitialized) return;
        //Debug.Log("<color=lightblue>BlockListView: Initializing...</color>");

        // Guardamos referencias y validamos
        m_WorkspaceModel = workspace ?? throw new ArgumentNullException(nameof(workspace));
        m_ToolboxConfig = config ?? throw new ArgumentNullException(nameof(config));
        m_WorkspaceView = workspaceView ?? throw new ArgumentNullException(nameof(workspaceView));
        m_categoryButtonContainer = categoryButtonContainer ?? throw new ArgumentNullException(nameof(categoryButtonContainer));
        m_BlockTemplateScrollRect = blockTemplateScrollRect ?? throw new ArgumentNullException(nameof(blockTemplateScrollRect));
        m_CategoryButtonPrefab = categoryButtonPrefab ?? throw new ArgumentNullException(nameof(categoryButtonPrefab));
        m_CategoryController = categoryController ?? throw new ArgumentNullException(nameof(categoryController), "CategoryController cannot be null for BlockListView initialization.");

        // Validamos configuración
        if (m_ToolboxConfig.BlockCategoryList == null)
        {
            Debug.LogError("BlockListView: Initialization failed - ToolboxConfig.BlockCategoryList is null!");
            this.enabled = false; return;
        }

        // Obtenemos/Aseguramos el contenedor de contenido del ScrollRect
        m_BlockTemplateContainerRect = m_BlockTemplateScrollRect.content as RectTransform;
        if (m_BlockTemplateContainerRect == null)
        {
            // Si ScrollRect no tenía content asignado, intentamos encontrar uno como hijo o crear uno.
            m_BlockTemplateContainerRect = m_BlockTemplateScrollRect.viewport?.GetComponentInChildren<RectTransform>(true); // Buscar hijo directo
            if (m_BlockTemplateContainerRect == null || m_BlockTemplateContainerRect == m_BlockTemplateScrollRect.transform) // Evitamos usar el propio ScrollRect como contenido
            {
               // Debug.Log("BlockListView: Creating Block Template Container dynamically...");
                m_BlockTemplateContainerRect = CreateAndConfigureBlockContainer(m_BlockTemplateScrollRect);
                m_BlockTemplateScrollRect.content = m_BlockTemplateContainerRect;
            }
            else
            {
              //  Debug.Log("BlockListView: Found existing RectTransform child for ScrollRect content.", m_BlockTemplateContainerRect);
                m_BlockTemplateScrollRect.content = m_BlockTemplateContainerRect;
            }


            if (m_BlockTemplateContainerRect == null) { Debug.LogError("Failed to find/create Block Template Container!"); this.enabled = false; return; }
        }

        // Aseguramos layout en el contenedor existente/creado
        EnsureLayoutComponents(m_BlockTemplateContainerRect.gameObject);

        // Obtenemos/Cacheamos nombres de categorías especiales
        m_CachedVariableCategoryName = GetCategoryNameByCustomType("VARIABLE");
        m_CachedProcedureCategoryName = GetCategoryNameByCustomType("PROCEDURE", "MYBLOCKS");

        // Registramos Observers para Variables y Procedimientos
        if (m_WorkspaceModel != null)
        {
            m_VariableObserver = Utilidades.GetOrAddComponent<VariableObserver>(this.gameObject);
            m_VariableObserver.SetTargetToolbox(this); // Pasamos referencia
            m_WorkspaceModel.VariableMap.AddObserver(m_VariableObserver);

            m_ProcedureObserver = Utilidades.GetOrAddComponent<ProcedureObserver>(this.gameObject);
            m_ProcedureObserver.SetTargetToolbox(this); // Pasamos referencia
            m_WorkspaceModel.ProcedureDB.AddObserver(m_ProcedureObserver);
            // Debug.Log("<color=lightblue>BlockListView: Observers registered.</color>");
        }

        m_IsInitialized = true;

        // Construimos la interfaz inicial del menú de categorías
        BuildMenu();

        // Mostrar la primera categoría
        StartCoroutine(SelectFirstCategoryAfterBuild()); // Seleccionar la primera categoría

        //Debug.Log("<color=green>BlockListView: Initialized and first category selected.</color>");
    }

    /// <summary>
    /// Crea dinámicamente el RectTransform 'Content' para un ScrollRect si no existe.
    /// </summary>
    private RectTransform CreateAndConfigureBlockContainer(ScrollRect scrollRectComponent)
    {
        GameObject containerGO = new GameObject("BlockContainer_");
        containerGO.layer = scrollRectComponent.gameObject.layer; // Copiar layer
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.SetParent(scrollRectComponent.viewport, false); // Ponerlo dentro del Viewport por defecto

        // Configuración RectTransform (Fill Stretch, Pivot Top-Center)
        containerRect.anchorMin = new Vector2(0, 1); // Top-Left Anchor
        containerRect.anchorMax = new Vector2(1, 1); // Top-Right Anchor 
        containerRect.pivot = new Vector2(0.5f, 1); // Top-Center Pivot
        containerRect.anchoredPosition = Vector2.zero; // Alinear con Top
        containerRect.sizeDelta = new Vector2(0, 100); // Ancho = 0 (depende de padre), Altura inicial pequeña
       // Debug.Log($"<color=red>CreateAndConfigureBlockContainer:</color> ScrollRect is '{scrollRectComponent?.name ?? "NULL"}', Viewport is '{scrollRectComponent?.viewport?.name ?? "NULL"}'. Attempting to parent '{containerGO.name}' to viewport.");
        containerRect.SetParent(scrollRectComponent.viewport, false);

        // Aseguramos Layout y Fitter 
        EnsureLayoutComponents(containerGO);

        // Debug.Log($"Dynamically created Block Container '{containerGO.name}' for ScrollRect '{scrollRectComponent.name}'", containerGO);
        return containerRect;
    }

    /// <summary>
    /// Asegura que el GameObject tenga VerticalLayoutGroup y ContentSizeFitter con configuración adecuada.
    /// </summary>
    private void EnsureLayoutComponents(GameObject go)
    {
    
        // VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = go.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = go.AddComponent<VerticalLayoutGroup>();
        // Ajustamos settings del VLG relativos a padding, spacing, alineamiento y childcontrol
        layoutGroup.padding = new RectOffset(5, 5, 10, 10); // Más padding arriba/abajo
        layoutGroup.spacing = 8f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter; // Centrar bloques horizontalmente
        layoutGroup.childControlWidth = true; 
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false; 
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // ContentSizeFitter
        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; 
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; 

        // Imagen invisible para que VLG/CSF funcione si no hay otra imagen
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false; // No interactivo
    }

    /// <summary>
    /// Construye el menú de botones de categorías.
    /// </summary>
    private void BuildMenu()
    {
        if (m_categoryButtonContainer == null || m_CategoryButtonPrefab == null || m_ToolboxConfig == null) return;

        ClearCategoryButtons(); // Limpiamos botones existentes

        // Creamos Botones
        ToggleGroup toggleGroup = Utilidades.GetOrAddComponent<ToggleGroup>(m_categoryButtonContainer.gameObject);
        toggleGroup.allowSwitchOff = false; 

        var categoriesToDisplay = m_ToolboxConfig.BlockCategoryList.Where(cat => cat?.Custom != "SEPARATOR").ToList(); // Excluir separadores

        foreach (var categoryConfig in categoriesToDisplay)
        {
            if (string.IsNullOrEmpty(categoryConfig.CategoryName)) continue;

            // Creamos el botón UI para la categoría
            GameObject buttonGO = CreateCategoryButtonUI(
                I18n.Get(categoryConfig.CategoryName) ?? categoryConfig.CategoryName, // Nombre display (I18n)
                categoryConfig.CategoryName, // Clave/Nombre interno
                categoryConfig.Color,       // Color
                toggleGroup
            );
            if (buttonGO == null) continue; // Si falla la creación

            // Obtenemos Toggle y asignamos listener
            Toggle toggle = buttonGO.GetComponent<Toggle>();
            if (toggle != null)
            {
                string currentCategoryName = categoryConfig.CategoryName; 

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        m_CategoryController.SelectCategory(currentCategoryName);
                    }
                });
                m_CategoryToggles[currentCategoryName] = toggle; // Cacheamos el toggle
            }
            else { Debug.LogError($"Failed to get Toggle for category button '{categoryConfig.CategoryName}'!"); }
        }

        // Forzamos Layout del contenedor de botones
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_categoryButtonContainer);
    }

    // Creamos un botón individual
    private GameObject CreateCategoryButtonUI(string displayName, string categoryKey, Color color, ToggleGroup toggleGroup)
    {
      //  Debug.Log($"Creating button UI for Key: {categoryKey}, Display: '{displayName}', Color: {color}"); 

        if (m_CategoryButtonPrefab == null) return null;
        GameObject buttonGO = Instantiate(m_CategoryButtonPrefab, m_categoryButtonContainer);
        buttonGO.name = $"CategoryBtn_{categoryKey}";
        TextMeshProUGUI label = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

        if (label == null) { Debug.LogError($"!! TextMeshProUGUI not found in prefab instance for {categoryKey}"); }
        else
        {
            label.text = displayName;
        }

        Image backgroundImage = buttonGO.GetComponent<Image>();
        if (backgroundImage == null) { Debug.LogError($"!! Background Image not found in prefab instance for {categoryKey}"); }
        else { backgroundImage.color = color; }

        Toggle toggle = Utilidades.GetOrAddComponent<Toggle>(buttonGO);
        toggle.group = toggleGroup;
        toggle.isOn = false;

        return buttonGO;
    }

    /// <summary>
    /// Selecciona programáticamente el toggle de una categoría por su nombre.
    /// Útil para la selección inicial o si otra parte de la UI quiere cambiar de categoría.
    /// </summary>
    public void SelectCategoryToggle(string categoryName)
    {
        if (m_CategoryToggles.TryGetValue(categoryName, out Toggle toggle))
        {
            if (toggle != null && !toggle.isOn)
            {
                toggle.isOn = true; // Activa el toggle (disparará el listener si no estaba ya activo)
            }
        }
        else
        {
            Debug.LogWarning($"BlockListView: Cannot select toggle for category '{categoryName}' - toggle not found in cache.", this);
        }
    }

    /// <summary>
    /// Corrutina para seleccionar la primera categoría después de que el layout del menú se estabilice.
    /// </summary>
    private IEnumerator SelectFirstCategoryAfterBuild()
    {
        yield return new WaitForEndOfFrame(); // Espera un poco más seguro que yield return null para layout
        yield return null;

        var firstValidCategory = m_ToolboxConfig?.BlockCategoryList.FirstOrDefault(c => c?.Custom != "SEPARATOR");
        if (firstValidCategory != null)
        {
            SelectCategoryToggle(firstValidCategory.CategoryName); // Usa el método que activa el toggle
        
        }

        else { Debug.LogWarning("No categories found to select initially."); }
    }

    /// <summary>
    ///  Limpia y muestra las plantillas de bloques para la categoría especificada.
    /// </summary>
    public void ShowBlockCategory(string categoryName, Color categoryColor)
    {
        if (!m_IsInitialized) { Debug.LogWarning("BlockListView not initialized.", this); return; }
        if (string.IsNullOrEmpty(categoryName)) { Debug.LogWarning("ShowBlockCategory called with empty name.", this); return; }
        if (m_BlockTemplateContainerRect == null) { Debug.LogError("ShowBlockCategory: Block Template Container is null!", this); return; }

        //Debug.Log($"<color=lightblue>BlockListView: ShowBlocks requested for Category: '{categoryName}'</color>", this);

        // Limpiamos completamente el contenedor de bloques plantilla
        ClearBlockTemplates();

        m_ActiveCategoryName = categoryName; // Actualizamos categoría activa
        if (m_CategoryTitleText != null) // Actualizamos título
            m_CategoryTitleText.text = I18n.Get(categoryName) ?? categoryName;

        // Poblamos con los bloques correctos
        PopulateContainer(categoryName, m_BlockTemplateContainerRect, categoryColor); // Pasamos contenedor y color

        // Ajustamos scroll y layout 

        // 1. Forzamos el layout del contenedor para que se ajuste a los nuevos bloques
        StartCoroutine(DelayedLayoutRebuild(m_BlockTemplateContainerRect));

        // 2. Hacemos scroll hasta arriba del contenedor
        if (m_BlockTemplateScrollRect != null)
        {
            StartCoroutine(DelayedScrollToTop(m_BlockTemplateScrollRect));
        }
    }

    /// <summary>
    /// Popula el contenedor de plantillas según el tipo de categoría (Estática, Variable, Procedimiento).
    /// </summary>
    private void PopulateContainer(string categoryName, RectTransform container, Color color)
    {
        if (container == null) return;

        string variableCatName = GetVariableCategoryName(); // Obtenemos nombre cacheado
        string procedureCatName = GetProcedureCategoryName(); // Obtenemos nombre cacheado

        if (categoryName.Equals(variableCatName, StringComparison.OrdinalIgnoreCase))
        {
            BuildVariableBlocksInternal(container);
        }
        else if (categoryName.Equals(procedureCatName, StringComparison.OrdinalIgnoreCase))
        {
            BuildProcedureBlocksInternal(container);
        }
        else // Categoría Estática
        {
            BuildStaticCategoryBlocksInternal(categoryName, container, color);
        }
    }


    /// <summary>
    /// Crea las plantillas para una categoría estática (definida en toolbox.xml).
    /// </summary>
    private void BuildStaticCategoryBlocksInternal(string categoryName, RectTransform container, Color categoryColor)
    {
        if (m_ToolboxConfig == null) return;
        var categoryConfig = m_ToolboxConfig.GetBlockCategory(categoryName);
        if (categoryConfig == null || categoryConfig.BlockList == null || categoryConfig.BlockList.Count == 0)
        {
            ShowEmptyMessage($"No blocks defined for '{categoryName}'."); return;
        }

        //Debug.Log($"BuildStaticCategoryBlocksInternal: Creating {categoryConfig.BlockList.Count} template views for '{categoryName}'.");
        foreach (string blockType in categoryConfig.BlockList)
        {
            if (string.IsNullOrEmpty(blockType)) continue;
            // Creamos la vista plantilla
            BlockView view = NewBlockView(blockType, container, categoryColor); // Pasamos color directamente
            
        }
    }

    /// <summary>
    /// Obtiene el Color asociado a un nombre de categoría específico,
    /// buscando en la configuración del toolbox (m_Config).
    /// </summary>
    /// <param name="categoryName">El nombre de la categoría a buscar.</param>
    /// <returns>El Color de la categoría, o Color.grey si no se encuentra.</returns>
    private Color GetColorOfCategory(string categoryName)
    {
        if (!m_IsInitialized || m_ToolboxConfig == null || string.IsNullOrEmpty(categoryName))
        {
            //Debug.LogWarning($"GetColorOfCategory: Cannot get color for '{categoryName}', Toolbox not ready or name empty.");
            return Color.grey;
        }

        // Buscamos la configuración de la categoría por nombre
        ToolboxBlockCategory categoryConfig = m_ToolboxConfig.GetBlockCategory(categoryName);

        if (categoryConfig != null)
        {
            // Devolvemos el color que ya fue inicializado en la categoría
            
            return categoryConfig.Color;
        }
        else
        {
            Debug.LogWarning($"GetColorOfCategory: Configuration not found for category '{categoryName}'. Using default grey.");
            return Color.grey; // Fallback si no se encuentra
        }
    }

    /// <summary>
    /// Crea las plantillas y el botón para la categoría Variables.
    /// </summary>
    private void BuildVariableBlocksInternal(RectTransform container)
    {
        Color categoryColor = GetColorOfCategory(m_CachedVariableCategoryName ?? Define.VARIABLE_CATEGORY_NAME);
      }

    /// <summary>
    /// Crea las plantillas para la categoría Procedimientos.
    /// </summary>
    protected void BuildProcedureBlocksInternal(RectTransform container)
    {
        Color categoryColor = GetColorOfCategory(m_CachedProcedureCategoryName ?? Define.PROCEDURE_CATEGORY_NAME);
      }

    /// <summary>
    /// Crea la vista template para el Toolbox.
    /// Incluye modelo plantilla, creación de vista via Factory, configuración para Toolbox, y trigger de drag.
    /// </summary>
    /// <param name="blockType">Tipo del bloque a crear.</param>
    /// <param name="parent">Transform padre donde instanciar la plantilla.</param>
    /// <param name="color">Color a aplicar al bloque.</param>
    /// <param name="siblingIndex">Índice opcional para insertar en la jerarquía.</param>
    /// <returns>La BlockView creada o null si falla.</returns>
    private BlockView NewBlockView(string blockType, RectTransform parent, Color color, int siblingIndex = -1)
    {
        if (string.IsNullOrEmpty(blockType)) return null;

        // 1. Creamos Modelo Plantilla (SIN workspace)
        BlockModel templateModel = BlockFactory.Instance.CreateBlock(null, blockType);
        if (templateModel == null) { Debug.LogWarning($"Could not create template MODEL for type: {blockType}"); return null; }

        // 2. Creamos Vista desde el Modelo Plantilla
        BlockView view = BlockViewFactory.CreateView(templateModel, this, parent);
        if (view == null)
        {
            Debug.LogError($"BlockViewFactory failed for template block type {blockType}. Disposing model.", this);
            templateModel.Dispose(); // Limpiamos modelo si la vista falló
            return null;
        }

        // 3. Configuramos la Vista para el Toolbox
        view.InToolbox = true;                  // Marcamos como plantilla
       // Debug.Log($"NewBlockView: Setting InToolbox = TRUE for Template BlockView '{view.gameObject.name}'");
        if (view.Block != null) view.Block.Movable = true; // El modelo asociado a la plantilla si es movible lógicamente
        view.gameObject.name = $"Template_{blockType}"; // Nombre 

        // 4. Asignamos padre y posición en UI
        if (parent == null) parent = m_BlockTemplateContainerRect; // Usamos contenedor por defecto
        view.ViewTransform.SetParent(parent, false);         // Establecemos padre visual
        
        if (siblingIndex >= 0) view.ViewTransform.SetSiblingIndex(siblingIndex); // Establecemos orden si se especifica
        else view.ViewTransform.SetAsLastSibling(); // Ponemos al final por defecto

        // 5. Configuramos Color 
        view.ChangeBgColor(color);

        // 6. Añadimos Trigger de Drag (Máscara + Script)
        GameObject dragTriggerGO = new GameObject($"DragTrigger_{blockType}");
        dragTriggerGO.transform.SetParent(view.transform, false); // Hijo del BlockView

        // Añadimos Image transparente como receptor de Raycast
        Image triggerImage = dragTriggerGO.AddComponent<Image>();
        triggerImage.color = Color.clear; // Invisible
        triggerImage.raycastTarget = true; // para recibir eventos

        // Hacemos que el trigger ocupe todo el espacio de la BlockView
        RectTransform triggerRect = dragTriggerGO.GetComponent<RectTransform>();
        triggerRect.anchorMin = Vector2.zero;
        triggerRect.anchorMax = Vector2.one;
        triggerRect.offsetMin = Vector2.zero;
        triggerRect.offsetMax = Vector2.zero;
        triggerRect.localScale = Vector3.one;

        // Añadimos el script que iniciará el drag
        BlockTemplateDragSource dragSource = Utilidades.GetOrAddComponent<BlockTemplateDragSource>(dragTriggerGO);
        dragSource.TemplateBlockView = view;
        dragSource.SourceToolbox = this; // Pasamos referencia a este Toolbox

        // Forzar un re-layout inicial si es necesario por Unity LayoutGroups
        LayoutRebuilder.ForceRebuildLayoutImmediate(view.GetComponent<RectTransform>());

        //Debug.Log($"BlockListView: Created Template BlockView: {view.gameObject.name}", view.gameObject);
        return view;
    }

    /// <summary>
    /// Inicia el proceso de clonar un bloque desde el toolbox y comenzar a arrastrar el clon.
    /// </summary>
    /// <param name="templateBlockView">La vista plantilla desde donde se inició el drag.</param>
    /// <param name="eventData">Datos del evento de inicio de drag.</param>
    public void StartDraggingFromToolbox(BlockView templateBlockView, PointerEventData eventData)
    {
        if (!m_IsInitialized || templateBlockView?.Block == null || m_WorkspaceModel == null || m_WorkspaceView == null || BlockDragController.Instance == null)
        {
            Debug.LogError("StartDraggingFromToolbox: Preconditions not met (Not initialized, null view/model/workspace/controller).", this);
            if (eventData != null) eventData.pointerDrag = null; // Cancelamos drag si podemos
            return;
        }
        if (!templateBlockView.InToolbox) // Seguridad extra
        {
            Debug.LogWarning("Attempted to start drag from a non-toolbox block using toolbox logic.", templateBlockView);
            if (eventData != null) eventData.pointerDrag = null;
            return;
        }

        string blockType = templateBlockView.BlockType;
        Color blockColor = GetColorOfBlock(blockType); // Obtenemos color del bloque plantilla
        Debug.Log($"<color=yellow>BlockListView: StartDraggingFromToolbox for type '{blockType}'</color>", this);

        // 1. Creamos el model en el WS
        BlockModel realBlockModel = BlockFactory.Instance.CreateBlock(m_WorkspaceModel, blockType);
        if (realBlockModel == null)
        {
            Debug.LogError($"StartDraggingFromToolbox: Failed to create BlockModel for type '{blockType}' using BlockFactory.", this);
            if (eventData != null) eventData.pointerDrag = null;
            return;
        }
        // Darle posición inicial basada en el ratón
        realBlockModel.XY = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_WorkspaceView.EventCamera);

        // 2. Creamos la vista para el modelo 
        BlockView cloneView = BlockViewFactory.CreateView(realBlockModel, this, m_WorkspaceView.CodingArea);
        if (cloneView == null)
        {
            Debug.LogError($"StartDraggingFromToolbox: BlockViewFactory failed to create BlockView for model {realBlockModel.ID} ({blockType}). Disposing model.", this);
            realBlockModel.Dispose(); // Limpiamos modelo huérfano
            if (eventData != null) eventData.pointerDrag = null;
            return;
        }

        // 3. Configuramos el Clon que ya NO está en toolbox, posicionamos
        cloneView.InToolbox = false;
        cloneView.transform.SetParent(m_WorkspaceView.CodingArea, true); // Ponerlo en el área de código (manteniendo pos global)
        cloneView.transform.SetAsLastSibling(); // Ponemos encima visualmente

        // Forzamos un re-layout inicial del clon para calcular su tamaño antes de calcular offset
        LayoutRebuilder.ForceRebuildLayoutImmediate(cloneView.ViewTransform);

        // 4. Informamos al BlockDragController que este clon es el que se va a empezar a arrastrar
        Debug.Log($" - Notifying BlockDragController to start drag for clone: {cloneView.gameObject.name}", this);
        // BlockDragController.Instance.PrepareForExternalDragStart(cloneView, eventData); // Un método para preparar antes de OnBeginDrag

        // 5. Iniciamos el drag en el clon programáticamente. Esto simula el IBeginDragHandler
        
        cloneView.OnBeginDrag(eventData); 

    }

    /// <summary>
    /// Obtiene el Color asociado a un tipo de bloque específico,
    /// encontrando primero la categoría a la que pertenece en la configuración
    /// del toolbox (m_ToolboxConfig).
    /// </summary>
    /// <param name="blockType">El tipo de bloque (ej. "motion_movesteps") a buscar.</param>
    /// <returns>El Color de la categoría del bloque, o Color.grey si no se encuentra
    /// o el toolbox no está inicializado.</returns>
    public Color GetColorOfBlock(string blockType) 
    {
        // Verificamos la inicialización y las dependencias necesarias
        if (!m_IsInitialized || m_ToolboxConfig == null || string.IsNullOrEmpty(blockType))
        {
            // Debug.LogWarning($"GetColorOfBlock: Cannot get color for '{blockType}', Toolbox not ready or type empty.");
            return Color.grey; // Color por defecto
        }

        ToolboxBlockCategory category = m_ToolboxConfig.GetBlockCategoryByType(blockType);

        if (category != null)
        {
            return category.Color;
        }
        else
        {
            Debug.LogWarning($"GetColorOfBlock: Block type '{blockType}' not found in any category within ToolboxConfig. Returning default grey.");
            return Color.grey; // Fallback a color por defecto
        }
    }

    /// <summary>
    /// Llamado por VariableObserver cuando el modelo de variables cambia.
    /// Refresca la UI del Toolbox SI la categoría Variables está activa.
    /// </summary>
    public void OnVariableUpdate(VariableUpdateData updateData)
    {
        if (!m_IsInitialized || m_WorkspaceModel == null) return;
        string variableCatName = GetVariableCategoryName();
        if (m_ActiveCategoryName == null || !m_ActiveCategoryName.Equals(variableCatName, StringComparison.OrdinalIgnoreCase)) return; // No está visible
        if (m_BlockTemplateContainerRect == null) return; // Sin contenedor

        // Debug.Log($"<color=orange>BlockListView.OnVariableUpdate: Refreshing Variable Category UI due to {updateData.Type} event.</color>", this);

        BuildVariableBlocksInternal(m_BlockTemplateContainerRect);
        // Re-layout forzado
        StartCoroutine(DelayedLayoutRebuild(m_BlockTemplateContainerRect));
    }

    /// <summary>
    /// Llamado por ProcedureObserver cuando el modelo de procedimientos cambia.
    /// Refresca la UI del Toolbox SI la categoría Procedimientos está activa.
    /// </summary>
    public void OnProcedureUpdate(ProcedureUpdateData updateData)
    {
        if (!m_IsInitialized || m_WorkspaceModel == null) return;
        string procedureCatName = GetProcedureCategoryName();
        if (m_ActiveCategoryName == null || !m_ActiveCategoryName.Equals(procedureCatName, StringComparison.OrdinalIgnoreCase)) return; // No está visible
        if (m_BlockTemplateContainerRect == null) return; // Sin contenedor

        // Debug.Log($"<color=purple>BlockListView.OnProcedureUpdate: Refreshing Procedure Category UI due to {updateData.Type} event.</color>", this);

        BuildProcedureBlocksInternal(m_BlockTemplateContainerRect);
        // Re-layout forzado
        StartCoroutine(DelayedLayoutRebuild(m_BlockTemplateContainerRect));
    }

    
    //  Limpieza y Utilidades 
    public void ClearBlockTemplates()
    {
        if (m_BlockTemplateContainerRect == null) return;
        foreach (Transform child in m_BlockTemplateContainerRect)
        {
            Destroy(child.gameObject); // Destruye todos los GameObjects de plantillas
        }
        // Limpiamos caches internos
        m_VariableGetterTemplateViews.Clear();
        m_VariableHelperTemplateViews.Clear();
        m_ProcedureCallerTemplateViews.Clear();
        // Debug.Log("BlockListView: Cleared block template area and caches.");
    }
    public void ClearCategoryButtons()
    {
        if (m_categoryButtonContainer == null) return;
        foreach (Transform child in m_categoryButtonContainer) { Destroy(child.gameObject); }
        m_CategoryToggles.Clear(); // Limpia la cache de toggles
       // Debug.Log("BlockListView: Cleared category buttons.");
    }

    public void ShowEmptyMessage(string message)
    {
        RectTransform container = m_BlockTemplateContainerRect;

        if (container == null) return;
        // Limpiamos antes de mostrar mensaje
        foreach (Transform child in container) { Destroy(child.gameObject); }

        GameObject messageGO = new GameObject("EmptyCategoryMessage");
        messageGO.transform.SetParent(container, false);
        // Añadimos TextMeshPro
        TextMeshProUGUI text = messageGO.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.color = Color.grey;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 14;
        // Añadimos LayoutElement para darle tamaño
        LayoutElement le = messageGO.AddComponent<LayoutElement>();
        le.minHeight = 50; // Un poco de espacio
        le.preferredWidth = -1; // Ancho flexible
        le.flexibleWidth = 1;
    }

    /// <summary>
    /// Corrutina que espera hasta el final del frame actual y luego fuerza
    /// la reconstrucción inmediata del layout para el RectTransform dado.
    /// Útil después de añadir/quitar/modificar elementos en un contenedor
    /// gestionado por LayoutGroups o ContentSizeFitter para asegurar que
    /// el layout de Unity UI se actualice correctamente.
    /// </summary>
    /// <param name="rect">El RectTransform cuyo layout se debe reconstruir.</param>
    private IEnumerator DelayedLayoutRebuild(RectTransform rect)
    {
       
        yield return new WaitForEndOfFrame();

        // Verificamos si el RectTransform todavía existe y está activo antes de forzar el layout
        if (rect != null && rect.gameObject != null && rect.gameObject.activeInHierarchy)
        {
            // Forzar al sistema de Layout a recalcular tamaño y posición para este RectTransform y sus hijos 
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            // Debug.Log($"DelayedLayoutRebuild: Forced rebuild for {rect.name}", rect.gameObject);
        }
        // else { Debug.LogWarning($"DelayedLayoutRebuild: RectTransform '{rect?.name}' no longer valid. Skipping rebuild."); }
    }

    /// <summary>
    /// Corrutina que espera un frame y luego establece la posición normalizada vertical
    /// de un ScrollRect a 1 (arriba del todo).
    /// Útil después de un DelayedLayoutRebuild para asegurar que el tamaño del contenido
    /// se ha calculado antes de intentar hacer scroll.
    /// </summary>
    /// <param name="scrollRect">El ScrollRect a desplazar.</param>
    private IEnumerator DelayedScrollToTop(ScrollRect scrollRect)
    {
        yield return null;
        // if (rect != null && rect.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        if (scrollRect != null && scrollRect.gameObject.activeInHierarchy)
            scrollRect.verticalNormalizedPosition = 1f;
    }
    

    void OnDestroy()
    {
        if (m_WorkspaceModel != null)
        {
            if (m_VariableObserver != null) m_WorkspaceModel.VariableMap?.RemoveObserver(m_VariableObserver);
            if (m_ProcedureObserver != null) m_WorkspaceModel.ProcedureDB?.RemoveObserver(m_ProcedureObserver);
        }
    }

    // Clase Interna Observer para Variables 
    private class VariableObserver : MonoBehaviour, IObserver<VariableUpdateData>
    {
        private BlockListView m_TargetToolbox;
        public void SetTargetToolbox(BlockListView toolbox) => m_TargetToolbox = toolbox;
        public void OnUpdated(object subject, VariableUpdateData args) => m_TargetToolbox?.OnVariableUpdate(args);
    }
    //  Clase Interna Observer para Procedimientos 
    private class ProcedureObserver : MonoBehaviour, IObserver<ProcedureUpdateData>
    {
        private BlockListView m_TargetToolbox;
        public void SetTargetToolbox(BlockListView toolbox) => m_TargetToolbox = toolbox;
        public void OnUpdated(object subject, ProcedureUpdateData args) => m_TargetToolbox?.OnProcedureUpdate(args);
    }

    /// <summary>
    /// Método helper para encontrar el nombre de la primera categoría que
    /// coincide con alguno de los tipos "custom" especificados en ToolboxConfig.
    /// Utiliza comparación insensible a mayúsculas/minúsculas.
    /// </summary>
    /// <param name="customTypes">Uno o más strings que representan los valores del atributo 'custom' a buscar.</param>
    /// <returns>El CategoryName de la primera categoría encontrada, o null si no se encuentra ninguna.</returns>
    private string GetCategoryNameByCustomType(params string[] customTypes)
    {
        if (m_ToolboxConfig?.BlockCategoryList == null || customTypes == null || customTypes.Length == 0)
        {
            return null; // No hay configuración o tipos custom para buscar
        }

        // Buscamos en la lista de categorías de la configuración
        foreach (var category in m_ToolboxConfig.BlockCategoryList)
        {
            // Validamos que la categoría y su campo 'Custom' no sean nulos
            if (category != null && !string.IsNullOrEmpty(category.Custom))
            {
                // Comprobamos si el Custom de esta categoría coincide con alguno de los buscados
                foreach (string customTypeToFind in customTypes)
                {
                    if (category.Custom.Equals(customTypeToFind, StringComparison.OrdinalIgnoreCase))
                    {
                        //Debug.Log($"Found category '{category.CategoryName}' for custom type '{customTypeToFind}'.");
                        return category.CategoryName;
                    }
                }
            }
        }

        //Debug.LogWarning($"No category found with custom type(s): {string.Join(", ", customTypes)}");
        return null;
    }

    private string GetVariableCategoryName()
    {
        if (m_CachedVariableCategoryName == null && m_ToolboxConfig?.BlockCategoryList != null)
        {
            m_CachedVariableCategoryName = m_ToolboxConfig.BlockCategoryList
                .FirstOrDefault(cat => "VARIABLE".Equals(cat?.Custom, StringComparison.OrdinalIgnoreCase))
                ?.CategoryName;
        }
        return m_CachedVariableCategoryName;
    }

    private string GetProcedureCategoryName()
    {
        if (m_CachedProcedureCategoryName == null && m_ToolboxConfig?.BlockCategoryList != null)
        {
            m_CachedProcedureCategoryName = m_ToolboxConfig.BlockCategoryList
                .FirstOrDefault(cat => "PROCEDURE".Equals(cat?.Custom, StringComparison.OrdinalIgnoreCase) ||
                                        "MYBLOCKS".Equals(cat?.Custom, StringComparison.OrdinalIgnoreCase)) 
                ?.CategoryName;
        }
        return m_CachedProcedureCategoryName;
    }

} // Fin de la clase BlockListView