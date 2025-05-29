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

    [Header("Configuración del Robot y Escena 3D")]
    [SerializeField] private GameObject m_RobotPrefab; // carga del robot dinámicamente como un prefab
    [SerializeField] private Transform m_RobotSpawnPoint;
    [SerializeField] private Camera m_main3DCamera; //Referencia a la cámara principal 3D. 
    [SerializeField] private GameObject m_robotEnvironmentRoot; // GameObject raíz del fondo 3D. 

    private GameObject m_currentRobotInstance;
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
            Logger.Log("<color=orange>AppController: Awake - Singleton instance created and set to DontDestroyOnLoad.</color>");
           
        }
        else
        {
            Destroy(gameObject);
            Logger.LogWarning("AppController: Duplicate instance of AppController detected. Destroying myself.");

        }
    }

    void OnDestroy()
    {
        ScratchBlocks.Dispose();
        if (Instance == this)
        {
            Instance = null;
            //Destroy(gameObject);
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
      //  Debug.Log("<color=orange>AppController: Start - Finding components...</color>");
        m_IsInitialized = false;
        yield return null;

        m_uiManager = FindFirstObjectByType<UICanvasView>();
        if (m_uiManager == null)
        {
            Debug.LogError("AppController: UICanvasManager not found!");
            yield break;
        }
       // Debug.Log("AppController: Found UICanvasView. Waiting for its core UI/View components to be ready (Awake phase)...", this);
        
        yield return new WaitUntil(() => m_uiManager.IsCoreComponentsReady());

        //  Debug.Log("AppController: UICanvasView components ready.");

        m_workspaceModel = m_uiManager.Workspace;

        m_categoryController = FindFirstObjectByType<CategoryController>();
        if (m_categoryController == null) m_categoryController = gameObject.AddComponent<CategoryController>();

        m_workspaceController = FindFirstObjectByType<WorkspaceController>();
        if (m_workspaceController == null) m_workspaceController = gameObject.AddComponent<WorkspaceController>();

        yield return new WaitUntil(() => m_uiManager.Workspace != null && m_uiManager.WorkSpaceView != null);
      //  Debug.Log($"AppController: UICanvasView reports core setup complete.");

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
       // Debug.Log($"AppController: Got Model (ID: {m_workspaceModel.Id}) and View components from UICanvasView.");

        RectTransform codingAreaRect = m_uiManager.CodingAreaPanelRect;
        RectTransform blockListAreaRect = m_uiManager.BlockListPanelRect;
        RectTransform categoryButtonContainer = m_uiManager.CategoryButtonContainerRect;
        ScrollRect middlePanelScrollRect = m_uiManager.MiddlePanelScrollRect;
         GameObject catButtonPrefab = m_uiManager.CategoryButtonPrefab;
        RectTransform dragLayerRect = m_uiManager.DragLayer;


       /* if (BlockObserver.Instance != null)
        {
            BlockObserver.Instance.Initialize(); // Llamará a su método de inicialización
        }
        else
        {
            Debug.LogError("AppController: BlockObserver.Instance is unexpectedly null after expected Awake(). Check Project Settings Script Execution Order for BlockObserver.");
            // Esto solo se ejecutaría si la configuración del orden falla de alguna manera extrema.
        }*/

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
      //  Debug.Log("<color=green>AppController: WorkSpaceView bound to Model and RectTransform.</color>", this);


        m_categoryController = FindFirstObjectByType<CategoryController>() ?? gameObject.AddComponent<CategoryController>();

        if (m_categoryController == null) m_categoryController = gameObject.AddComponent<CategoryController>();
        m_categoryController.InitializeController(m_blockListView, m_toolboxConfig); 
      //  Debug.Log("AppController: CategoryController initialized.");

        m_workspaceController = FindFirstObjectByType<WorkspaceController>() ?? gameObject.AddComponent<WorkspaceController>();

        if (m_workspaceController == null) m_workspaceController = gameObject.AddComponent<WorkspaceController>();
        m_workspaceController.InitializeController(m_workspaceModel, m_workspaceView);
       // Debug.Log("AppController: WorkspaceController initialized.");

        m_executionController = FindFirstObjectByType<ExecutionController>() ?? gameObject.AddComponent<ExecutionController>();
        //if (m_executionController == null) m_executionController = gameObject.AddComponent<ExecutionController>();
        
        // m_executionController.InitializeController(m_workspaceModel);

        if (m_executionController != null)
        {
            // ¡Esta línea es CRÍTICA y llama a la versión corregida de InitializeController!
            // Que a su vez asegura que CSharp.Interpreter y CSharp.Runner estén inicializados.
            m_executionController.InitializeController(m_workspaceModel);
        }
        else
        {
            Debug.LogError("AppController: FAILED to find or create ExecutionController! Cannot proceed with execution setup.");
            yield break;
        }

      /*  if (BlockObserver.Instance != null)
        {
            BlockObserver.Instance.SubscribeToExecutionController(m_executionController);
            Logger.Log("<color=green>AppController: BlockObserver subscription to ExecutionController successful.</color>");
            BlockObserver.Instance.Initialize(); // Reinicia el robot (esta parte tuya ya funciona)
        }
        else
        {
            Debug.LogError("AppController.Start(): BlockObserver.Instance is NULL. Check Script Execution Order for BlockObserver and ensure it exists in the scene.");
        }
      */
        //   Debug.Log("AppController: ExecutionController found/created.");

        m_inputController = FindFirstObjectByType<InputController>() ?? gameObject.AddComponent<InputController>();

        if (m_inputController == null) m_inputController = gameObject.AddComponent<InputController>();
        
        //m_inputController.Initialize(m_workspaceView);
    //    Debug.Log("AppController: InputController found/created.");

        BlockDragController dragController = FindFirstObjectByType<BlockDragController>(); // ?? gameObject.AddComponent<BlockDragController>();

        if (dragController == null)
        {
            //Debug.LogWarning("AppController: BlockDragController not found in scene. Adding one to AppController GameObject.");
           
            dragController = gameObject.AddComponent<BlockDragController>();
        }

        m_blockDragController = dragController;

        m_connectionController = FindFirstObjectByType<BlockConnectionController>() ?? gameObject.AddComponent<BlockConnectionController>();

        m_connectionController.InitializeController(m_workspaceModel, m_workspaceView, m_blockDragController);

    //    Debug.Log("AppController: ConnectionController initialized.");

        dragController.InitializeController(m_workspaceModel, m_workspaceView, m_workspaceController, m_connectionController, m_uiManager.DragLayer);
     //   Debug.Log("AppController: BlockDragController initialized.");

        m_blockListView.InitializeToolbox(
            m_workspaceModel,
            m_toolboxConfig,
            m_workspaceView,
            categoryButtonContainer, 
            middlePanelScrollRect,   
            catButtonPrefab,         
            m_categoryController
        );

        // Asegurar que la cámara 3D y el entorno estén deshabilitados al inicio.
        if (m_robotEnvironmentRoot != null)
        {
            m_robotEnvironmentRoot.SetActive(false); // El fondo 3D inicialmente oculto.
        }
        else
        {
            Debug.LogWarning("AppController: 'm_robotEnvironmentRoot' no está asignado en el Inspector. El entorno 3D no se gestionará.");
        }

        if (m_main3DCamera != null)
        {
            m_main3DCamera.enabled = true; // La cámara 3D inicialmente habilitada.
        }
        else
        {
            Debug.LogWarning("AppController: 'm_main3DCamera' no está asignado en el Inspector. La cámara 3D no se gestionará.");
        }


        m_IsInitialized = true;
    }

    /// <summary>
    /// Pedir a UICanvasView que cambie la visibilidad de la UI 
    ///Activar la cámara 3D.
    ///Mostrar/Activar el GameObject raíz de tu fondo 3D.
    ///Instanciar(o activar/reiniciar si ya existe) el robot.
    /// </summary>
    public void TriggerExecution()
    {
        Debug.Log("<color=blue>AppController: Triggering Execution Mode.</color>");

        // Alternar visibilidad de la UI (ocultar paneles, cambiar iconos)
        
        m_uiManager?.SetUISimulationState(true);

        //Activar la cámara 3D y el entorno 3D
        if (m_main3DCamera != null)
        {
            m_main3DCamera.gameObject.SetActive(true);
            m_main3DCamera.enabled = true; // Habilita la cámara para renderizar la escena 3D.
        }
        if (m_robotEnvironmentRoot != null)
        {
            m_robotEnvironmentRoot.SetActive(true); // Muestra el escenario 3D.
        }


       // StartCoroutine(StartCountdownAndExecutionSequence());

        // Instanciar/Reiniciar el robot
        if (m_currentRobotInstance == null && m_RobotPrefab != null && m_RobotSpawnPoint != null)
        {
            // Instancia el robot si aún no existe.
            m_currentRobotInstance = Instantiate(m_RobotPrefab, m_RobotSpawnPoint.position, m_RobotSpawnPoint.rotation);
            m_currentRobotInstance.transform.SetParent(m_robotEnvironmentRoot.transform);
            Debug.Log("AppController: Robot instanciado.");
        }
        else if (m_currentRobotInstance != null)
        {
            // Si el robot ya existe de una ejecución anterior, solo se actíva.
            m_currentRobotInstance.SetActive(true);
            
            Debug.Log("AppController: Robot existente activado.");
        }
        else
        {
            Debug.LogWarning("AppController: Prefab del Robot, punto de aparición o escenario 3D no asignado o instanciado.");
            return; // No podemos ejecutar sin robot/configuración.
        }


        StopAllCoroutines(); // Detiene todas las corrutinas de AppController, incluida cualquier cuenta atrás anterior

        if (m_executionController == null)
        {
            Debug.LogError("AppController: ExecutionController is null. Cannot start program.");
            m_uiManager.SetRobotMessageText("Error: Sistema de ejecución no iniciado.", true);
            return;
        }

       
        bool canStart = m_executionController.PreCheckExecutableBlocks(); // Crear este método nuevo en ExecutionController

        if (canStart)
        {
            // Solo si hay algo ejecutable, iniciamos la secuencia visual y la ejecución real
            StartCoroutine(StartCountdownAndExecutionSequence());
        }
        else
        {
            Debug.LogWarning("AppController: No executable blocks found in workspace. Not starting simulation.");
            m_uiManager.SetRobotMessageText("No hay bloques para ejecutar.", true);
        
            StartCoroutine(ResetUIAfterNoExecutableBlocks(2.0f)); // Coroutine para mostrar mensaje y luego restaurar UI
        }
    }


    private IEnumerator ResetUIAfterNoExecutableBlocks(float delay)
    {
        // Opcional: mostrar un mensaje "No hay bloques" antes de restaurar UI
        m_uiManager.SetRobotMessageText("No hay bloques para ejecutar.", true);
        yield return new WaitForSeconds(delay); // Espera unos segundos para que se lea

        // Ocultar mensaje y restaurar UI de edición
        m_uiManager.SetRobotMessageText("", false);
        m_uiManager.SetUISimulationState(false);

        // Destruir el robot si se había instanciado para el chequeo
        if (m_currentRobotInstance != null)
        {
            Destroy(m_currentRobotInstance);
            m_currentRobotInstance = null;
            Debug.Log("AppController: Robot de chequeo destruido.");
        }

        Debug.Log("AppController: UI restaurada a modo edición tras no encontrar bloques ejecutables.");
    }


    /// <summary>
    /// Este método desactiva la simulación y restaura la UI de desarrollo
    /// Pedir a UICanvasView que restaure la UI
    /// Desactivar la cámara 3D.
    /// Ocultar el GameObject raíz de tu fondo 3D.
    /// Destruir la instancia actual del robot (para una limpieza simple).
    ///  </summary>

    public void TriggerStop()
    {
        RobotBehaviour robotBehaviour = m_currentRobotInstance?.GetComponent<RobotBehaviour>();
        robotBehaviour?.StopAllActions(); // Asegura que el robot deje de moverse

        //  Ocultar la cámara 3D y el entorno 3D
        if (m_main3DCamera != null)
        {
            m_main3DCamera.enabled = true; // no Deshabilita la cámara 3D.
        }
        if (m_robotEnvironmentRoot != null)
        {
            m_robotEnvironmentRoot.SetActive(false); // Oculta tu escenario 3D.
        }

        // Destruir la instancia del robot para limpiar el estado para la próxima ejecución.
        if (m_currentRobotInstance != null)
        {
            Destroy(m_currentRobotInstance);
            m_currentRobotInstance = null; // Limpiar la referencia.
            Debug.Log("AppController: Robot destruido.");
        }

        // Restaurar la visibilidad de la UI de diseño.
        m_uiManager?.SetUISimulationState(false);
        
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

    private IEnumerator StartCountdownAndExecutionSequence()
    {
        //  LÓGICA DE INICIALIZACIÓN DEL ROBOT 
        if (m_RobotPrefab == null || m_RobotSpawnPoint == null)
        {
            Debug.LogError("AppController: Prefab del Robot o punto de aparición no asignado. No se puede iniciar la simulación.");
            yield break; // Aborta la corrutina
        }

        if (m_currentRobotInstance == null)
        {
            m_currentRobotInstance = Instantiate(m_RobotPrefab, m_RobotSpawnPoint.position, m_RobotSpawnPoint.rotation);
            m_currentRobotInstance.transform.SetParent(m_robotEnvironmentRoot.transform); 
            Debug.Log("AppController: Robot instanciado.");
        }
        else
        {
            // Si el robot ya existe ( tras un Stop/Start rápido), lo reposicionamos y activamos
            m_currentRobotInstance.transform.position = m_RobotSpawnPoint.position;
            m_currentRobotInstance.transform.rotation = m_RobotSpawnPoint.rotation;
            m_currentRobotInstance.SetActive(true);
            // Asegurarse de que no tenga movimientos pendientes
            RobotBehaviour rb = m_currentRobotInstance.GetComponent<RobotBehaviour>();
            if (rb != null) rb.StopAllActions();
            Debug.Log("AppController: Robot existente activado y reposicionado.");
        }
        // Asegurarse de que el robot se ve
        RobotBehaviour robotBehaviour = m_currentRobotInstance.GetComponent<RobotBehaviour>();
        if (robotBehaviour == null)
        {
            Debug.LogError("AppController: El robot no tiene el script RobotBehaviour.");
            m_uiManager.SetRobotMessageText("Error: Script RobotBehaviour no encontrado.", true);
            yield return new WaitForSeconds(3);
            m_uiManager.SetRobotMessageText("", false);
            yield break; // Aborta si no hay script de comportamiento
        }

        //  LÓGICA DEL CONTADOR DE CUENTA ATRÁS 
        m_uiManager.SetCountdownText("3", true);
        yield return new WaitForSeconds(1);
        m_uiManager.SetCountdownText("2", true);
        yield return new WaitForSeconds(1);
        m_uiManager.SetCountdownText("1", true);
        yield return new WaitForSeconds(1);
        m_uiManager.SetCountdownText("¡GO!", true);
        yield return new WaitForSeconds(0.5f);
        m_uiManager.SetCountdownText("", false); // Oculta el contador


        //  LÓGICA DE MOVIMIENTO DE PRUEBA 
      /*  m_uiManager.SetRobotMessageText("Moviendo robot 10 pasos...", true);
        Debug.Log("<color=green>AppController: Llamando a MoveRobotOverTime(10) en Robot para la prueba.</color>");
        
       // yield return robotBehaviour.MoveRobotOverTime(10); // Llama a la corrutina y espera

        m_uiManager.SetRobotMessageText("Movimiento de 10 pasos completado.", true);
        yield return new WaitForSeconds(2); // Muestra el mensaje por 2 segundos
        m_uiManager.SetRobotMessageText("", false); // Oculta el mensaje
      */
        //  INICIAR EJECUCIÓN REAL DE BLOQUES 

        Debug.Log("AppController: Iniciando ejecución de bloques via m_executionController.");
        m_executionController?.StartExecution();
    }

    }//fin clase AppController

