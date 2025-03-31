/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Crea la vista del espacio de trabajo
 */

using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System; 


public class WorkSpaceView : MonoBehaviour
{
    [Header("UI Setup (Assign in Inspector or via UICanvasView)")]
    
    [SerializeField] private RectTransform m_codingArea;

    [Header("View Components (Assign or Find)")]
   
    [SerializeField] private BlockStatusView m_StatusView;

    private WorkspaceModel m_WorkspaceModel;
    private BlockDragController m_BlockDragController; 
    private ExecutionController m_ExecutionController;
    private WorkSpaceView m_WorkSpaceView;
    
    private Dictionary<string, BlockView> m_blockViews = new Dictionary<string, BlockView>();

    public RectTransform CodingAreaRect => m_codingArea;
    public Canvas RootCanvas { get; private set; }

    public Camera EventCamera => RootCanvas?.worldCamera;
  
    public static WorkSpaceView Instance { get; private set; }
   
    public void InitializeView(WorkspaceModel model, BlockDragController dragController, RectTransform codingAreaRect)
    {
        Debug.Log("WorkspaceView Initializing...");

        // Validar y obtener referencias
        m_codingArea = codingAreaRect; // Asignar desde el parámetro
        if (m_codingArea == null)
        {
            Debug.LogError("WorkspaceView: CodingArea RectTransform was not provided during initialization! Cannot place blocks.", this.gameObject);
            this.enabled = false;
            if (m_codingArea == null) return;
        }

        // m_BlockDragController = dragController; 

        //  Limpiar y Vincular 
        ClearAllBlockViews();
        BindModel(model); // 

        Debug.Log("WorkspaceView Initialized and Bound.");
    }

    // Vincula la vista al modelo y se suscribe a sus eventos
    public void BindModel(WorkspaceModel model)
    {
        if (m_WorkspaceModel == model) return;
        UnbindModel(); // Desuscribir del modelo anterior

        m_WorkspaceModel = model;

        if (m_WorkspaceModel == null)
        {
            Debug.LogWarning("WorkspaceView: BindModel called with null model.");
            return;
        }

        // Suscribirse a los eventos globales del WorkspaceModel
        m_WorkspaceModel.OnChange += HandleWorkspaceChange;

        // Poblar la vista con los bloques existentes en el modelo al inicio
        PopulateInitialBlocks();
        Debug.Log($"WorkSpaceView bound to WorkspaceModel {m_WorkspaceModel.Id}");

    }

    // Desvincula del modelo y limpia
    public void UnbindModel()
    {
        if (m_WorkspaceModel != null)
        {
            m_WorkspaceModel.OnChange -= HandleWorkspaceChange;
            m_WorkspaceModel = null;
        }
        ClearAllBlockViews();
    }


    //  Creación/Destrucción de Vistas 

    private void HandleWorkspaceChange(WorkspaceModel workspace, WorkspaceChangeType changeType, object payload)
    {
        if (workspace != m_WorkspaceModel || this == null) return; // Seguridad

        try
        {
            switch (changeType)
            {
                case WorkspaceChangeType.BlockAdded:
                    if (payload is BlockModel addedBlock) CreateBlockView(addedBlock);
                    break;

                case WorkspaceChangeType.BlockRemoved:
                    if (payload is BlockModel removedBlock) RemoveBlockView(removedBlock);
                    break;

                case WorkspaceChangeType.BlockMoved:
                    // El BlockView ya actualizó su posición XY al recibir el evento del modelo.
                    //TODO ¿Necesitamos hacer algo aquí? Quizás redibujar conexiones si se superponen?
                    
                    break;

                case WorkspaceChangeType.ConnectionCreated:
                case WorkspaceChangeType.ConnectionBroken:
                    // Los BlockViews/ConnectionViews implicados se actualizan solos a través de sus observadores del modelo.
                    
                    QueueFullLayoutUpdate(); //Podríamos forzar un re-layout general si la conexión afecta mucho
                    break;

                case WorkspaceChangeType.Clear:
                    ClearAllBlockViews();
                    break;

                case WorkspaceChangeType.LoadFinish:
                    // Después de cargar, asegurar que todas las vistas estén posicionadas y layout OK
                    
                    Debug.Log("WorkspaceView: Forcing layout after LoadFinish.");
                    QueueFullLayoutUpdate();
                    break;

                    //TODO Caso Variables/Procedimientos -Si WorkspaceView necesita mostrarlos de alguna forma
                    //TODO  case WorkspaceChangeType.VariableAdded: UpdateVariableDisplays(); break;
                    // ...
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in WorkspaceView.HandleWorkspaceChange ({changeType}): {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Crea las vistas para los bloques ya existentes en el modelo )
    private void PopulateInitialBlocks()
    {
        if (m_WorkspaceModel == null) return;
        ClearAllBlockViews(); // Limpiar primero
        Debug.Log($"WorkspaceView: Populating initial blocks ({m_WorkspaceModel.BlockDatabase.Count} total)...");
        // Iterar sobre TODOS los bloques, no solo TopBlocks, porque necesitamos crear
        
        foreach (BlockModel block in m_WorkspaceModel.TopBlocks) // Solo procesa TopBlocks (raíces)
        {
            CreateBlockView(block); 
        }
       
        // Limpiar vistas existentes
        /*ClearAllBlockViews();
        // Crear vista para cada bloque en el modelo
        foreach (BlockModel block in m_WorkspaceModel.GetAllBlocks())
        { // Todos los bloques
            CreateBlockView(block);
        }*/
        // Forzar layout al final
        QueueFullLayoutUpdate();
    }


    // Crea la vista para un BlockModel específico
    private void CreateBlockView(BlockModel blockModel)
    {
        if (blockModel == null || m_blockViews.ContainsKey(blockModel.ID))
            return; 

        Debug.Log($"WorkspaceView: Creating BlockView for {blockModel.Type} ({blockModel.ID})");

        BlockView newView = BlockViewFactory.CreateView(blockModel, this); 
        if (newView == null)
        {
            Debug.LogError($"BlockViewFactory failed to create view for block type {blockModel.Type}");
            return;
        }

        //  Configurar Padre y Posición Inicial
        newView.transform.SetParent(m_codingArea, false);
        newView.XY = blockModel.XY; // Posicionar visualmente donde dice el modelo

       
        newView.BindModel(blockModel); 

        
        m_blockViews[blockModel.ID] = newView;

 
    }


    // Elimina la vista para un BlockModel específico
    private void RemoveBlockView(BlockModel blockModel)
    {
        if (blockModel == null || !m_blockViews.TryGetValue(blockModel.ID, out BlockView viewToRemove))
            return; 

        Debug.Log($"WorkspaceView: Removing BlockView for {blockModel.Type} ({blockModel.ID})");

        m_blockViews.Remove(blockModel.ID); // Quitar del diccionario

        // Desvincular antes de destruir 
        viewToRemove.UnbindModel();

        // Destruir el GO de la vista
        Destroy(viewToRemove.gameObject);
    }

    // Limpia todas las vistas de bloques
    private void ClearAllBlockViews()
    {
        // Copiar valores - vamos a modificar el diccionario al destruir
        List<BlockView> viewsToClear = new List<BlockView>(m_blockViews.Values);
        m_blockViews.Clear(); // Limpiar diccionario primero

        foreach (BlockView view in viewsToClear)
        {
            if (view != null)
            {
                view.UnbindModel(); // Desvincular
                Destroy(view.gameObject);
            }
        }
        Debug.Log("WorkspaceView: Cleared all block views.");
    }


    //  Búsqueda de Vistas Hijas

    public BlockView GetBlockView(BlockModel model)
    {
        if (model == null) return null;
        m_blockViews.TryGetValue(model.ID, out BlockView view);
        return view;
    }

    public ConnectionView GetConnectionView(ConnectionModel connectionModel)
    {
        BlockView blockView = GetBlockView(connectionModel?.SourceBlockModel);
        return blockView?.FindConnectionView(connectionModel); 
    }

    public FieldView GetFieldView(FieldModel fieldModel)
    {
        BlockView blockView = GetBlockView(fieldModel?.ParentInput?.SourceBlockModel);
        return blockView?.FindFieldView(fieldModel); 
    }


    // --- Layout Update ---
    private bool m_needsFullLayoutUpdate = false;
    public void QueueFullLayoutUpdate() => m_needsFullLayoutUpdate = true;

    void LateUpdate()
    {
        if (m_needsFullLayoutUpdate)
        {
            m_needsFullLayoutUpdate = false;
            Debug.Log("WorkspaceView: Forcing full layout update...");
            // Forzar un re-layout de todas las vistas Top level
            foreach (var blockView in m_blockViews.Values.Where(bv => bv.BlockModel != null && bv.BlockModel.IsPotentiallyTopBlock()))
            { 
              // Iniciar cálculo desde el bloque raíz
                blockView.QueueForceLayoutUpdate(); 
            }
            
        }
    }

    //  Limpieza 
    void OnDestroy()
    {
        UnbindModel(); // Desuscribirse del modelo
                      
    }

    void Awake()
    {
        Debug.Log("WorkSpaceView: Awake starting...");

        // Singleton Setup
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Si persiste entre escenas
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Duplicate WorkSpaceView instance detected on {gameObject.name}. Destroying duplicate.", this.gameObject);
            Destroy(gameObject);
            return;
        }

        RootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (RootCanvas == null)
        {
            Debug.LogError("WorkSpaceView: Root Canvas not found! This component needs to be under a Canvas hierarchy.", this.gameObject);
            this.enabled = false; // No puede funcionar sin Canvas
            return;
        }

        // Buscar StatusView si no está asignada 
        if (m_StatusView == null) m_StatusView = GetComponentInChildren<BlockStatusView>();
        
        Debug.Log("WorkSpaceView: Awake finished basic setup.");
        
    }

    void Start() 
    {
        m_ExecutionController = FindFirstObjectByType<ExecutionController>(); 
        m_WorkSpaceView = FindFirstObjectByType<WorkSpaceView>(); 

        if (m_ExecutionController == null) Debug.LogError("ExecutionController not found", this.gameObject);
        if (m_WorkSpaceView == null) Debug.LogError("WorkSpaceView not found", this.gameObject);

        // Suscribirse
        if (m_ExecutionController != null)
        {
            //TODO
        }
    }

    public void InitializeView(WorkspaceModel model, BlockDragController dragController) 
    {
        Debug.Log("WorkspaceView Initializing...");

        if (m_codingArea == null || RootCanvas == null)
        {
            Debug.LogError("WorkspaceView cannot initialize, core components missing (CodingArea or RootCanvas). Check Awake logs.", this.gameObject);
            return;
        }

        m_BlockDragController = dragController;
        if (m_BlockDragController == null) Debug.LogWarning("WorkspaceView initialized without a BlockDragController.", this.gameObject);


        ClearAllBlockViews();
        BindModel(model); 

        Debug.Log("WorkspaceView Initialized and Bound.");
    }

   
    internal void RegisterBlockView(BlockView view)
    {
        if (view?.BlockModel != null && !m_blockViews.ContainsKey(view.BlockModel.ID))
        {
            m_blockViews.Add(view.BlockModel.ID, view);
        }
        else if (view?.BlockModel != null)
        {
            Debug.LogWarning($"BlockView for {view.BlockModel.ID} already registered.");
        }
    }
    

    internal void UnregisterBlockView(BlockView view)
    {
        if (view?.BlockModel != null)
        {
            m_blockViews.Remove(view.BlockModel.ID);
        }
    }
}