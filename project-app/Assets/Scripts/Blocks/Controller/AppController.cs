/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.3
 * 
 * Descripción: Gestor central de la aplicación (Singleton), coordinando diferentes partes del sistema que no son estrictamente UI o Modelo/Vista de bloques.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AppController : MonoBehaviour
{
    public static AppController Instance { get; private set; }

    private UICanvasView m_uiManager;
    private WorkSpaceModel m_workspaceModel;
    private WorkSpaceView m_workspaceView;
    private ToolboxConfig m_toolboxConfig;
    private BlockListView m_blockListView;

    //Controladores
    private ExecutionController m_executionController;
    private InputController m_inputController;
    private CategoryController m_categoryController;   
    private WorkspaceController m_workspaceController;
    private BlockDragController m_blockDragController;
    private BlockConnectionController m_connectionController;

    private bool m_IsInitialized = false;

    public CategoryController GetCategoryController()
    {
        return m_categoryController;
    }
    void Awake()
    {
        m_IsInitialized = false;
        if (Instance == null)
        {
            Instance = this;
            ScratchBlocks.Init();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        ScratchBlocks.Dispose();
        if (Instance == this)
        {
            Instance = null;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Devuelve true si la inicialización principal en AppController.Start() ha terminado.
    /// Usado por otras clases para esperar dependencias.
    /// </summary>
    /// <returns>True si está inicializado, false en caso contrario.</returns>
    public bool IsInitialized()
    {
        return m_IsInitialized;
    }

    IEnumerator Start()
    {
        Debug.Log("<color=orange>AppController: Start - Finding components...</color>");
        m_IsInitialized = false;
        yield return null;

        m_uiManager = FindFirstObjectByType<UICanvasView>();
        if (m_uiManager == null)
        {
            Debug.LogError("AppController: UICanvasManager not found!");
            yield break;
        }
        Debug.Log("AppController: Found UICanvasView. Waiting for its core UI/View components to be ready (Awake phase)...", this);
        
        yield return new WaitUntil(() => m_uiManager.IsCoreComponentsReady()); 

        Debug.Log("AppController: UICanvasView components ready.");

        m_categoryController = FindFirstObjectByType<CategoryController>();
        if (m_categoryController == null) m_categoryController = gameObject.AddComponent<CategoryController>();

        m_workspaceController = FindFirstObjectByType<WorkspaceController>();
        if (m_workspaceController == null) m_workspaceController = gameObject.AddComponent<WorkspaceController>();

        yield return new WaitUntil(() => m_uiManager.Workspace != null && m_uiManager.WorkSpaceView != null);
        Debug.Log($"AppController: UICanvasView reports core setup complete.");

        m_workspaceModel = m_uiManager.Workspace;
        m_workspaceView = m_uiManager.WorkSpaceView;
        m_blockListView = m_uiManager.Toolbox;       
        m_toolboxConfig = m_uiManager.ToolboxConfig;

        if (m_workspaceModel == null || m_workspaceView == null)
        {
            Debug.LogError("AppController: Failed to get Workspace Model or View from UICanvasManager!");
            yield break;
        }

        if (m_workspaceModel == null || m_workspaceView == null || m_blockListView == null || m_toolboxConfig == null)
        {
            Debug.LogError("AppController: FAILED to get critical Model/View components from UICanvasView AFTER waiting!");
            yield break;
        }
        Debug.Log($"AppController: Got Model (ID: {m_workspaceModel.Id}) and View components from UICanvasView.");


        RectTransform codingAreaRect = m_uiManager.CodingAreaPanelRect;
        RectTransform blockListAreaRect = m_uiManager.BlockListPanelRect;
        RectTransform categoryButtonContainer = m_uiManager.CategoryButtonContainerRect;
        ScrollRect middlePanelScrollRect = m_uiManager.MiddlePanelScrollRect;
         GameObject catButtonPrefab = m_uiManager.CategoryButtonPrefab; 

        if (codingAreaRect == null || blockListAreaRect == null || categoryButtonContainer == null || middlePanelScrollRect == null || catButtonPrefab == null)
        {
            Debug.LogError("AppController: Failed to get required UI element references from UICanvasView Properties! Check UICanvasView Awake/Properties.", this);
            enabled = false; yield break;
        }

        m_workspaceView.BindModel(
          m_workspaceModel,
          m_blockListView,
          codingAreaRect, 
          null
      );
        Debug.Log("<color=green>AppController: WorkSpaceView bound to Model and RectTransform.</color>", this);


        m_categoryController = FindFirstObjectByType<CategoryController>() ?? gameObject.AddComponent<CategoryController>();

        if (m_categoryController == null) m_categoryController = gameObject.AddComponent<CategoryController>();
        m_categoryController.InitializeController(m_blockListView, m_toolboxConfig); 
        Debug.Log("AppController: CategoryController initialized.");

        m_workspaceController = FindFirstObjectByType<WorkspaceController>() ?? gameObject.AddComponent<WorkspaceController>();

        if (m_workspaceController == null) m_workspaceController = gameObject.AddComponent<WorkspaceController>();
        m_workspaceController.InitializeController(m_workspaceModel, m_workspaceView);
        Debug.Log("AppController: WorkspaceController initialized.");

        m_executionController = FindFirstObjectByType<ExecutionController>() ?? gameObject.AddComponent<ExecutionController>();
        if (m_executionController == null) m_executionController = gameObject.AddComponent<ExecutionController>();
        
        // m_executionController.Initialize(m_workspaceModel);
        Debug.Log("AppController: ExecutionController found/created.");

        m_inputController = FindFirstObjectByType<InputController>() ?? gameObject.AddComponent<InputController>();

        if (m_inputController == null) m_inputController = gameObject.AddComponent<InputController>();
        
        //m_inputController.Initialize(m_workspaceView);
        Debug.Log("AppController: InputController found/created.");

        BlockDragController dragController = FindFirstObjectByType<BlockDragController>(); // ?? gameObject.AddComponent<BlockDragController>();

        if (dragController == null)
        {
            Debug.LogWarning("AppController: BlockDragController not found in scene. Adding one to AppController GameObject.");
           
            dragController = gameObject.AddComponent<BlockDragController>();
        }

        m_blockDragController = dragController;

        m_connectionController = FindFirstObjectByType<BlockConnectionController>() ?? gameObject.AddComponent<BlockConnectionController>();

        m_connectionController.InitializeController(m_workspaceModel, m_workspaceView, m_blockDragController);

        Debug.Log("AppController: ConnectionController initialized.");

        dragController.InitializeController(m_workspaceModel, m_workspaceView, m_workspaceController, m_connectionController, m_uiManager.DragLayer);
        Debug.Log("AppController: BlockDragController initialized.");

        m_blockListView.InitializeToolbox(
            m_workspaceModel,
            m_toolboxConfig,
            m_workspaceView,
            categoryButtonContainer, 
            middlePanelScrollRect,   
            catButtonPrefab,         
            m_categoryController
        );
       // Debug.Log("<color=green>AppController: BlockListView initialized.</color>", this);

        Debug.Log("<color=green>AppController: Initialization of dependent controllers complete.</color>");
        
        m_IsInitialized = true;
    }

    public void TriggerExecution()
    {
        Debug.Log("Ejecutar acción de inicio");
        m_executionController?.StartExecution();
    }

    public void TriggerStop()
    {
        Debug.Log("Detener ejecución");
        m_executionController?.StopExecution();
    }
    public void TriggerSave()
    {
        Debug.Log("Guardar datos");
        m_uiManager?.SaveWorkspace();
    }

    public void TriggerLoad()
    {
        Debug.Log("Cargar datos");
        m_uiManager?.LoadWorkspace();
    }

}//fin clase AppController
