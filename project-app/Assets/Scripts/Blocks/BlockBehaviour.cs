/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/01/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 * 
 */

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Para manejar eventos de UI


public class BlockBehaviour : MonoBehaviour, IBeginDragHandler
{
    private string m_BlockType;// Almaceno la información del bloque
    private Text m_BlockText; // Referencia al texto UI dentro del prefab
    private Vector2 m_TouchOffset; // Almacena la diferencia entre el punto de toque y la posición del bloque
    private bool isDraggable = true;

    //Método para inicializar el bloque
    public void Initialize(BlockDataLoader.BlockData blockData )
    {
        this.m_BlockType = blockData.type;
        this.m_BlockText = GetComponentInChildren<Text>();

        // Si el prefab tiene un Text, actualiza su contenido
        if (this.m_BlockText != null)
        {
            this.m_BlockText.text = blockData.type;
        }
        Debug.Log($"Bloque inicializado: {blockData.type} ");

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"Bloque {gameObject.name} comenzó a ser arrastrado");

        transform.SetParent(GameObject.Find("RightPanel").transform, true);

        // Si el bloque está conectado, lo desenchufamos antes de moverlo.
        Block block = GetComponent<Block>();
        if (block != null)
        {
            block.UnPlug();
        }

        SetOrphan(); //  Marca el bloque como huérfano.

        // 🔹 Calculamos la diferencia entre el punto de toque y la posición del bloque
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            Camera.main,
            out localPos
        );

        m_TouchOffset = (Vector2)transform.localPosition - localPos;

        Debug.Log($"Offset del toque calculado: {m_TouchOffset}");
    }

    //Cuando un usuario arrastra un bloque desde la Toolbox y lo suelta en el Workspace, este método se encarga de cambiar su contexto y ubicarlo correctamente

    public void SetOrphan()
    {
       //TODO
    }

    public  void OnPickBlockView()
    {
        //TODO
    }

    public void SetDraggable(bool isDraggable)
    {
        // Configurar el estado de arrastrado del bloque
        this.isDraggable = isDraggable;

        // Si el bloque es draggable, aseguramos que tenga un componente `CanvasGroup`
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = isDraggable; // Permitir que el bloque reciba eventos solo si es draggable
        canvasGroup.alpha = isDraggable ? 1f : 0.5f; // Reducir opacidad si no es draggable
    }

}
