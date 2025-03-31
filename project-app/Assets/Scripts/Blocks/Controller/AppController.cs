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
 * Versión: 1.0.0
 * 
 * Descripción: Controlador principal de la aplicación, se encarga de inicializar los controladores y vistas de la aplicación.
 */

using System.Collections;
using UnityEngine;

public class AppController : MonoBehaviour
{
    // --- Singleton o Acceso Estático ---
    public static AppController Instance { get; private set; }
    // Referencias a otros Controladores y Servicios 
    [SerializeField] private WorkspaceController m_workspaceController;
    [SerializeField] private BlockDragController m_blockDragController;
    [SerializeField] private InputController m_inputController;
    [SerializeField] private CategoryController m_categoryController;
    [SerializeField] private ExecutionController m_executionController;

    //Referencias a Vistas Principales 
    [SerializeField] private WorkSpaceView m_workspaceView;
    [SerializeField] private BlockListView m_blockListView; 
    [SerializeField] private UICanvasView m_uiCanvasView; 
    [SerializeField] private BlockStatusView m_blockStatusView; 

    //Instancia del Modelo
    private WorkspaceModel m_workspaceModel;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else if (Instance != this) { Destroy(gameObject); return; }

        BlockDataLoader.LoadAllDefinitions();
    }

    IEnumerator Start()
    {
        Debug.Log("<color=orange>AppController: Start() running...</color>");

        // --- Crear el Modelo ---
        // Usa la variable miembro
        m_workspaceModel = new WorkspaceModel();
        if (m_workspaceModel == null)
        {
            Debug.LogError("Failed to create WorkspaceModel!"); yield break; 
        }
        Debug.Log("AppController: WorkspaceModel created.");


        //Validar/Encontrar Referencias a Vistas y Controladores
       
        if (!FindAndValidateCoreComponents()) { yield break; } // Detener si falta algo crítico


        // Esperar a que UICanvasView  cree su layout ---
        Debug.Log("AppInitializer: Waiting one frame for UICanvasView layout...");
        yield return null;

        // Revalidar contenedores de UICanvasView
        if (!ValidateUICanvasRefs(m_uiCanvasView))
        {
            Debug.LogError("UICanvasView did not create required containers!"); yield break;
        }
        Debug.Log("AppInitializer: UICanvasView panels ready.");


        // Inyección de Dependencias / Inicialización 
        Debug.Log("AppInitializer: Initializing MVC components...");

        // Inicializar Controladores
        m_workspaceController.InitializeController(m_workspaceModel, m_workspaceView);
        m_inputController.InitializeController(m_workspaceController);
        m_executionController.InitializeController(m_workspaceModel);
        m_blockDragController.InitializeController(m_workspaceModel, m_workspaceView, m_workspaceController);

        //  Inicializar CategoryController 
        m_categoryController.InitializeController(m_blockListView);

        // Inicializar BlockListView)
        m_blockListView.InitializeView(m_categoryController, m_uiCanvasView.CategoryButtonContainer, m_uiCanvasView.BlockTemplateContainerParent); 

        // Inicializar las otras vistas que dependen de componentes ya listos
        m_workspaceView.InitializeView(m_workspaceModel, m_blockDragController, m_uiCanvasView.CodingAreaRect);
        m_blockStatusView.InitializeView(m_workspaceView, m_executionController);


        // Disparar la lógica inicial de categorías
        m_categoryController.StartDisplayingCategories();


        Debug.Log("<color=green>AppController: Initialization Complete.</color>");
        yield break;


    }
    private bool FindAndValidateCoreComponents()
    {
        bool ok = true;
        if (m_uiCanvasView == null) { m_uiCanvasView = FindFirstObjectByType<UICanvasView>(); if (m_uiCanvasView == null) { Debug.LogError("AppController: m_uiCanvasView missing!"); ok = false; } }
        if (m_blockListView == null) { m_blockListView = FindFirstObjectByType<BlockListView>(); if (m_blockListView == null) { Debug.LogError("AppController: m_blockListView missing!"); ok = false; } }
        if (m_workspaceView == null) { m_workspaceView = FindFirstObjectByType<WorkSpaceView>(); if (m_workspaceView == null) { Debug.LogError("AppController: m_workspaceView missing!"); ok = false; } }
        if (m_categoryController == null) { m_categoryController = FindFirstObjectByType<CategoryController>(); if (m_categoryController == null) { Debug.LogError("AppController: m_categoryController missing!"); ok = false; } }
        if (m_workspaceController == null) { m_workspaceController = FindFirstObjectByType<WorkspaceController>(); if (m_workspaceController == null) { Debug.LogError("AppController: m_workspaceController missing!"); ok = false; } }
        if (m_blockDragController == null) { m_blockDragController = FindFirstObjectByType<BlockDragController>(); if (m_blockDragController == null) { Debug.LogError("AppController: m_blockDragController missing!"); ok = false; } }
        if (m_inputController == null) { m_inputController = FindFirstObjectByType<InputController>(); if (m_inputController == null) { Debug.LogError("AppController: m_inputController missing!"); ok = false; } }
        if (m_executionController == null) { m_executionController = FindFirstObjectByType<ExecutionController>(); if (m_executionController == null) { Debug.LogError("AppController: m_executionController missing!"); ok = false; } }
        if (m_blockStatusView == null) { m_blockStatusView = FindFirstObjectByType<BlockStatusView>(); if (m_blockStatusView == null) { Debug.LogError("AppController: m_blockStatusView missing!"); ok = false; } }
        return ok;
    }


    // VALIDAR UICanvasView INTERNAMENTE 
   
    private bool ValidateUICanvasRefs(UICanvasView uicv)
    {
        if (uicv == null) { Debug.LogError("Passed UICanvasView is null!"); return false; }
        if (uicv.CategoryButtonContainer == null) { Debug.LogError("AppController check: UICanvasView.CategoryButtonContainer is null!"); return false; }
        if (uicv.BlockTemplateContainerParent == null) { Debug.LogError("AppController check: UICanvasView.BlockTemplateContainer is null!"); return false; }
        if (uicv.CodingAreaRect == null) { Debug.LogError("AppController check: UICanvasView.CodingAreaContainer is null!"); return false; }
        return true;
    }
    private bool ValidateReferences()
    {
        bool valid = true;
        //  chequear y loguear error
        bool CheckRef(object obj, string name)
        {
            if (obj == null)
            {
                Debug.LogError($"AppController: Critical reference '{name}' is not assigned!");
                return false;
            }
            return true;
        }

        valid &= CheckRef(m_workspaceController, nameof(m_workspaceController));
        valid &= CheckRef(m_categoryController, nameof(m_categoryController));
        valid &= CheckRef(m_inputController, nameof(m_inputController));
        valid &= CheckRef(m_blockDragController, nameof(m_blockDragController));
        valid &= CheckRef(m_executionController, nameof(m_executionController));

        valid &= CheckRef(m_uiCanvasView, nameof(m_uiCanvasView));
        valid &= CheckRef(m_workspaceView, nameof(m_workspaceView));
        valid &= CheckRef(m_blockListView, nameof(m_blockListView));

        return valid;
    }

    //  encontrar o añadir componentes 
    private T FindOrAddComponent<T>(GameObject targetObject, T assignedComponent, string componentName) where T : Component
    {
        if (targetObject == null)
        {
            Debug.LogError($"AppController: Cannot find/add {componentName}, target GameObject is null.");
            return null;
        }

        T component = assignedComponent; // Usa el asignado en Inspector si existe
        if (component == null) component = targetObject.GetComponentInChildren<T>(); // Intenta buscarlo en hijos
        if (component == null) component = targetObject.GetComponent<T>(); // Intenta buscarlo en el mismo objeto
        if (component == null)
        {
            Debug.LogWarning($"AppController: {componentName} not found on or in children of {targetObject.name}. Adding component.");
            component = targetObject.AddComponent<T>();
        }
        return component;
    }


    // Placeholder para métodos futuros
    
    private void LoadInitialWorkspace()
    {
        Debug.Log("AppController: Loading initial workspace (if any)...");
        string savedXml = PlayerPrefs.GetString("LastWorkspace", "");
        if (!string.IsNullOrEmpty(savedXml))
        {
            WorkspaceSerializer.LoadFromXml(savedXml, m_workspaceModel); // Necesitas esta clase
            m_workspaceModel.FireChangePublic(WorkspaceChangeType.LoadFinish, null); // Notificar a la vista
        }
    }
    public void SaveWorkspaceState() { 
        //TODO Obtener estado de WorkspaceModel y serializar usando XmlSerializer
    }

    private void SaveWorkspaceOnQuit()
    {
        Debug.Log("AppController: Saving workspace...");
        string xml = WorkspaceSerializer.SaveToXml(m_workspaceModel);
        PlayerPrefs.SetString("LastWorkspace", xml);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        SaveWorkspaceOnQuit();
    }

    // --- Limpieza ---
    void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("AppController: Destroying...");
            m_workspaceModel?.Clear(); 
            Instance = null;
        }
    }
}
