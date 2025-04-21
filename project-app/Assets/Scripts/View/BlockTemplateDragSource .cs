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
public BlockView TemplateBlockView;
[Tooltip("Asigna aquí el BaseToolbox (o tu equivalente) que contiene esta plantilla")]
public BlockListView SourceToolbox; 

void Start()
{
    
    if (TemplateBlockView == null)
    {
        Debug.LogError($"BlockTemplateDragSource en {gameObject.name} no tiene asignada TemplateBlockView.", this);
    }
    if (SourceToolbox == null && BlockDragController.Instance != null && BlockDragController.Instance.GetComponentInParent<WorkSpaceView>() != null)
    {
        SourceToolbox = BlockDragController.Instance.GetComponentInParent<WorkSpaceView>()?.Toolbox; 
        if (SourceToolbox == null)
        {
            // Debug.LogWarning($"BlockTemplateDragSource en {gameObject.name}: No se encontró SourceToolbox automáticamente.", this);
        }
    }
    else if (SourceToolbox == null)
    {
        // Debug.LogError($"BlockTemplateDragSource en {gameObject.name} no tiene asignado SourceToolbox y no se pudo encontrar automáticamente.", this);
    }
}

public void OnBeginDrag(PointerEventData eventData)
{
    if (TemplateBlockView == null || SourceToolbox == null)
        {
        Debug.LogError("Cannot start template drag: TemplateBlockView is not assigned.", this);
        eventData.pointerDrag = null; // Cancela el drag de Unity
        return;
    }
        // Debug.Log($"BlockTemplateDragSource: OnBeginDrag - Requesting drag start for template {TemplateBlockView.BlockType}");
    if (BlockDragController.Instance == null)
    {
        Debug.LogError("BlockTemplateDragSource: BlockDragController.Instance is null!", this);
        eventData.pointerDrag = null;
        return;
    }
    BlockDragController.Instance?.StartDraggingTemplateInternal(TemplateBlockView, SourceToolbox, eventData);
}

public void OnDrag(PointerEventData eventData)
{

    BlockDragController.Instance?.HandleDrag(null, eventData);
}

public void OnEndDrag(PointerEventData eventData)
{
    
    BlockDragController.Instance?.HandleEndDrag(null, eventData); 
}
}