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

using UnityEngine;
using UnityEngine.EventSystems; 
using System;
using UnityEngine.UI;
using System.Collections.Generic;

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
    private Canvas m_RootCanvas;            

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }
    public void InitializeController(WorkSpaceModel Workspace, WorkSpaceView WorkSpaceView, WorkspaceController wsController)
    {
        m_Workspace = Workspace ?? throw new ArgumentNullException(nameof(Workspace));
        m_workspaceController = wsController ?? throw new ArgumentNullException(nameof(wsController));
        m_WorkspaceView = WorkSpaceView ?? throw new ArgumentNullException(nameof(WorkSpaceView));

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
            m_RootCanvas = m_WorkspaceView.RootCanvas;     
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

    public void StartDraggingTemplate(BlockView templateBlockView, BaseToolbox sourceToolbox, PointerEventData eventData)
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
           m_CachedCamera,
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

    private void TryStartDrag(BlockView blockView, PointerEventData eventData, bool isTemplate, BaseToolbox sourceToolbox = null)
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

    public void StartDraggingBlockInternal(BlockView blockView, PointerEventData eventData)
    {
        if (blockView == null || blockView.Block == null)
        {
            Debug.LogError("StartDraggingBlockInternal: blockView or its model is null.");
            ResetDragState(eventData);
            return;
        }

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

        PrepareVisualDrag(eventData);
    }

    public void StartDraggingTemplateInternal(BlockView templateBlockView, BaseToolbox sourceToolbox, PointerEventData eventData)
    {
        if (templateBlockView?.Block == null || sourceToolbox == null)
        {
            Debug.LogError("StartDraggingTemplateInternal: templateBlockView, its model, or sourceToolbox is null.");
            ResetDragState(eventData);
            return;
        }

        BlockModel templateModel = templateBlockView.Block;
        Debug.Log($"<color=yellow>BlockDragController: Starting drag - TEMPLATE for {templateModel.Type}</color>");

        Vector2 startPosLogical = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);

        m_PendingCloneModel = m_workspaceController.RequestCloneBlockBegin(templateModel, startPosLogical);
        if (m_PendingCloneModel == null)
        {
            Debug.LogError("Failed to create clone model from WorkspaceController.");
            ResetDragState(eventData);
            return;
        }

        //BlockView cloneView = BlockViewFactory.CreateView(m_PendingCloneModel, sourceToolbox);
        BlockView cloneView = BlockViewFactory.CreateView(m_PendingCloneModel, sourceToolbox);
        if (cloneView == null)
        {
            Debug.LogError("BlockViewFactory failed to create view for the cloned model.");
            m_PendingCloneModel.Dispose(false);
            m_PendingCloneModel = null;
            ResetDragState(eventData);
            return;
        }

        m_WasTemplateClone = true;
        m_DraggingBlockView = cloneView;
        m_DraggingBlockModel = m_PendingCloneModel;
        m_PendingCloneModel = null; 

        m_DraggingBlockView.InToolbox = false;
        m_DraggingBlockView.ViewTransform.SetParent(m_CodingAreaRect, true);
        m_DraggingBlockView.XY = startPosLogical; 
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_DraggingBlockView.GetComponent<RectTransform>());


        Debug.Log($"<color=yellow>BlockDragController: Created clone {m_DraggingBlockModel.ID}, preparing visual drag.</color>");

        PrepareVisualDrag(eventData);
    }

    private void PrepareVisualDrag(PointerEventData eventData)
    {
        if (m_DraggingBlockView == null || m_DraggingBlockView.ViewTransform == null)
        {
            Debug.LogError("PrepareVisualDrag: DraggingBlockView or its transform is null!");
            return;
        }
        if (m_DraggingBlockView.gameObject == null)
        {
            Debug.LogError("PrepareVisualDrag: DraggingBlockView GameObject has been destroyed!");
            m_IsDragging = false; 
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
           (RectTransform)m_DraggingBlockView.ViewTransform.parent, 
           eventData.position,
           m_CachedCamera,
           out Vector2 localPointerPos);

        m_DragOffsetViewSpace = m_DraggingBlockView.ViewTransform.anchoredPosition - localPointerPos;

        m_DraggingBlockView.transform.SetAsLastSibling();
        var cg = m_DraggingBlockView.GetComponent<CanvasGroup>();
        if (cg == null) cg = m_DraggingBlockView.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; 

        ClearHighlight();
        Debug.Log($"<color=lightblue>BlockDragController: Visual drag prepared for {m_DraggingBlockModel?.ID ?? "UNKNOWN"}.</color>");
    }

    public void HandleDrag(BlockView blockView, PointerEventData eventData)
    {
        if (!m_IsDragging && m_IsPotentialDrag && blockView != null)
        {
            TryStartDrag(blockView, eventData, blockView.InToolbox, m_WorkspaceView?.Toolbox);
            if (!m_IsDragging) return; 
        }

        if (!m_IsDragging || m_DraggingBlockView != blockView || m_DraggingBlockView == null) return;

        Vector2 localPointerPosition;
        RectTransform parentRect = m_DraggingBlockView.ViewTransform.parent as RectTransform;
        if (parentRect == null) parentRect = m_CodingAreaRect; 
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                m_CachedCamera, 
                out localPointerPosition))
        {
            m_DraggingBlockView.ViewTransform.anchoredPosition = localPointerPosition + m_DragOffsetViewSpace;
        }
        else
        {
            Debug.LogWarning("HandleDrag: ScreenPointToLocalPointInRectangle failed.");
            return; 
        }

        Vector2 currentLogicalPos = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);
        ConnectionModel oldBestTarget = m_BestTargetConnection; 
        m_BestTargetConnection = null;
        m_SourceDragConnection = null;
        float closestRadiusSq = m_SnapDistance * m_SnapDistance; 

        List<ConnectionModel> myConnections = m_DraggingBlockModel.GetDraggingConnections();

        foreach (ConnectionModel myConn in myConnections)
        {
            if (myConn == null || m_Workspace == null) continue; 

            BlockConnectionDB oppositeDB = m_Workspace.GetConnectionDB(myConn.OppositeType);
            if (oppositeDB == null) continue; 

            Vector2 myConnCurrentLogicalPos = currentLogicalPos; 

            ConnectionModel neighbour;
            float radiusSq;

            oppositeDB.SearchForClosest(myConn, (int)m_ConnectionSearchRadius, currentLogicalPos, out neighbour, out radiusSq);

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

        if (oldBestTarget != m_BestTargetConnection)
        {
            if (oldBestTarget != null)
            {
                ConnectionView oldView = m_WorkspaceView?.GetConnectionView(oldBestTarget);
                oldView?.Highlight(false);
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
                }
            }
            else
            {
                m_HighlightedTargetView = null; 
            }
        }

        m_WorkspaceView?.CheckTrashBin(m_DraggingBlockView);
    }

    public void HandleEndDrag(BlockView blockView, PointerEventData eventData)
    {
        if (!m_IsDragging || m_DraggingBlockView != blockView || m_DraggingBlockView == null)
        {
            m_IsPotentialDrag = false; 
            return;
        }

        Debug.Log($"<color=cyan>BlockDragController: Ending drag - Block {m_DraggingBlockModel?.ID ?? "UNKNOWN"}</color>");
        m_IsPotentialDrag = false; 

        bool connected = false;
        bool deleted = false;

        if (m_BestTargetConnection != null && m_SourceDragConnection != null)
        {
            Debug.Log($"<color=green>... Attempting connection: {m_SourceDragConnection.SourceBlock.Type} -> {m_BestTargetConnection.SourceBlock.Type}</color>");
            if (m_workspaceController.RequestConnection(m_SourceDragConnection, m_BestTargetConnection))
            {
                Debug.Log("<color=green>... Connection SUCCESSFUL.</color>");
                connected = true;
            }
            else
            {
                Debug.LogWarning("<color=yellow>... Connection FAILED (requested but controller/model rejected).</color>");
                if (m_SourceDragConnection != null && m_SourceDragConnection.IsConnected)
                    m_SourceDragConnection.Disconnect();
            }
        }

        if (!connected && m_WorkspaceView.IsOverTrashBin(m_DraggingBlockView))
        {
            Debug.Log("<color=red>... Dropped over trash bin.</color>");
            if (m_WasTemplateClone)
            {
                Debug.Log("<color=red>... Cancelling pending clone.</color>");
                CancelPendingClone(m_DraggingBlockModel, "Dropped in trash");
            }
            else
            {
                Debug.Log($"<color=red>... Requesting deletion of block {m_DraggingBlockModel.ID}.</color>");
                m_workspaceController.RequestDeleteBlock(m_DraggingBlockModel);
            }
            deleted = true; 
        }

        if (!connected && !deleted)
        {
            Vector2 finalLogicalPosition = m_WorkspaceView.ScreenPointToWorkspaceLogicalPosition(eventData.position, m_CachedCamera);

        
            Vector2 viewSize = m_DraggingBlockView.ViewTransform.rect.size;
            // Vector2 pivotOffset = new Vector2(viewSize.x * m_DraggingBlockView.ViewTransform.pivot.x, viewSize.y * (1 - m_DraggingBlockView.ViewTransform.pivot.y)); // Offset del pivote
            
            // float scale = 1.0f; // Asumiendo escala 1 por ahora
            // Vector2 logicalOffset = new Vector2(pivotOffset.x / scale, pivotOffset.y / scale); 
            // finalLogicalPosition -= logicalOffset; // Ajustar para centrar

            finalLogicalPosition += m_DragOffsetViewSpace; 

            if (m_WasTemplateClone)
            {
                Debug.Log($"<color=blue>... Confirming add & move for CLONE {m_DraggingBlockModel.ID} to {finalLogicalPosition}.</color>");
               
                m_workspaceController.RequestBlockMove(m_DraggingBlockModel, finalLogicalPosition);
                m_workspaceController.ConfirmAddBlock(m_DraggingBlockModel); 

            }
            else
            {
                Debug.Log($"<color=blue>... Requesting move for BLOCK {m_DraggingBlockModel.ID} to {finalLogicalPosition}.</color>");
                m_workspaceController.RequestBlockMove(m_DraggingBlockModel, finalLogicalPosition);
            }
        }

        ClearHighlight();
        ResetDragState(eventData);
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

   
}//fin clase BlockDragController

