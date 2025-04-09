/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */

using UnityEngine.EventSystems;
using UnityEngine;

[RequireComponent(typeof(BlockView))]
public class ToolboxBlockDragger : MonoBehaviour, IBeginDragHandler
{
    private BlockView m_templateBlockView;     
    private BlockView m_cloneBlockView = null; 
    private WorkSpaceView m_targetWorkspaceView;
    private CanvasGroup m_canvasGroup;        
    private Vector3 m_startDragOffset;      
    private BaseToolbox m_sourceToolbox;
    private RectTransform m_canvasRect;       
    private CanvasGroup m_templateCanvasGroup;
    public void Init(WorkSpaceView targetView)
    {
        m_targetWorkspaceView = targetView;
        m_templateBlockView = GetComponent<BlockView>();
        if (m_targetWorkspaceView == null) Debug.LogError("Target WorkspaceView is null!");
        if (m_templateBlockView == null) Debug.LogError("Template BlockView is null!");

        m_canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();

        m_canvasRect = m_targetWorkspaceView.RootCanvas.GetComponent<RectTransform>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_templateBlockView == null || m_templateBlockView.Block == null || 
            m_targetWorkspaceView == null || m_canvasRect == null ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            Debug.LogWarning($"ToolboxBlockDragger: BeginDrag conditions not met (Block: {m_templateBlockView?.Block?.Type}, TargetWS: {m_targetWorkspaceView != null})");
            eventData.pointerDrag = null;
            return;
        }
        Debug.Log($"<color=#FF7F50>ToolboxDragger: Begin Drag on {m_templateBlockView.Block.Type}</color>");

        Vector2 initialLogicalPos = Utilidades.Screen2WorkspacePos(m_targetWorkspaceView.Workspace,
                                                             m_targetWorkspaceView.CodingArea,
                                                             eventData.position,
                                                             m_targetWorkspaceView.RootCanvas);

        m_cloneBlockView = m_targetWorkspaceView.CloneBlockView(m_templateBlockView, m_sourceToolbox, initialLogicalPos);

        if (m_cloneBlockView == null || m_cloneBlockView.gameObject == null) 
        {
            Debug.LogError("ToolboxBlockDragger: Failed to clone block view!");
            eventData.pointerDrag = null;
            return;
        }
        // Debug.Log($"ToolboxDragger: Clone created: {m_cloneBlockView.gameObject.name}");

        eventData.pointerDrag = m_cloneBlockView.gameObject; 
        
        if (m_templateCanvasGroup != null)
        {
            m_templateCanvasGroup.alpha = 0.5f;
            m_templateCanvasGroup.blocksRaycasts = false;
        }

       
        BlockView clonedBlockViewComponent = m_cloneBlockView.GetComponent<BlockView>();
        if (clonedBlockViewComponent != null)
        {
            // Debug.Log("ToolboxDragger: Initiating drag externally on clone...");
            clonedBlockViewComponent.InitiateDragFromExternal(eventData); 
        }
        else
        {
            Debug.LogError($"CRITICAL: Clone {m_cloneBlockView.gameObject.name} doesn't have BlockView component!");
            Destroy(m_cloneBlockView.gameObject);
            eventData.pointerDrag = null;
            if (m_templateCanvasGroup != null) { m_templateCanvasGroup.alpha = 1f; m_templateCanvasGroup.blocksRaycasts = true; } 
            m_cloneBlockView = null; 
            return; 
        }

    }

}//fin clase ToolboxBlockDragger