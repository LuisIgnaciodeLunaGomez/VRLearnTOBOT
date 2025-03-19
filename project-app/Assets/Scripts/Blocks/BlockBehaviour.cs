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
using System.Collections.Generic;
using System.Linq;

public class BlockBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string m_BlockType;// Almaceno la información del bloque
    private Text m_BlockText; // Referencia al texto UI dentro del prefab
    private Vector2 m_TouchOffset; // Almacena la diferencia entre el punto de toque y la posición del bloque
    private bool isDraggable = true; //Indica si el bloque se puede arrastrar
    private bool isTemplate = false; //Indica si el bloque es una plantilla de la ToolBox de scratch
    private Block m_block; //Referencia al bloque lógico
    private WorkSpace workSpace;
    //Gestionar las conexiones
    [SerializeField] public BlockConnection nextConnection { get; private set; }
    [SerializeField] public BlockConnection previousConnection { get; private set; }
    [SerializeField] private BlockConnection closestConnection;
    [SerializeField] private BlockConnection previousClosestConnection;
    private Image m_blockImage; //Imagen del bloque para resaltar la conexión más cercana
    private List<BlockConnection> inputConnections;
    private GameObject shadowObject; // Objeto para mostrar una sombra del bloque a semejanza de scratch
    private const int MAXRADIUS = 100; //Radio máximo para buscar conexiones cercanas
    public Block blockModel => m_block;

    public bool isATemplate => isTemplate;

    public string blockType => m_BlockType;
    /**
    * Descripción: Método para inicializar el bloque
    * @param: BlockDataLoader.BlockData blockData
    */

    public void Initialize(BlockDataLoader.BlockData blockData, WorkSpace workspace )
    {
        this.m_BlockType = blockData.type;
        this.m_BlockText = GetComponentInChildren<Text>();

        // Si el prefab tiene un Text, actualiza su contenido
        if (this.m_BlockText != null)
        {
            this.m_BlockText.text = blockData.type;
        }
        Debug.Log($"Initialize: BlockBehaviour: Bloque inicializado: {blockData.type} ");

        this.m_block = new Block(blockData.type, Vector2.zero, workspace);
        this.m_block.Initialize(blockData);
        this.m_block.SetBlockBehaviour(this);

        this.nextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this. inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();

        if (this.nextConnection == null || this.previousConnection == null)
        {
            Debug.LogError($"Initialize: BlockBehaviour: Conexiones no inicializadas correctamente para {m_BlockType}.");
        }
        else
        {
            Debug.Log($"Initialize: BlockBehaviour: Conexiones inicializadas: next={nextConnection.type}, previous={previousConnection.type}, SourceBlock={this.gameObject.name}");
        }

        // this.NextConnection = new BlockConnection(this, EConnection.NextStatement);
        //   this.PreviousConnection = new BlockConnection(this, EConnection.PrevStatement);

    }

    public void SetBlock(Block block)
    {
        if (block == null)
        {
            Debug.LogError("SetBlock: BlockBehaviour: El bloque proporcionado es nulo.");
            return;
        }
        this.m_block = block;
        this.m_block.SetBlockBehaviour(this);
        this.nextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this.inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();
        Debug.Log($"SetBlock: BlockBehaviour:  Bloque {m_BlockType} configurado con conexiones: next={nextConnection?.type}, previous={previousConnection?.type}");
    }

    public Block GetBlock()
    {
        return this.m_block;
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
        Debug.Log($"OnBeginDrag: BlockBehaviour: Bloque {gameObject.name} comenzó a ser arrastrado");

        if (isTemplate)
        {
            
            GameObject clonedBlock = OnPickBlockView(eventData);
            if (clonedBlock != null)
            {
                // clonedBlock.GetComponent<BlockBehaviour>().OnBeginDrag(eventData);
                BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();

                clonedBehaviour.workSpace = this.workSpace;
                clonedBehaviour.OnBeginDrag(eventData);
                eventData.pointerDrag = clonedBlock;
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
            this.m_block.UnPlug(); // Desconecta el bloque de otros bloques

            this.SetOrphan(); //  Marca el bloque como huérfano.
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

        Debug.Log($"OnBeginDrag: BlockBehaviour: Offset del blooque calculado: {m_TouchOffset}");
    }

    /**
     * Descripción: Cuando un usuario arrastra un bloque desde la Toolbox y lo suelta en el Workspace, este método se encarga de cambiar su contexto y ubicarlo correctamente
     */

    public void SetOrphan()
    {
        if (this.m_block == null)
        {
            Debug.LogWarning($"SetOrphan: m_block es null en {gameObject.name}, no se puede resetear conexiones.");
            return;
        }

        // Desconectar el bloque de su conexión superior (si existe)
        if (this.m_block.previousConnection != null && this.m_block.previousConnection.isConnected)
        {
            this.m_block.previousConnection.Disconnect();
            Debug.Log($"Bloque {m_BlockType} desconectado de su conexión superior.");
        }

        // Actualizar la jerarquía en el modelo lógico
        if (this.m_block.parentBlock != null)
        {
            this.m_block.parentBlock.childBlocks.Remove(this.m_block);
            this.m_block.SetParent(null); // Esto lo añade a TopBlocks en WorkSpace
            Debug.Log($"Jerarquía actualizada: {m_BlockType} ahora es huérfano.");
        }
        else if (!this.m_block.workSpace.TopBlocks.Contains(this.m_block))
        {
            this.m_block.workSpace.AddTopBlocks(this.m_block);
            Debug.Log($"Bloque {m_BlockType} añadido a TopBlocks como huérfano.");
        }

        // Asegurar que la posición se sincronice con la UI
        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();
        Debug.Log($"Posición del bloque huérfano {m_BlockType} sincronizada a {transform.localPosition}");
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

        //workSpace.AddBlock(clonedBehaviour);
        clonedBehaviour.workSpace = this.workSpace;
        clonedBehaviour.SetBlock(new Block(m_BlockType, localPos, workSpace));
        clonedBehaviour.m_block.XY = localPos;
        clonedBehaviour.UpdateConnectionPosition();
        workSpace.AddBlock(clonedBehaviour); //Registro del bloque clonado

        Debug.Log($"OnPickBlockView: BlockBehaviour: Bloque clonado {clonedBehaviour.m_BlockType} registrado en WorkSpace");

        // Iniciar el arrastre en el clon
        clonedBehaviour.OnBeginDrag(eventData);
         return clonedBlock; // Salir para no mover la plantilla
    }


    /**
     * Descripción: Método llamado cuando el usuario arrastra un bloque
     * @param: PointerEventData eventData
     */
    public void OnDrag(PointerEventData eventData)
    {
    
        if (!isDraggable || isTemplate) return; //Si no es arrastrable, no hacemos nada

        if (this.m_block == null)
        {
            Debug.LogError("OnDrag: BlockBehaviour: m_block es null, no se puede mover el bloque.");
            return;
        }

        if (this.workSpace == null)
        {
            Debug.LogError("OnDrag: BlockBehaviour: workSpace es null, no se puede buscar conexiones cercanas.");
            return;
        }


        Vector2 localPos; // Almacena la posición local del bloque

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        //Actualizo la posición del bloque

        transform.localPosition = localPos + m_TouchOffset;

        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();

        Vector2 dxy = (Vector2)transform.localPosition - this.m_block.XY;
        closestConnection = workSpace.FindClosest(this.previousConnection, 100, dxy);

        Debug.Log($"OnDrag: BlockBehaviour: Closest connection found: {closestConnection?.type} at {closestConnection?.position}, SourceBlock: {closestConnection?.sourceBlock?.gameObject.name}");

        if (closestConnection != this.previousClosestConnection)
        {
            if (this.previousClosestConnection != null)
            {
                this.previousClosestConnection.Highlight(false);
            }
            if (this.closestConnection != null)
            {
                this.closestConnection.Highlight(true);

                //Muestro la sombra en la posición de conexión del bloque

                this.shadowObject.SetActive(true);

                RectTransform targetRect = closestConnection.sourceBlock.GetComponent<RectTransform>();
                float blockHeight = GetComponent<RectTransform>().rect.height;
              //  this.shadowObject.transform.localPosition = closestConnection.position + new Vector2(0, -blockHeight);

                // Si conectamos con NextStatement, la sombra va arriba; con PrevStatement, va abajo
                if (closestConnection.type == EConnection.NextStatement)
                {
                    shadowObject.transform.localPosition = closestConnection.position + new Vector2(0, blockHeight);
                }
                else
                {
                    shadowObject.transform.localPosition = closestConnection.position + new Vector2(0, -blockHeight);
                }

                Debug.Log($"OnDrag: BlockBehaviour: Sombra activada en {shadowObject.transform.localPosition} cerca de {targetRect.anchoredPosition}");
            }

            else
            {
                this.shadowObject.SetActive(false);
                if (!workSpace.HasOtherBlocks(this))
                {
                    Debug.Log("No hay otros bloques en el área de programación con los que conectarse.");
                }
            }
            this.previousClosestConnection = closestConnection;
        }
    }

    /**
     * Descripción: Método llamado cuando el usuario suelta un bloque
     * @param: PointerEventData eventData
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this.isDraggable || this.isTemplate) return; //Si no es arrastrable, no hacemos nada

        Debug.Log($"Bloque {gameObject.name} terminó de ser arrastrado");

        // Oculto la sombra al soltar
        shadowObject.SetActive(false);

        // Restaurar la interactividad del bloque
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // Verificar si el bloque está en el área correcta
        Transform workspace = GameObject.Find("CodingArea").transform;

        bool insideWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
        (RectTransform)workspace, eventData.position, eventData.pressEventCamera
        );
        Debug.Log($"Screen Position: {eventData.position}, inside CodingArea: {insideWorkspace}");


        // if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)workspace, eventData.position, eventData.pressEventCamera))

        if (insideWorkspace)
        {
            transform.SetParent(workspace, true);
            Debug.Log("Bloque colocado en el área de programación.");

            // Me asesguro de que las posiciones estén actualizadas antes de buscar
            this.m_block.XY = transform.localPosition;
            this.UpdateConnectionPosition();

            Vector2 dxy = (Vector2)transform.localPosition - this.m_block.XY;
            this.closestConnection = this.workSpace.FindClosest(this.previousConnection, MAXRADIUS, dxy);

            //Debug.Log($"OnEndDrag: Closest connection al soltar: {closestConnection?.type} at {closestConnection?.position}");

            Debug.Log("OnEndDrag: Intentando conectar previousConnection: " + (this.previousConnection != null ? this.previousConnection.type.ToString() : "null"));
            Debug.Log("OnEndDrag: Con closestConnection: " + (this.closestConnection != null ? this.closestConnection.type.ToString() : "null"));

            if (this.closestConnection != null && this.previousConnection.CanConnect(this.closestConnection))
            {
                Debug.Log($"Conectando {this.previousConnection.type} con {closestConnection.type}");
                this.previousConnection.Connect(this.closestConnection);
                transform.localPosition = closestConnection.position - new Vector2(0, GetComponent<RectTransform>().rect.height);
                this.closestConnection.Highlight(false);
                this.previousClosestConnection = null;
                this.closestConnection = null;
            }
            else
            {
                // Intentar con nextConnection si previousConnection no conecta
                this.closestConnection = this.workSpace.FindClosest(this.nextConnection, MAXRADIUS, dxy);
                if (this.closestConnection != null && this.nextConnection.CanConnect(this.closestConnection))
                {
                    Debug.Log($"Conectando {this.nextConnection.type} con {closestConnection.type}");
                    this.nextConnection.Connect(this.closestConnection);
                    transform.localPosition = closestConnection.position + new Vector2(0, GetComponent<RectTransform>().rect.height);
                    this.closestConnection.Highlight(false);
                    this.previousClosestConnection = null;
                    this.closestConnection = null;
                }
                else
                {
                    Debug.LogWarning("No se encontró una conexión válida al soltar.");
                }
            }
        }
      
        else
        {
            this.workSpace.RemoveBlock(this);
            Destroy(gameObject);
            Debug.Log("Bloque descartado porque no está en el CodingArea.");
        }
    }
  
    //Para la gestión de conexiones

    public void UpdateConnectionPosition()
    {
        RectTransform rect = GetComponent<RectTransform>();
        float blockHeight = rect.rect.height;

        // La conexión "previous" está en la parte superior del bloque
        this.previousConnection.position = transform.localPosition + new Vector3(0, blockHeight, 0);
        // La conexión "next" está en la parte inferior del bloque
        this.nextConnection.position = transform.localPosition;

        //this.nextConnection.position = transform.localPosition;
       // this.previousConnection.position = transform.localPosition + new Vector3(0, GetComponent<RectTransform>().rect.height,0);

    }

    private void HandleConnectionState(UpdateState state)
    {

       /* this.m_blockImage.color = state switch
        {
            UpdateState.Highlight => Color.green,
            UpdateState.UnHighlight => Color.white,
            _ => this.m_blockImage.color // Mantiene el color actual si el estado no cambia el color
        };*/

        Debug.Log(state switch
        {
            UpdateState.Connected => "Conexión establecida.",
            UpdateState.Disconnected => "Conexión rota.",
            _ => ""
        });
    }

    void Start()
    {
        workSpace = FindFirstObjectByType<WorkSpace>();

        if (workSpace == null)
        {
            Debug.LogError("Start: BlockBehaviour: No se ha encontrado el objeto WorkSpace.");
        }
        this.m_blockImage = GetComponent<Image>();
        if (this.m_blockImage == null)
        {
            this.m_blockImage = gameObject.AddComponent<Image>();
            this.m_blockImage.color = Color.white;
        }

        //Creo la sombra de los bloques para resaltar la conexión más cercana
        this.CreateShadow();

        Debug.Log("Start: BlockBehaviour: Creada la sombra del bloque");

        if (!isTemplate)
        {
            workSpace.AddBlock(this);
            Debug.Log($"Start: BlockBehavour: Bloque {m_BlockType} registrado en WorkSpace con isTemplate: {isTemplate}");
        }

        this.nextConnection.onStateChanged += this.HandleConnectionState;
        this.previousConnection.onStateChanged += this.HandleConnectionState;
        foreach (var inputConnection in this.inputConnections)
        {
            inputConnection.onStateChanged += this.HandleConnectionState;
        }
    }

    private void CreateShadow()
    {
        if (this.shadowObject != null)
        {
            Destroy(this.shadowObject); // Evito duplicados
        }
        this.shadowObject = new GameObject("Shadow");
        this.shadowObject.transform.SetParent(transform.parent, false);
        Image shadowImage = this.shadowObject.AddComponent<Image>();
        shadowImage.sprite = this.m_blockImage.sprite;
        if (shadowImage.sprite == null)
        {
            Debug.LogError($"No se encontró sprite para la sombra en {gameObject.name}. Usando un color sólido.");
            shadowImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris translúcido sin sprite
        }
        else
        {
            shadowImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris translúcido con sprite
        }
        //shadowImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris translúcido
        shadowImage.type = this.m_blockImage.type;
        RectTransform shadowRect = this.shadowObject.GetComponent<RectTransform>();
        shadowRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        shadowRect.localScale = transform.localScale;
        this.shadowObject.SetActive(false); // Ocultar la sombra inicialmente

        Debug.Log($"CreateShadow: BlockBehaviour: Sombra creada para {gameObject.name} con tamaño {shadowRect.sizeDelta}");
    }


}
