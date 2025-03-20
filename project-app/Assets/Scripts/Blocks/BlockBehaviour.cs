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
 * Versión: 1.0.1
 * 
 * Descripción: Clase que gestiona el comportamiento de los bloques en la interfaz de usuario
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

    private BlockBehaviour topShadowCollision;    // Bloque colisionando con la sombra superior
    private BlockBehaviour bottomShadowCollision; // Bloque colisionando con la sombra inferior

    //Gestionar las conexiones
    [SerializeField] public BlockConnection NextConnection { get; private set; }
    [SerializeField] public BlockConnection previousConnection { get; private set; }
    [SerializeField] private BlockConnection closestConnection;
    [SerializeField] private BlockConnection previousClosestConnection;
    private Image m_blockImage; //Imagen del bloque para resaltar la conexión más cercana
    private List<BlockConnection> inputConnections;
    private GameObject shadowObject; // Objeto para mostrar una sombra del bloque a semejanza de scratch
    private const int MAXRADIUS = 100; //Radio máximo para buscar conexiones cercanas

    private GameObject shadowTop;
    private GameObject shadowBottom;
    public Block blockModel => m_block;

    public bool isATemplate => isTemplate;

    public string blockType => m_BlockType;
    /**
    * Descripción: Método para inicializar el bloque
    * @param: BlockDataLoader.BlockData blockData
    */

    public void Initialize(BlockDataLoader.BlockData blockData, WorkSpace workspace)
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

        this.NextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this.inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();

        if (this.NextConnection == null || this.previousConnection == null)
        {
            Debug.LogError($"Initialize: BlockBehaviour: Conexiones no inicializadas correctamente para {m_BlockType}.");
        }
        else
        {
            Debug.Log($"Initialize: BlockBehaviour: Conexiones inicializadas: next={NextConnection.type}, previous={previousConnection.type}, SourceBlock={this.gameObject.name}");
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
        this.NextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this.inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();
        Debug.Log($"SetBlock: BlockBehaviour:  Bloque {m_BlockType} configurado con conexiones: next={NextConnection?.type}, previous={previousConnection?.type}");
    }

    public Block GetBlock()
    {
        return this.m_block;
    }

    /**
     * Descripción: Método para establecer si el bloque es una plantilla
     * @param: bool is Template
     */
    public void SetAsTemplate(bool isTemplate)
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

            GameObject clonedBlock = this.OnPickBlockView(eventData);
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

        if (!isDraggable) return; //Si no es arrastrable, no hacemos nada 

        // Cambiar el padre del bloque al panel derecho
        transform.SetParent(GameObject.Find("CodingArea").transform, true);

        // Si el bloque está conectado, lo desenchufamos antes de moverlo.
        //Block block = GetComponent<Block>();
        if (this.m_block != null)
        {
            this.m_block.UnPlug(); // Desconecta el bloque de otros bloques
            Debug.Log($"OnBeginDrag: BlockBehaviour: Bloque {gameObject.name} desconectado correctamente.");

        }
        else
        {
            this.SetOrphan(); //  Marca el bloque como huérfano si no estaba conectado a otro
            Debug.Log($"OnBeginDrag: BlockBehaviour: Bloque {gameObject.name} marcado como huérfano.");

        }

        // SetOrphan(); //  Marca el bloque como huérfano.

        // Calculamos la diferencia entre el punto de toque y la posición del bloque
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

       this.m_TouchOffset = (Vector2)transform.localPosition - localPos;

        // Crear las sombras si no existen
        if (this.shadowTop == null || this.shadowBottom == null)
        {
            this.CreateShadows(); // Método para crear las sombras
        }

        // Activar las sombras al comenzar el arrastre
        if (this.shadowTop != null) this.shadowTop.SetActive(true);
        if (this.shadowBottom != null) this.shadowBottom.SetActive(true);

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
            //this.m_block.previousConnection.Disconnect();
            // Debug.Log($"Bloque {m_BlockType} desconectado de su conexión superior.");

            Debug.LogWarning($"SetOrphan: El bloque {this.m_BlockType} sigue conectado. No se marcará como huérfano.");
            this.m_block.previousConnection.Disconnect();
            return;
        }

        // Actualizar la jerarquía en el modelo lógico
        if (this.m_block.parentBlock != null)
        {
            this.m_block.parentBlock.childBlocks.Remove(this.m_block);
            this.m_block.SetParent(null); // Esto lo añade a TopBlocks en WorkSpace
            Debug.Log($"Jerarquía actualizada: {this.m_BlockType} ahora es huérfano.");
        }
        else if (!this.m_block.workSpace.TopBlocks.Contains(this.m_block))
        {
            this.m_block.workSpace.AddTopBlocks(this.m_block);
            Debug.Log($"Bloque {this.m_BlockType} añadido a TopBlocks como huérfano.");
        }

        // Asegurar que la posición se sincronice con la UI
        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();
        Debug.Log($"Posición del bloque huérfano {this.m_BlockType} sincronizada a {transform.localPosition}");
    }

    /**
     * Descripción Método utilizado para clonar un bloque si este es una plantilla
    */
    public GameObject OnPickBlockView(PointerEventData eventData)
    {
        GameObject clonedBlock = Instantiate(gameObject, transform.parent);
        clonedBlock.tag = "Block";
        BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();
        clonedBehaviour.SetAsTemplate(false); //Desactiva la plantilla
        clonedBehaviour.SetDraggable(true); //Activa el arrastre

        // Asegurar que el bloque tenga un `BoxCollider2D`
        if (clonedBlock.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = clonedBlock.AddComponent<BoxCollider2D>();
            collider.size = clonedBlock.GetComponent<RectTransform>().sizeDelta;
        }

        // Asegurar que el bloque tenga un `Rigidbody2D`
        if (clonedBlock.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = clonedBlock.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // Para que no sea afectado por la gravedad
        }

        //Me  aseguro que el bloque tenga un CanvasGroup en vez de Canvas
        CanvasGroup canvasGroup = clonedBlock.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = clonedBlock.AddComponent<CanvasGroup>();
        }

        // Habilitar raycasts para que el bloque pueda seguir interactuando en la UI
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f; // Asegura que el bloque sea visible

        // Mover el clon al CodingArea
        Transform codingArea = GameObject.Find("CodingArea").transform;
        clonedBlock.transform.SetParent(codingArea, false);
        clonedBlock.transform.SetAsLastSibling();
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

        if (this.m_block == null || this.workSpace == null)
        {
            //Debug.LogError("OnDrag: No se puede mover el bloque porque m_block o workSpace es null.");
            return;
        }

        Vector2 localPos; // Almacena la posición local del bloque mientras se arrastra

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        //Actualizo la posición del bloque

        transform.localPosition = localPos + m_TouchOffset;
        this.UpdateShadowPosition(); // Actualizo la sombra junto con el bloque
        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();

        //this.shadowTop.SetActive(true);
       // this.shadowBottom.SetActive(true);

        // Los bloques en el área de codificación revisan si pueden recibir conexión
        foreach (BlockBehaviour block in workSpace.blocksInWorkspace)
        {
            if (block == this) continue; // Evita comparar consigo mismo

            if (block.CanReceiveConnection(this))
            {
                block.ShowShadow(this);
            }
            else
            {
                block.HideShadow();
            }
        }

    }

    public bool CanReceiveConnection(BlockBehaviour movingBlock)
    {
        float blockHeight = GetComponent<RectTransform>().rect.height;
        float blockWidth = GetComponent<RectTransform>().rect.width;

        float movingBlockX = movingBlock.transform.localPosition.x;
        float movingBlockY = movingBlock.transform.localPosition.y;

        float myX = transform.localPosition.x;
        float myY = transform.localPosition.y;

        // Verificar si está dentro del área de conexión en X (alineación horizontal)
        bool isAlignedHorizontally = Mathf.Abs(movingBlockX - myX) <= blockWidth * 0.5f;

        // Verificar si el bloque en movimiento está por encima o por debajo dentro del rango de conexión
        bool isAbove = (movingBlockY < myY) && (myY - movingBlockY <= blockHeight);
        bool isBelow = (movingBlockY > myY) && (movingBlockY - myY <= blockHeight);

        return isAlignedHorizontally && (isAbove || isBelow);
    }

    /**
    * Descripción: Método que muestra la sombra del bloque
    * @param: BlockConnection targetConnection
    */
    public void ShowShadow(BlockBehaviour movingBlock)
    {
        if (shadowObject == null)
        {
            this.CreateShadows();
        }

        // Solo mostrar la sombra si el bloque en movimiento está en CodingArea o RightPanel
        if (!this.IsInsideAllowedArea(movingBlock.transform))
        {
            this.HideShadow();
            return;
        }

        shadowObject.SetActive(true);
        float blockHeight = GetComponent<RectTransform>().rect.height;

        if (movingBlock.transform.localPosition.y > transform.localPosition.y)
        {
            // Si el bloque en movimiento está por debajo, la sombra va arriba
            shadowObject.transform.localPosition = transform.localPosition + new Vector3(0, blockHeight, 0);
        }
        else
        {
            // Si el bloque en movimiento está por encima, la sombra va abajo
            shadowObject.transform.localPosition = transform.localPosition - new Vector3(0, blockHeight, 0);
        }
    }
    private bool IsInsideAllowedArea(Transform blockTransform)
    {
        GameObject codingArea = GameObject.Find("CodingArea");
        GameObject rightPanel = GameObject.Find("RightPanel");

        return IsInsidePanel(blockTransform, codingArea) || IsInsidePanel(blockTransform, rightPanel);
    }

    private bool IsInsidePanel(Transform blockTransform, GameObject panel)
    {
        if (panel == null) return false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, blockTransform.position, null);
    }

    public void HideShadow()
    {
        if (shadowObject != null)
        {
            shadowObject.SetActive(false);
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

        // Ocultar las sombras al finalizar el arrastre
        if (this.shadowTop != null)
        {
             // Destroy(shadowTop);

            shadowTop.SetActive(false);
        }
        if (this.shadowBottom != null)
        {

           // Destroy(shadowBottom);
            shadowBottom.SetActive(false);

        }

        // Oculto la sombra al soltar
        //this.shadowObject.SetActive(false);

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

            //Debug.Log("OnEndDrag: Intentando conectar previousConnection: " + (this.previousConnection != null ? this.previousConnection.type.ToString() : "null"));
            // Debug.Log("OnEndDrag: Con closestConnection: " + (this.closestConnection != null ? this.closestConnection.type.ToString() : "null"));

            /*   if (this.closestConnection != null && this.previousConnection.CanConnect(this.closestConnection))
               {
                  // Debug.Log($"Conectando {this.previousConnection.type} con {closestConnection.type}");
                   this.previousConnection.Connect(this.closestConnection);
                   transform.localPosition = closestConnection.position - new Vector2(0, GetComponent<RectTransform>().rect.height);
                   this.closestConnection.Highlight(false);
                   this.previousClosestConnection = null;
                   this.closestConnection = null;
               }
               else
               {
                   // Intentar con nextConnection si previousConnection no conecta
                   this.closestConnection = this.workSpace.FindClosest(this.NextConnection, MAXRADIUS, dxy);
                   if (this.closestConnection != null && this.NextConnection.CanConnect(this.closestConnection))
                   {
                      // Debug.Log($"Conectando {this.NextConnection.type} con {closestConnection.type}");
                       this.NextConnection.Connect(this.closestConnection);
                       transform.localPosition = closestConnection.position + new Vector2(0, GetComponent<RectTransform>().rect.height);
                       this.closestConnection.Highlight(false);
                       this.previousClosestConnection = null;
                       this.closestConnection = null;
                   }
                   else
                   {
                       //Debug.LogWarning("No se encontró una conexión válida al soltar.");
                       foreach (var inputConnection in this.inputConnections)
                       {
                           this.closestConnection = this.workSpace.FindClosest(inputConnection, MAXRADIUS, dxy);
                           if (this.closestConnection != null && inputConnection.CanConnect(this.closestConnection))
                           {
                               inputConnection.Connect(this.closestConnection);
                               break;
                           }
                       }
                   }

                   // Si después de todo esto no se encontró conexión válida
                   if (this.closestConnection == null)
                   {
                      // Debug.LogWarning($"OnEndDrag: No se encontró una conexión válida para {gameObject.name}. Se mantendrá como bloque independiente.");
                       this.SetOrphan(); // Se deja en `WorkSpace`, pero sin conexión
                   }
               }
           }*/
            // Intentar conectar usando la colisión de la sombra superior
            if (topShadowCollision != null && CanConnectTo(topShadowCollision, ConnectionZone.Top))
            {
                ConnectTo(topShadowCollision, ConnectionZone.Top);
            }
            // Intentar conectar usando la colisión de la sombra inferior
            else if (bottomShadowCollision != null && CanConnectTo(bottomShadowCollision, ConnectionZone.Bottom))
            {
                ConnectTo(bottomShadowCollision, ConnectionZone.Bottom);
            }
            else
            {
                Debug.LogWarning($"OnEndDrag: No se encontró una conexión válida para {gameObject.name}. Se mantendrá como bloque independiente.");
                this.SetOrphan(); // Sin colisión, queda independiente
            }

            // Desactivar las sombras
            if (shadowTop != null) shadowTop.SetActive(false);
            if (shadowBottom != null) shadowBottom.SetActive(false);
        }
        else
        {
            // Antes de eliminar el bloque, destruir las sombras
            if (shadowTop != null)
            {
                Destroy(shadowTop);
                shadowTop = null;
            }
            if (shadowBottom != null)
            {
                Destroy(shadowBottom);
                shadowBottom = null;
            }
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
        this.NextConnection.position = transform.localPosition;

        //this.nextConnection.position = transform.localPosition;
        // this.previousConnection.position = transform.localPosition + new Vector3(0, GetComponent<RectTransform>().rect.height,0);
        //Debug.Log($"UpdateConnectionPosition: BlockBehaviour: PreviousConnection position updated to {previousConnection.position}, NextConnection position updated to {NextConnection.position}");
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
        gameObject.tag = "Block"; //Se añade el tag "Block" al bloque
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

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            RectTransform rect = GetComponent<RectTransform>();
            collider.size = rect.sizeDelta; // Ajustar al tamaño del bloque
        }
        collider.isTrigger = true; // Configurar como trigger
        //Creo la sombra de los bloques para resaltar la conexión más cercana
        this.CreateShadows();

        Debug.Log("Start: BlockBehaviour: Creada la sombra del bloque");

        if (!isTemplate)
        {
            workSpace.AddBlock(this);
            Debug.Log($"Start: BlockBehavour: Bloque {m_BlockType} registrado en WorkSpace con isTemplate: {isTemplate}");
        }

        this.NextConnection.onStateChanged += this.HandleConnectionState;
        this.previousConnection.onStateChanged += this.HandleConnectionState;
        foreach (var inputConnection in this.inputConnections)
        {
            inputConnection.onStateChanged += this.HandleConnectionState;
        }

        // Asegurarse de que las sombras estén ocultas al inicio
        if (shadowTop != null) shadowTop.SetActive(false);
        if (shadowBottom != null) shadowBottom.SetActive(false);
    }


    /**
     * Descripción: Método que destaca un bloque al que se puede conectar
     * @param: bool dato
     */
    public void Highlight(bool dato)
    {
        if (dato) this.m_blockImage.color = Color.green;

    }

    private void CreateShadows()
    {
        // Destruir sombras existentes para evitar duplicados
        if (shadowTop != null) Destroy(shadowTop);
        if (shadowBottom != null) Destroy(shadowBottom);

        if (isTemplate)
        {
            Debug.Log("CreateShadows: No se crean sombras para bloques en la toolbox");
            return;
        }

        // Obtener la referencia al RightPanel
        GameObject rightPanel = GameObject.Find("CodingArea");
        if (rightPanel == null || this.transform.parent != rightPanel.transform)
        {
            Debug.LogError("$\"CreateShadows: El bloque {gameObject.name} NO está en CodingArea. No se crea la sombra");
            return;
        }
        // Obtener el tamaño del bloque
        RectTransform blockRect = GetComponent<RectTransform>();
        float blockHeight = blockRect.rect.height;

        // Verificar si el bloque está en el RightPanel antes de crear la sombra
        if (transform.parent == rightPanel.transform)
        {
            Debug.LogWarning("CreateShadows: La sombra solo se muestra en el CodingArea.");
            // Crear sombra superior
            this.shadowTop = CreateShadowContainer("ShadowTopContainer", new Vector2(0, blockHeight / 2));

            // Crear sombra inferior
            this.shadowBottom = CreateShadowContainer("ShadowBottomContainer", new Vector2(0, -blockHeight / 2));
            return;
        }

        // Configurar eventos para la sombra superior
        ShadowCollision topShadowScript = shadowTop.GetComponent<ShadowCollision>();
        topShadowScript.OnBlockEntered += (block) => topShadowCollision = block;
        topShadowScript.OnBlockExited += (block) => { if (topShadowCollision == block) topShadowCollision = null; };

        // Configurar eventos para la sombra inferior
        ShadowCollision bottomShadowScript = shadowBottom.GetComponent<ShadowCollision>();
        bottomShadowScript.OnBlockEntered += (block) => bottomShadowCollision = block;
        bottomShadowScript.OnBlockExited += (block) => { if (bottomShadowCollision == block) bottomShadowCollision = null; };

        // Asegurarse de que estén ocultas al crearse
        shadowTop.SetActive(false);
        shadowBottom.SetActive(false);

    }

    // Método auxiliar para crear contenedores de sombra
    private GameObject CreateShadowContainer(string name, Vector2 offset)
    {
        // Obtener el RectTransform del bloque
        RectTransform blockRect = GetComponent<RectTransform>();

        //Creo el contenedor de la sombra
        GameObject shadowContainer = new GameObject(name);
        shadowContainer.transform.SetParent(transform.parent, false); // Hermano del bloque
        shadowContainer.transform.SetSiblingIndex(transform.GetSiblingIndex()); // Detrás del bloque

        //Configuro el RectTransform de la sombra
        RectTransform shadowRect = shadowContainer.AddComponent<RectTransform>();
        shadowRect.anchorMin = blockRect.anchorMin; // Copiar anchors mínimos
        shadowRect.anchorMax = blockRect.anchorMax; // Copiar anchors máximos
        shadowRect.pivot = blockRect.pivot; // Copiar pivot
        shadowRect.sizeDelta = blockRect.sizeDelta; // Mismo tamaño que el bloque
        //shadowRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        //shadowRect.localScale = Vector3.one; // Mismo tamaño que el bloque (ajusta si deseas escalado)


        float scaleFactor = 0.32f;
        shadowRect.localScale = new Vector3(scaleFactor, scaleFactor, 1); // Escala reducida
        Image shadowImage = shadowContainer.AddComponent<Image>();
        shadowImage.sprite = this.m_blockImage.sprite;
        shadowImage.color = new Color(0, 0, 0, 0.3f); // Sombra oscura translúcida

        // Añado BoxCollider2D para detectar la colisión
        BoxCollider2D shadowCollider = shadowContainer.AddComponent<BoxCollider2D>();
        shadowCollider.size = shadowRect.sizeDelta; // Tamaño igual a la sombra
        shadowCollider.isTrigger = true; // No bloquea el movimiento del bloque
        // Posición inicial
        shadowRect.anchoredPosition = (Vector2)transform.localPosition + offset;

        Canvas shadowCanvas = shadowContainer.AddComponent<Canvas>();
        shadowCanvas.overrideSorting = false;

        // Añadir script para gestionar colisiones
        ShadowCollision shadowScript = shadowContainer.AddComponent<ShadowCollision>();
        shadowScript.SetShadowImage(shadowImage);

        return shadowContainer;
    }
    // Método para actualizar la posición de la sombra cuando el bloque se mueve
    private void UpdateShadowPosition()
    {
        if (shadowTop == null || shadowBottom == null) return;

        // Solo actualizar si las sombras están activas (durante el arrastre)
        if (!shadowTop.activeSelf || !shadowBottom.activeSelf) return;

        GameObject codingArea = GameObject.Find("CodingArea");
        if (transform.parent != codingArea.transform)
        {
            this.shadowTop.SetActive(false);
            this.shadowBottom.SetActive(false);
            return;
        }

        RectTransform blockRect = GetComponent<RectTransform>();
        float blockHeight = blockRect.rect.height;
        float blockWidth = blockRect.rect.width;
        float blockX = blockRect.anchoredPosition.x;
        float blockY = blockRect.anchoredPosition.y;
        float scaleFactor = 0.32f; // Ajusta este valor según tu configuración
        float shadowHeight = blockHeight * scaleFactor;
        float offset = 20f; // Espacio en píxeles entre la sombra y el bloque
        // Posicionar sombra superior (arriba del bloque)
        shadowTop.GetComponent<RectTransform>().anchoredPosition =
             new Vector2(blockX, blockY + shadowHeight - offset);

        // Posicionar sombra inferior (debajo del bloque)
        shadowBottom.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(blockX, blockY - shadowHeight + offset);

        //Debug.Log($"Bloque: X={blockX}, Y={blockY}, Altura={blockHeight}");
       // Debug.Log($"Sombra Superior: Y={blockY + blockHeight * scaleFactor - offset}");
       // Debug.Log($"Sombra Inferior: Y={blockY - blockHeight * scaleFactor + offset}");

        shadowTop.SetActive(true);
        shadowBottom.SetActive(true);
    }

    // Métodos auxiliares para la conexión
    private bool CanConnectTo(BlockBehaviour otherBlock, ConnectionZone zone)
    {
        if (zone == ConnectionZone.Top)
        {
            return this.previousConnection.CanConnect(otherBlock.NextConnection);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            return this.NextConnection.CanConnect(otherBlock.previousConnection);
        }
        return false;
    }

    private void ConnectTo(BlockBehaviour otherBlock, ConnectionZone zone)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (zone == ConnectionZone.Top)
        {
            this.previousConnection.Connect(otherBlock.NextConnection);
            transform.localPosition = otherBlock.NextConnection.position - new Vector2(0, rect.rect.height);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            this.NextConnection.Connect(otherBlock.previousConnection);
            transform.localPosition = otherBlock.previousConnection.position + new Vector2(0, rect.rect.height);
        }
        Debug.Log($"Conectado {m_BlockType} con {otherBlock.m_BlockType} en {zone}");
    }

}
