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
 * Versión: 1.0.1
 * 
 * Descripción: Controlador de arrastre de bloques en el espacio de trabajo.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI;

public class BlockDragController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{   
    public static BlockDragController Instance { get; private set; }

    private WorkSpaceModel _workspace;

    private WorkSpaceModel m_Workspace {
        get { return _workspace; }
        set
        {
            
         //   Debug.LogError($"<color=red>HASHCODE_CHECK - BlockDragController - m_Workspace Setter Called!");
            //Debug.LogError($"  -> Current Value HashCode: {_workspace?.GetHashCode()}");
           // Debug.LogError($"  -> New Value HashCode Attempting to Set: {value?.GetHashCode()}");

            _workspace = value; 
        }
    } 
    private WorkSpaceView m_WorkspaceView;

    //Referencia a controladores
    private WorkspaceController m_workspaceController;
    private BlockConnectionController m_connectionController; //Inyecto el controlador


    private BlockView m_DraggingBlockView = null; //Clon o bloque arrastrado A
    private BlockModel m_DraggingBlockModel = null; //Modelo clon A
    private Vector2 m_ScreenSpaceDragOffset;

    public BlockView DragginBlockView => m_DraggingBlockView; //Propiedad para acceder a la vista del bloque arrastrado

    public BlockModel DraggingBlockModel => m_DraggingBlockModel; //Propiedad para acceder al modelo del bloque arrastrado
    private bool m_IsPotentialDrag = false; 
    private bool m_IsDragging = false;

    private bool m_WasTemplateClone = false;
    private BlockModel m_PendingCloneModel = null;
    private RectTransform m_DragLayerRect;

    //Conexiones
   // private ConnectionModel m_BestTargetConnection = null;      
   // private ConnectionModel m_SourceDragConnection = null;      
   // private ConnectionView m_HighlightedTargetView = null;

    //Depuración
    private RectTransform m_RootCanvasRect;
   
    private float dragThreshold = 5f; //Umbral para el inicio del arrastre
    private Vector2 m_PointerDownPosition;

    [Tooltip("Radius in Workspace logical units to search for connections.")]
    [SerializeField] private int m_ConnectionSearchRadius = 60; 
    [Tooltip("Max distance in Workspace logical units for a valid snap.")]
    [SerializeField] private int m_SnapDistance = 40; 

    private Camera m_CachedCamera; 
    private RectTransform m_CodingAreaRect; 
   // private Canvas m_RootCanvas;            

    void Awake()
    {

        //Debug.LogError("HASHCODE_CHECK - MiControlador - AWAKE - HashCode(this): " + this.GetHashCode());
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    /// <summary>
    /// Initializes the BlockDragController with references to the workspace model, view, and controller.
    /// </summary>
    /// <param name="dragLayerRect"> The RectTransform of the drag layer where blocks will be dragged.</param>
    /// <param name="Workspace"> The workspace model to be used.</param>
    /// <param name="WorkSpaceView"> The workspace view to be used.</param>
    /// <param name="wsController"> The workspace controller to be used.</param>
    /// <param name="dragLayerRect"> The RectTransform of the drag layer where blocks will be dragged.</param>"
    public void InitializeController(WorkSpaceModel Workspace, WorkSpaceView WorkSpaceView, WorkspaceController wsController, BlockConnectionController connController, RectTransform dragLayerRect)
    {
        m_Workspace = Workspace ?? throw new ArgumentNullException(nameof(Workspace));
        m_workspaceController = wsController ?? throw new ArgumentNullException(nameof(wsController));
        m_WorkspaceView = WorkSpaceView ?? throw new ArgumentNullException(nameof(WorkSpaceView));
        m_connectionController = connController ?? throw new ArgumentNullException(nameof(connController));

        m_DragLayerRect = dragLayerRect;

        if (m_WorkspaceView != null)
        {
            m_CodingAreaRect = m_WorkspaceView.CodingArea;
           // Debug.Log($"BlockDragController Initialized: m_CodingAreaRect is {(m_CodingAreaRect == null ? "NULL" : m_CodingAreaRect.name)}", this);
   
            m_CachedCamera = m_WorkspaceView.EventCamera;
            Canvas rootCanvas = m_WorkspaceView.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.isRootCanvas)
            {
                m_RootCanvasRect = rootCanvas.transform as RectTransform; // Guardar el RectTransform del Canvas
                m_CachedCamera = rootCanvas.worldCamera; // Obtener cámara (null para Overlay)
               // Debug.Log($"BlockDragController Initialized. RootCanvas: {m_RootCanvasRect?.name ?? "NULL"}, Camera: {m_CachedCamera?.name ?? "NULL (Overlay?)"}");
            }
            else
            {
                Debug.LogError("BlockDragController: Could not find root Canvas!", this);
   
            }

        }
        else
        {
            Debug.LogError("BlockDragController: WorkspaceView is null, cannot get RootCanvas or CodingArea!");
        }

        //Debug.LogError($"<color=red>HASHCODE_CHECK - BlockDragController Initialize - Received/Stored Workspace HashCode: {m_Workspace?.GetHashCode()}");

    }

    private void ResetDragState()
    {
        m_IsDragging = false;
        if (m_DraggingBlockView != null)
        {
            var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true; 
        }

        // Limpiar Clon A 
        BlockView wasDragging = m_DraggingBlockView;

        m_DraggingBlockView = null;
        m_DraggingBlockModel = null;

        if (wasDragging != null)
        {
            var cg = wasDragging.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;
        }

        m_PendingCloneModel = null; 

       // m_BestTargetConnection = null;
       // m_SourceDragConnection = null;
       // ClearHighlight(); 
        m_WasTemplateClone = false;
    }

    /*
    private void ClearHighlight()
    {
        if (m_HighlightedTargetView != null)
        {
            m_HighlightedTargetView.Highlight(false); 
            m_HighlightedTargetView = null;
        }
    }*/

    private ConnectionView FindConnectionView(ConnectionModel model)
    {
        if (model?.SourceBlock == null || m_WorkspaceView == null) return null;

        BlockView blockView = m_WorkspaceView.GetBlockView(model.SourceBlock);
        if (blockView == null) return null;

        switch (model.Type)
        {
            case EConnection.OutputValue:
                return blockView.GetConnectionView(EConnection.OutputValue);
            case EConnection.PrevStatement:
                return blockView.GetConnectionView(EConnection.PrevStatement);
            case EConnection.NextStatement:
                if (blockView.Block.NextConnection == model)
                    return blockView.GetConnectionView(EConnection.NextStatement);
                else 
                {
                    for (int i = 0; i < blockView.Block.InputList.Count; i++)
                    {
                        if (blockView.Block.InputList[i].Connection == model)
                            return blockView.GetInputConnectionView(i); 
                    }
                }
                break; 
            case EConnection.InputValue:
                for (int i = 0; i < blockView.Block.InputList.Count; i++)
                {
                    if (blockView.Block.InputList[i].Connection == model)
                        return blockView.GetInputConnectionView(i); 
                }
                break;
        }
        Debug.LogWarning($"FindConnectionView: Could not find view for connection type {model.Type} on block {model.SourceBlock.ID}");
        return null;
    }
    /*
    private void HighlightConnection(ConnectionView targetView)
    {
        if (targetView != null)
        {
            targetView.Highlight(true);
          //  m_HighlightedTargetView = targetView;
        }
    }*/

    public void RegisterPendingClone(BlockModel clonedModel)
    {
        if (clonedModel == null) return;
        m_PendingCloneModel = clonedModel;

       // Debug.Log($"BlockDragController: Registered pending clone {clonedModel.ID}. Waiting for StartDraggingTemplate.");
    }

    public void CancelPendingClone(BlockModel modelToCancel = null, string reason = "Cancelled")
    {
        if (modelToCancel == null) modelToCancel = m_PendingCloneModel;
        if (modelToCancel == null) return; 

        Debug.Log($"BlockDragController: Cancelling pending clone {modelToCancel.ID}. Reason: {reason}");

        BlockView viewToDestroy = m_WorkspaceView?.GetBlockView(modelToCancel);
        if (viewToDestroy != null)
        {
            viewToDestroy.Dispose();
            Debug.Log($"BlockDragController: Destroyed BlockView for cancelled clone {modelToCancel.ID}.");
        }

        if (m_DraggingBlockModel == modelToCancel)
        {
            ResetDragState(); 
        }
        else
        {
            if (m_PendingCloneModel == modelToCancel)
                m_PendingCloneModel = null;
        }
    }

    private Vector2 GetPointerPosInWorkspace(PointerEventData eventData)
    {
        Vector2 localPos = Vector2.zero;
        if (m_CodingAreaRect != null && m_CachedCamera != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CodingAreaRect, eventData.position, null, out localPos);
            Debug.Log($"Camera Used for Conversion: {m_CachedCamera?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError("GetPointerPosInWorkspace: Missing CodingAreaRect or CachedCamera!");
           
        }
        return localPos;
    }

    void OnDestroy()
    {
        if (m_PendingCloneModel != null)
        {
            CancelPendingClone(m_PendingCloneModel, "Controller Destroyed");
        }

        if (Instance == this)
        {
            Instance = null;
        }
        ResetDragState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsPotentialDrag = true;
        m_PointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (m_IsPotentialDrag && !m_IsDragging)
        {
            m_IsPotentialDrag = false;
            
        }
    }
   
    //Método que se encarga de arrastar un bloque dentro de la Vista (WSView)
    public void StartDraggingBlockInternal(BlockView blockView, PointerEventData eventData)
    {

        if (blockView == null || blockView.Block == null)
        {
            Debug.LogError("StartDraggingBlockInternal: blockView or its model is null.");
            ResetDragState(eventData);
            return;
        }
     
        //Posición del puntero del ratón

        Debug.Log($"---> PointerDown Screen Position (Block): {eventData.position} <---");
        m_WasTemplateClone = false;
        m_DraggingBlockView = blockView;
        m_DraggingBlockModel = blockView.Block;

        if (m_DraggingBlockModel == null)
        {
            Debug.LogError("StartDraggingBlockInternal: BlockView has no associated model!");
            ResetDragState(eventData);
            return;
        }
        if (!m_DraggingBlockModel.Movable)
        {
            Debug.Log($"BlockModel {m_DraggingBlockModel.ID} is not movable.");
            ResetDragState(eventData);
            return;
        }

       // Debug.Log($"<color=magenta>BlockDragController: Starting drag - BLOCK {m_DraggingBlockModel.ID}</color>");

        m_workspaceController.RequestBlockUnplug(m_DraggingBlockModel, true);

     //  Debug.Log($"   Reparenting '{blockView.name}' to DragLayer '{m_DragLayerRect.name}'", this);
        m_DraggingBlockView.transform.SetParent(m_DragLayerRect, true);

        if (!PrepareVisualDrag(eventData)) 
        {
            Debug.LogError($"PrepareVisualDrag failed for block {m_DraggingBlockModel.ID}. Aborting drag.", this);
            m_DraggingBlockView.transform.SetParent(m_CodingAreaRect, true); 
            ResetDragState(eventData);
            return;
        }

        m_IsDragging = true; // Marcar como arrastrando 
      //  Debug.Log($"<color=green>BlockDragController: Drag successfully initiated for BLOCK {m_DraggingBlockModel.ID}. Offset: {m_ScreenSpaceDragOffset}</color>", this);
    }

    //Método que se encarga de arrastrar una plantilla de bloque que está en la ToolBox y lo posiciona en la DragLayer
    public IEnumerator StartDraggingTemplateInternal(BlockView templateBlockView, BlockListView sourceToolbox, PointerEventData eventData  /*,Vector2 clickOffsetInTemplate*/)
    {
        if (m_IsDragging) { Debug.LogWarning("Already dragging, ignoring request."); yield break; }

        if (templateBlockView?.Block == null || sourceToolbox == null || m_DragLayerRect == null || m_RootCanvasRect == null) 
        {
            Debug.LogError($"StartDraggingTemplateInternal: Preconditions failed. Missing Block/Toolbox/DragLayer/RootCanvas.");
            ResetDragState(eventData);
            yield break; ;
        }

        m_WasTemplateClone = true;
        BlockModel templateModel = templateBlockView.Block;
        Vector2 initialPointerScreenPos = eventData.position;
        Debug.Log($"<color=#FF9900>BlockDragController: Start Drag TEMPLATE '{templateModel.Type}'</color>");

        //Posición del puntero del ratón

        // Debug.Log($"---> PointerDown Screen Position (Template): {eventData.position} <---");

        // Creo modelo y su vista Clonada
        Vector2 logicalStartPos = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(initialPointerScreenPos, m_CachedCamera);
        BlockModel cloneModel = m_workspaceController.RequestCloneBlockBegin(templateModel, logicalStartPos);
        if (cloneModel == null) { yield break; }

        // Clon A 
        BlockView cloneViewA = BlockViewFactory.CreateView(cloneModel, sourceToolbox);
        if (cloneViewA == null) { 
            cloneModel.Dispose(false); 
            ResetDragState(eventData); 
            yield break; }
        cloneViewA.name += "_CloneA_DragLayer";
        m_DraggingBlockView = cloneViewA; 
        m_DraggingBlockModel = cloneModel;
        m_DraggingBlockView.InToolbox = false;

        Vector3 templateWorldPos = CalculateWorldPosition(templateBlockView.ViewTransform, Vector2.up);

        Vector2 targetAnchoredPosTopLeft;

        RectTransform parentTarget = m_DragLayerRect; // DragLayer

        if (!ScreenPosToLocalPosInTarget(templateWorldPos, parentTarget, out targetAnchoredPosTopLeft))
        {
            Debug.LogError("Failed to calculate target pos for Clone B!"); /* cleanup */ yield break;
        }

        // Clon A (DragLayer)
        m_DraggingBlockView.transform.SetParent(parentTarget, false);

        SetRectTransformTopLeft(m_DraggingBlockView.ViewTransform);
        m_DraggingBlockView.ViewTransform.anchoredPosition = targetAnchoredPosTopLeft;
        m_DraggingBlockView.transform.SetAsLastSibling(); // Poner A encima
        // Debug.Log($"   Clone A AnchoredPos SET to: {targetLocalPosInDragLayer} (Parent: DragLayer)");

      //  Debug.Log($"   DraggingBlock AnchoredPos(0,1 ref / 0.5 anchors) SET to: {targetAnchoredPosTopLeft} (Parent: RootCanvas)");

        //Calculamos Offset Pantalla 
        Vector2 cloneActualPivotScreenPos = RectTransformUtility.WorldToScreenPoint(m_CachedCamera, m_DraggingBlockView.transform.position); // Pos pantalla del pivote
        m_ScreenSpaceDragOffset = cloneActualPivotScreenPos - initialPointerScreenPos; // Offset desde ratón a pivot
        
        // Configuración final
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>() ?? m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
       // ClearHighlight();
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_DraggingBlockView.ViewTransform);
        m_IsDragging = true;
        //Debug.Log($"<color=green>BlockDragController: Drag initiated for scaled/offset CLONE {m_DraggingBlockModel.ID}. Offset Refers to TopLeft Pivot.</color>");
        
    }

    private bool IsPointerOverCodingArea(PointerEventData eventData)
    {
        if (m_CodingAreaRect == null)
        {
            Debug.LogError("IsPointerOverCodingArea: CodingAreaRect is null!");
            return false; 
        }

        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(
        m_CodingAreaRect,
        eventData.position,
        null // ScreenSpace - Overlay
    );
        Debug.Log($"IsPointerOverCodingArea: PointerPos={eventData.position}, IsOverCodingArea={isOver}");
        return isOver;
    }

    ///
    /// <summary>
    /// Prepara visualmente la BlockView para ser arrastrada. Calcula el offset inicial,
    /// ajusta el CanvasGroup y el orden en la jerarquía.
    /// </summary>
    /// <param name="eventData">Los datos del evento de puntero que iniciaron el drag.</param>
    /// 
    private bool PrepareVisualDrag(PointerEventData eventData)
    {
        if (m_DraggingBlockView == null || m_DraggingBlockView.ViewTransform == null || m_DraggingBlockView.gameObject == null)
        {
           // Debug.LogError("PrepareVisualDrag: Invalid dragging block view state.");
            ResetDragState(eventData); 
            return false;
        }
        //Vector2 currentAnchoredPos = m_DraggingBlockView.ViewTransform.anchoredPosition;

        // Recalculamos la posición del puntero relativa al padre actual

        RectTransform parentRect = m_DraggingBlockView.ViewTransform.parent as RectTransform;
        if (parentRect == null) {
            Debug.LogError($"PrepareVisualDrag: Block '{m_DraggingBlockView.name}' has no parent RectTransform after being parented!");
            parentRect = m_CodingAreaRect; 
            if (parentRect == null)
            {
                ResetDragState(eventData); return false;
            }

        }

        //Calculo posición Local del Puntero en el Padre
        Vector2 localPointerPosInParent;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, null,/*m_CachedCamera, <--- null por screen.Overlay*/ out localPointerPosInParent))
        {
          //  Debug.Log($"Calling ScreenPointToLocalPointInRectangle with Camera: {((m_CachedCamera == null) ? "NULL" : m_CachedCamera.name)}");
         //   Debug.LogWarning($"PrepareVisualDrag(Block): Conversion Screen->ParentLocal FAILED.");
            // localPointerPosInParent = currentAnchoredPos; 
            m_ScreenSpaceDragOffset = Vector2.zero;
            return false;
        }

        Debug.Log($"PrepareVisualDrag: Pointer Local Pos in Parent '{parentRect.name}': {localPointerPosInParent}");

        //Determinamos la AnchoredPosition  del Bloque

        Vector2 currentBlockAnchoredPos = m_DraggingBlockView.ViewTransform.anchoredPosition;
  //      Debug.Log($"PrepareVisualDrag: Block's current AnchoredPosition: {currentBlockAnchoredPos}");

        //Calculo el Offset Visual 
        m_ScreenSpaceDragOffset = currentBlockAnchoredPos - localPointerPosInParent;

      //  Debug.Log($"PrepareVisualDrag: Calculated Offset = BlockPivot ({currentBlockAnchoredPos}) - PointerLocal ({localPointerPosInParent}) = {m_ScreenSpaceDragOffset}");

        // m_DragOffsetViewSpace = currentAnchoredPos - localPointerPosInParent;
        m_DraggingBlockView.transform.SetAsLastSibling(); //Permite mostrarlo encima

        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
        if (cg == null) cg = m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; //Desactivar raycasts para evitar interacciones mientras se arrastra

       // ClearHighlight();
        //Debug.Log($"<color=lightblue>BlockDragController: Visual drag prepared for {m_DraggingBlockModel?.ID ?? "UNKNOWN"} (View: {m_DraggingBlockView.name}). Offset: {m_ScreenSpaceDragOffset}</color>");
        return true;

    }

    /// <summary>
    /// Llamado repetidamente durante el drag .Actualiza la posición visual y busca conexiones.
    /// </summary>
    public void HandleDrag(/*BlockView blockView,*/ PointerEventData eventData)
    {
        //Debug.LogError($"<color=red>HASHCODE_CHECK - HandleDrag - BlockDragController - Using Workspace HashCode: {m_Workspace?.GetHashCode()}");

        if (!m_IsDragging || m_DraggingBlockView == null)
        {
            //Debug.LogWarning($"HandleDrag called while m_IsDragging=true but m_DraggingBlockView is null!");

       
            Debug.LogWarning($"HandleDrag skipping. Controller not in valid dragging state. IsDragging:{m_IsDragging}, DraggingView is null:{m_DraggingBlockView == null}", this.gameObject);


            if (m_IsDragging && m_DraggingBlockView == null) ResetDragState(eventData);

            return; 

        }

        // Calculamos la posición local en DragLayer
        Vector2 localPointerPosition;
        RectTransform parentRect = m_DragLayerRect; //<----Padre es el Drag Layer cuando inicio el movimiento en la toolbox

        if (parentRect == null) { Debug.LogError("..."); ResetDragState(eventData); return; } // Safety null check
        Camera currentCamera = m_CachedCamera; 
        if (m_WorkspaceView?.RootCanvas != null && m_WorkspaceView.RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) currentCamera = null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                 parentRect,
                 eventData.position,
                 null,//null,//m_CachedCamera, <--- null por screen.Overlay 
                 out localPointerPosition))
        {
          
            Vector2 newAnchoredPos = localPointerPosition + m_ScreenSpaceDragOffset;

            m_DraggingBlockView.XY = newAnchoredPos;
        }
        else
        {
            Debug.LogWarning($"HandleDrag: Failed to convert screen point {eventData.position} to DragLayer local for {m_DraggingBlockView.gameObject.name}. Cannot update position.", this.gameObject);
        }

         List<ConnectionModel> dragginConnections = m_DraggingBlockModel.GetDraggingConnections();

        Vector2 blockModelCurrentLogicalPosition = m_DraggingBlockModel.XY;

       // Debug.LogError($"HASHCODE_CHECK - HandleDrag - BlockDragController - Using Workspace HashCode: {m_Workspace?.GetHashCode()}");

        m_connectionController.ProcessDrag(m_DraggingBlockModel, dragginConnections, m_DraggingBlockModel.XY);
        
       // m_WorkspaceView?.CheckTrashBin(m_DraggingBlockView); //Revisar la lógica de la papelera ya que si no cae en CodingArea se debe de borrar, ahora mismo no se borra 
    }

    public void HandleEndDrag(/*BlockView blockView,*/ PointerEventData eventData)
    {
        //Debug.Log($"<color=red>BlockDragController.HandleEndDrag: Entered. IsDragging={m_IsDragging}. DraggingView={m_DraggingBlockView?.name}</color>");

       // Debug.LogError($"<color=red> HASHCODE_CHECK - HandleEndDrag - BlockDragController - Using Workspace HashCode: {m_Workspace?.GetHashCode()}");

        if (!m_IsDragging || m_DraggingBlockView /*!= blockView || m_DraggingBlockView */== null)
        {
            // m_IsPotentialDrag = false;
            Debug.LogWarning($"HandleEndDrag called, but controller was not in valid dragging state. IsDragging:{m_IsDragging}, DraggingView is null:{m_DraggingBlockView == null}. Just resetting.", this.gameObject);
            ResetDragState(eventData);
            m_connectionController.ResetPotentialConnection(); //Reseto el controlador de conexion 
            return;
        }
     
        string blockId = m_DraggingBlockModel?.ID ?? "UNKNOWN";

        Debug.Log($"<color=cyan>BlockDragController: Ending drag - Block {blockId} ({m_DraggingBlockView.name} ) Pointer Screen Pos: {eventData.position}</color>");

        m_IsPotentialDrag = false;

        //Finalizar la búsqueda de conexión y obtener los candidatos.
        m_connectionController.FinalizePotentialConnection();
        ConnectionModel finalTargetStationaryModelConn;
        ConnectionModel finalSourceDraggedModelConn;
        bool canConnect = m_connectionController.GetFinalizedConnections(out finalTargetStationaryModelConn, out finalSourceDraggedModelConn);

        bool connected = false;
        bool placedInWorkspace = false;
        bool deleted = false;

        if (canConnect)
        {
            Debug.Log($"<color=lime>BlockDragController: Potential connection found!</color> Attempting to connect DRAGGED: {ConnectionModel.GetConnectionModelID(finalSourceDraggedModelConn)} -> STATIONARY: {ConnectionModel.GetConnectionModelID(finalTargetStationaryModelConn)}");

            ConnectionModel superiorConn, inferiorConn;

            // finalSourceDraggedModelConn es del bloque arrastrado
            // finalTargetStationaryModelConn es del bloque estacionario

            if (finalSourceDraggedModelConn.IsSuperior) // El conector del bloque ARRASTRADO es "hembra" (Next o Input)
            {
                // Por lo tanto, el conector del bloque ARRASTRADO debe ser el SUPERIOR
                superiorConn = finalSourceDraggedModelConn;
                inferiorConn = finalTargetStationaryModelConn; // Y el ESTACIONARIO el INFERIOR (Prev o Output)
                                                               // Log.Log($"   Case 1: Dragged is Superior ({superiorConn.Type}). Stationary is Inferior ({inferiorConn.Type}).");
            }
            else // El conector del bloque ARRASTRADO es "macho" (Prev o Output)
            {
                // Por lo tanto, el conector del bloque ESTACIONARIO debe ser el SUPERIOR (Next o Input)
                superiorConn = finalTargetStationaryModelConn;
                inferiorConn = finalSourceDraggedModelConn;   // Y el ARRASTRADO el INFERIOR
                                                              // Logger.Log($"   Case 2: Dragged is Inferior ({inferiorConn.Type}). Stationary is Superior ({superiorConn.Type}).");
            }

            // Comprobación de compatibilidad de tipos antes de conectar
            // Aquí definimos cómo chequear si son Statement o Value basándonos en el enum EConnection
            bool superiorIsStatement = superiorConn.Type == EConnection.NextStatement || superiorConn.Type == EConnection.PrevStatement;
            bool inferiorIsStatement = inferiorConn.Type == EConnection.NextStatement || inferiorConn.Type == EConnection.PrevStatement;

            bool superiorIsValue = superiorConn.Type == EConnection.InputValue || superiorConn.Type == EConnection.OutputValue;
            bool inferiorIsValue = inferiorConn.Type == EConnection.InputValue || inferiorConn.Type == EConnection.OutputValue;

            bool typesAreCompatible =
                (superiorIsStatement && inferiorIsStatement) ||
                (superiorIsValue && inferiorIsValue);

            if (!typesAreCompatible)
            {
                Logger.LogError($"<color=red>BlockDragController: CONNECTION TYPE MISMATCH! Cannot connect Superior {superiorConn.Type} (ID: {ConnectionModel.GetConnectionModelID(superiorConn)}) to Inferior {inferiorConn.Type} (ID: {ConnectionModel.GetConnectionModelID(inferiorConn)}).</color>");
                canConnect = false; // Anular la conexión
            }
            else
            {
                // Logger.Log($"<color=magenta>BlockDragController: Calling Connect - Superior: {ConnectionModel.GetConnectionModelID(superiorConn)} ({superiorConn.SourceBlock.Id}) ---> Inferior: {ConnectionModel.GetConnectionModelID(inferiorConn)} ({inferiorConn.SourceBlock.Id})</color>");
                try
                {
                    // Ejecutar la conexión a NIVEL DE MODELO: El SUPERIOR llama a Connect con el INFERIOR
                    superiorConn.Connect(inferiorConn); // Esto debería disparar eventos que ConnectionView escuchará
                    connected = true;
                    placedInWorkspace = true; // Si se conecta, está en el workspace

                    Debug.Log($"  <color=green>MODEL Connection SUCCESSFUL.</color> ParentBlock of DraggingBlock ({m_DraggingBlockModel.ID}) is now: {m_DraggingBlockModel.ParentBlock?.ID ?? "NULL"}. OutputConnection Target: {m_DraggingBlockModel.OutputConnection?.TargetBlock?.ID ?? "NULL"}. PrevConnection Target: {m_DraggingBlockModel.PreviousConnection?.TargetBlock?.ID ?? "NULL"}", m_DraggingBlockView.gameObject);

                    if (m_WasTemplateClone)
                    {
                        Debug.Log($"BlockDragController: Confirming add for template clone {m_DraggingBlockModel.ID} due to successful connection.");
                        m_workspaceController.EnsureBlockRegistered(m_DraggingBlockModel);
                        m_PendingCloneModel = null;
                        Debug.Log($"BlockDragController: Template clone {m_DraggingBlockModel.ID} successfully CONNECTED. Ensured registration.", m_DraggingBlockView.gameObject);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"<color=red>BlockDragController: MODEL Connect FAILED:</color> {e.ToString()}");
                    connected = false;
                  
                }
            }
        }

        //Si no se conectó, intentar colocar en el workspace o manejar el descarte.
        if (!connected)
        {
            bool isValidDropPlacement = m_connectionController.HandleDropPlacement(m_DraggingBlockModel, m_WasTemplateClone, eventData.position, this);

            if (isValidDropPlacement)
            {
                Debug.Log($"<color=yellow>BlockDragController: Block {blockId} placed freely in CodingArea.</color>");
                
                m_DraggingBlockView.transform.SetParent(m_CodingAreaRect, true); // worldPositionStays = true

                if (m_DraggingBlockModel.ParentBlock != null)
                {
                    Debug.LogError("BlockDragController: Freely dropped block still has a ParentBlock. This shouldn't happen here.");
                }

                if (m_DraggingBlockModel.PreviousConnection != null && m_DraggingBlockModel.PreviousConnection.IsConnected)
                {
                    Debug.LogError("BlockDragController: Freely dropped block still has PrevConnection connected.");
                    m_DraggingBlockModel.PreviousConnection.Disconnect();
                }

                RectTransform draggingBlockRect = m_DraggingBlockView.GetRectTransform();
                RectTransform codingAreaRect = m_WorkspaceView.CodingArea;

                // Convertir la posición actual del puntero (más el offset) a la local del CodingArea
                Vector2 finalPointerScreenPos = eventData.position;
                Vector2 desiredScreenPos = finalPointerScreenPos + m_ScreenSpaceDragOffset;
                Vector2 newAnchoredPos = draggingBlockRect.anchoredPosition; ;//= ScreenPosToLocalPosInTarget(desiredScreenPos, m_CodingAreaRect);
                if (ScreenPosToLocalPosInTarget(desiredScreenPos, m_CodingAreaRect, out newAnchoredPos))
                {
                    m_DraggingBlockView.XY = newAnchoredPos;
                    m_DraggingBlockModel.XY = m_WorkspaceView.VisualAnchoredPositionToLogicalXY(newAnchoredPos,  codingAreaRect);

                    m_DraggingBlockModel.UnPlug();

                    placedInWorkspace = true;
                    if (m_WasTemplateClone)
                    {
                        m_workspaceController.ConfirmAddBlock(m_DraggingBlockModel); 
                        m_PendingCloneModel = null;
                        Debug.Log($"BlockDragController: Template clone {m_DraggingBlockModel.ID} placed FREELY. Confirmed as TopBlock.", m_DraggingBlockView.gameObject);

                    }
                    // Para bloques existentes, su modelo XY se actualizó. Su vista está posicionada.
                }
            }
            else // no en un área de drop válida ( fuera del CodingArea)
            {
                if (m_DraggingBlockModel != null)
                {
                    if (m_WasTemplateClone)
                    {
                        // Si es un clon de la toolbox, lo destruimos completamente.
                        if (m_DraggingBlockView != null && m_DraggingBlockView.gameObject != null)
                        {
                            m_DraggingBlockView.Dispose(); 
                        }
                        m_DraggingBlockModel.Dispose(false); // Limpia el modelo.
                        Debug.Log($"   Template clone '{blockId}' destroyed (invalid drop).");
                    }
                    else
                    {
                   
                        m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
                        Debug.Log($"   Existing block '{blockId}' requested for deletion (invalid drop).");
                        // Destruir la instancia visual que estaba en el drag layer
                        if (m_DraggingBlockView != null && m_DraggingBlockView.gameObject != null)
                        {
                                   Destroy(m_DraggingBlockView.gameObject); // Destruye el objeto visual que se estaba arrastrando.
                        }
                    }
                }
                //this.gameObject.SetActive(false);

                //  }
            }
        }

        //Limpieza final del estado de arrastre.
        CleanUpAfterDrag(placedInWorkspace || connected, eventData); // Si se colocó o conectó, es un éxito para cleanup.

        if (m_WasTemplateClone && (deleted || (!placedInWorkspace && !connected)))
        {
            // Si era un clon y fue borrado o no colocado/conectado, asegurar -> pendingClone se limpia
            if (m_PendingCloneModel != null)
            {
                Debug.Log($"[HandleEndDrag] Clearing m_PendingCloneModel ({m_PendingCloneModel?.ID}) after unsuccessful drop of template.");
              
                m_workspaceController.CancelPendingClone(m_PendingCloneModel);
                m_PendingCloneModel = null;
            }
        }

        // Controlador de conexión esté completamente reseteado para el próximo drag
        m_connectionController.ResetPotentialConnection();


    }

    /// <summary>
    /// Maneja la lógica de modelo cuando el bloque se suelta en un área válida del workspace.
    /// </summary>
    private void HandleValidWorkspaceDrop(Vector2 finalLogicalPos)
    {
        Debug.Log($"   Requesting MODEL move to logical position: {finalLogicalPos}");
        m_workspaceController.RequestBlockMove(m_DraggingBlockModel, finalLogicalPos);

        if (m_WasTemplateClone)
        {
            Debug.Log($"   Confirming clone add: {m_DraggingBlockModel.ID}");
            m_workspaceController.ConfirmAddBlock(m_DraggingBlockModel);
        }
    }

    /// <summary>
    /// Maneja la lógica de modelo/vista cuando el bloque se suelta en la papelera.
    /// </summary>
    private void HandleTrashDrop()
    {
        if (m_WasTemplateClone)
        {
            Debug.Log("   Cancelling pending clone (Dropped in trash).");
            if (m_DraggingBlockModel != null)
            {
                Debug.Log($"   Disposing cancelled clone model: {m_DraggingBlockModel.ID}");
                m_DraggingBlockModel.Dispose(false);
            }
        }
        else
        {
            Debug.Log($"   Requesting deletion of existing block: {m_DraggingBlockModel.ID}");
            m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
        }
    }

    /// <summary>
    /// Maneja la lógica de modelo/vista cuando el bloque se suelta fuera de áreas válidas.
    /// </summary>
    private void HandleInvalidDrop()
    {
        if (m_DraggingBlockModel == null) // Seguridad, aunque no debería pasar si se llama desde HandleEndDrag con un drag activo
        {
            Debug.LogWarning("HandleInvalidDrop called with null m_DraggingBlockModel.");
            return;
        }

        string blockId = m_DraggingBlockModel.ID; // Para logs

        if (m_WasTemplateClone) // Si el bloque arrastrado era un clon de la toolbox
        {
            Debug.Log($"HandleInvalidDrop: Template clone '{blockId}' dropped in invalid location. Destroying.");

            //  Destruir el GameObject de la BlockView (el clon visual)
            if (m_DraggingBlockView != null && m_DraggingBlockView.gameObject != null)
            {
                
                m_DraggingBlockView.Dispose();
             
                // Destroy(m_DraggingBlockView.gameObject);
                // Debug.Log($"   Destroyed BlockView GameObject for template clone '{blockId}'.");
            }

            // Limpiar/disponer el BlockModel (el clon de datos)
        
            m_DraggingBlockModel.Dispose(false); // `false` podría significar no intentar añadir a pila de undo si la tienes.
            Debug.Log($"   Disposed BlockModel for template clone '{blockId}'.");
        }
        else // Si era un bloque existente del workspace que se sacó y se soltó mal
        {
            Debug.Log($"HandleInvalidDrop: Existing block '{blockId}' dropped in invalid location. Returning to original state/position (o manejar de otra forma).");

            // Opción A: Devolver el bloque a su última posición válida en el workspace

            if (m_DraggingBlockView != null && m_DraggingBlockView.gameObject != null)
            {
                Debug.LogWarning($"  Existing block '{blockId}' dropped invalidly. Attempting to 'hide' its view and let model persist. Consider definitive action.");
                // m_DraggingBlockView.gameObject.SetActive(false);

                m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
                // m_DraggingBlockView.Dispose() se llamará cuando el Workspace elimine el modelo y su vista.
                Debug.Log($"   Requested deletion of existing block '{blockId}'.");
            }
            else if (m_DraggingBlockModel != null) // Si solo tenemos modelo
            {
                Debug.LogWarning($"   Existing model '{blockId}' dropped invalidly (no view?). Requesting deletion.");
                m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
            }
        }

        // Importante: Como estamos manejando m_DraggingBlockView/Model directamente aquí,
        // ResetDragState (llamado después en HandleEndDrag) no intentará acceder a
        // un m_DraggingBlockView que ya podría estar destruido.
        // Pero es bueno nulificar las referencias en ResetDragState de todas formas.

    }

    /// <summary>
    /// Realiza la limpieza final después de procesar el final del drag.
    /// </summary>
    private void CleanUpAfterDrag(bool wasPlacedOrConnected, PointerEventData eventData)
    {
        //ClearHighlight();

        BlockView justDraggedView = m_DraggingBlockView;

        string draggedModelId = m_DraggingBlockModel?.ID; 
        ResetDragState(eventData); 

        if (wasPlacedOrConnected && justDraggedView != null)
        {
            var cg = justDraggedView.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                //Debug.Log($"   Re-enabled raycasts for placed/connected block '{draggedModelId}' ({justDraggedView.name})");
            }

        }
        else
        {
            //Debug.Log($"   Block '{draggedModelId}' was deleted or cancelled, no final view setup needed.");
        }

       // Debug.Log("<color=cyan>BlockDragController: Drag sequence finished.</color>");
    }

    private void ResetDragState(PointerEventData eventData = null)
    {
        if (eventData != null && eventData.pointerDrag == m_DraggingBlockView?.gameObject)
        {
            //Debug.Log("ResetDragState: Clearing pointerDrag data.");
            eventData.pointerPress = null;
            eventData.pointerDrag = null;
            // UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); 
        }
        else if (eventData == null && m_DraggingBlockView != null)
        {
            //Debug.Log("ResetDragState: No event data, ensuring visual reset.");
        }

        m_IsDragging = false;
        m_IsPotentialDrag = false; 

        if (m_DraggingBlockView != null)
        {
            //Debug.Log($"ResetDragState: Re-enabling raycasts for {m_DraggingBlockView.gameObject.name}");
            var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;

            var le = m_DraggingBlockView.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
 
        }

        m_DraggingBlockView = null;
        m_DraggingBlockModel = null; 
      //  m_BestTargetConnection = null;
      //  m_SourceDragConnection = null;
      //  ClearHighlight(); 
        m_WasTemplateClone = false;

    }

    /// <summary>
    /// Busca la mejor conexión candidata basada en la posición lógica actual. REVISAR Y METER EN UN CONTROLADOR ESPECIFICO
    /// </summary>
    private void FindBestConnection(Vector2 currentLogicalPos)
    {
      //  m_BestTargetConnection = null;
       // m_SourceDragConnection = null;
        if (m_DraggingBlockModel == null || m_Workspace == null) return;

        float closestRadiusSq = m_SnapDistance * m_SnapDistance;
        List<ConnectionModel> myConnections = m_DraggingBlockModel.GetDraggingConnections();

        foreach (ConnectionModel myConn in myConnections)
        {
            if (myConn == null) continue;
            BlockConnectionDB oppositeDB = m_Workspace.GetConnectionDB(myConn.OppositeType);
            if (oppositeDB == null) continue;

            ConnectionModel neighbour;
            float radiusSq;

            // Usa la BBDD para buscar la conexión más cercana del tipo opuesto
            oppositeDB.SearchForClosest(myConn, m_ConnectionSearchRadius, currentLogicalPos, out neighbour, out radiusSq);

            if (neighbour != null && radiusSq < closestRadiusSq)
            {
                if (myConn.CanConnectWithReason(neighbour) == ConnectionModel.CAN_CONNECT)
                {
                    closestRadiusSq = radiusSq;
              //      m_BestTargetConnection = neighbour;
               //     m_SourceDragConnection = myConn;
                }
            }
        }
    }

    public void OnDragBlock(PointerEventData eventData)
    {
        if (!m_IsDragging || m_DraggingBlockView == null) return;

        //Calcular Objetivo Pantalla
        Vector2 currentPointerScreenPos = eventData.position;
        Vector2 targetPivotScreenPos = currentPointerScreenPos + m_ScreenSpaceDragOffset;

        //Convertir a Local de DragLayer (Padre de Clon A)
        Vector2 targetLocalPosInDragLayer;
        if (ScreenPosToLocalPosInTarget(targetPivotScreenPos, m_DragLayerRect, out targetLocalPosInDragLayer))
        {
            // Mover Clon A
            m_DraggingBlockView.ViewTransform.anchoredPosition = targetLocalPosInDragLayer;
        } 

      
        Vector2 currentLogicalPos = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);
       // ConnectionModel oldBestTarget = m_BestTargetConnection;
        //FindBestConnection(currentLogicalPos);
     //   UpdateHighlighting(oldBestTarget);
       // m_WorkspaceView?.CheckTrashBin(m_DraggingBlockView); 
    }

    public void OnEndBlockDrag(PointerEventData eventData)
    {
        if (!m_IsDragging || m_DraggingBlockView == null)
        {
            //DestroyDebugCloneB(); 
            ResetDragState(eventData);
            return;
        }

        //  Guardar estado necesario antes de ResetDragState 
        string blockId = m_DraggingBlockModel?.ID ?? "UNKNOWN";
        string viewNameA = m_DraggingBlockView.name;
        BlockView blockToPlaceOrConnect = m_DraggingBlockView; // Clon A
        BlockModel modelToUpdate = m_DraggingBlockModel;
        bool wasClone = m_WasTemplateClone; 
    //    ConnectionModel finalTargetConnection = m_BestTargetConnection;
   //     ConnectionModel finalSourceConnection = m_SourceDragConnection;

        Debug.Log($"<color=cyan>BlockDragController: Ending drag - Block {blockId} ({viewNameA})</color>");
        Vector2 finalPointerScreenPos = eventData.position;

        bool overCodingArea = IsPointerOverArea(finalPointerScreenPos, m_CodingAreaRect);

        //  Lógica de Decisión Final 
        bool connected = false;
        bool placedInWorkspace = false;
        // Colocar en Workspace
        if (!connected && overCodingArea)
        {
            placedInWorkspace = true;
            // Calcular pos final en CodingArea usando offset pantalla
            Vector2 finalTargetPivotScreenPos = finalPointerScreenPos + m_ScreenSpaceDragOffset;
            Vector2 finalLocalPosInCodingArea;
            if (ScreenPosToLocalPosInTarget(finalTargetPivotScreenPos, m_CodingAreaRect, out finalLocalPosInCodingArea))
            {
                // Reparentar y Posicionar
                if (blockToPlaceOrConnect.transform.parent != m_CodingAreaRect)
                {
                    blockToPlaceOrConnect.transform.SetParent(m_CodingAreaRect, true);
                    blockToPlaceOrConnect.transform.SetAsLastSibling();
                }
                StandardizeRectTransform(blockToPlaceOrConnect.ViewTransform); // Asegurar anchors/pivot
                blockToPlaceOrConnect.ViewTransform.anchoredPosition = finalLocalPosInCodingArea;

                // Actualizar Modelo
                HandleValidWorkspaceDrop(modelToUpdate, finalLocalPosInCodingArea, wasClone); // Pasar flag
            } 
        }

        //  Drop Inválido
        if (!connected && !placedInWorkspace)
        {
            Debug.Log("<color=orange>... Dropped OUTSIDE Coding Area or other invalid state. Invalid drop.</color>");
            HandleInvalidDrop(modelToUpdate, blockToPlaceOrConnect, wasClone); // Pasar modelo, vista y flag
        }

        //  Limpieza  
        //ClearHighlight(); 
        //DestroyDebugCloneB(); 
                          
        ResetDragStateInternalsOnly(eventData);

        // Reactivar raycasts del Clon A SI SE COLOCÓ bien y aún existe
        if ((placedInWorkspace || connected) && blockToPlaceOrConnect != null && blockToPlaceOrConnect.gameObject != null)
        {
            var cgA = blockToPlaceOrConnect.GetComponent<CanvasGroup>();
            if (cgA != null) cgA.blocksRaycasts = true;
            Debug.Log($"   Re-enabled raycasts for {blockToPlaceOrConnect.name}");
        }
        else
        {
            Debug.Log("   Block was likely destroyed (invalid drop), no raycast reset needed.");
        }

        Debug.Log("<color=cyan>BlockDragController: Drag sequence finished.</color>");

    }

    private void HandleValidWorkspaceDrop(BlockModel model, Vector2 finalLocalPosVisual, bool wasClone)
    {
        Vector2 finalLogicalPos = m_WorkspaceView.VisualAnchoredPositionToLogicalXY(finalLocalPosVisual, m_CodingAreaRect);
        m_workspaceController.RequestBlockMove(model, finalLogicalPos);
        if (wasClone)
        {
            m_workspaceController.ConfirmAddBlock(model);
        }
        Debug.Log($"HandleValidWorkspaceDrop: Model {model.ID} moved/confirmed. VisualPos: {finalLocalPosVisual} -> LogicalPos: {finalLogicalPos}");
    }

    private void HandleInvalidDrop(BlockModel model, BlockView view, bool wasClone)
    {
        if (model == null) return; 

        if (wasClone)
        {
            Debug.Log($"   Cancelling pending clone {model.ID} (Invalid Drop).");
            model.Dispose(false); 
        }
        else
        {
            Debug.Log($"   Requesting deletion of existing block {model.ID} (Invalid Drop).");
            m_workspaceController.RequestDeleteBlock(model);
        }
        if (view != null && view.gameObject != null)
        {
            view.Dispose();
        }
    }

    /// <summary>
    /// Resetea solo las variables internas del controlador, sin tocar el estado
    /// del bloque que podría haber sido colocado.
    /// </summary>
    private void ResetDragStateInternalsOnly(PointerEventData eventData = null)
    {
        m_IsDragging = false;

        if (eventData != null && eventData.pointerDrag == m_DraggingBlockView?.gameObject)
        {
            eventData.pointerPress = null;
            eventData.pointerDrag = null;
        }

       // m_BestTargetConnection = null;
       // m_SourceDragConnection = null;
        m_WasTemplateClone = false;

        //DestroyDebugCloneB();
    }

    /// <summary>
    /// Convierte una posición de pantalla (Screen Space) a la posición local
    /// dentro de un RectTransform específico.
    /// </summary>
    private bool ScreenPosToLocalPosInTarget(Vector2 screenPos, RectTransform targetParent, out Vector2 localPos)
    {
        localPos = Vector2.zero;
        if (targetParent == null)
        {
            Debug.LogError("ScreenPosToLocalPosInTarget: targetParent is null!");
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
          targetParent,
          screenPos,
          m_CachedCamera, // <-- Usar la cámara cacheada (o null si es Overlay)
          out localPos);
    }

    /// <summary>
    /// Helper para estandarizar pivot y anchors de un RectTransform.
    /// Recomendado: Pivot y Anchors al centro (0.5) para cálculos consistentes.
    /// También resetea la escala a (1,1,1).
    /// </summary>
    private void StandardizeRectTransform(RectTransform rect)
    {
        if (rect == null)
        {
            Debug.LogWarning("StandardizeRectTransform: RectTransform is null.");
            return;
        }
        // Debug.Log($"Standardizing Rect: {rect.name}. Initial Pivot={rect.pivot}, Anchors Min={rect.anchorMin}, Max={rect.anchorMax}, Scale={rect.localScale}");
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.localScale = new Vector2(0.5f,0.5f); // Asegurar escala unitaria
        // Debug.Log($"   ... Standardized Pivot={rect.pivot}, Anchors Min={rect.anchorMin}, Max={rect.anchorMax}, Scale={rect.localScale}");
    }

    /// <summary>
    /// Verifica si una posición de pantalla está sobre el área visual
    /// definida por un RectTransform específico.
    /// </summary>
    /// <param name="pointerScreenPos">La posición del puntero en coordenadas de pantalla.</param>
    /// <param name="areaRect">El RectTransform del área a comprobar.</param>
    /// <returns>True si el puntero está sobre el área, False en caso contrario.</returns>
    public bool IsPointerOverArea(Vector2 pointerScreenPos, RectTransform areaRect)
    {
        if (areaRect == null)
        {
            //Debug.LogWarning("IsPointerOverArea: areaRect is null!"); // Puede ser ruidoso
            return false;
        }
        return RectTransformUtility.RectangleContainsScreenPoint(
           areaRect,
           pointerScreenPos,
           m_CachedCamera 
       );
    }

    // <summary>
    /// Helper para establecer anchors y pivot específicos en un RectTransform.
    /// </summary>
    private void SetAnchorsAndPivot(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        if (rect == null) return;
        // Debug.Log($"Setting AP: {rect.name}, AnchorMin={anchorMin}, AnchorMax={anchorMax}, Pivot={pivot}");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
    }

    /// <summary>
    /// Configura el Pivot y los Anchors de un RectTransform a Top-Left (0, 1).
    /// También resetea la escala a (1,1,1).
    /// </summary>
    /// <param name="rect">El RectTransform a modificar.</param>
    private void SetRectTransformTopLeft(RectTransform rect)
    {
        if (rect == null)
        {
            Debug.LogWarning("SetRectTransformTopLeft: RectTransform is null.");
            return;
        }
        
        // Debug.Log($"Setting Top-Left: {rect.name}. Initial Pivot={rect.pivot}, Anchors Min={rect.anchorMin}, Max={rect.anchorMax}, Scale={rect.localScale}");
        rect.pivot = new Vector2(0f, 1f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.localScale = new Vector2(0.5f, 0.5f); 
        // Debug.Log($"   ... Set Pivot={rect.pivot}, Anchors Min={rect.anchorMin}, Max={rect.anchorMax}, Scale={rect.localScale}");
    }

    /// <summary>
    /// Calcula la posición en el espacio mundial de un punto normalizado
    /// dentro del rectángulo de un RectTransform.
    /// (0,0) = Abajo-Izquierda, (1,1) = Arriba-Derecha, (0.5,0.5) = Centro (Pivot independiente)
    /// </summary>
    /// <param name="rectTransform">El RectTransform de referencia.</param>
    /// <param name="normalizedPoint">El punto normalizado (valores de 0 a 1) dentro del rectángulo.</param>
    /// <returns>La posición mundial del punto normalizado.</returns>
    public static Vector3 CalculateWorldPosition(RectTransform rectTransform, Vector2 normalizedPoint)
    {
        if (rectTransform == null)
        {
            Debug.LogError("CalculateWorldPosition: rectTransform is null!");
            return Vector3.zero;
        }

        // Obtener las esquinas locales del rectángulo relativas al pivot
        // rect.rect devuelve { x: posXLocalRelativaAlPivotDeMinX, y: posYLocalRelativaAlPivotDeMinY, width: ..., height: ... }
        Rect rect = rectTransform.rect;

        // Calcular la posición local del punto normalizado RELATIVA AL PIVOT
        // Encontrar la esquina inferior izquierda local (relativa al pivot)
        float localMinX = rect.x;
        float localMinY = rect.y;

        // Calcular la posición dentro del rect basada en el punto normalizado
        float localX = localMinX + rect.width * normalizedPoint.x;
        float localY = localMinY + rect.height * normalizedPoint.y;

        Vector3 localPointPosition = new Vector3(localX, localY, 0); // Z es 0 en espacio local 2D

        // Convertir la posición local (relativa al pivot) a posición mundial
        // transform.TransformPoint hace exactamente esto.
        Vector3 worldPosition = rectTransform.TransformPoint(localPointPosition);

        return worldPosition;
    }

    // Método público para permitir a otros consultar si un bloque modelo específico está siendo arrastrado
    public bool IsDraggingBlock(BlockModel model)
    {
        // Verifico si actualmente estamos arrastrando Y si el modelo que se está arrastrando es el modelo que me preguntan.
        return m_IsDragging && m_DraggingBlockModel == model;
    }
}//fin clase BlockDragController

