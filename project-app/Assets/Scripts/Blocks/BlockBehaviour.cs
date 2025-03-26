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
 * Versión: 1.0.2
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

    #region propiedades
    private string m_BlockType;// Almaceno la información del bloque
    private Text m_BlockText; // Referencia al texto UI dentro del prefab
    private Vector2 m_TouchOffset; // Almacena la diferencia entre el punto de toque y la posición del bloque
    private bool m_isDraggable = true; //Indica si el bloque se puede arrastrar
    private bool m_isTemplate = false; //Indica si el bloque es una plantilla de la ToolBox de scratch
    private Block m_block; //Referencia al bloque lógico
    private WorkSpace workSpace;
    public ShadowZone zone;
    private BlockBehaviour m_topShadowCollision;    // Bloque colisionando con la sombra superior
    private BlockBehaviour m_bottomShadowCollision; // Bloque colisionando con la sombra inferior

    private BlockBehaviour m_collidingWithTopShadowOf; // Bloque estático cuya sombra superior está colisionando
    private BlockBehaviour m_collidingWithBottomShadowOf; // Bloque estático cuya sombra inferior está colisionando
    //Gestionar las conexiones
    [SerializeField] public BlockConnection nextConnection { get; private set; }
    [SerializeField] public BlockConnection previousConnection { get; private set; }
    [SerializeField] private BlockConnection closestConnection;
    [SerializeField] private BlockConnection previousClosestConnection;
    private Image m_blockImage; //Imagen del bloque para resaltar la conexión más cercana
    private List<BlockConnection> m_inputConnections;
    private GameObject m_shadowObject; // Objeto para mostrar una sombra del bloque a semejanza de scratch
    private const int MAXRADIUS = 100; //Radio máximo para buscar conexiones cercanas
    private ConnectionZone m_currentConnectionZone = ConnectionZone.None;
    private GameObject  m_shadowTop; //Sombra superior
    private GameObject m_shadowBottom; //Sombra inferior
    private bool m_isTopShadowLocked = false; // Esta la sombra Top bloqueada
    private bool m_isBottomShadowLocked = false; //Esta la sombra Bottom bloqueada
    public Block blockModel => m_block;

    public bool isATemplate => m_isTemplate;

    public bool isDraggable => m_isDraggable;

    public string blockType => m_BlockType;
    
    private bool m_IsShadow = false;

    public bool IsShadow => m_IsShadow;
    public GameObject shadowTop
    {
        get => m_shadowTop;
        set => m_shadowTop = value;
    }

    public GameObject shadowBottom
    {
        get => m_shadowBottom;
        set => m_shadowBottom = value;
    }
    public bool isTopShadoLocked 
        {
            get => m_isTopShadowLocked;
            set => m_isTopShadowLocked = value;
        }

    public bool isBottomShadowLocked
    {
        get => m_isBottomShadowLocked;
        set => m_isBottomShadowLocked = value;
    }

    public BlockBehaviour collidingWithTopShadowOF
    {
        get => m_collidingWithTopShadowOf;
        set => m_collidingWithTopShadowOf = value;
    }

    public BlockBehaviour collidingWithBottomShadowOf
    {
        get => m_collidingWithBottomShadowOf;
        set => m_collidingWithBottomShadowOf = value;
    }

    #endregion
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
       // Debug.Log($"Initialize: BlockBehaviour: Bloque inicializado: {blockData.type} ");

        this.m_block = new Block(blockData.type, Vector2.zero, workspace);
        this.m_block.Initialize(blockData);
        this.m_block.SetBlockBehaviour(this);

        this.nextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this.m_inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();

        if (this.nextConnection == null || this.previousConnection == null)
        {
           // Debug.LogError($"Initialize: BlockBehaviour: Conexiones no inicializadas correctamente para {m_BlockType}.");
        }
        else
        {
           // Debug.Log($"Initialize: BlockBehaviour: Conexiones inicializadas: next={nextConnection.type}, previous={previousConnection.type}, SourceBlock={this.gameObject.name}");
        }

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
        this.m_inputConnections = this.m_block.inputList
            .Where(i => i.Connection != null)
            .Select(i => i.Connection)
            .ToList();
       // Debug.Log($"SetBlock: BlockBehaviour:  Bloque {m_BlockType} configurado con conexiones: next={nextConnection?.type}, previous={previousConnection?.type}");
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
        this.m_isTemplate = isTemplate;
    }

    /**
     * Descripción: Método para activar o desactivar el arrastre del bloque
     * @param: bool isDraggable
     */

    public void SetDraggable(bool isDragable)
    {
        this.m_isDraggable = isDragable;
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
        Debug.Log($"OnBeginDrag: BlockBehaviour: Bloque {blockModel.ID} comenzó a ser arrastrado");

        // Clonar si es una plantilla y delegar el arrastre
        GameObject clonedBlock = CloneBlockIfTemplate(eventData);
        if (clonedBlock != null) return;

        if (!CanDrag()) return;

        ClearShadowCollisions();
        
        DisconnectIfConnected();

        CalculateTouchOffset(eventData);

        ConfigureShadowsForDragging();

    }

    /**
    * Descripción: Método para clonar un bloque si este es una plantilla
    * @param: PointerEventData eventData
    */
    private GameObject CloneBlockIfTemplate(PointerEventData eventData)
    {
        if (m_isTemplate)
        {
            GameObject clonedBlock = OnPickBlockView(eventData);
            if (clonedBlock != null)
            {
                BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();
                clonedBehaviour.workSpace = this.workSpace;
                clonedBehaviour.OnBeginDrag(eventData);
                eventData.pointerDrag = clonedBlock;
                return clonedBlock;
            }
        }
        return null;
    }

    /**
     * Descripción: Método para limpiar las sombras del bloque que identifican la zona de colisión
     */
    private void ClearShadowCollisions()
    {
        m_collidingWithTopShadowOf = null;
        m_collidingWithBottomShadowOf = null;
    }

    /**
     * Descripción: Método para desconectar el bloque si está conectado
     */
    private void DisconnectIfConnected()
    {
        if (m_block != null)
        {
            m_block.UnPlug();
            BlockView blockView = GetComponent<BlockView>();
            if (blockView != null) blockView.RemovePositionOnDrag();
        }
        else
        {
            SetOrphan();
            Debug.Log($"OnBeginDrag: Bloque {gameObject.name} marcado como huérfano.");
        }
    }

    /**
     * Descripción: Método para calcular el desplazamiento (TouchOffset) del bloque
     * @param: PointerEventData eventData
     */
    private void CalculateTouchOffset(PointerEventData eventData)
    {
        RectTransform codingAreaRect = (RectTransform)GameObject.Find("CodingArea").transform;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            codingAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );
        m_TouchOffset = (Vector2)transform.localPosition - localPos;
    }

    /**
     * Descripción: Método para marcar un bloque como huérfano
     */
    public void SetOrphan()
    {
        if (!ValidateBlock()) return;

        if (IsBlockConnected())
        {
            DisconnectBlock();
            return;
        }

        UpdateBlockHierarchy();
        SyncBlockPosition();
       
    }
    /**
     * Descripción: Método que comprueba si el bloque esta asignado
     * 
     */
    private bool ValidateBlock()
    {
        if (m_block == null)
        {
            Debug.LogWarning($"SetOrphan: m_block es null en {gameObject.name}, no se puede resetear conexiones.");
            return false;
        }
        return true;
    }

    /**
     * Descripción: método revisa si el bloque tiene una conexión superior activa.
     */
    private bool IsBlockConnected()
    {
        return m_block.previousConnection != null && m_block.previousConnection.isConnected;
    }

    /**
     * Descripcion: Método que desconecta el bloque de su conexión superior
     */
    private void DisconnectBlock()
    {
        m_block.previousConnection.Disconnect();
    }

    /**
     * Descripción: Método que actualiza la jerarquía del bloque
     */
    private void UpdateBlockHierarchy()
    {
        if (m_block.parentBlock != null)
        {
            m_block.parentBlock.childBlocks.Remove(m_block);
            m_block.SetParent(null); // Esto lo añade a TopBlocks en WorkSpace
            Debug.Log($"Jerarquía actualizada: {m_BlockType} ahora es huérfano.");
        }
        else if (!m_block.workSpace.TopBlocks.Contains(m_block))
        {
            m_block.workSpace.AddTopBlocks(m_block);
            Debug.Log($"Bloque {m_BlockType} añadido a TopBlocks como huérfano.");
        }
    }
    /**
     * Descripción: Método que actualiza la posición del bloque y sus conexiones para reflejar la posición en la UI
     */
    private void SyncBlockPosition()
    {
        m_block.XY = transform.localPosition;
        m_block.UpdateConnectionPositions();
        Debug.Log($"Posición del bloque huérfano {m_BlockType} sincronizada a {transform.localPosition}");
    }
    /**
     * Descripción Método utilizado para clonar un bloque si este es una plantilla
     * @param: PointerEventData eventData
    */
    public GameObject OnPickBlockView(PointerEventData eventData)
    {
        GameObject clonedBlock = CloneBlock();
        BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();

        RectTransform rect = clonedBlock.GetComponent<RectTransform>();
        ConfigureRectTransform(rect);

        EnsureComponents(clonedBlock);

        Vector2 localPos;
        MoveToCodingArea(clonedBlock, eventData, out localPos);

        ConfigureBlockLogic(clonedBehaviour, localPos);

        ManageShadows(clonedBehaviour);

        
        eventData.pointerDrag = clonedBlock;

        return clonedBlock;
       
    }
    /**
     * Descripción: método crea una copia del bloque original y establece sus propiedades básicas
     */
    private GameObject CloneBlock()
    {
        GameObject clonedBlock = Instantiate(gameObject, transform.parent);
        clonedBlock.tag = "Block";
        BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();
        clonedBehaviour.SetAsTemplate(false); // Desactiva la plantilla
        clonedBehaviour.SetDraggable(true);   // Activa el arrastre
        clonedBehaviour.workSpace = WorkSpace.Instance; ;
        RegisterBlock(clonedBehaviour);
        
        return clonedBlock;
    }

    /**
     * Descripción: Método para ajustar los anclajes y el pivot del RectTransform del bloque
     */
    private void ConfigureRectTransform(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0, 1); // Ancla superior izquierda
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    /**
     * Descripción: Método para asegurar que el bloque tenga un BoxCollider2D, un CanvasGroup y un Rigidbody2D
     * @param: GameObject block
     */
    private void EnsureComponents(GameObject block)
    {
        if (block.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
            collider.size = block.GetComponent<RectTransform>().sizeDelta;
        }

        if (block.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = block.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // No afectado por gravedad
        }

        CanvasGroup canvasGroup = block.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = block.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = true;  // Habilitar raycasts
        canvasGroup.interactable = true;   // Permitir interacción
        canvasGroup.alpha = 1f;            // Asegurar visibilidad
    }

    /** 
     * Descripción: Método para mover el bloque a la zona de código
     * @param: GameObject block
     * @param: PointerEventData eventData
     * @param: out Vector2 localPos
     */
    private void MoveToCodingArea(GameObject block, PointerEventData eventData, out Vector2 localPos)
    {
        Transform codingArea = GameObject.Find("CodingArea").transform;
        block.transform.SetParent(codingArea, false);
        block.transform.SetAsLastSibling();

        RectTransform codingAreaRect = (RectTransform)codingArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            codingAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );
        block.GetComponent<RectTransform>().localPosition = localPos;
    }

    /**
     * Descripción: Método que Asigna el workSpace, crea un nuevo Block y vincula el modelo lógic
     * @param: BlockDataLoader.BlockData blockData
     * @param: Vector2 position
     */
    private void ConfigureBlockLogic(BlockBehaviour behaviour, Vector2 position)
    {
        behaviour.workSpace = this.workSpace;
        Block newBlock = new Block(m_BlockType, position, workSpace);
        behaviour.SetBlock(newBlock);
        behaviour.m_block.XY = position;

        BlockView view = behaviour.GetComponent<BlockView>();
        if (view != null)
        {
            view.BindModel(behaviour.GetBlock(), behaviour.GetBlock().blockData, FindFirstObjectByType<WorkSpaceView>());
        }
    }

    /**
     * Descripción: Método que crea y actualiza las sombras del bloque
     * @param: BlockBehaviour behaviour
     */
    private void ManageShadows(BlockBehaviour behaviour)
    {
        behaviour.CreateShadows();
        behaviour.UpdateShadowPosition(); // Asegurar posición inicial correcta
    }

    /**
     * Descripción: Método que registra el bloque en el workSpace
     * @param: BlockBehaviour behaviour
     */
    private void RegisterBlock(BlockBehaviour behaviour)
    {
        workSpace.AddBlock(behaviour);
        Debug.Log($"OnPickBlockView: BlockBehaviour: Bloque clonado {behaviour.m_BlockType} registrado en WorkSpace");
    }

    /**
     * Descripción: Método llamado cuando el usuario arrastra un bloque
     * @param: PointerEventData eventData
     */
    public void OnDrag(PointerEventData eventData)
    {
        // Validar estado inicial
        if (!CanDrag() && !Istemplate()) return;

        if (this.m_block == null || this.workSpace == null)
        {
            return;
        }

        Vector2 localPos = CalculateLocalPosition(eventData);

        // Mover el bloque y actualizar su posición lógica
        MoveBlock(localPos);

        // Actualizar la posición de las sombras
        UpdateShadowPosition();

        // Configurar las sombras para el arrastre
        ConfigureShadowsForDragging();

        // Actualizar la vista del bloque
        UpdateBlockViewPosition();
 
    }

    /**
     * Descripción: Método para indicar que el bloque es arrastrable
     */
    private bool CanDrag()
    {
        return m_isDraggable;
    }

    /**
     * Descripción: Método para indicar si el bloque es una plantilla
     */
    private bool Istemplate()
    {
        return m_isTemplate;
    }

    /** 
     * Descripción: Método para calcular la posición local del bloque
     * @param : PointerEventData eventData
     */
    private Vector2 CalculateLocalPosition(PointerEventData eventData)
    {
        RectTransform parentRect = (RectTransform)transform.parent;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );
        return localPos + m_TouchOffset;
    }

    /**
     * Descripción: Método para mover un bloque a una posición determinada
     */
    private void MoveBlock(Vector2 position)
    {
        transform.localPosition = position;
        m_block.XY = position;
        m_block.UpdateConnectionPositions();
    }

    /**
     * Descripción: Método para configurar las sombras del bloque
     */
    private void ConfigureShadowsForDragging()
    {
        if (m_shadowTop != null)
        {
            m_shadowTop.SetActive(true);
            m_shadowTop.GetComponent<BoxCollider2D>().enabled = false;
            m_shadowTop.GetComponent<Image>().enabled = false;
        }
        if (m_shadowBottom != null)
        {
            m_shadowBottom.SetActive(true);
            m_shadowBottom.GetComponent<BoxCollider2D>().enabled = false;
            m_shadowBottom.GetComponent<Image>().enabled = false;
        }
    }

    /**
     * Descripción: Método para actualizar la posición de las sombras del bloque
     */
    private void UpdateBlockViewPosition()
    {
        BlockView blockView = GetComponent<BlockView>();
        if (blockView != null && blockView.Block != null)
        {
            blockView.UpdatePosition(transform.localPosition);
        }
    }

    /**
     * Descripción: Método para actualizar la posición de las sombras del bloque
     */
    private void CheckConnectionProximity()
    {
        SpriteConnectionDetector detector = GetComponent<SpriteConnectionDetector>();
        if (detector == null) return;

        Image blockImage = GetComponent<Image>();
        if (blockImage == null) return;

        float proximityThreshold = 50f; // Umbral de cercanía en unidades
        bool isNearConnection = false;

        foreach (BlockBehaviour otherBlock in workSpace.blocksInWorkspace)
        {
            if (otherBlock == this) continue;

            SpriteConnectionDetector otherDetector = otherBlock.GetComponent<SpriteConnectionDetector>();
            if (otherDetector == null) continue;

            Vector3 myTop = detector.GetWorldTopConnection();
            Vector3 myBottom = detector.GetWorldBottomConnection();
            Vector3 otherTop = otherDetector.GetWorldTopConnection();
            Vector3 otherBottom = otherDetector.GetWorldBottomConnection();

            if (Vector3.Distance(myBottom, otherTop) < proximityThreshold ||
                Vector3.Distance(myTop, otherBottom) < proximityThreshold)
            {
                isNearConnection = true;
                break;
            }
        }

        blockImage.color = isNearConnection ? Color.yellow : Color.white;
    }

    /**
     * Descripción: Método para conectar un bloque a otro 
     */
    private void ConnectToStaticBlock(BlockBehaviour staticBlock, ConnectionZone zone)
    {
        SpriteConnectionDetector myDetector = GetComponent<SpriteConnectionDetector>();
        SpriteConnectionDetector staticDetector = staticBlock.GetComponent<SpriteConnectionDetector>();
        if (myDetector == null || staticDetector == null) return;

        Vector3 myBottom = myDetector.GetWorldBottomConnection();
        Vector3 myTop = myDetector.GetWorldTopConnection();
        Vector3 staticTop = staticDetector.GetWorldTopConnection();
        Vector3 staticBottom = staticDetector.GetWorldBottomConnection();

        if (zone == ConnectionZone.Top)
        {
            // Sombra arriba: dinámico debajo del estático
            this.nextConnection.Connect(staticBlock.previousConnection);
            transform.position = staticBottom - (myTop - transform.position); // Alinear mi Top con su Bottom
        }
        else if (zone == ConnectionZone.Bottom)
        {
            // Sombra abajo: dinámico encima del estático
            this.previousConnection.Connect(staticBlock.nextConnection);
            transform.position = staticTop - (myBottom - transform.position); // Alinear mi Bottom con su Top
        }

        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();

        BlockView blockView = GetComponent<BlockView>();
        if (blockView != null) blockView.UpdatePosition(transform.localPosition);
    }

    /**
     * Descripción: Método 
     */
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
     * Descripción: Método llamado cuando el usuario suelta un bloque en la zona de codificación
     * @param: PointerEventData eventData
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this.m_isDraggable || this.m_isTemplate) return; //Si no es arrastrable, no hacemos nada

        // Capturar la posición final del arrastre antes de cualquier cambio
        Vector2 finalPosition = CalculateFinalPosition(eventData);
        
        // Calcular posición final en el CodingArea

        finalPosition += m_TouchOffset;
        transform.localPosition = finalPosition;

        Debug.Log($"Bloque {blockModel.ID} terminó de ser arrastrado, posición final: {finalPosition}");

        // Restaurar la interactividad del bloque
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        if (IsInsideWorkspace(eventData))
    
        {
           
            transform.SetParent(GameObject.Find("CodingArea").transform, true);
            Debug.Log($"Bloque {blockModel.ID} colocado en el área de programación {blockModel.XY}.");

            if (m_block == null)
            {
                Debug.LogError("OnEndDrag: m_block es NULL. No se puede continuar.");
                return;
            }

            //UpdateBlockPosition(finalPosition);

            if (this.m_shadowTop == null || this.m_shadowBottom == null)
            {
                this.CreateShadows();
            }

          //  AddBlockToCodingArea(finalPosition, eventData);

            HandleShadowCollisions();

            //UpdateBlockPosition(transform.localPosition);

            ReactivateShadows();

        }
        else
        {
            // Eliminar bloque si está fuera del área
            DiscardBlock();

        }
    }

    /**
     * Descripción: Método para calcular la posición final del bloque en el área de codificación
     * @param: PointerEventData eventData
     */
    private Vector2 CalculateFinalPosition(PointerEventData eventData)
    {
        RectTransform codingAreaRect = (RectTransform)GameObject.Find("CodingArea").transform;
        Vector2 finalPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            codingAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out finalPosition
        );
        finalPosition += m_TouchOffset;
        return finalPosition;
    }

    /**
     * Descripción: Método para verficiar si el evento de arrastre está dentro del área de trabajo
     */
    private bool IsInsideWorkspace(PointerEventData eventData)
    {
        Transform workspace = GameObject.Find("CodingArea").transform;
        return RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)workspace, eventData.position, eventData.pressEventCamera
        );
    }

    private void AddBlockToCodingArea(Vector2 position, PointerEventData eventData)
    {

        RectTransform codingAreaRect = GameObject.Find("CodingArea").GetComponent<RectTransform>();
        // Verificar si está dentro del CodingArea
        if (RectTransformUtility.RectangleContainsScreenPoint(codingAreaRect, position, eventData.pressEventCamera))
        {
            transform.SetParent(codingAreaRect.transform, true); // Hacerlo hijo del CodingArea
            if (workSpace == null)
            {
                Debug.LogError("workSpace es null en AddBlockToCodingArea. No se puede añadir el bloque.");
                return;
            }

            workSpace.staticBlocks.Add(this); // Añadir al conjunto de bloques estáticos
            HandleShadowCollisions(); // Gestionar conexiones
        }
        else
        {

            if (workSpace == null)
            {
                Debug.LogError("workSpace es null en AddBlockToCodingArea. No se puede remover el bloque.");
                return;
            }
            workSpace.staticBlocks.Remove(this); // Eliminar del conjunto si sale
                                                 // Aquí puedes decidir qué hacer con el bloque dinámico, como devolverlo a su origen
        }


    }


    /**
     * Descripción: Método para actualizar la posición del bloque
     * @param: Vector2 position
     */
    private void UpdateBlockPosition(Vector2 position)
    {
        m_block.XY = position;
        //UpdateConnectionPosition();
        BlockView blockView = GetComponent<BlockView>();
        if (blockView != null && blockView.Block != null)
        {
            blockView.UpdatePosition(position);
            Debug.Log($"BlockView actualizado a: {position} - {blockView.Block.ID}");
        }
        else
        {
            SetOrphan();
            Debug.LogWarning("BlockView o m_Block es null, bloque marcado como huérfano.");
        }
    }

    /**
     * Descripción: Método para manejar las colisiones dentro de las sombras
     */
    private void HandleShadowCollisions()
    {
        
        RectTransform myRect = GetComponent<RectTransform>();
        float myHeight = myRect.rect.height;

        // Obtener el Transform del CodingArea
        Transform codingArea = GameObject.Find("CodingArea").transform;

        // Iterar sobre los hijos del CodingArea
        foreach (Transform child in codingArea)
        {
            BlockBehaviour block = child.GetComponent<BlockBehaviour>();
            if (block != null)
            {
                Debug.Log($"Gestionando conexiones Bloque {block.blockModel.ID} encontrado en CodingArea en posición: {child.localPosition}");
            }
        }

        foreach (BlockBehaviour staticBlock in workSpace.blocksInWorkspace)
        {
            if (staticBlock == this || staticBlock.isDraggable) continue;

            RectTransform staticRect = staticBlock.GetComponent<RectTransform>();
            float staticHeight = staticRect.rect.height;
            Vector2 staticPosition = staticBlock.transform.localPosition;

           if (staticBlock.collidingWithTopShadowOF == this)
            {
                Vector2 newPosition = new Vector2(staticPosition.x, staticPosition.y + (staticHeight / 2) + (myHeight / 2));
                transform.localPosition = newPosition;
                m_block.XY = newPosition;
                nextConnection.Connect(staticBlock.previousConnection);
                Debug.Log($"Bloque {m_BlockType} conectado encima de {staticBlock.m_BlockType} en posición: {newPosition}");
                HideShadows();
                break;
            }
            else if (staticBlock.collidingWithBottomShadowOf == this)
            {
                Vector2 newPosition = new Vector2(staticPosition.x, staticPosition.y - (staticHeight / 2) - (myHeight / 2));
                transform.localPosition = newPosition;
                m_block.XY = newPosition;
                previousConnection.Connect(staticBlock.nextConnection);
                Debug.Log($"Bloque {m_BlockType} conectado debajo de {staticBlock.m_BlockType} en posición: {newPosition}");
                HideShadows();
                break;
            }
        }
    }

    /**
     * Descripción: Método para ocultar las sombras del bloque
     */
    private void HideShadows()
    {
        if (m_shadowTop != null) m_shadowTop.GetComponent<Image>().enabled = false;
        if (m_shadowBottom != null) m_shadowBottom.GetComponent<Image>().enabled = false;
    }

    /**
     * Descripción: Método para reactivar las sombras del bloque
     */
    private void ReactivateShadows()
    {
        if (m_shadowTop != null)
        {
            m_shadowTop.SetActive(true);
            m_shadowTop.GetComponent<BoxCollider2D>().enabled = true;
            m_shadowTop.GetComponent<Image>().enabled = false;
        }
        if (m_shadowBottom != null)
        {
            m_shadowBottom.SetActive(true);
            m_shadowBottom.GetComponent<BoxCollider2D>().enabled = true;
            m_shadowBottom.GetComponent<Image>().enabled = false;
        }
    }

    /**
     * Descripción: Método para descartar un bloque si no está en el área de trabajo
     */
    private void DiscardBlock()
    {
        if (m_shadowTop != null) Destroy(m_shadowTop);
        if (m_shadowBottom != null) Destroy(m_shadowBottom);
        if (m_block != null && !string.IsNullOrEmpty(m_block.ID))
        {
            BlockView.RemoveBlockPosition(m_block.ID);
        }
        workSpace.RemoveBlock(this);
        Destroy(gameObject);
        Debug.Log("Bloque descartado porque no está en el CodingArea.");
    }

    /**
     * Descripción: Método para ocultar todas las sombras estáticas
     */
    private void HideAllStaticShadows()
    {
        foreach (BlockBehaviour staticBlock in workSpace.blocksInWorkspace)
        {
            if (staticBlock != this)
            {
                if (staticBlock.m_shadowTop != null) staticBlock.m_shadowTop.GetComponent<Image>().enabled = false;
                if (staticBlock.m_shadowBottom != null) staticBlock.m_shadowBottom.GetComponent<Image>().enabled = false;
            }
        }
    }
    //Para la gestión de conexiones

    /**
     * Descripción: Método para actualizar la posición de las conexiones del bloque
     */
    public void UpdateConnectionPosition()
    {
        RectTransform rect = GetComponent<RectTransform>();
        float blockHeight = rect.rect.height;

        // La conexión "previous" está en la parte superior del bloque
        this.previousConnection.position = new Vector2(transform.localPosition.x, transform.localPosition.y + (blockHeight / 2));
        // La conexión "next" está en la parte inferior del bloque
        this.nextConnection.position = new Vector2(transform.localPosition.x, transform.localPosition.y - (blockHeight / 2));

        //this.nextConnection.position = transform.localPosition;
        // this.previousConnection.position = transform.localPosition + new Vector3(0, GetComponent<RectTransform>().rect.height,0);
        //Debug.Log($"UpdateConnectionPosition: BlockBehaviour: PreviousConnection position updated to {previousConnection.position}, NextConnection position updated to {NextConnection.position}");
    }

    /**
     * Descripción: Método para actualizar la posición de las sombras del bloque
     */
    private void HandleConnectionState(UpdateState state)
    {

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
        workSpace = FindFirstObjectByType<WorkSpace>(); // Busca el componente WorkSpace en la escena

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
        collider.enabled= true; // Activar el collider
        //Creo la sombra de los bloques para resaltar la conexión más cercana
        this.CreateShadows(); 

       // Debug.Log("Start: BlockBehaviour: Creada la sombra del bloque");
     
        if (!m_isTemplate)
        {
            workSpace.AddBlock(this);
          //  Debug.Log($"Start: BlockBehavour: Bloque {m_BlockType} registrado en WorkSpace con isTemplate: {m_isTemplate}");
        }

        this.nextConnection.onStateChanged += this.HandleConnectionState;
        this.previousConnection.onStateChanged += this.HandleConnectionState;
        foreach (var inputConnection in this.m_inputConnections)
        {
            inputConnection.onStateChanged += this.HandleConnectionState;
        }

        
    }

    /**
     * Descripcion: Método que crea las sombras para los bloques
     */
    private void CreateShadows()
    {
       
        // Destruir sombras existentes para evitar duplicados
        if (this.m_shadowTop != null) Destroy(this.m_shadowTop);
        if (this.m_shadowBottom != null) Destroy(this.m_shadowBottom);

        if (m_isTemplate)
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


        this.m_shadowTop = CreateShadowContainer("ShadowTopContainer", new Vector2(0, blockHeight / 2));
       // Debug.LogWarning($"Sombra creada con éxito en la parte superior del bloque {blockModel.ID}");

        this.m_shadowBottom = CreateShadowContainer("ShadowBottomContainer", new Vector2(0, -blockHeight / 2));

       // Debug.LogWarning($"Sombra creada con éxito en la parte infoerior del bloque {blockModel.ID}");

        // Inicializar ShadowCollision para cada sombra
        ShadowCollision topShadowScript = m_shadowTop.GetComponent<ShadowCollision>();
        Image topShadowImage = m_shadowTop.GetComponent<Image>();
        topShadowScript.Initialize(this, ConnectionZone.Top, topShadowImage);

        ShadowCollision bottomShadowScript = m_shadowBottom.GetComponent<ShadowCollision>();
        Image bottomShadowImage = m_shadowBottom.GetComponent<Image>();
        bottomShadowScript.Initialize(this, ConnectionZone.Bottom, bottomShadowImage);

        // Asegurar de que las imágenes estén ocultas al inicio
        if (m_shadowTop != null) m_shadowTop.GetComponent<Image>().enabled = false;
        if (m_shadowBottom != null) m_shadowBottom.GetComponent<Image>().enabled = false;



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

        float scaleFactor = 0.32f;
        shadowRect.localScale = new Vector3(scaleFactor, scaleFactor, 1); // Escala reducida
        Image shadowImage = shadowContainer.AddComponent<Image>();
        shadowImage.sprite = this.m_blockImage.sprite;
        shadowImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Sombra oscura translúcida esto crea el color de fondo de la sombra ES CORRECTO!!!!!
      //  Debug.LogWarning("CreateShadowContainer: Sombra creada con éxito con color Gris de fondo");
        // Añado BoxCollider2D para detectar la colisión
        BoxCollider2D shadowCollider = shadowContainer.AddComponent<BoxCollider2D>();
        shadowCollider.size = shadowRect.sizeDelta*1.5f; // Tamaño igual a la sombra
        shadowCollider.isTrigger = true; // No bloquea el movimiento del bloque
        // Posición inicial
        shadowRect.anchoredPosition = (Vector2)transform.localPosition + offset;

        // Añadir script para gestionar colisiones
        ShadowCollision shadowScript = shadowContainer.AddComponent<ShadowCollision>();
        shadowScript.Initialize(this, (name.Contains("Top") ? ConnectionZone.Top : ConnectionZone.Bottom), shadowImage);

        Debug.Log($"Sombra {name} creada con tamaño: {shadowRect.sizeDelta}, Posición: {shadowRect.anchoredPosition} y para el bloque {blockModel.ID}");

        return shadowContainer;
    }

    void Awake()
    {

        this.m_blockImage = GetComponent<Image>();
        if (this.m_blockImage == null)
        {
            this.m_blockImage = gameObject.AddComponent<Image>();
            this.m_blockImage.color = Color.white;
        }
        workSpace = WorkSpace.Instance; // Usa el singleton
        if (workSpace == null)
        {
            Debug.LogError("No se encontró una instancia de WorkSpace en la escena.");
        }

    }
    // Método para actualizar la posición de la sombra cuando el bloque se mueve
    private void UpdateShadowPosition()
    {
        if (this.m_shadowTop == null || this.m_shadowBottom == null) return;

        // Solo actualizar si las sombras están activas (durante el arrastre)
        if (!this.m_shadowTop.activeSelf || !this.m_shadowBottom.activeSelf) return;

        GameObject codingArea = GameObject.Find("CodingArea");
        if (transform.parent != codingArea.transform)
        {
            this.m_shadowTop.SetActive(false);
            this.m_shadowBottom.SetActive(false);
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
        this.m_shadowTop.GetComponent<RectTransform>().anchoredPosition =
             new Vector2(blockX, blockY + shadowHeight - offset);

        // Posicionar sombra inferior (debajo del bloque)
        this.m_shadowBottom.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(blockX, blockY - shadowHeight + offset);

    }

    // Métodos auxiliares para la conexión
    private bool CanConnectTo(BlockBehaviour otherBlock, ConnectionZone zone)
    {
        if (zone == ConnectionZone.Top)
        {
            return this.previousConnection.CanConnect(otherBlock.nextConnection);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            return this.nextConnection.CanConnect(otherBlock.previousConnection);
        }
        return false;
    }

    /**
     * Descripción: Método para conectar dos bloques
     * @param: BlockBehaviour otherBlock
     * @param: ConnectionZone zone
     */
    private void ConnectTo(BlockBehaviour otherBlock, ConnectionZone zone)
    {
        RectTransform myRect = GetComponent<RectTransform>();
        RectTransform otherRect = otherBlock.GetComponent<RectTransform>();
        float myHeight = myRect.rect.height;
        float otherHeight = otherRect.rect.height;
        if (zone == ConnectionZone.Top)
        {
            this.previousConnection.Connect(otherBlock.nextConnection);
            this.transform.localPosition = otherBlock.nextConnection.position - new Vector2(0, myRect.rect.height);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            this.nextConnection.Connect(otherBlock.previousConnection);
            this.transform.localPosition = otherBlock.previousConnection.position + new Vector2(0, myRect.rect.height);
        }

        // Actualizar la posición lógica del bloque
        this.m_block.XY = this.transform.localPosition;
        this.m_block.UpdateConnectionPositions();
        Debug.Log($"Conectado {m_BlockType} con {otherBlock.m_BlockType} en {zone}");
    }
   

    public void SetConnectionZone(ConnectionZone zone)
    {
        if (this.m_currentConnectionZone != ConnectionZone.None && this.m_currentConnectionZone != zone)
        {
            // Si ya hay una zona activa y no es la misma, no cambiamos
            return;
        }

        this.m_currentConnectionZone = zone;
        if (zone == ConnectionZone.Top)
        {
            this.m_isTopShadowLocked = true;
            this.m_isBottomShadowLocked = false;
            if (this.m_shadowTop != null) this.m_shadowTop.GetComponent<Image>().enabled = true;
           if (this.m_shadowBottom != null) this.m_shadowBottom.GetComponent<Image>().enabled = false;
   

        }
        else if (zone == ConnectionZone.Bottom)
        {
            this.m_isTopShadowLocked = false;
            this.m_isBottomShadowLocked = true;
            if (this.m_shadowTop != null) this.m_shadowTop.GetComponent<Image>().enabled = false;
           if (this.m_shadowBottom != null) this.m_shadowBottom.GetComponent<Image>().enabled = true;
         
        }
        else
        {
            this.m_isTopShadowLocked = false;
            this.m_isBottomShadowLocked = false;
            if (this.m_shadowTop.GetComponent<Image>() != null)
            {
                if (this.m_shadowTop != null) this.m_shadowTop.GetComponent<Image>().enabled = false;
                if (this.m_shadowBottom != null) this.m_shadowBottom.GetComponent<Image>().enabled = false;
            }
        }
    }

    public void ClearConnectionZone()
    {
        this.m_currentConnectionZone = ConnectionZone.None;
        this.m_isTopShadowLocked = false;
        this.m_isBottomShadowLocked = false;
        if (this.m_shadowTop != null) this.m_shadowTop.GetComponent<Image>().enabled = false;
        if (this.m_shadowBottom != null) this.m_shadowBottom.GetComponent<Image>().enabled = false;
    }
}
