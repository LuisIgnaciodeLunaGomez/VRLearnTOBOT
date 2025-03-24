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
    private bool isDraggable = true; //Indica si el bloque se puede arrastrar
    private bool isTemplate = false; //Indica si el bloque es una plantilla de la ToolBox de scratch
    private Block m_block; //Referencia al bloque lógico
    private WorkSpace workSpace;
    public ShadowZone zone;
    private BlockBehaviour m_topShadowCollision;    // Bloque colisionando con la sombra superior
    private BlockBehaviour m_bottomShadowCollision; // Bloque colisionando con la sombra inferior

    private BlockBehaviour m_collidingWithTopShadowOf; // Bloque estático cuya sombra superior está colisionando
    private BlockBehaviour m_collidingWithBottomShadowOf; // Bloque estático cuya sombra inferior está colisionando
    //Gestionar las conexiones
    [SerializeField] public BlockConnection NextConnection { get; private set; }
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

    public bool isATemplate => isTemplate;

    public string blockType => m_BlockType;

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
        Debug.Log($"Initialize: BlockBehaviour: Bloque inicializado: {blockData.type} ");

        this.m_block = new Block(blockData.type, Vector2.zero, workspace);
        this.m_block.Initialize(blockData);
        this.m_block.SetBlockBehaviour(this);

        this.NextConnection = this.m_block.nextConnection;
        this.previousConnection = this.m_block.previousConnection;
        this.m_inputConnections = this.m_block.inputList
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
        this.m_inputConnections = this.m_block.inputList
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

        if (isTemplate) //Si el bloque es una plantilla (proviene del MiddlePanel), se procede a su clonación
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

        this.m_collidingWithTopShadowOf = null;
        this.m_collidingWithBottomShadowOf = null;
       

        // Si el bloque está conectado, lo desenchufamos antes de moverlo.
        
        if (this.m_block != null)
        {
            this.m_block.UnPlug(); // Desconecta el bloque de otros bloques
            BlockView blockView = GetComponent<BlockView>();
            if (blockView != null) blockView.RemovePositionOnDrag(); // Eliminar posición al arrastrar

        }
        else
        {
            this.SetOrphan(); //  Marca el bloque como huérfano si no estaba conectado a otro
            Debug.Log($"OnBeginDrag: BlockBehaviour: Bloque {gameObject.name} marcado como huérfano.");

        }

        // Se calcula la diferencia entre el punto de toque y la posición del bloque
        Vector2 localPos;
        RectTransform codingAreaRect = (RectTransform)GameObject.Find("CodingArea").transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //(RectTransform)transform.parent,
            codingAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );
       

        this.m_TouchOffset = (Vector2)transform.localPosition - localPos;

        // Desactivar colliders de sombras al iniciar el arrastre
        if (this.m_shadowTop != null)
        {   this.m_shadowTop.SetActive(true);
            this.m_shadowTop.GetComponent<BoxCollider2D>().enabled = false;
            
            this.m_shadowTop.GetComponent<Image>().enabled = false;


        }
        if (this.m_shadowBottom != null)
        {   this.m_shadowBottom.SetActive(true); 
            this.m_shadowBottom.GetComponent<BoxCollider2D>().enabled = false;
            
            this.m_shadowBottom.GetComponent<Image>().enabled = false;
        }
       
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
     * @param: PointerEventData eventData
    */
    public GameObject OnPickBlockView(PointerEventData eventData)
    {
        GameObject clonedBlock = Instantiate(gameObject, transform.parent);
        clonedBlock.tag = "Block";
        BlockBehaviour clonedBehaviour = clonedBlock.GetComponent<BlockBehaviour>();
        clonedBehaviour.SetAsTemplate(false); //Desactiva la plantilla
        clonedBehaviour.SetDraggable(true); //Activa el arrastre

        // Configurar RectTransform
        RectTransform rect = clonedBlock.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); // Ancla superior izquierda
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);

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

        // Calcular la posición inicial del clon basada en coordenadas del CodingArea
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
           (RectTransform)codingArea,
           eventData.position,
           eventData.pressEventCamera,
           out localPos
        );
        rect.localPosition = localPos;

        // Depurar
        Debug.Log($"Cloned Block Initial Pos in CodingArea: {localPos}, Screen Pos: {eventData.position}");
        //clonedBlock.transform.localPosition = localPos;
        
        eventData.pointerDrag = clonedBlock;

        //workSpace.AddBlock(clonedBehaviour);
        clonedBehaviour.workSpace = this.workSpace;
        clonedBehaviour.SetBlock(new Block(m_BlockType, localPos, workSpace));
        clonedBehaviour.m_block.XY = localPos;
        var view = clonedBlock.GetComponent<BlockView>();
        if (view != null)
        {
            view.BindModel(clonedBehaviour.GetBlock(), clonedBehaviour.GetBlock().blockData, FindFirstObjectByType<WorkSpaceView>());
        }

        // Crear sombras después de mover al CodingArea
        clonedBehaviour.CreateShadows();
        clonedBehaviour.UpdateShadowPosition(); // Asegurar posición inicial correcta

        // clonedBehaviour.UpdateConnectionPosition();
        workSpace.AddBlock(clonedBehaviour); //Registro del bloque clonado

        Debug.Log($"OnPickBlockView: BlockBehaviour: Bloque clonado {clonedBehaviour.m_BlockType} registrado en WorkSpace");
        eventData.pointerDrag = clonedBlock;

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

        // Desactivar las sombras al comenzar el arrastre*/
        if (this.m_shadowTop != null)
        {

            this.m_shadowTop.SetActive(true);
            this.m_shadowTop.GetComponent<BoxCollider2D>().enabled = false;
            this.m_shadowTop.GetComponent<Image>().enabled = false;
        }
        if (this.m_shadowBottom != null)
        {
            this.m_shadowBottom.SetActive(true);
            this.m_shadowBottom.GetComponent<BoxCollider2D>().enabled = false;
            this.m_shadowBottom.GetComponent<Image>().enabled = false;
        }
       
        BlockView blockView = GetComponent<BlockView>();
        if (blockView != null && blockView.Block != null)
        {
            blockView.UpdatePosition(transform.localPosition);
        }

    }

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
            this.NextConnection.Connect(staticBlock.previousConnection);
            transform.position = staticBottom - (myTop - transform.position); // Alinear mi Top con su Bottom
        }
        else if (zone == ConnectionZone.Bottom)
        {
            // Sombra abajo: dinámico encima del estático
            this.previousConnection.Connect(staticBlock.NextConnection);
            transform.position = staticTop - (myBottom - transform.position); // Alinear mi Bottom con su Top
        }

        this.m_block.XY = transform.localPosition;
        this.m_block.UpdateConnectionPositions();

        BlockView blockView = GetComponent<BlockView>();
        if (blockView != null) blockView.UpdatePosition(transform.localPosition);
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
     * Descripción: Método llamado cuando el usuario suelta un bloque en la zona de codificación
     * @param: PointerEventData eventData
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this.isDraggable || this.isTemplate) return; //Si no es arrastrable, no hacemos nada

        Debug.Log($"Bloque {gameObject.name} terminó de ser arrastrado");
        // Capturar la posición final del arrastre antes de cualquier cambio
        Vector2 finalPosition = transform.localPosition;
        Debug.Log($"Posición final del arrastre: {finalPosition}");

        // this.transform.SetParent(GameObject.Find("CodingArea").transform, true);

        // Calcular posición final en el CodingArea
       
        RectTransform codingAreaRect = (RectTransform)GameObject.Find("CodingArea").transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            codingAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out finalPosition
        );
        finalPosition += m_TouchOffset;
        transform.localPosition = finalPosition;

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

        // Debug.Log($"Screen Position: {eventData.position}, inside CodingArea: {insideWorkspace}");

       if (insideWorkspace || !insideWorkspace)
        {  
            transform.SetParent(workspace, true);
            Debug.Log("Bloque colocado en el área de programación.");

            if (m_block == null)
            {
                Debug.LogError("OnEndDrag: m_block es NULL. No se puede continuar.");
                return;
            }
            // Me asesguro de que las posiciones estén actualizadas antes de buscar
            this.m_block.XY = transform.localPosition;
            this.UpdateConnectionPosition();

            BlockView blockView = GetComponent<BlockView>();
            if (blockView != null && blockView.Block != null)
            {
                blockView.UpdatePosition(finalPosition); // Registrar posición en blockPositions
                Debug.Log($"BlockView actualizado a: {finalPosition}");
            }
            else
            {
                this.SetOrphan();
                Debug.LogWarning("BlockView o m_Block es null, bloque marcado como huérfano.");
            }

            // Crear las sombras si no existen
            if (this.m_shadowTop == null || this.m_shadowBottom == null)
            {
                this.CreateShadows();
            }

            //////////////////////////TRABAJANDO EN LAS COLISIONES/////////////////////////////////////
            #region colisiones
            // Verificar colisiones con sombras para conexión
            RectTransform myRect = GetComponent<RectTransform>();
            float myHeight = myRect.rect.height;

            // Buscar bloques estáticos con colisiones activas
            foreach (BlockBehaviour staticBlock in workSpace.blocksInWorkspace)
            {
                if (staticBlock == this) continue;

                RectTransform staticRect = staticBlock.GetComponent<RectTransform>();
                float staticHeight = staticRect.rect.height;
                Vector2 staticPosition = staticBlock.transform.localPosition;

                if (staticBlock.m_topShadowCollision == this)
                {
                    finalPosition = new Vector2(staticPosition.x, staticPosition.y + staticHeight);
                    transform.localPosition = finalPosition;
                    m_block.XY = finalPosition;
                    previousConnection.Connect(staticBlock.NextConnection);
                    Debug.Log($"Bloque {m_BlockType} conectado encima de {staticBlock.m_BlockType} " +
                              $"en posición: {finalPosition}");
                    break;
                }
                else if (staticBlock.m_bottomShadowCollision == this)
                {
                    finalPosition = new Vector2(staticPosition.x, staticPosition.y - myHeight);
                    transform.localPosition = finalPosition;
                    m_block.XY = finalPosition;
                    NextConnection.Connect(staticBlock.previousConnection);
                    Debug.Log($"Bloque {m_BlockType} conectado debajo de {staticBlock.m_BlockType} " +
                              $"en posición: {finalPosition}");
                    break;
                }
            }

            #endregion
            /////////////////////// FIN TRABAJANDO EN LAS COLISIONES//////////////////////////////////////

            if (RectTransformUtility.RectangleContainsScreenPoint(codingAreaRect, eventData.position, eventData.pressEventCamera))
            {
                //Debug.Log("Bloque colocado en el área de programación.");
                this.m_block.XY = finalPosition;
                this.UpdateConnectionPosition();

                // BlockView blockView = GetComponent<BlockView>();
                if (blockView != null && blockView.Block != null)
                {
                    blockView.UpdatePosition(finalPosition);
                    Debug.Log($"BlockView actualizado a: {finalPosition} - {blockView.Block.ID}");
                }
                else
                {
                    this.SetOrphan();

                    blockView.UpdatePosition(finalPosition);
                    Debug.Log($"Bloque huerfano en: {finalPosition} - {blockView.Block.ID}");
                }
            }
            else
            {
                this.workSpace.RemoveBlock(this);
                Destroy(gameObject);
                Debug.Log("Bloque descartado porque no está en el CodingArea.");

                // Antes de eliminar el bloque, destruir las sombras
                if (this.m_shadowTop != null)
                {
                    Destroy(this.m_shadowTop);
                    this.m_shadowTop = null;
                }
                if (this.m_shadowBottom != null)
                {
                    Destroy(this.m_shadowBottom);
                    this.m_shadowBottom = null;
                }
            }
            

            if (GetComponent<BlockView>()?.Block != null)
            {
                Vector2 finalPos = transform.localPosition;
               // Debug.Log($"Asignando posición a BlockView: {finalPos}");
                GetComponent<BlockView>().UpdatePosition(finalPos);
            }
            Canvas.ForceUpdateCanvases();
         

            // Reactivar sombras para el bloque ahora estático
             if (this.m_shadowTop != null)
             {
                 this.m_shadowTop.SetActive(true);
                 this.m_shadowTop.GetComponent<BoxCollider2D>().enabled = true;

                //this.m_shadowTop.GetComponent<Image>().color =  new Color(1f, 0f, 1f);//Color rosa
                this.m_shadowTop.GetComponent<Image>().enabled = false;
               // Debug.Log("Las sombras se muestran en color rosa");

            }
             if (this.m_shadowBottom != null)
             {
                 this.m_shadowBottom.SetActive(true);
                 this.m_shadowBottom.GetComponent<BoxCollider2D>().enabled = true;

               // this.m_shadowBottom.GetComponent<Image>().color =  new Color(1f, 0f, 1f);//Color rosa
                this.m_shadowBottom.GetComponent<Image>().enabled = false;
            }
            // BlockView.PrintBlockSummary();
        }
        else
        {
            // Antes de eliminar el bloque, destruir las sombras
            if (this.m_shadowTop != null)
            {
                Destroy(this.m_shadowTop);
                this.m_shadowTop = null;
            }
            if (this.m_shadowBottom != null)
            {
                Destroy(this.m_shadowBottom);
                this.m_shadowBottom = null;
            }

            // Eliminar la entrada de blockPositions antes de destruir el bloque
            if (m_block != null && !string.IsNullOrEmpty(m_block.ID))
            {
                BlockView.RemoveBlockPosition(m_block.ID);
            }
            else
            {
                Debug.LogWarning("No se pudo eliminar de blockPositions: m_block o su ID es null.");
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
        collider.enabled= true; // Activar el collider
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
        foreach (var inputConnection in this.m_inputConnections)
        {
            inputConnection.onStateChanged += this.HandleConnectionState;
        }

    }


    /**
     * Descripción: Método que destaca un bloque al que se puede conectar
     * @param: bool dato
     */
    public void Highlight(bool dato)
    {
        if (dato) this.m_blockImage.color = Color.green;

    }

    /**
     * Descripcion: Método que crea las sombras para los bloques
     */
    private void CreateShadows()
    {
       
        // Destruir sombras existentes para evitar duplicados
        if (this.m_shadowTop != null) Destroy(this.m_shadowTop);
        if (this.m_shadowBottom != null) Destroy(this.m_shadowBottom);

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


        this.m_shadowTop = CreateShadowContainer("ShadowTopContainer", new Vector2(0, blockHeight / 2));
        Debug.LogWarning("Sombra creada con éxito en la parte superior del bloque");
        this.m_shadowBottom = CreateShadowContainer("ShadowBottomContainer", new Vector2(0, -blockHeight / 2));

        Debug.LogWarning("Sombra creada con éxito en la parte infoerior del bloque");

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
        Debug.LogWarning("CreateShadowContainer: Sombra creada con éxito con color Gris de fondo");
        // Añado BoxCollider2D para detectar la colisión
        BoxCollider2D shadowCollider = shadowContainer.AddComponent<BoxCollider2D>();
        shadowCollider.size = shadowRect.sizeDelta*1.5f; // Tamaño igual a la sombra
        shadowCollider.isTrigger = true; // No bloquea el movimiento del bloque
        // Posición inicial
        shadowRect.anchoredPosition = (Vector2)transform.localPosition + offset;

        //Canvas shadowCanvas = shadowContainer.AddComponent<Canvas>();
        // shadowCanvas.overrideSorting = false;

        // Añadir script para gestionar colisiones
        // Añadir script para gestionar colisiones
        ShadowCollision shadowScript = shadowContainer.AddComponent<ShadowCollision>();
        shadowScript.Initialize(this, (name.Contains("Top") ? ConnectionZone.Top : ConnectionZone.Bottom), shadowImage);

        Debug.Log($"Sombra {name} creada con tamaño: {shadowRect.sizeDelta}, Posición: {shadowRect.anchoredPosition}");

        return shadowContainer;
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
            return this.previousConnection.CanConnect(otherBlock.NextConnection);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            return this.NextConnection.CanConnect(otherBlock.previousConnection);
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
            this.previousConnection.Connect(otherBlock.NextConnection);
            this.transform.localPosition = otherBlock.NextConnection.position - new Vector2(0, myRect.rect.height);
        }
        else if (zone == ConnectionZone.Bottom)
        {
            this.NextConnection.Connect(otherBlock.previousConnection);
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
        else if (zone == ConnectionZone.Bottom && this.m_isBottomShadowLocked)
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
