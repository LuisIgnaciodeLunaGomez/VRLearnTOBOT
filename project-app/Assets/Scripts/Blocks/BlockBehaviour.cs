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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Para manejar eventos de UI

public class BlockBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string m_BlockType;// Almaceno la información del bloque
    private Text m_BlockText; // Referencia al texto UI dentro del prefab
    private Vector2 m_TouchOffset; // Almacena la diferencia entre el punto de toque y la posición del bloque
    private bool isDraggable = true; //Indica si el bloque se puede arrastrar
    private bool isTemplate = false; //Indica si el bloque es una plantilla de la ToolBox de scratch
    private Block m_block; //Referencia al bloque lógico

    /**
    * Descripción: Método para inicializar el bloque
    * @param: BlockDataLoader.BlockData blockData
    */

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

    public void SetBlock(Block block)
    {
        this.m_block = block;
    }

    /**
     * Descripción: Método para establecer si el bloque es una plantilla
     * @param: bool is Template
     */
    public void SetAsTemplate (bool isTemplate)
    {
        this.isTemplate = isTemplate;
    }

    /**
     * Descripción: Método para activar o desactivar el arrastre del bloque
     * @param: bool isDraggable
     */

    public void SetDraggable(bool isDragable)
    {
        this.isDraggable = isDragable;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = isDragable; //Activa y desactiva las colisiones y la interacción
        canvasGroup.alpha = isDragable ? 1f : 0.5f;
    }

    /**
     * Descripcion: Método llamado para iniciar el arrastre del bloque
     * @param: PointerEventData eventData
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"Bloque {gameObject.name} comenzó a ser arrastrado");

        if (isTemplate)
        {
            
            GameObject clonedBlock = OnPickBlockView(eventData);
            if (clonedBlock != null)
            {
                clonedBlock.GetComponent<BlockBehaviour>().OnBeginDrag(eventData);
            }
            return;
        }

        if(!isDraggable) return; //Si no es arrastrable, no hacemos nada 

        // Cambiar el padre del bloque al panel derecho
        transform.SetParent(GameObject.Find("CodingArea").transform, true);

        // Si el bloque está conectado, lo desenchufamos antes de moverlo.
        //Block block = GetComponent<Block>();
        if (this.m_block != null)
        {
            this.m_block.UnPlug();
        }

        SetOrphan(); //  Marca el bloque como huérfano.

        // Calculamos la diferencia entre el punto de toque y la posición del bloque
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        m_TouchOffset = (Vector2)transform.localPosition - localPos;

        Debug.Log($"Offset del toque calculado: {m_TouchOffset}");
    }

    /**
     * Descripción: Cuando un usuario arrastra un bloque desde la Toolbox y lo suelta en el Workspace, este método se encarga de cambiar su contexto y ubicarlo correctamente
     */

    public void SetOrphan()
    {
        //Block blocks = GetComponent<Block>();
        if (this.m_block != null)
        {
            //TODO reseta conexiones en el modelo lógico
                
        }
    }

    /**
     * Descripción Método utilizado para clonar un bloque si este es una plantilla
    */   
    public  GameObject OnPickBlockView(PointerEventData eventData)
    {
         GameObject clonedBlock = Instantiate(gameObject, transform.parent);
          
         BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();
         clonedBehaviour.SetAsTemplate(false); //Desactiva la plantilla
         clonedBehaviour.SetDraggable(true); //Activa el arrastre

            // Mover el clon al CodingArea
         Transform codingArea = GameObject.Find("CodingArea").transform;
         clonedBlock.transform.SetParent(codingArea, false);

        //clonedBlock.transform.localScale = Vector3.one;

        // Calcular la posición inicial del clon basada en el cursor
        Vector2 localPos;
         RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)codingArea,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
         );
            clonedBlock.transform.localPosition = localPos;

        eventData.pointerDrag = clonedBlock;
       

        // Iniciar el arrastre en el clon
        clonedBehaviour.OnBeginDrag(eventData);
         return clonedBlock; // Salir para no mover la plantilla
    }

 

    /**
     * Descripción: Método llamado cuando el usuario arrastra un bloque
     * @param: PointerEventData eventData
     */
    public void  OnDrag(PointerEventData eventData)
    {
    
        if (!isDraggable || isTemplate) return; //Si no es arrastrable, no hacemos nada

        Vector2 localPos; // Almacena la posición local del bloque

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        //Actualizo la posición del bloque

        transform.localPosition = localPos + m_TouchOffset;
    }

    /**
     * Descripción: Método llamado cuando el usuario suelta un bloque
     * @param: PointerEventData eventData
     */
    public void OnEndDrag(PointerEventData eventData)
    {
         if(!isDraggable || isTemplate) return; //Si no es arrastrable, no hacemos nada

         Debug.Log($"Bloque {gameObject.name} terminó de ser arrastrado");

        // Restaurar la interactividad del bloque
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // Verificar si el bloque está en el área correcta
        Transform workspace = GameObject.Find("CodingArea").transform;
        if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)workspace, eventData.position, eventData.pressEventCamera))
        {
            transform.SetParent(workspace, true);
            Debug.Log("Bloque colocado en el área de programación.");
        }
        else
        {
            Destroy(gameObject); // Elimina el bloque si no está en el espacio de trabajo
            Debug.Log("Bloque descartado porque no está en el CodingArea.");
        }
        //TODO lógica para conectar el bloque a otro si es necesario
        // Block block = GetComponent<Block>();
        //if (block != null) Block.ResetConnection();
    }

  
}
