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
    private CategoryController m_categoryController;   // Añadir referencia al CategoryController
    private WorkspaceController m_workspaceController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ScratchBlocks.Init();
            DontDestroyOnLoad(gameObject);

            InitializeControllers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Método para centralizar la inicialización de controladores
    private void InitializeControllers()
    {
        m_categoryController = FindFirstObjectByType<CategoryController>();
        if (m_categoryController == null)
        {
            m_categoryController = gameObject.AddComponent<CategoryController>();
            Debug.Log("<color=green>AppController: CategoryController added.</color>");
        }
        else { Debug.Log("<color=cyan>AppController: CategoryController found existing.</color>"); }

        m_workspaceController = FindFirstObjectByType<WorkspaceController>();
        if (m_workspaceController == null)
        {
            m_workspaceController = gameObject.AddComponent<WorkspaceController>();
            Debug.Log("<color=green>AppController: WorkspaceController added.</color>");
        }
        else { Debug.Log("<color=cyan>AppController: WorkspaceController found existing.</color>"); }

        m_executionController = FindFirstObjectByType<ExecutionController>();
        if (m_executionController == null)
        {
            m_executionController = gameObject.AddComponent<ExecutionController>();
            Debug.Log("<color=green>AppController: ExecutionController added.</color>");
        }
        else { Debug.Log("<color=cyan>AppController: ExecutionController found existing.</color>"); }

        m_inputController = FindFirstObjectByType<InputController>();
        if (m_inputController == null)
        {
            m_inputController = gameObject.AddComponent<InputController>();
            Debug.Log("<color=green>AppController: InputController added.</color>");
        }
        else { Debug.Log("<color=cyan>AppController: InputController found existing.</color>"); }
    }
    void OnDestroy()
    {
       //ScratchBlocks.Dispose();
        
    }

    IEnumerator Start()
    {
        Debug.Log("<color=orange>AppController: Start - Finding components...</color>");

        m_uiManager = FindFirstObjectByType<UICanvasView>();
        if (m_uiManager == null)
        {
            Debug.LogError("AppController: UICanvasView not found!");
            yield break; // O salir o manejar de otra forma
        }
        else
        {
            m_workspaceModel = m_uiManager.Workspace;
            m_workspaceView = m_uiManager.WorkSpaceView;
            m_blockListView = m_uiManager.Toolbox;
            m_toolboxConfig = m_uiManager.ToolboxConfig;

            // Validar si las referencias obtenidas de m_uiManager son válidas
            if (m_workspaceModel == null || m_workspaceView == null || m_blockListView == null || m_toolboxConfig == null)
            {
                Debug.LogError("AppController: Failed to get critical components (Model/View/List/Config) from UICanvasView!");
                yield break;
            }


            // Asegurarse que los controladores encontrados/añadidos en Awake están listos
            if (m_categoryController != null)
            {
                m_categoryController.InitializeController(m_blockListView, m_toolboxConfig);
                Debug.Log("<color=lightblue>AppController: CategoryController initialized via InitializeController.</color>");
            }
            else { Debug.LogError("AppController: m_categoryController is null after Awake! Cannot initialize it."); }

            Debug.Log("<color=green>AppController: Initialization of dependent controllers and UI references complete.</color>");
        }

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
