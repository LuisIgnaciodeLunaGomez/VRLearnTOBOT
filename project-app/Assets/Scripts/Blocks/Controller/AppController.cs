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
        Debug.Log("AppController: Waiting for UICanvasView core components...");
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
        m_blockListView = m_uiManager.Toolbox;       // Obtener la BlockListView (Toolbox)
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

        m_categoryController = FindFirstObjectByType<CategoryController>() ?? gameObject.AddComponent<CategoryController>();
        m_workspaceController = FindFirstObjectByType<WorkspaceController>() ?? gameObject.AddComponent<WorkspaceController>();
        m_executionController = FindFirstObjectByType<ExecutionController>() ?? gameObject.AddComponent<ExecutionController>();
        m_inputController = FindFirstObjectByType<InputController>() ?? gameObject.AddComponent<InputController>();
        m_blockDragController = FindFirstObjectByType<BlockDragController>() ?? gameObject.AddComponent<BlockDragController>();

        if (m_categoryController != null)
            m_categoryController.InitializeController(m_blockListView, m_toolboxConfig); 
        else Debug.LogError("AppC: Failed to init CategoryController");

        if (m_workspaceController != null)
            m_workspaceController.InitializeController(m_workspaceModel, m_workspaceView);
        else Debug.LogError("AppC: Failed to init WorkspaceController");

        if (m_blockDragController != null)
            m_blockDragController.InitializeController(m_workspaceModel, m_workspaceView, m_workspaceController);
        else Debug.LogError("AppC: Failed to init BlockDragController");

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
