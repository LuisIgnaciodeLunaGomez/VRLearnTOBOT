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

public class BlockDragController : MonoBehaviour
{    public static BlockDragController Instance { get; private set; }

    private WorkSpaceModel m_Workspace; 
    private WorkSpaceView m_WorkspaceView;
    private WorkspaceController m_workspaceController; 

    private BlockView m_DraggingBlockView = null;
    private BlockModel m_DraggingBlockModel = null;        
    private Vector2 m_DragOffsetViewSpace;                   
    private bool m_IsDragging = false;
    private bool m_WasTemplateClone = false;
    private BlockModel m_PendingCloneModel = null;          

    private ConnectionModel m_BestTargetConnection = null;      
    private ConnectionModel m_SourceDragConnection = null;      
    private ConnectionView m_HighlightedTargetView = null;

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

}//fin clase BlockDragController

