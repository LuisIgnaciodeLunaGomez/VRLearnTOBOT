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
 * Descripción:  proveer el RectTransform del m_codingArea y la referencia al Canvas / Camera
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkSpaceView : MonoBehaviour
{
    private RectTransform m_codingArea;        
    private BaseToolbox m_toolbox;           
    private BlockStatusView m_blockStatusView;
    //[SerializeField] private PlayControlView m_playControlView; //Los controles de ejecución TODO: Implementar

    public BaseToolbox Toolbox => m_toolbox;
    public BlockStatusView BlockStatusView => m_blockStatusView;
    //public PlayControlView PlayControlView => m_playControlView; 
    public RectTransform CodingArea => m_codingArea;
    public BlockStatusView StatusView { get; private set; }
    //  Referencia al Modelo Lógico (de UBlockly) 
    private WorkSpaceModel m_WorkspaceModel;
    public WorkSpaceModel Workspace => m_WorkspaceModel;
    private Dictionary<string, BlockView> m_blockViews = new Dictionary<string, BlockView>();
    // Cache 
    public Canvas RootCanvas { get; private set; } 
    public Camera EventCamera => RootCanvas?.worldCamera;
    public static WorkSpaceView Active {get; private set;}

    void Awake()
    {
        if (Active == null)
        {
            Active = this;
            // DontDestroyOnLoad(gameObject)
        }
        else if (Active != this)
        {
            Debug.LogWarning($"Duplicate WorkSpaceView instance on {gameObject.name}. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("WorkSpaceView: Awake starting...");
    }

    /**
     * Descripción: Vincula el modelo lógico a esta vista.
     * @param workspace El modelo lógico de UBlockly a vincular.
     * @param toolboxRef Referencia al Toolbox (BaseToolbox) para la vista.
     * @param codingAreaRect RectTransform del área de código donde se colocarán los bloques.
     * @param statusViewRef Referencia opcional al BlockStatusView para mostrar el estado de ejecución.
     */
    public void BindModel(WorkSpaceModel workspace, BaseToolbox toolboxRef, RectTransform codingAreaRect, BlockStatusView statusViewRef = null)
    {
        Debug.Log($"<color=cyan>WorkSpaceView ({GetInstanceID()}): BindModel called.</color>", this);

        if (m_WorkspaceModel != null && m_WorkspaceModel != workspace)
        {
            UnbindModel(); // Desvincular modelo anterior si es diferente
        }
        else if (m_WorkspaceModel == workspace && m_WorkspaceModel != null)
        {
            Debug.LogWarning($"WorkSpaceView: BindModel called with the same workspace model {workspace.Id}. Rebinding/refreshing.", this);
            CleanViews();
        }

        m_WorkspaceModel = workspace;
        m_toolbox = toolboxRef;
        m_codingArea = codingAreaRect;     
        m_blockStatusView = statusViewRef;  

        // Validaciones
        if (m_WorkspaceModel == null)
        {
            Debug.LogError("WorkSpaceView.BindModel: Cannot bind a null workspace!", this);
            return;
        }
        if (m_toolbox == null)
        {
            
            Debug.LogWarning("WorkSpaceView.BindModel: Toolbox reference is null!", this);
        }
        if (m_codingArea == null)
        {
            Debug.LogError("WorkSpaceView.BindModel: CodingArea reference is null! This is essential.", this);
            this.enabled = false; // Desactivar si falta el área
            return;
        }
        if (m_blockStatusView == null) {
            Debug.LogWarning("WorkSpaceView.BindModel: BlockStatusView reference is null.", this);
         }

        Debug.Log($"<color=lightblue>WorkSpaceView: Binding to Workspace {workspace.Id}, Toolbox: {m_toolbox?.GetType().Name ?? "NULL"}, CodingArea: {m_codingArea?.name ?? "NULL"}</color>");

        //Debug.Log($"<color=lightblue>WorkSpaceView: Binding to Workspace {workspace.Id}, Toolbox: {m_toolbox?.name}, CodingArea: {m_codingArea?.name}</color>");

        if (workspace.TopBlocks.Count > 0)
        {
            Debug.Log($"<color=lightblue>WorkSpaceView: Model has {workspace.TopBlocks.Count} top blocks. Building views...</color>");
            BuildViews(); 
        }
        else
        {
            Debug.Log("<color>WorkSpaceView: Model is empty, no initial views to build.</color>");
        }

        Debug.Log($"<color=green>WorkSpaceView: Successfully bound to Workspace {workspace.Id}.</color>");
    }

    /**
     * Descripción: Desvincula el modelo lógico de esta vista.
     * Limpia las vistas de bloques y otros elementos visuales.
     */
    public void UnbindModel()
    {
        if (m_WorkspaceModel == null) return;
        Debug.Log($"WorkSpaceView: Unbinding from Workspace {m_WorkspaceModel.Id}...");
       // m_playControlView?.Reset(); 
        m_toolbox?.Clean();       
        CleanViews();
        m_WorkspaceModel.Dispose(); 
        m_WorkspaceModel = null;
    }

    #region Gestión de Vistas 

    /**
     *Descripción: Obtiene la BlockView asociada a un bloque lógico 
     * Busca en el diccionario de vistas usando el ID del bloque.
     * Si la vista fue destruida pero no removida, la elimina del diccionario.
     * @param block El bloque lógico del que se quiere obtener la vista.
     * @return La BlockView asociada al bloque, o null si no se encuentra.
     */
    public BlockView GetBlockView(BlockModel block)
    {
        if (block == null) return null;
        m_blockViews.TryGetValue(block.ID, out var view);
      
        if (view != null && view.gameObject == null)
        {
            Debug.LogWarning($"GetBlockView found a destroyed view for block {block.ID}. Removing reference.");
            m_blockViews.Remove(block.ID);
            return null;
        }
        return view;
    }

    /**
     * Descripción: Añade una BlockView al diccionario de vistas. Se usa para mantener el diccionario actualizado con las vistas activas.
     * @param blockView La BlockView a añadir al diccionario.
     */
    public void AddBlockView(BlockView blockView)
    {
        if (blockView?.Block == null) return;
        m_blockViews[blockView.Block.ID] = blockView;
    }

    /**
     * Descripción: Remueve una BlockView del diccionario de vistas. Se usa para limpiar referencias a vistas destruidas o no válidas.
     */
    public void RemoveBlockView(BlockView blockView)
    {
        if (blockView?.Block != null)
        {
            m_blockViews.Remove(blockView.Block.ID);
        }
    }

    /**
     * Descripción: Clona el modelo y crea una nueva vista para él en la posición dada.
     * @param originalBlockView La BlockView original que se quiere clonar.
     * @param logicalPosition La posición lógica donde se colocará la nueva vista.
     * @return La nueva BlockView clonada, o null si hubo un error.
     */
    public BlockView CloneBlockView(BlockView originalBlockView, Vector2 logicalPosition)
    {
        if (originalBlockView?.Block == null)
        {
            Debug.LogError("CloneBlockView: Original BlockModel or its model is null");
            return null;
        }

        BlockModel newBlockModel = originalBlockView.Block.Clone();
        if (newBlockModel == null)
        {
            Debug.LogError($"Failed to clone BlockModel Model {originalBlockView.Block.ID}");
            return null;
        }

        newBlockModel.XY = logicalPosition; // Establecemos la posición lógica inicial
        BlockView newView = BlockViewFactory.CreateView(newBlockModel);

        if (newView != null)
        {
            newView.InToolbox = false; // No está en la toolbox
            newView.transform.SetParent(m_codingArea, false); // Padre visual
            newView.XY = logicalPosition; // Sincronizar posición visual 
                                          
            Debug.Log($"Cloned block {originalBlockView.Block.ID} -> New block {newBlockModel.ID} with View {newView.name}");
        }
        else
        {
            Debug.LogError($"BlockViewFactory failed to create view for cloned block {newBlockModel.ID}");
            newBlockModel.Dispose(false); // Limpiamos el modelo clonado si la vista falló
        }

        return newView;
    }

    /**
     * Descripción: Construye las vistas para todos los bloques Top existentes en el modelo Workspace.
     */ 
    public void BuildViews()
    {
        if (m_WorkspaceModel == null) return;
        Debug.Log($"WorkSpaceView: BuildViews for {m_WorkspaceModel.TopBlocks.Count} top blocks...");
        CleanViews();

        List<BlockModel> topBlocks = m_WorkspaceModel.GetTopBlocks(false); // Obtener TopBlocks del modelo
        foreach (BlockModel block in topBlocks)
        {
            BuildBlockViewRecursive(block); 
        }
        Debug.Log($"WorkSpaceView: BuildViews finished. Total views in dict: {m_blockViews.Count}");
        // LayoutRebuilder.ForceRebuildLayoutImmediate(m_codingArea); 
    }

    /** Descripción: Construye la vista de un bloque y sus bloques hijos de forma recursiva.
     * @param block El bloque lógico del que se quiere construir la vista.
     * @return La BlockView creada, o null si hubo un error.
     */
    private BlockView BuildBlockViewRecursive(BlockModel block)
    {
        if (block == null) return null;
        // Evitamos crear vistas duplicadas si ya existe por alguna razón
        if (m_blockViews.ContainsKey(block.ID)) return m_blockViews[block.ID];

        BlockView view = BlockViewFactory.CreateView(block);
        if (view == null) return null; // Falló la creación

        // Configuración de la vista creada (padre, posición, estado)
        view.InToolbox = false; // Pertenece al workspace
        view.transform.SetParent(m_codingArea, false);
        view.XY = block.XY;
     
        // Procesar bloques conectados a Inputs
        foreach (InputModel input in block.InputList)
        {
            if (input.Connection != null && input.Connection.IsConnected)
            {
                BuildBlockViewRecursive(input.Connection.TargetBlock);
            }
        }

        // Procesar bloque conectado a Next
        if (block.NextConnection != null && block.NextConnection.IsConnected)
        {
            BuildBlockViewRecursive(block.NextConnection.TargetBlock);
        }

        return view;
    }

    /**
     * Descripción: Limpia todas las vistas de bloque gestionadas por este WorkspaceView.
     */
    public void CleanViews()
    {
        if (m_blockViews.Count == 0) return;
        Debug.Log($"WorkSpaceView: Cleaning {m_blockViews.Count} views...");

        // Copio keys o values porque Dispose modificará el diccionario
        List<BlockView> viewsToDispose = new List<BlockView>(m_blockViews.Values);
        m_blockViews.Clear(); // Limpiamos el diccionario 

        foreach (var view in viewsToDispose)
        {
            if (view != null && view.gameObject != null)
            {
          
                view.Dispose();
            }
        }
        Debug.Log("WorkSpaceView: CleanViews finished.");
    }

    /** 
     * Descripción: Encuentra la ConnectionView visual asociada a un ConnectionModel lógico específico.
     * @param model El ConnectionModel del que se quiere obtener la vista.
     * @return La ConnectionView asociada al modelo, o null si no se encuentra.
     */
    public ConnectionView GetConnectionView(ConnectionModel model)
    {
        if (model == null)
        {
            Debug.LogWarning("GetConnectionView called with a null ConnectionModel.");
            return null;
        }
        if (model.SourceBlock == null)
        {
            Debug.LogWarning($"GetConnectionView called for a ConnectionModel without a SourceBlockModel (ID: {model.Type}, Block?: {model.SourceBlock?.ID ?? "NULL"})");
            return null;
        }

        BlockView sourceBlockView = GetBlockView(model.SourceBlock); 
        if (sourceBlockView == null)
        {
           
            Debug.LogWarning($"GetConnectionView could not find the BlockView for SourceBlock ID: {model.SourceBlock.ID}");
            return null;
        }

        return sourceBlockView.FindConnectionView(model);
    }

    private BlockView m_BlockOverTrash = null;

    /**
     * Descripción: Verifica si un bloque está sobre el área de la papelera.
     * @param blockView La BlockView que se quiere verificar.
     */
    public void CheckTrashBin(BlockView blockView)
    {
        // Lógica para detectar si las coordenadas de blockView están sobre el icono/área de la papelera- revisar lo que vamos a hacer con la papelera
        bool isOver = false; 
        if (TrashCanRect != null)
        {
            Vector3[] worldCorners = new Vector3[4];
            TrashCanRect.GetWorldCorners(worldCorners);
            Rect trashWorldRect = new Rect(worldCorners[0].x, worldCorners[0].y, worldCorners[2].x - worldCorners[0].x, worldCorners[2].y - worldCorners[0].y);
            isOver = trashWorldRect.Contains(blockView.transform.position); 

            HighlightTrashBin(isOver); 
        }

        m_BlockOverTrash = isOver ? blockView : null;
    }

    /**
     * Descripción: Verifica si un bloque está sobre la papelera.
     * @param blockView La BlockView que se quiere verificar.
     * @return true si el bloque está sobre la papelera, false en caso contrario.
     */
    public bool IsOverTrashBin(BlockView blockView)
    {
        return m_BlockOverTrash == blockView; // Devuelve true si este bloque era el último sobre la papelera
    }

    /**
     * Descripción: Resalta o desresalta la papelera visualmente.
     * @param highlight true para resaltar, false para desresaltar.
     */
    public void HighlightTrashBin(bool highlight) { /*TODO*/}
    public RectTransform TrashCanRect;

    #endregion


    void OnDestroy()
    {
        if (Active == this)
        {
         
            Debug.Log("WorkSpaceView.OnDestroy: Cleaning up UBlockly...");
            ScratchBlocks.Dispose(); 
            BlockViewSettings.Dispose(); // Limpia caché de settings
            BlockResMgr.Dispose();       // Limpia caché de recursos
            // Resources.UnloadUnusedAssets(); 

            Active = null; // Limpiar referencia estática
        }
        Debug.Log("WorkSpaceView: Destroyed completely.");
    }

    /**
     * Descripción: Método de limpieza al destruir la vista.
     * Se asegura de desvincular el modelo y limpiar las vistas.
     */
    public void Dispose()
    {
        Debug.Log("WorkSpaceView.Dispose() called.");
        UnbindModel(); 
    }

    /**
     * Descripción: Convierte una posición de pantalla a la posición lógica dentro del espacio de trabajo.
     * @param screenPoint La posición de pantalla a convertir.
     * @param eventCamera La cámara asociada al evento (puede ser null si el Canvas es Screen Space Overlay).
     * @return La posición lógica dentro del espacio de trabajo.
     */
    public Vector2 ScreenPointToWorkspaceLogicalPosition(Vector2 screenPoint, Camera eventCamera)
    {
        if (m_codingArea == null || RootCanvas == null)
        {
            Debug.LogError("ScreenPointToWorkspaceLogicalPosition: CodingArea or RootCanvas is null!");
            return Vector2.zero;
        }

        Vector2 localPoint; //relativo al pivot del m_codingArea.
                           

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_codingArea, screenPoint, eventCamera, out localPoint
        );

        if (!success)
        {
            Debug.LogWarning("ScreenPointToLocalPointInRectangle failed. Returning zero.");
            return Vector2.zero;
        }
      
        float workspaceScale = 1.0f; // TODO: Reemplazar con Workspace.Scale o similar - float workspaceScale = Workspace?.Options?.Scale ?? 1.0f;

        //Origen del Workspace:  (0,0) esquina superior izquierda.
        Vector2 workspaceOriginOffset = Vector2.zero; 
        float codingAreaHeight = m_codingArea.rect.height;
        Vector2 logicalPosition;
        logicalPosition.x = (localPoint.x / workspaceScale);
        logicalPosition.y = (codingAreaHeight / workspaceScale) - (localPoint.y / workspaceScale); // Y lógica = AltoTotal - Y_UI

        logicalPosition -= workspaceOriginOffset;
      
        // Debug.Log($"ScreenToLogic: Screen={screenPoint}, Local={localPoint}, Logic={logicalPosition}");

        return logicalPosition;
    }
} // Fin Clase WorkSpaceView

   
