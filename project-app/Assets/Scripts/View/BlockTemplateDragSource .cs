/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 09/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 */

using UnityEngine.EventSystems;
using UnityEngine;
public class BlockTemplateDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{ 

    [Tooltip("Asigna aquí la BlockView de la plantilla asociada a esta máscara")]
    public BlockView m_TemplateBlockView;
    [Tooltip("Asigna aquí el BaseToolbox (o tu equivalente) que contiene esta plantilla")]
    public BlockListView m_SourceToolbox;
    private Camera m_CachedEventCamera = null; // Cachear la cámara
    private BlockController m_ClonedBlockController = null;
    void Start()
    {
       /* Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                m_CachedEventCamera = null;
            else
                m_CachedEventCamera = rootCanvas.worldCamera; // Usar worldCamera o specific event camera
        }
        if (m_CachedEventCamera == null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
          
            Debug.LogWarning("BlockTemplateDragSource could not find necessary camera for UI space.", this);
            // m_CachedEventCamera = Camera.main; 
        }*/

        if (m_TemplateBlockView == null)
        {
            m_TemplateBlockView = GetComponentInParent<BlockView>();
            if (m_TemplateBlockView == null)
                Debug.LogError($"BlockTemplateDragSource en '{gameObject.name}' no tiene ni ha podido encontrar su BlockView padre.", this);
        }
        if (m_SourceToolbox == null && BlockDragController.Instance != null && BlockDragController.Instance.GetComponentInParent<WorkSpaceView>() != null)
        {
            m_SourceToolbox = BlockDragController.Instance.GetComponentInParent<WorkSpaceView>()?.Toolbox;
            if (m_SourceToolbox == null)
            {
                m_SourceToolbox = GetComponentInParent<BlockListView>();
                if (m_SourceToolbox == null)
                    Debug.LogError($"BlockTemplateDragSource en '{gameObject.name}' no tiene ni ha podido encontrar su BlockListView (Toolbox) padre.", this);
            }
        }
       /* else if (m_SourceToolbox == null)
        {
            // Debug.LogError($"BlockTemplateDragSource en {gameObject.name} no tiene asignado SourceToolbox y no se pudo encontrar automáticamente.", this);
        }*/
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Validaciones previas
        if (m_TemplateBlockView == null || m_TemplateBlockView.Block == null || BlockDragController.Instance == null || WorkspaceController.Instance == null)
        {
            Debug.LogError("BlockTemplateDragSource: Dependencias críticas (Template, DragController, WorkspaceController) no encontradas. No se puede iniciar el drag.");
            eventData.pointerDrag = null; // Cancelar el drag de Unity
            return;
        }

        Debug.Log($"<color=orange>BlockTemplateDragSource: OnBeginDrag para la plantilla de tipo '{m_TemplateBlockView.Block.Type}'</color>");

        // 2. Creación del bloque real en el workspace.
        // Le pedimos al WorkspaceController que cree un bloque del mismo tipo que nuestra plantilla.
        // La posición inicial es irrelevante porque el BlockDragController la ajustará inmediatamente.
        m_ClonedBlockController = WorkspaceController.Instance.CreateNewBlock(
            m_TemplateBlockView.Block.Type,
            Vector2.zero
        );

        if (m_ClonedBlockController == null)
        {
            Debug.LogError("OnBeginDrag: FAILED to create the new block via WorkspaceController.");
            eventData.pointerDrag = null;
            return;
        }

        // 3.  Iniciamos el drag del NUEVO bloque, no de la plantilla.
        // Pasamos el controlador del clon al controlador de arrastre.
        BlockDragController.Instance.StartDrag(m_ClonedBlockController, eventData);
    }

    public void OnDrag(PointerEventData eventData)
{

    BlockDragController.Instance?.HandleDrag(/*null,*/ eventData);
}

    public void OnEndDrag(PointerEventData eventData)
    {

        // BlockDragController.Instance?.HandleEndDrag(/*null,*/ eventData);

        if (m_ClonedBlockController != null && BlockDragController.Instance.IsDragging)
        {
            BlockDragController.Instance.EndDrag(eventData);
        }

        // Limpiamos la referencia para el próximo drag
        m_ClonedBlockController = null;
    }

   // public bool IsDragging => BlockDragController.Instance != null && BlockDragController.Instance.IsDragging && BlockDragController.Instance.DraggingView == TemplateBlockView;
}