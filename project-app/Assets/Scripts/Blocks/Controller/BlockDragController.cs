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
 * Descripción:
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

    private WorkSpaceModel m_Workspace; 
    private WorkSpaceView m_WorkspaceView;
    private WorkspaceController m_workspaceController;

    private BlockView m_DraggingBlockView = null;
    private BlockModel m_DraggingBlockModel = null;
    private Vector2 m_DragOffsetViewSpace;
    private bool m_IsPotentialDrag = false; 
    private bool m_IsDragging = false;
    private bool m_WasTemplateClone = false;
    private BlockModel m_PendingCloneModel = null;
    private RectTransform m_DragLayerRect;
    private ConnectionModel m_BestTargetConnection = null;      
    private ConnectionModel m_SourceDragConnection = null;      
    private ConnectionView m_HighlightedTargetView = null;

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
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }
    public void InitializeController(WorkSpaceModel Workspace, WorkSpaceView WorkSpaceView, WorkspaceController wsController, RectTransform dragLayerRect)
    {
        m_Workspace = Workspace ?? throw new ArgumentNullException(nameof(Workspace));
        m_workspaceController = wsController ?? throw new ArgumentNullException(nameof(wsController));
        m_WorkspaceView = WorkSpaceView ?? throw new ArgumentNullException(nameof(WorkSpaceView));
        m_DragLayerRect = dragLayerRect;

        if (m_WorkspaceView == null)
        {
            Debug.LogError("BlockDragController: Could not find UBlockly.UGUI.WorkspaceView!");
            m_WorkspaceView = FindFirstObjectByType<WorkSpaceView>();
        }

        if (m_Workspace == null || m_WorkspaceView == null || m_workspaceController == null)
            Debug.LogError("BlockDragController: Missing core references after initialization!");
        else
        {
            m_CodingAreaRect = m_WorkspaceView.CodingArea;
            Debug.Log($"BlockDragController Initialized: m_CodingAreaRect is {(m_CodingAreaRect == null ? "NULL" : m_CodingAreaRect.name)}", this);
         //   m_RootCanvas = m_WorkspaceView.RootCanvas;     
            m_CachedCamera = m_WorkspaceView.EventCamera; 
        }
    }

    private void ResetDragState()
    {
        m_IsDragging = false;
        if (m_DraggingBlockView != null)
        {
            var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true; 
        }
        m_DraggingBlockView = null;
        m_DraggingBlockModel = null;
        m_PendingCloneModel = null; 
        m_BestTargetConnection = null;
        m_SourceDragConnection = null;
        ClearHighlight(); 
        m_WasTemplateClone = false;
    }

    private void ClearHighlight()
    {
        if (m_HighlightedTargetView != null)
        {
            m_HighlightedTargetView.Highlight(false); 
            m_HighlightedTargetView = null;
        }
    }

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

    private void HighlightConnection(ConnectionView targetView)
    {
        if (targetView != null)
        {
            targetView.Highlight(true);
            m_HighlightedTargetView = targetView;
        }
    }

    public void RegisterPendingClone(BlockModel clonedModel)
    {
        if (clonedModel == null) return;
        m_PendingCloneModel = clonedModel;

        Debug.Log($"BlockDragController: Registered pending clone {clonedModel.ID}. Waiting for StartDraggingTemplate.");
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CodingAreaRect, eventData.position, m_CachedCamera, out localPos);
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


    public void StartDraggingBlock(BlockView blockView, PointerEventData eventData) 
    {
        if (m_IsDragging) {  eventData.pointerDrag = null; return; }
        if (blockView == null) { eventData.pointerDrag = null; return; }

        m_WasTemplateClone = false; 
        m_DraggingBlockView = blockView;
        m_DraggingBlockModel = blockView.Block; 

        if (m_DraggingBlockModel == null)
        {
            Debug.LogError("StartDraggingBlock: BlockView has no associated UBlockly.BlockModel model!");
            eventData.pointerDrag = null;
            ResetDragState();
            return;
        }
        if (!m_DraggingBlockModel.Movable)
        {
            Debug.Log($"BlockModel {m_DraggingBlockModel.ID} is not movable.");
            eventData.pointerDrag = null; 
            ResetDragState();
            return;
        }

        Debug.Log($"BlockDragController: Started dragging BLOCK {m_DraggingBlockModel.ID}");
        m_IsDragging = true; 

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_DraggingBlockView.ViewTransform, 
            eventData.position,
            m_CachedCamera,        
            out m_DragOffsetViewSpace);

        m_DraggingBlockView.transform.SetAsLastSibling();
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
        if (cg == null) cg = m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; 

        m_workspaceController.RequestBlockUnplug(m_DraggingBlockModel, true);

        ClearHighlight(); 
    }

    public void StartDraggingTemplate(BlockView templateBlockView, BlockListView sourceToolbox, PointerEventData eventData)
    {
        if (m_IsDragging) {  eventData.pointerDrag = null; return; }
        if (templateBlockView?.Block == null) { eventData.pointerDrag = null; return; }

        BlockModel templateModel = templateBlockView.Block; 

        Debug.Log($"BlockDragController: Start dragging TEMPLATE for {templateModel.Type}");

        Vector2 startPosLogical = GetPointerPosInWorkspace(eventData);
        m_PendingCloneModel = m_workspaceController.RequestCloneBlockBegin(templateModel, startPosLogical);
        if (m_PendingCloneModel == null)
        {
            Debug.LogError("Failed to create clone model from WorkspaceController.");
            eventData.pointerDrag = null;
            return;
        }

        BlockView cloneView = BlockViewFactory.CreateView(m_PendingCloneModel, sourceToolbox);
        if (cloneView == null)
        {
            Debug.LogError("BlockViewFactory failed to create view for the cloned model.");
            m_PendingCloneModel.Dispose(false); 
            m_PendingCloneModel = null;
            eventData.pointerDrag = null;
            return;
        }

        m_WasTemplateClone = true;
        m_DraggingBlockView = cloneView;       
        m_DraggingBlockModel = m_PendingCloneModel; 

        m_DraggingBlockView.InToolbox = false; 
                                              
        m_DraggingBlockView.ViewTransform.SetParent(m_CodingAreaRect, true); 
        m_DraggingBlockView.XY = startPosLogical; 

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
           m_DraggingBlockView.ViewTransform,
           eventData.position,
           m_CachedCamera,//null,//m_CachedCamera, <--- null por screen.Overlay
           out m_DragOffsetViewSpace); 

        m_DraggingBlockView.ViewTransform.SetAsLastSibling(); 
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
        if (cg == null) cg = m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;            

        m_IsDragging = true; 

        Debug.Log($"BlockDragController: Started dragging CLONE {m_DraggingBlockModel.ID}");
        ClearHighlight();
    }

    private Vector2 GetPointerPosInParent(PointerEventData eventData)
    {
        Vector2 localPointerPosition;
        RectTransform parentRect = m_DraggingBlockView.ViewTransform.parent as RectTransform;
        if (parentRect == null) parentRect = m_CodingAreaRect; 

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, m_CachedCamera, out localPointerPosition))
        {
            return localPointerPosition;
        }
        Debug.LogError("GetPointerPosInParent failed!");
        return Vector2.zero;
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

    private void TryStartDrag(BlockView blockView, PointerEventData eventData, bool isTemplate, BlockListView sourceToolbox = null)
    {
        if (!m_IsPotentialDrag || m_IsDragging) return;

        if ((eventData.position - m_PointerDownPosition).magnitude >= dragThreshold)
        {
           
            m_IsDragging = true;
            m_IsPotentialDrag = false;

            if (isTemplate)
            {
                StartDraggingTemplateInternal(blockView, sourceToolbox, eventData);
            }
            else
            {
                StartDraggingBlockInternal(blockView, eventData);
            }
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

        Debug.Log($"<color=magenta>BlockDragController: Starting drag - BLOCK {m_DraggingBlockModel.ID}</color>");

        m_workspaceController.RequestBlockUnplug(m_DraggingBlockModel, true);

        Debug.Log($"   Reparenting '{blockView.name}' to DragLayer '{m_DragLayerRect.name}'", this);
        m_DraggingBlockView.transform.SetParent(m_DragLayerRect, true);

        if (!PrepareVisualDrag(eventData)) 
        {
            Debug.LogError($"PrepareVisualDrag failed for block {m_DraggingBlockModel.ID}. Aborting drag.", this);
            m_DraggingBlockView.transform.SetParent(m_CodingAreaRect, true); 
            ResetDragState(eventData);
            return;
        }

        m_IsDragging = true; // Marcar como arrastrando 
        Debug.Log($"<color=green>BlockDragController: Drag successfully initiated for BLOCK {m_DraggingBlockModel.ID}. Offset: {m_DragOffsetViewSpace}</color>", this);
    }

    //Método que se encarga de arrastrar una plantilla de bloque que está en la ToolBox y lo posiciona en la DragLayer
    public IEnumerator StartDraggingTemplateInternal(BlockView templateBlockView, BlockListView sourceToolbox, PointerEventData eventData)
    {
        if (templateBlockView?.Block == null || sourceToolbox == null)
        {
            Debug.LogError("StartDraggingTemplateInternal: templateBlockView, its model, or sourceToolbox is null.");
            ResetDragState(eventData);
            yield break; ;
        }

        //Posición del puntero del ratón

        Debug.Log($"---> PointerDown Screen Position (Template): {eventData.position} <---");


        BlockModel templateModel = templateBlockView.Block;

        //string cloneModelId = "UNKNOWN_ID";
        //string cloneViewName = "UNKNOWN_VIEW";
        //int cloneInstanceId = 0;

        Debug.Log($"<color=yellow>BlockDragController: Starting drag - TEMPLATE for {templateModel.Type}</color>  Pointer Screen Pos: {eventData.position}");
       
        //Creamos model Clon
        Vector2 startPosLogical = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);//null) o m_CachedCamera;
        Debug.Log($"    Pointer ScreenPos: {eventData.position} -> Initial Logical Pos: {startPosLogical}");
        
        BlockModel CloneModel = m_workspaceController.RequestCloneBlockBegin(templateModel, startPosLogical);

        if (CloneModel == null)
        {
            Debug.LogError("Failed to create clone model from WorkspaceController.");
            ResetDragState(eventData);
            yield break; ;
        }

        //cloneModelId = CloneModel.ID; // Para debug

        //Creamos la vista del clon
        BlockView cloneView = BlockViewFactory.CreateView(CloneModel, sourceToolbox);
        if (cloneView == null)
        {
            Debug.LogError("BlockViewFactory failed to create view for the cloned model.");
            CloneModel.Dispose(false);
            ResetDragState(eventData);
            yield break; ;
        }

        Debug.Log($"<color=lightblue>...BlockView '{cloneView.gameObject.name}' created for clone ID '{CloneModel.ID}'.</color>");

        //cloneViewName = cloneView.gameObject.name;

        // Debug.Log($"<color=lightblue>...BlockView {cloneViewName} created for clone.</color>");
        //cloneInstanceId = cloneView.gameObject.GetInstanceID();

        //Debug.Log($"<color=lightblue>...BlockView {cloneViewName} ({cloneInstanceId}) created for clone.</color>");

        m_WasTemplateClone = true;
        m_DraggingBlockView = cloneView;
        m_DraggingBlockModel = CloneModel;
        m_IsDragging = true;
        m_DraggingBlockView.InToolbox = false; //No está en la toolbox

        Debug.Log($"<color=orange>Assigning Parent: m_CodingAreaRect is {(m_CodingAreaRect == null ? "NULL" : m_CodingAreaRect.name)} for {cloneView.name}</color>", this);

        if (m_DragLayerRect == null)
        {
            Debug.LogError("CANNOT SetParent: m_DragLayerRectis NULL!", this);
            
            ResetDragState(eventData); 
           // m_DraggingBlockModel?.Dispose(false);
            yield break;
        }

        Debug.Log($"   Reparenting '{cloneView.name}' TEMPORARILY to DragLayer '{m_DragLayerRect.name}'", this);
        m_DraggingBlockView.transform.SetParent(m_DragLayerRect, true); // Cambiamos a DragLayer

        //Calculamos dónde está el puntero en  DragLayer
        Vector2 initialPointerLocalInDragLayer;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_DragLayerRect, eventData.position, m_CachedCamera, out initialPointerLocalInDragLayer))
        {
            Debug.LogError("... FAILED to convert initial pointer pos to DragLayer local space ...");
            // Limpiar clon
            if (CloneModel != null) CloneModel.Dispose(false);
            if (cloneView != null && cloneView.gameObject != null) Destroy(cloneView.gameObject);
            ResetDragState(eventData);
            yield break;
        }
        Debug.Log($"   Initial Pointer Local in DragLayer: {initialPointerLocalInDragLayer}");

        //Calculamos dónde se hizo clic dentro del bloque (relativo a su pivot)
        Vector2 initialPointerLocalInBlock;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_DraggingBlockView.ViewTransform, eventData.position, m_CachedCamera, out initialPointerLocalInBlock))
        {
            Debug.LogWarning($"... FAILED to convert initial pointer pos to BlockView local space. Using zero block offset.", m_DraggingBlockView);
            initialPointerLocalInBlock = Vector2.zero; 
        }
        Debug.Log($"   Initial Pointer Local in Block (Offset from Pivot 0,1): {initialPointerLocalInBlock}");

        //Calcular la anchoredPosition para el bloque en DragLayer para que el punto de clic coincida con el puntero.
        //    Posición del pivot = (Posición del puntero en dragLayer) - (Vector desde pivot del bloque hasta punto de clic en bloque)
        Vector2 targetAnchoredPos = initialPointerLocalInDragLayer - initialPointerLocalInBlock;
        Debug.Log($"   Calculated Target AnchoredPos in DragLayer: {targetAnchoredPos}");

        m_DraggingBlockView.ViewTransform.anchoredPosition = targetAnchoredPos;
        Debug.Log($"   Block AnchoredPosition SET EXPLICITLY to: {targetAnchoredPos}");


        m_DragOffsetViewSpace = targetAnchoredPos - initialPointerLocalInDragLayer;//initialPointerLocalInDragLayer;
        // Equivalente a: m_DragOffsetViewSpace = -initialPointerLocalInBlock;
        Debug.Log($"   Final Calculated Offset (BlockPivot - PointerInDragLayer): {m_DragOffsetViewSpace}");

        m_DraggingBlockView.transform.SetAsLastSibling(); // Dibujar encima
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>() ?? m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // Que no interfiera con eventos mientras se arrastra
        ClearHighlight(); // Limpiar resaltados previos

        m_IsDragging = true; // Marcar como arrastrando 
        Debug.Log($"<color=green>BlockDragController: Drag successfully initiated for CLONE {m_DraggingBlockModel.ID}. Final Offset: {m_DragOffsetViewSpace}</color>");

    }

    private bool IsPointerOverCodingArea(PointerEventData eventData)
    {
        if (m_CodingAreaRect == null) return false; // Si no hay área, no está encima

        // Comprueba si la posición del puntero en pantalla está dentro del RectTransform del CodingArea
        return RectTransformUtility.RectangleContainsScreenPoint(
            m_CodingAreaRect,
            eventData.position,
            null // null cámara - ScreenSpaceOverlay
        );
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
            Debug.LogError("PrepareVisualDrag: Invalid dragging block view state.");
            ResetDragState(eventData); 
            return false;
        }
        //Vector2 currentAnchoredPos = m_DraggingBlockView.ViewTransform.anchoredPosition;

        // Recalcula la posición del puntero relativa al padre actual

        RectTransform parentRect = m_DraggingBlockView.ViewTransform.parent as RectTransform;
        if (parentRect == null) {
            Debug.LogError($"PrepareVisualDrag: Block '{m_DraggingBlockView.name}' has no parent RectTransform after being parented!");
            parentRect = m_CodingAreaRect; 
            if (parentRect == null)
            {
                ResetDragState(eventData); return false;
            }

        }

        //Calculamos posición Local del Puntero en el Padre
        Vector2 localPointerPosInParent;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, null,/*m_CachedCamera, <--- null por screen.Overlay*/ out localPointerPosInParent))
        {
            Debug.LogWarning($"PrepareVisualDrag(Block): Conversion Screen->ParentLocal FAILED.");
            // localPointerPosInParent = currentAnchoredPos; 
            m_DragOffsetViewSpace = Vector2.zero;
            return false;
        }

        Debug.Log($"PrepareVisualDrag: Pointer Local Pos in Parent '{parentRect.name}': {localPointerPosInParent}");

        //Determinar la AnchoredPosition  del Bloque

        Vector2 currentBlockAnchoredPos = m_DraggingBlockView.ViewTransform.anchoredPosition;
        Debug.Log($"PrepareVisualDrag: Block's current AnchoredPosition: {currentBlockAnchoredPos}");

        //Calculamos el Offset Visual 
        m_DragOffsetViewSpace = currentBlockAnchoredPos - localPointerPosInParent;

        Debug.Log($"PrepareVisualDrag: Calculated Offset = BlockPivot ({currentBlockAnchoredPos}) - PointerLocal ({localPointerPosInParent}) = {m_DragOffsetViewSpace}");

        // m_DragOffsetViewSpace = currentAnchoredPos - localPointerPosInParent;
        m_DraggingBlockView.transform.SetAsLastSibling(); //Permite mostrarlo encima

        //Añadimos CanvasGroup si no existe
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
        if (cg == null) cg = m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; //Desactivar raycasts para evitar interacciones mientras se arrastra

        ClearHighlight();
        Debug.Log($"<color=lightblue>BlockDragController: Visual drag prepared for {m_DraggingBlockModel?.ID ?? "UNKNOWN"} (View: {m_DraggingBlockView.name}). Offset: {m_DragOffsetViewSpace}</color>");
        return true;

    }

    /// <summary>
    /// Llamado repetidamente durante el drag .Actualiza la posición visual y busca conexiones.
    /// </summary>
    public void HandleDrag(BlockView blockView, PointerEventData eventData)
    {
        if (!m_IsDragging || m_DraggingBlockView == null)
        {
             Debug.LogWarning($"HandleDrag called while m_IsDragging=true but m_DraggingBlockView is null!");

            return; 
        }

        if (blockView != null && m_DraggingBlockView != blockView)
        {
            Debug.Log($"HandleDrag ignoring event from {blockView.name}, currently dragging {m_DraggingBlockView.name}");
            return;
        }

        Debug.Log($"HandleDrag: Processing drag for {m_DraggingBlockView.name}");

        Vector2 localPointerPosition;
        RectTransform parentRect = m_DragLayerRect; //<----Padre es el Drag Layer cuando inicio el movimiento en la toolbox
      //  if (parentRect == null) parentRect = m_CodingAreaRect; 
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                m_CachedCamera,//null,//m_CachedCamera, <--- null por screen.Overlay 
                out localPointerPosition))
        {
            
            //m_DraggingBlockView.ViewTransform.anchoredPosition = localPointerPosition + m_DragOffsetViewSpace;
            Vector2 newAnchoredPos = localPointerPosition + m_DragOffsetViewSpace;
            Debug.Log($"  - PointerLocal: {localPointerPosition}, Offset: {m_DragOffsetViewSpace}, New AnchoredPos: {newAnchoredPos}");
            //  m_DraggingBlockView.ViewTransform.anchoredPosition = localPointerPosition;// newAnchoredPos;
            m_DraggingBlockView.ViewTransform.anchoredPosition = newAnchoredPos;
        }
        else
        {
            Debug.LogWarning("HandleDrag: ScreenPointToLocalPointInRectangle failed.");
            return; 
        }

        // Calcular posición lógica para búsqueda de conexiones
        Vector2 currentLogicalPos = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);

        Debug.Log($"  - Current Logical Pos: {currentLogicalPos}");

        ConnectionModel oldBestTarget = m_BestTargetConnection;

       // FindBestConnection(currentLogicalPos);

        FindConnectionView(oldBestTarget);

        UpdateHighlighting(oldBestTarget);

        m_WorkspaceView?.CheckTrashBin(m_DraggingBlockView);
    }

    public void HandleEndDrag(BlockView blockView, PointerEventData eventData)
    {
        if (!m_IsDragging || m_DraggingBlockView /*!= blockView || m_DraggingBlockView */== null)
        {
            m_IsPotentialDrag = false;
            return;
        }

        if (blockView != null && m_DraggingBlockView != blockView)
        {
            return;
        }

        string blockId = m_DraggingBlockModel?.ID ?? "UNKNOWN";
        string viewName = m_DraggingBlockView.name;

        Debug.Log($"<color=cyan>BlockDragController: Ending drag - Block {blockId} ({m_DraggingBlockView.name})</color>");
        Debug.Log($"    Pointer Screen Pos: {eventData.position}");

        m_IsPotentialDrag = false;

        ConnectionModel finalTargetConnection = m_BestTargetConnection; // La conexión encontrada
        ConnectionModel finalSourceConnection = m_SourceDragConnection; // La conexión que quería conectar
        bool overTrash = m_WorkspaceView.IsOverTrashBin(m_DraggingBlockView);
        bool overCodingArea = IsPointerOverCodingArea(eventData);

        bool connected = false;
        bool deleted = false;
        bool placedInWorkspace = false;

        // 1. ¿Hay conexión válida?
        if (finalTargetConnection != null && finalSourceConnection != null)
        {
            Debug.Log($"<color=green>... Drop detected over connection. Attempting: {finalSourceConnection.SourceBlock.Type} -> {finalTargetConnection.SourceBlock.Type}</color>");

            Debug.Log($"   Reparenting '{viewName}' to CodingArea '{m_CodingAreaRect.name}' before connecting.");
            m_DraggingBlockView.transform.SetParent(m_CodingAreaRect, true); //<----Padre: CodingArea
            m_DraggingBlockView.transform.SetAsLastSibling(); 

            if (m_workspaceController.RequestConnection(finalSourceConnection, finalTargetConnection))
            {
                connected = true;
                placedInWorkspace = true; 
                Debug.Log("<color=green>... Connection SUCCESSFUL.</color>");
            }
            else
            {
                Debug.LogWarning("<color=yellow>... Connection FAILED (Controller/Model Rejected). Treating as free drop.</color>");
            }
        }

        if (!connected && overTrash) //No va a ser necesario tras la lógica de si no esta en el RightPanel desaparece imitando comportamiento scratch
        {
            deleted = true;
            Debug.Log("<color=red>... Dropped over Trash Bin.</color>");
            HandleTrashDrop();
        }

        if (!connected && !deleted && overCodingArea)
        {
            placedInWorkspace = true;
            Debug.Log($"<color=blue>... Dropped inside CodingArea '{m_CodingAreaRect.name}'. Placing block.</color>");

            // Calculamos posición local final dentro del CodingArea
            Vector2 finalPointerLocalInCodingArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CodingAreaRect, eventData.position, m_CachedCamera, out finalPointerLocalInCodingArea);
            Vector2 finalAnchoredPosInCodingArea = finalPointerLocalInCodingArea + m_DragOffsetViewSpace; // Usa el mismo offset global

            if (m_DraggingBlockView.transform.parent != m_CodingAreaRect) //<---- Padre_ codingArea
            {
                Debug.Log($"   Reparenting '{viewName}' to CodingArea '{m_CodingAreaRect.name}'.");
                m_DraggingBlockView.transform.SetParent(m_CodingAreaRect, true);
            }
            m_DraggingBlockView.transform.SetAsLastSibling();

            // Aplicar Posición Final
            m_DraggingBlockView.ViewTransform.anchoredPosition = finalAnchoredPosInCodingArea;
            Debug.Log($"   Final AnchoredPos in CodingArea set to: {finalAnchoredPosInCodingArea}");

        }

        if (!connected && !deleted && !placedInWorkspace)
        {
            deleted = true; 
            Debug.Log($"<color=orange>... Dropped OUTSIDE Coding Area (Over Toolbox? Left Panel? Off screen?). Invalid drop.</color>");
            HandleInvalidDrop(); 
        }

        CleanUpAfterDrag(placedInWorkspace, eventData);

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
        if (m_WasTemplateClone)
        {
            Debug.Log("   Cancelling pending clone (Dropped outside valid area).");
            if (m_DraggingBlockModel != null)
            {
                Debug.Log($"   Disposing cancelled clone model: {m_DraggingBlockModel.ID}");
                m_DraggingBlockModel.Dispose(false);
            }
            else
            {

                Debug.Log($"   Requesting deletion of existing block (Dropped outside): {m_DraggingBlockModel.ID}");
                m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
            }
        }
    }

    /// <summary>
    /// Realiza la limpieza final después de procesar el final del drag.
    /// </summary>
    private void CleanUpAfterDrag(bool wasPlacedOrConnected, PointerEventData eventData)
    {
        ClearHighlight();

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

        Debug.Log("<color=cyan>BlockDragController: Drag sequence finished.</color>");
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
        m_BestTargetConnection = null;
        m_SourceDragConnection = null;
        ClearHighlight(); 
        m_WasTemplateClone = false;

    }

    /// <summary>
    /// Busca la mejor conexión candidata basada en la posición lógica actual.
    /// </summary>
    private void FindBestConnection(Vector2 currentLogicalPos)
    {
        m_BestTargetConnection = null;
        m_SourceDragConnection = null;
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
                    m_BestTargetConnection = neighbour;
                    m_SourceDragConnection = myConn;
                }
            }
        }
    }

    /// <summary>
    /// Actualiza el estado visual del resaltado de conexiones.
    /// </summary>
    private void UpdateHighlighting(ConnectionModel oldBestTarget)
    {
        if (oldBestTarget != m_BestTargetConnection) 
        {
            if (oldBestTarget != null)
            {
                ConnectionView oldView = m_WorkspaceView?.GetConnectionView(oldBestTarget);
                oldView?.Highlight(false);
            }

            if (m_HighlightedTargetView != null /*&& m_HighlightedTargetView.Model != m_BestTargetConnection*/)
            {
                m_HighlightedTargetView.Highlight(false); 
                m_HighlightedTargetView = null;
            }

            if (m_BestTargetConnection != null)
            {
                ConnectionView newView = m_WorkspaceView?.GetConnectionView(m_BestTargetConnection);
                if (newView != null)
                {
                    HighlightConnection(newView); 
                }
                else
                {
                    m_HighlightedTargetView = null; 
                    Debug.LogWarning($"Could not find ConnectionView for BestTargetConnection: {m_BestTargetConnection.SourceBlock.Type}:{m_BestTargetConnection.Type}");
                }
            }
        }
    }

}//fin clase BlockDragController

