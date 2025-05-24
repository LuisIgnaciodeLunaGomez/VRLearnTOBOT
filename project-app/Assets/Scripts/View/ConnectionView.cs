/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 10/03/2025
 * 
 * Versión: 2.0.0 (Revisión)
 * 
 * Descripción:  Manejo de las conexiones entre bloques es responsable de mostrar el "Highlight" para identificar la sombra del bloque y de llevar a cabo el snap entre bloques.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

public class ConnectionView : BaseView
{
    [SerializeField] protected EConnection m_ConnectionType; 
    public EConnection ConnectionType
    {
        get => m_ConnectionType;
        internal set => m_ConnectionType = value;
    }

    private Image m_BgImage;

    public Image BgImage => m_BgImage;

    private ConnectionModel m_ConnectionModel;
    public ConnectionModel ConnectionModel => m_ConnectionModel;

    protected BlockView m_TargetBlockView;
    protected BlockView m_SourceBlockView;
    public BlockView TargetBlockView => m_TargetBlockView;
    public BlockView SourceBlockView => m_SourceBlockView;
    
    private GameObject m_HighlightObject;
    private RectTransform m_RectTransform;

    public RectTransform GetRectTransformInternal() 
    {
        if (m_RectTransform == null)
        {
            m_RectTransform = GetComponent<RectTransform>();
            if (m_RectTransform == null)
            {
                Debug.LogError($"{System.DateTime.Now:HH:mm:ss.fff} [CV.GetRectTransformInternal ({gameObject.name})] CRITICAL - GetComponent<RectTransform>() devolvió null. ¡Asegúrate de que el GameObject tiene un RectTransform!", this.gameObject);
            }
        }
        return m_RectTransform;
    }

    public override ViewType Type => ViewType.Connection;

    [Tooltip("Prefab to instantiate for highlighting this connection.")]
    [SerializeField] private GameObject m_HighlightPrefab; 
    private GameObject m_HighlightInstance;
    private MemorySafeConnectionObserver m_Observer; 

    public override Vector2 ChildStartXY
    {
        get
        {
            if (m_ConnectionType == EConnection.NextStatement)
                return BlockViewSettings.Instance.StatementConnectPointRect.position; 
            return base.ChildStartXY; 
        }
    }

    protected override Vector2 CalculateSize()
    {
        
        return BlockViewSettings.Instance?.ConnectionSize ?? new Vector2(10, 10);
    }

    protected override void InitializeView()
    {
        base.InitializeView();
       
        Image foundImage = GetComponent<Image>();
        if (foundImage != null && foundImage.gameObject != null) 
        {
            m_BgImage = foundImage;
          //  Debug.Log($"ConnectionView ({gameObject.name}): InitializeView found AND assigned BgImage: {m_BgImage.gameObject.name} (InstanceID: {m_BgImage.GetInstanceID()})", gameObject);
        }
        else
        {
            m_BgImage = null; 
            if (foundImage == null)
            {
              //  Debug.LogError($"ConnectionView ({gameObject.name}): Standard Image component NOT found on self! Check prefab.", gameObject);
            }
            else
            { 
              //  Debug.LogError($"ConnectionView ({gameObject.name}): Found Image component but its GameObject is NULL! Possible corruption or timing issue?", gameObject);
            }
        }
    }

    public virtual void BindModel(ConnectionModel connectionModel, BlockView sourceBlockView)
    {
        // Debug.Log($"ConnectionView ({gameObject.name}): BindModel START. Model ID received: {ConnectionModel.GetConnectionModelID(connectionModel)}, SourceView: {sourceBlockView?.gameObject.name}", this.gameObject);
        string logPrefix = $"[CV.BindModel '{gameObject.name}' ({m_ConnectionType})]";

        if (m_ConnectionModel == connectionModel && m_SourceBlockView == sourceBlockView && m_ConnectionModel != null)
        {
            Debug.LogWarning($"{logPrefix}] SKIPPING - Same model & source view already bound.");
            return;
        }

        if (m_ConnectionModel != null) UnBindModel();

       // Debug.Log($"ConnectionView ({gameObject.name}): Assigning m_ConnectionModel.", this.gameObject);

        m_SourceBlockView = sourceBlockView;
        m_ConnectionModel = connectionModel;

       // Debug.Log($"ConnectionView ({gameObject.name}): m_ConnectionModel assigned: {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}.", this.gameObject);

        if (m_ConnectionModel == null)
        {
           // Debug.Log($"ConnectionView ('{gameObject.name}'): Bound with NULL model. No observers.", this.gameObject);
            Highlight(false);
            gameObject.SetActive(false);
            return; // Salir.
        }
        gameObject.SetActive(true);

        if (m_SourceBlockView == null)
        {
            Debug.LogError($"ConnectionView ({gameObject.name}): BindModel called without a valid sourceBlockView!", this);
            
            // throw new ArgumentNullException(nameof(sourceBlockView), "ConnectionView requires a valid source BlockView during BindModel.");
            return;
        }

        m_SourceBlockView = sourceBlockView;
        m_ConnectionModel = connectionModel;

        if (m_ConnectionModel == null) {
            Debug.LogError($"ConnectionView ({gameObject.name}): BindModel called with NULL model!", this);
            Highlight(false);
            return; }

        if (m_ConnectionModel.Type != this.m_ConnectionType) {
            
            Debug.LogError($"ConnectionView ({gameObject.name}): BindModel called with a model of type {m_ConnectionModel.Type}, but this view is of type {this.m_ConnectionType}.", this);
            UnBindModel(); 
            return; }

        /*  if (connectionModel.Type != this.ConnectionType)
              throw new ArgumentException($"ConnectionView type mismatch! View is {ConnectionType}, Model is {connectionModel.Type}", nameof(connectionModel));
        */
        // if (m_ConnectionModel.SourceBlock == null && m_SourceBlockView?.Block != null) m_ConnectionModel.SourceBlock = m_SourceBlockView.Block;
        //else if (m_ConnectionModel.SourceBlock != m_SourceBlockView?.Block) Debug.LogError($"ConnectionView on {m_SourceBlockView?.name} bound to model from a different block!", this);

        m_Observer = new MemorySafeConnectionObserver(this);
        m_ConnectionModel.AddObserver(m_Observer);

        Debug.Log($"<color=white> {System.DateTime.Now:HH:mm:ss.fff} Inicialización de los bloques plantilla en la toolbox {logPrefix}] Added observer to model {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}."); 
        /*if (m_ConnectionModel.IsConnected && m_ConnectionModel.TargetConnection != null)
        {
            OnConnectStateUpdated(m_ConnectionModel.IsSuperior ?
                                   UpdateState.Connected :
                                   UpdateState.AcceptConnection);
        }*/

        if (m_ConnectionModel.IsConnected)
        {
            ConnectionModel partnerOnInit = m_ConnectionModel.TargetConnection;
            if (partnerOnInit != null)
            {
                // Debug.Log($"{logPrefix}] Model is already connected to {ConnectionModel.GetConnectionModelID(partnerOnInit)}. Triggering initial state update.");
                if (DetermineVisualReceiverRole(m_ConnectionModel, partnerOnInit))
                {
                    PerformVisualAttach(partnerOnInit);
                }
                else
                {
                    PerformInferiorConnectedStateUpdate(partnerOnInit);
                }
            }
            else
            { 
                Debug.LogError($"{logPrefix}] Model inconsistency: IsConnected=true but TargetConnection is null! Forcing detach state.");
                PerformVisualDetach(null);
            }
        }
        else
        {
            // Debug.Log($"{logPrefix}] Model is not connected. Ensuring no target view ref.");
            m_TargetBlockView = null;
        }

        OnXYUpdated();

        Debug.Log($"Inicialización de los bloques plantilla en la toolbox{logPrefix}] BindModel FINISHED for model {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}.");
    }

    public virtual void UnBindModel()
    {
        if (m_ConnectionModel == null) return;

        string logPrefix = $"[CV.UnBindModel '{gameObject.name}' ({m_ConnectionType})]";
        Debug.Log($"{logPrefix}] START Unbinding model {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}.");

        if (m_Observer != null)
        {
            m_ConnectionModel.RemoveObserver(m_Observer);
            m_Observer = null; 
        }

        Highlight(false);

        if (m_TargetBlockView != null)
        {
            /*if (m_ConnectionModel.IsSuperior)
            {
                OnDetached();
            }
            else
            {
                m_TargetBlockView = null;
            }*/
            if (DetermineVisualReceiverRole(m_ConnectionModel, m_ConnectionModel.TargetConnection))
            {
                // Debug.Log($"{logPrefix}] Was visual receiver. Performing visual detach for child: {m_TargetBlockView.name}.");
                PerformVisualDetach(m_ConnectionModel.TargetConnection);
            }
            else
            {
                // Debug.Log($"{logPrefix}] Was visual donor/inferior. Just clearing target view ref for: {m_TargetBlockView.name}.");
                m_TargetBlockView = null;
            }

        }
       
        m_ConnectionModel = null;

        m_SourceBlockView = null;

        Debug.Log($"{logPrefix}] FINISHED Unbind.");
    }

    /// <summary>
    /// Determina si esta ConnectionView debe actuar como el "receptor" visual
    /// que se encarga de adjuntar y posicionar la vista del 'partnerConnection'.
    /// </summary>
    private bool DetermineVisualReceiverRole(ConnectionModel selfConnection, ConnectionModel partnerConnection)
    {
        if (selfConnection == null) return false;
        EConnection selfType = selfConnection.Type;

        // NextStatement recibe un PrevStatement
        if (selfType == EConnection.NextStatement)
        {
            // if (partnerConnection != null && partnerConnection.Type != EConnection.PrevStatement) return false; 
            return true;
        }

        // conexión INPUT recibe al bloque hijo
        // Comprobamos si esta conexión pertenece a un Input del bloque fuente
        if (selfConnection.Input != null)
        {
            // InputValue recibe un OutputValue
            if (selfConnection.Input.Type == EConnection.InputValue && selfType == EConnection.InputValue)
            {
                
                // if (partnerConnection != null && partnerConnection.Type != EConnection.OutputValue) return false;
                return true;
            }
            // Input  tipo Statement ( NextStatement en  InputModel) recibe PrevStatement
            
        
            if (selfConnection.Input.Type == EConnection.NextStatement)
            {
                // if (partnerConnection != null && partnerConnection.Type != EConnection.PrevStatement) return false;
                return true; 
            }
        }

        return false;
    }

    /// <summary>
    /// Llamado por el observador cuando el modelo de conexión cambia de estado.
    /// Dirige la acción visual basada en el rol determinado.
    /// </summary>
    internal void OnConnectStateUpdated(UpdateState updateState)
    {
        if (m_ConnectionModel == null) { /* Log warning y return */ return; }

        string viewGoName = gameObject.name;
        string modelConnId = ConnectionModel.GetConnectionModelID(m_ConnectionModel);

        ConnectionModel partnerModel = m_ConnectionModel.TargetConnection;

        bool amITheStationaryBlockInThisInteraction = false;
        if (BlockDragController.Instance != null && m_SourceBlockView != null)
        {
            // SourceBlockView de la ConnectionView no es el que se está arrastrando,ConnectionView pertenece al bloque estacionario.
            if (m_SourceBlockView.Block != BlockDragController.Instance.DraggingBlockModel)
            {
                amITheStationaryBlockInThisInteraction = true;
            }
        }
        string partnerModelId = ConnectionModel.GetConnectionModelID(partnerModel);

        Logger.Log($"[CV.OnUpdate ENTRY] View: '{viewGoName}', Model: {modelConnId}, Received State: {updateState}. Current Partner: {partnerModelId}");

        switch (updateState)
        {
            case UpdateState.Connected:
                if (partnerModel == null) { 
                    Logger.LogError($"[CV.OnUpdate '{gameObject.name}'] Connected state but no partner model!", this);
                    return;
                }


                if (DetermineVisualReceiverRole(m_ConnectionModel, partnerModel))
                {
                    Logger.Log($"  Action: ACTING AS VISUAL RECEIVER (Stationary Block). Model: {modelConnId}. Partner: {partnerModelId}. Triggering Visual Attach.");

                    PerformVisualAttach(partnerModel);
                }
                else
                {
                    Logger.Log($"  Action: Acting as DONOR/INFERIOR or Part of DRAGGED Block. Model: {modelConnId}. Partner: {partnerModelId}. Triggering Inferior State Update.");
                    PerformInferiorConnectedStateUpdate(partnerModel);
                }
                break;

            case UpdateState.Disconnected:
               
                bool wasReceiver = (m_ConnectionModel.Type == EConnection.NextStatement) ||
                                (m_ConnectionModel.Type == EConnection.InputValue && m_ConnectionModel.Input != null) ||
                                (m_ConnectionModel.Input?.Type == EConnection.NextStatement); 

                //Debug.Log($"[CV.OnUpdate DISCONNECTED] View: '{viewGoName}'. Connection broken. Was receiver?: {wasReceiver}");

                if (wasReceiver)
                {
                    // Debug.Log("    -> Initiating Visual Detach.");
                    PerformVisualDetach(null); 
                }
                else
                {
                    //  Debug.Log("    -> Inferior/Donor action: Clearing target view reference.");
                    m_TargetBlockView = null;
                }
                break;

            case UpdateState.Highlight: Highlight(true); break;
            case UpdateState.UnHighlight: Highlight(false); break;
            case UpdateState.BumpedAway:
                // Solo el inferior se mueve (no el receptor)
                if (!DetermineVisualReceiverRole(m_ConnectionModel, null))
                {
                    if (m_SourceBlockView != null && BlockViewSettings.Instance != null)
                        m_SourceBlockView.XY += BlockViewSettings.Instance.BumpAwayOffset;
                }
                break;
            case UpdateState.AcceptConnection: Logger.Log($"[CV '{viewGoName}'] Ignored AcceptConnection state."); break;
            case UpdateState.CancelConnection: Logger.Log($"[CV '{viewGoName}'] Ignored CancelConnection state."); break;

        }
        // Debug.Log($"[CV.OnUpdate EXIT] View: '{viewGoName}'");
    }

    /// <summary>
    /// Ejecuta la lógica visual para adjuntar y posicionar la vista del bloque hijo.
    /// Llamado sólamente por la ConnectionView receptora.
    /// </summary>
    /// 
    protected virtual void PerformVisualAttach(ConnectionModel newlyAttachedPartnerModel)
    {
        if (newlyAttachedPartnerModel?.SourceBlock == null || m_ConnectionModel == null || m_SourceBlockView == null)
        {
            Logger.LogError($"[{gameObject.name}.PerformVisualAttach] CRITICAL PRECONDITION FAILED: Partner or self model/source view is null!", this);
            return;
        }

        string logPrefix = $"[CV.PerformVisualAttach '{gameObject.name}'({m_ConnectionModel.Type}) FOR {ConnectionModel.GetConnectionModelID(newlyAttachedPartnerModel)}]";
        Highlight(false); 

        BlockView partnerBlockView = m_SourceBlockView.WorkspaceView?.GetBlockView(newlyAttachedPartnerModel.SourceBlock);
        if (partnerBlockView == null)
        {
            Logger.LogError($"{logPrefix} Failed to get BlockView for partner '{newlyAttachedPartnerModel.SourceBlock.ID}'.", this);
            return;
        }

        m_TargetBlockView = partnerBlockView; // Ya sea  hijo visual o  padre visual.

        // Identificar roles para la manipulación visual.

        BlockView viewToMove;            // BlockView que efectivamente cambiará su posición/parent.
        BlockView stationaryContextView; // BlockView que sirve de referencia y no se mueve.
        Transform newVisualParent;       // Nuevo padre Transform para viewToMove.
        Vector2 targetAnchoredPos;       // Nueva anchoredPosition para viewToMove.

        if (m_SourceBlockView.Block == BlockDragController.Instance.DraggingBlockModel)
        {
            //bloque arrastrado, y además soy el superior en la conexión.
            
           // Logger.Log($"{logPrefix} - Case: DRAGGED is SUPERIOR ('{m_SourceBlockView.name}' receives stationary '{partnerBlockView.name}')", this);

            viewToMove = m_SourceBlockView;           // Bloque arrastrado se mueve inicialmente.
            stationaryContextView = partnerBlockView; // Estacionario es la referencia para el bloque arrastrado.

            //Calcular la posición final del bloque arrastrado
            ConnectionView myReceiverConnectionView = this; //  Next o Input del bloque arrastrado

           // Logger.Log($"{logPrefix}   Preparing to get childDonorConnectionView. partnerBlockView is {(partnerBlockView == null ? "NULL" : partnerBlockView.name)}. newlyAttachedPartnerModel.Type is {newlyAttachedPartnerModel.Type}.", this);

            ConnectionView childDonorConnectionView = partnerBlockView.GetConnectionView(newlyAttachedPartnerModel.Type); // Prev o Output del estacionario

            //Logger.Log($"{logPrefix}   Child donor connection view: {childDonorConnectionView?.name} ({childDonorConnectionView?.ConnectionType})", this);


            if (myReceiverConnectionView == null || childDonorConnectionView == null) 
            {
                Logger.LogError($"{logPrefix} CRITICAL: myReceiverConnectionView or childDonorConnectionView IS NULL. myReceiver: {(myReceiverConnectionView == null ? "NULL" : myReceiverConnectionView.name)}, childDonor: {(childDonorConnectionView == null ? "NULL" : childDonorConnectionView.name)}", this);

                return;
            
            }

            // Logs de diagóstico detallado 
            RectTransform childDonorRT = null;
            if (childDonorConnectionView != null)
            { 
                Logger.Log($"{logPrefix}   Obteniendo RectTransform de childDonorConnectionView ('{childDonorConnectionView.name}', GO: '{childDonorConnectionView.gameObject.name}')...", this);
                childDonorRT = childDonorConnectionView.GetRectTransform(); // Llama al método y captura el resultado.
                if (childDonorRT == null)
                {
                    Logger.LogError($"{logPrefix}   ERROR GRAVE: childDonorConnectionView.GetRectTransform() DEVOLVIÓ NULL para '{childDonorConnectionView.name}'. Su m_ViewTransform era: {(childDonorConnectionView.M_ViewTransform_Para_Debug == null ? "NULL" : "ASIGNADO")}", childDonorConnectionView.gameObject);
                  
                }
                else
                {
                    Logger.Log($"{logPrefix}   childDonorConnectionView.GetRectTransform() OK. Name: {childDonorRT.name}", this);
                }
            }

            RectTransform myReceiverRT = null;
            if (myReceiverConnectionView != null)
            {
                Logger.Log($"{logPrefix}   Obteniendo RectTransform de myReceiverConnectionView (this) ('{myReceiverConnectionView.name}', GO: '{myReceiverConnectionView.gameObject.name}')...", this);
                myReceiverRT = myReceiverConnectionView.GetRectTransform();
                if (myReceiverRT == null)
                {
                    Logger.LogError($"{logPrefix}   ERROR GRAVE: myReceiverConnectionView.GetRectTransform() DEVOLVIÓ NULL para '{myReceiverConnectionView.name}'. Su m_ViewTransform era: {(myReceiverConnectionView.M_ViewTransform_Para_Debug == null ? "NULL" : "ASIGNADO")}", myReceiverConnectionView.gameObject);
                }
                else
                {
                    Logger.Log($"{logPrefix}   myReceiverConnectionView.GetRectTransform() OK. Name: {myReceiverRT.name}", this);
                }
            }

            RectTransform viewToMoveRT = null;
            if (viewToMove != null)
            { // viewToMove es un BlockView
                Logger.Log($"{logPrefix}   Obteniendo RectTransform de viewToMove ('{viewToMove.name}', GO: '{viewToMove.gameObject.name}')...", this);
                viewToMoveRT = viewToMove.GetRectTransform();
                if (viewToMoveRT == null)
                {
                    Logger.LogError($"{logPrefix}   ERROR GRAVE: viewToMove.GetRectTransform() DEVOLVIÓ NULL para '{viewToMove.name}'. Su m_ViewTransform era: {(viewToMove.M_ViewTransform_Para_Debug == null ? "NULL" : "ASIGNADO")}", viewToMove.gameObject);
                }
                else
                {
                    Logger.Log($"{logPrefix}   viewToMove.GetRectTransform() OK. Name: {viewToMoveRT.name}", this);
                }
            }

            // Comprobación de la cámara y el workspace
            if (m_SourceBlockView == null)
            {
                Logger.LogError($"{logPrefix} CRITICAL FAILURE: m_SourceBlockView is NULL before WorkspaceView checks!", this);
                return;
            }
            if (m_SourceBlockView.WorkspaceView == null)
            {
                Logger.LogError($"{logPrefix} CRITICAL: m_SourceBlockView.WorkspaceView IS NULL. Block: {m_SourceBlockView.name}", m_SourceBlockView.gameObject);
            }
           /* else
            {
                if (m_SourceBlockView.WorkspaceView.EventCamera == null)
                {
                    Logger.LogError($"{logPrefix} CRITICAL: m_SourceBlockView.WorkspaceView ('{m_SourceBlockView.WorkspaceView.name}') .EventCamera is NULL. RootCanvas of WSV: {m_SourceBlockView.WorkspaceView.RootCanvas?.name ?? "NULL WSV.RootCanvas"}. RootCanvas.worldCamera of WSV: {m_SourceBlockView.WorkspaceView.RootCanvas?.worldCamera?.name ?? "NULL WSV.RootCanvas.worldCam"}", this);
                    return; //return prematuro se elimina
                }
            }*/

            WorkSpaceView wsView = m_SourceBlockView.WorkspaceView;

            if (wsView.RootCanvas == null)
            {
                Logger.LogError($"{logPrefix} CRITICAL: wsView.RootCanvas IS NULL.", this); return;
            }

            Canvas rootCanvas = wsView.RootCanvas;
           // Camera eventCameraForConversion = wsView.EventCamera;

            if (rootCanvas == null)
            {
                Logger.LogError($"{logPrefix} CRITICAL: m_SourceBlockView.WorkspaceView.RootCanvas is NULL.", this);
                return;
            }
            /*  Camera camForConversion = m_SourceBlockView.WorkspaceView.EventCamera;

              if (camForConversion == null)
              {
                  Logger.LogError($"{logPrefix} CRITICAL: Could not determine a valid camera for coordinate conversion. EventCamera was NULL. WorkspaceView: {m_SourceBlockView.WorkspaceView.name}", this);
              }*/
            // --- FIN LOGS DE DIAGNÓSTICO DETALLADO ---

            //  Vector3 targetReceiverConnWorldPos = childDonorConnectionView.GetRectTransform().position;
            if (childDonorRT == null)
            {
                Logger.LogError($"{logPrefix} No se puede calcular targetReceiverConnWorldPos porque childDonorRT es NULL.");
                return; // O maneja el error apropiadamente
            }

            Vector3 targetReceiverConnWorldPos = childDonorRT.position;
            //  Vector3 targetReceiverConnWorldPos = childDonorConnectionView.GetRectTransform().position; //POSIBLE ERRROR????

            // Vector3 offsetFromMyBlockPivotToMyConnPivot = myReceiverConnectionView.GetRectTransform().position - viewToMove.GetRectTransform().position;
            if (myReceiverRT == null)
            {
                Logger.LogError($"{logPrefix} No se puede calcular offsetFromMyBlockPivotToMyConnPivot porque myReceiverRT es NULL.");
                return;
            }
            if (viewToMoveRT == null)
            { // viewToMoveRT viene del GetRectTransform() del BlockView
                Logger.LogError($"{logPrefix} No se puede calcular offsetFromMyBlockPivotToMyConnPivot porque viewToMoveRT es NULL.");
                return;
            }
            Vector3 offsetFromMyBlockPivotToMyConnPivot = myReceiverRT.position - viewToMoveRT.position;
           // Vector3 offsetFromMyBlockPivotToMyConnPivot = myReceiverConnectionView.GetRectTransform().position - viewToMove.GetRectTransform().position;
            Vector3 newMyBlockPivotWorldPos = targetReceiverConnWorldPos - offsetFromMyBlockPivotToMyConnPivot;

            RectTransform parentOfViewToMoveRT = viewToMove.transform.parent as RectTransform;
            if (parentOfViewToMoveRT == null)
            {
                Logger.LogError($"{logPrefix} El padre de viewToMove ('{viewToMove.name}') no es un RectTransform. Es '{viewToMove.transform.parent?.name ?? "NULL"}' de tipo {viewToMove.transform.parent?.GetType().FullName ?? "N/A"}.", this);
                return;
            }

            // Conversión de coordenadas y asignación de posición 
            // EventCamera sea null para ScreenSpaceOverlay.
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                
                targetAnchoredPos = parentOfViewToMoveRT.InverseTransformPoint(newMyBlockPivotWorldPos);
                Logger.Log($"{logPrefix} ScreenSpaceOverlay: newMyBlockPivotWorldPos={newMyBlockPivotWorldPos}, targetAnchoredPos (local in parent)={targetAnchoredPos}", this);
            }
            else //  Canvas en modo ScreenSpaceCamera o WorldSpace
            {
                Camera camForConversion = wsView.EventCamera; 
                if (camForConversion == null)
                {
                    Logger.LogError($"{logPrefix} CRITICAL: EventCamera is NULL for a non-Overlay Canvas ({rootCanvas.renderMode})! Block: {m_SourceBlockView.name}. Cannot perform coordinate conversion.", this);
                  
                    if (rootCanvas.renderMode == RenderMode.WorldSpace)
                    {
                        camForConversion = Camera.main;
                        if (camForConversion == null)
                        {
                            Logger.LogError($"{logPrefix} CRITICAL: Camera.main is also NULL for WorldSpace. Returning.", this);
                            return;
                        }
                        Logger.LogWarning($"{logPrefix} Using Camera.main as fallback for WorldSpace canvas.", this);
                    }
                    else
                    {
                        return; 
                    }
                }

                Vector2 screenP = camForConversion.WorldToScreenPoint(newMyBlockPivotWorldPos);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentOfViewToMoveRT,
                    screenP,
                    camForConversion, 
                    out targetAnchoredPos
                ))
                {
                    Logger.LogWarning($"{logPrefix} {rootCanvas.renderMode}: ScreenPointToLocalPointInRectangle returned false. ScreenPoint might be outside parent rect. screenP={screenP}, parent={parentOfViewToMoveRT.name}", this);
                
                }
                Logger.Log($"{logPrefix} {rootCanvas.renderMode}: newMyBlockPivotWorldPos={newMyBlockPivotWorldPos}, screenPoint={screenP}, targetAnchoredPos={targetAnchoredPos}", this);
            }

            viewToMove.XY = targetAnchoredPos; // Mueve el bloque arrastrado a la posición de snap.

            //  Reparentado visual del bBloque sstacionario
            newVisualParent = m_SourceBlockView.GetNextStatementContainerTransform();
            if (newVisualParent == null)
            {
                Logger.LogWarning($"{logPrefix} GetNextStatementContainerTransform for '{m_SourceBlockView.name}' (dragged superior) is null. Defaulting to m_SourceBlockView.transform.", this);
                newVisualParent = m_SourceBlockView.transform; // Fallback
            }

            // stationaryContextView es el 'partnerBlockView'
            RectTransform partnerRT = stationaryContextView.GetRectTransform();
            if (partnerRT != null)
            {
                ConfigureChildRectForConnection(partnerRT, m_ConnectionModel, newVisualParent.GetComponent<LayoutGroup>() != null);
                stationaryContextView.transform.SetParent(newVisualParent, true); // true: mantener posición mundial
                partnerRT.localScale = Vector3.one;
                partnerRT.localRotation = Quaternion.identity;
                // Ajustar la posición local del hijo estacionario dentro del contenedor del bloque arrastrado.
                // Si el contenedor tiene un LayoutGroup, se encargará. Si no, posicionamiento explícito.
                if (newVisualParent.GetComponent<LayoutGroup>() == null)
                {
                 
                    partnerRT.anchoredPosition = new Vector2(BlockViewSettings.Instance.StatementIndent, 0);
                }
                Logger.Log($"{logPrefix}   Moved self (dragged superior) to snap. Reparented stationary child '{stationaryContextView.name}' to '{newVisualParent.name}'. Child local pos (if no layout): {partnerRT.anchoredPosition}.", this);
            }
            else
            {
                Logger.LogError($"{logPrefix} stationaryContextView.GetRectTransform() is null. Cannot reparent.", this);
            }

            // Layouts: el contenedor dentro del arrastrado y el propio arrastrado.
            if (newVisualParent is RectTransform rtContainer) LayoutRebuilder.MarkLayoutForRebuild(rtContainer);
            LayoutRebuilder.MarkLayoutForRebuild(viewToMoveRT); // arrastrado y he cambiado.
        }
        else // CASO:  bloque estático, y soy el superior.
        {
            
            // El 'partnerBlockView' es el arrastrado y será mi hijo visual.
            Logger.Log($"{logPrefix} - Case: STATIONARY is SUPERIOR ('{m_SourceBlockView.name}' receives dragged '{partnerBlockView.name}')", this);

            viewToMove = partnerBlockView;             // El arrastrado es quien se mueve.
            stationaryContextView = m_SourceBlockView; // Estacionario) es el contexto.
            newVisualParent = GetVisualParentTransformForChild(partnerBlockView); // El container DENTRO de mí.

            if (newVisualParent == null) 
            { 
               
                newVisualParent = m_SourceBlockView.transform; }

            ConfigureChildRectForConnection(viewToMove.GetRectTransform(), m_ConnectionModel, newVisualParent.GetComponent<LayoutGroup>() != null);
            viewToMove.transform.SetParent(newVisualParent, false);
            viewToMove.GetRectTransform().localScale = Vector3.one;
            viewToMove.GetRectTransform().localRotation = Quaternion.identity;

            targetAnchoredPos = CalculateTargetAnchoredPosition(viewToMove, newVisualParent); // Posición del arrastrado dentro del container
            viewToMove.GetRectTransform().anchoredPosition = targetAnchoredPos;

            Logger.Log($"{logPrefix}   Reparented dragged child '{viewToMove.name}' to '{newVisualParent.name}'. Child local pos: {targetAnchoredPos}.", this);

            // Layouts
            if (newVisualParent is RectTransform rtContainerInMe) LayoutRebuilder.MarkLayoutForRebuild(rtContainerInMe);
            LayoutRebuilder.MarkLayoutForRebuild(m_SourceBlockView.GetRectTransform());
        }
    }

    /// <summary>
    /// Actualiza el estado de esta ConnectionView cuando actúa como INFERIOR y se conecta.
    /// Principalmente actualiza referencias y estado visual mínimo.
    /// </summary>
    protected virtual void PerformInferiorConnectedStateUpdate(ConnectionModel partnerModel)
    {
        // Debug.Log($"[CV.PerformInferiorUpdate '{gameObject.name}' ({m_ConnectionType})] Partner: {ConnectionModel.GetConnectionModelID(partnerModel)}");
        Highlight(false); // Asegurar que no hay highlight
        // Actualizar referencia a la vista conectada (padre visual)
        if (partnerModel?.SourceBlock != null && m_SourceBlockView != null)
        {
            // TargetBlockView para una conexión inferior es SourceBlockView del partner
            m_TargetBlockView = m_SourceBlockView.WorkspaceView?.GetBlockView(partnerModel.SourceBlock);
            //  Debug.Log($"  - Set TargetBlockView reference to: {m_TargetBlockView?.name ?? "NULL"}");
        }
        else
        {
            m_TargetBlockView = null;
            // Debug.Log($"  - Cleared TargetBlockView reference.");
        }
    }

    protected void ConfigureChildRectForConnection(RectTransform childRect, ConnectionModel selfReceptorModel, bool parentHasLayoutGroup)
    {
        if (childRect == null || selfReceptorModel == null) return;

        //Conexiones de Statement (NextStatement o Input de tipo Statement)
        if (selfReceptorModel.Type == EConnection.NextStatement ||
            (selfReceptorModel.Input != null && selfReceptorModel.Input.Type == EConnection.NextStatement))
        {
            childRect.pivot = new Vector2(0, 1);    // Top-Left
                                                    
            childRect.anchorMin = new Vector2(0, 1);
            childRect.anchorMax = new Vector2(0, 1);
        }
        // Caso 2: Conexiones de Valor (InputValue)
        else if (selfReceptorModel.Type == EConnection.InputValue && selfReceptorModel.Input != null)
        {
            childRect.pivot = new Vector2(0.5f, 0.5f); // Center
            childRect.anchorMin = new Vector2(0.5f, 0.5f); // Center
            childRect.anchorMax = new Vector2(0.5f, 0.5f); // Center
        }
        else
        {
            Logger.LogWarning($"[CV.ConfigureChildRect '{gameObject.name}'] Unhandled selfReceptorModel type: {selfReceptorModel.Type} with Input: {selfReceptorModel.Input?.Name ?? "N/A"}. Defaulting to TopLeft.", childRect);
            childRect.pivot = new Vector2(0, 1);
            childRect.anchorMin = new Vector2(0, 1);
            childRect.anchorMax = new Vector2(0, 1);
        }
    }

    /// <summary>
    /// Ejecuta la lógica visual para desatachar un hijo visualmente.
    /// Llamado SOLO por la ConnectionView RECEPTORA.
    /// </summary>
    protected virtual void PerformVisualDetach(ConnectionModel detachingPartnerModel) 
    {
        if (m_TargetBlockView != null)
        {
            BlockView viewToDetach = m_TargetBlockView;
            m_TargetBlockView = null; // Limpiar referencia

          // string logPrefix = $"[CV.PerformVisualDetach '{gameObject.name}' ({m_ConnectionType})]";
          //  Logger.Log($"{logPrefix}] Detaching child view: {viewToDetach?.name ?? "TargetBlockView was already null"}. Partner was {ConnectionModel.GetConnectionModelID(detachingPartnerModel)}");

            Transform originalVisualParent = viewToDetach.transform.parent;

            viewToDetach.SetOrphan(true);

            // Reconstruir el layout del contenedor original
            if (originalVisualParent is RectTransform rtParent)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rtParent);
            }
            if (m_SourceBlockView.ViewTransform is RectTransform rtSource && rtSource != originalVisualParent)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rtSource); // También el source si es diferente y pudo cambiar
            }
            Logger.Log($"[CV.PerformVisualDetach '{gameObject.name}'] Detached '{viewToDetach.name}'. Orphaned and requested layout rebuild for '{originalVisualParent?.name}'.", this);

        }
    }

    /// <summary>
    /// Determina el Transform que debe ser el padre visual del bloque hijo
    /// cuando se conecta a ESTA ConnectionView (que actúa como receptora).
    /// </summary>
    protected Transform GetVisualParentTransformForChild(BlockView childView)
    {
        //  ConnectionView actúa como padre directo: Inputs
        if (m_ConnectionModel.Type == EConnection.InputValue && m_ConnectionModel.Input != null)
        {
            // La propia ConnectionView (que está dentro del InputView) es el padre.
            return this.transform;
        }

        // NextStatement: Usar el contenedor dedicado en el SourceBlockView.
        if (m_ConnectionModel.Type == EConnection.NextStatement && m_SourceBlockView != null)
        {
            return m_SourceBlockView.GetNextStatementContainerTransform() ?? this.transform; 
        }

        // Input de Statement - su ConnectionView dentro del Input de Statement debe ser el padre visual.
        if (m_ConnectionModel.Input?.Type == EConnection.NextStatement && this.m_SourceBlockView != null)
        {
            // ConnectionView que representa la entrada del Statement y es el padre visual.
            return this.transform;
        }

        // Fallback 
        Debug.LogError($"[{gameObject.name}.GetVisualParentTransform] No se pudo determinar el padre visual correcto para tipo {m_ConnectionModel.Type} / Input {m_ConnectionModel.Input?.Name}. Usando this.transform como fallback.", this);
        return this.transform;
    }

    /// <summary>
    /// Calcula la posición local (anchoredPosition) donde debe colocarse la childView
    /// relativa a su nuevo padre visual (visualParentTransform) para un encaje perfecto.
    /// </summary>
    private Vector2 CalculateTargetAnchoredPosition(BlockView childView, Transform visualParentTransform)
    {
        if (m_ConnectionModel == null) return Vector2.zero;

        string logPrefix = $"[CV.CalcTargetPos '{gameObject.name}']";
        // Debug.Log($"{logPrefix} Calculating for child '{childView?.name}' relative to parent '{visualParentTransform?.name}' (My ConnType: {m_ConnectionType})");


        BlockViewSettings settings = BlockViewSettings.Instance;
        if (settings == null) 
        { 
            return Vector2.zero; 
        }

        Vector2 position = Vector2.zero; // Posición por defecto si no se calcula

        // Conexión NextStatement (Next, el child es Prev) --
        if (m_ConnectionModel.Type == EConnection.NextStatement)
        {
        
            float indentX = settings.StatementIndent;
            float indentY = 0; //contenido del child empieza en el top del container

            //Comprobación importante para ver si usa el padre visual (container) un VLG
            VerticalLayoutGroup layoutGroup = visualParentTransform?.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                position = new Vector2(layoutGroup.padding.left, 0);
                // Debug.Log($"{logPrefix} (Next): Parent '{visualParentTransform.name}' has VerticalLayoutGroup. Setting target AnchoredPos to ({position.x:F2}, {position.y:F2}) (Y will likely be overridden). Layout Padding Left: {layoutGroup.padding.left}");
            }
            else
            {
                // Sin Layout Group: Posicionamiento manual.
                position = new Vector2(indentX, indentY);
                // Debug.Log($"{logPrefix} (Next): No LayoutGroup on parent '{visualParentTransform.name}'. Setting target AnchoredPos manually to ({position.x:F2}, {position.y:F2}).");
            }
        }
        // Conexión InputValue (Input, el child es Output) --
        else if (m_ConnectionModel.Type == EConnection.InputValue && m_ConnectionModel.Input != null)
        {
               
            position = Vector2.zero; // Se centran si sus tamaños son iguales y los pivotes coinciden

            // Ajuste fino 
            // RectTransform childRect = childView.GetRectTransform();
            // RectTransform myRect = m_RectTransform;
            // float offsetX = (myRect.rect.width - childRect.rect.width) * (myRect.pivot.x - 0.5f); // Ajuste para centrar pivotes diferentes a 0.5
            // float offsetY = (myRect.rect.height - childRect.rect.height) * (myRect.pivot.y - 0.5f);
            // position = new Vector2(offsetX, offsetY);
            // Debug.Log($"{logPrefix} (Input): Target AnchoredPos for centered placement (assuming 0.5 pivots): {position.ToString("F2")}");
        }
        // Conexión Input de Statement (Conexión del Input Stmt, el child es Prev) 
        else if (m_ConnectionModel.Input?.Type == EConnection.NextStatement)
        {
            float indentX = settings.StatementIndent;
            float indentY = 0;
            position = new Vector2(indentX, indentY);
            //  Debug.Log($"{logPrefix} (StmtInput): Target AnchoredPos (relative to self): ({position.x:F2}, {position.y:F2}).");

        }
        else
        {
            Debug.LogError($"{logPrefix} Failed to calculate target position for connection type {m_ConnectionModel.Type}!");
        }

        // Debug.Log($"{logPrefix} FINAL Calculated Target Anchored Position = {position.ToString("F2")}");
        return position;
    }


    /// <summary>
    /// Asegura que el RectTransform del bloque hijo tenga la configuración
    /// de Pivot y Anchors adecuada ANTES de asignarle la anchoredPosition.
    /// ZLocal a 0 y Scale a 1 también por seguridad.
    /// </summary>
    private void StandardizeChildRectTransformForConnection(RectTransform childRect, Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (childRect == null) return;
        childRect.pivot = pivot;
        childRect.anchorMin = anchorMin;
        childRect.anchorMax = anchorMax;
        childRect.localScale = Vector3.one;
        Vector3 currentLocalPos = childRect.localPosition;
        childRect.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, 0);
    }

    protected virtual void HandleModelUpdate(ConnectionModel model, ConnectionUpdateEvent eventType, ConnectionModel partner)
    {
        if (model != m_ConnectionModel) return; 

        switch (eventType)
        {
            case ConnectionUpdateEvent.Connected:
                // Debug.Log($"{Type} on {m_SourceBlockView?.name} received Connected");
                break;

            case ConnectionUpdateEvent.Disconnected:
                // Debug.Log($"{Type} on {m_SourceBlockView?.name} received Disconnected");
                break;

        }
    }
    protected internal override void OnXYUpdated()
    {
        //Debug.Log($"OnXYUpdated START for {gameObject.name}. Model:{ConnectionModel.GetConnectionModelID(m_ConnectionModel)}. SourceView Valid: {m_SourceBlockView != null}. Is InToolbox: {m_SourceBlockView?.InToolbox}", this.gameObject);

        if (m_SourceBlockView != null && m_SourceBlockView.InToolbox)
        {
            //  Debug.Log($"OnXYUpdated para ConnectionView de Plantilla '{gameObject.name}'. Saltando DB.", this.gameObject);
            if (m_ConnectionModel != null && m_ConnectionModel.InDB) m_ConnectionModel.InDB = false;
            return;
        }
        if (m_ConnectionModel == null) { Debug.LogError("OnXYUpdated: m_ConnectionModel is NULL!", this); return; }
        if (m_SourceBlockView == null) { Debug.LogError($"OnXYUpdated: m_SourceBlockView is NULL for connection {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}!", this); return; }
        WorkSpaceView workspaceView = m_SourceBlockView.WorkspaceView;
        if (workspaceView == null) { Debug.LogError($"OnXYUpdated: SourceBlockView '{m_SourceBlockView.gameObject.name}' has NULL WorkSpaceView (and not InToolbox)!", this); return; }

        //Debug.Log($"OnXYUpdated START calculation for Workspace Connection '{gameObject.name}'. InDB Flag: {m_ConnectionModel.InDB}.", this.gameObject);
        Canvas canvas = workspaceView.RootCanvas;
        Camera eventCamera = workspaceView.EventCamera;
        if (canvas == null) { Debug.LogError($"OnXYUpdated ({gameObject.name}): RootCanvas is NULL!", this); return; }
        if (workspaceView.CodingArea == null) { Debug.LogError($"OnXYUpdated ({gameObject.name}): CodingArea is NULL!", this); return; }

        Vector2 screenPoint;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) screenPoint = RectTransformUtility.WorldToScreenPoint(null, ViewTransform.position);
        else screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, ViewTransform.position);
        Vector2 newLocation = workspaceView.ScreenPointToWorkspaceLogicalPosition(screenPoint, eventCamera);
        m_ConnectionModel.Location = newLocation;
        // Debug.Log($"  Calculated ScreenPoint: {screenPoint}, New Logical Location in Model: ({m_ConnectionModel.Location.x:F2}, {m_ConnectionModel.Location.y:F2})", this.gameObject);

        BlockConnectionDB db = m_ConnectionModel.DB;

        if (db != null)
        {
            if (!m_ConnectionModel.Hidden)
            {
                if (m_ConnectionModel.InDB)
                {
                    // Debug.Log($"  DB exists. InDB=true. Calling UpdateConnectionLocation.", this.gameObject);
                  //  db.UpdateConnectionLocation(m_ConnectionModel); //Eliminar
                }

            }
            else
            {
                if (m_ConnectionModel.SourceBlock?.Workspace != null)
                {
                    Debug.LogError($"OnXYUpdated ({gameObject.name}, Model:{ConnectionModel.GetConnectionModelID(m_ConnectionModel)}): CRITICAL - Block BELONGS TO Workspace '{m_ConnectionModel.SourceBlock.Workspace.Id}' BUT ConnectionModel.DB is NULL! Type: {m_ConnectionModel.Type}", this);
                    if (m_ConnectionModel.InDB) m_ConnectionModel.InDB = false;
                }

            }

            /* if (m_ConnectionModel.IsSuperior && m_TargetBlockView != null)
             {
                 Debug.Log($"  Propagating OnXYUpdated from {gameObject.name} to Target Block View '{m_TargetBlockView.gameObject.name}'.", this.gameObject);
                 m_TargetBlockView.OnXYUpdated();
             }*/
            //  Debug.Log($"OnXYUpdated END for {gameObject.name}.", this.gameObject);
        }
    }

    protected virtual void OnAttached() 
    {
        ConnectionModel partnerModel = m_ConnectionModel?.TargetConnection;
        if (partnerModel == null || m_ConnectionModel == null) { Debug.LogError($"[{gameObject.name}.OnAttached] Critical error: Model or PartnerModel is null."); return; }

        string thisViewName = gameObject.name;
        string thisModelConnId = ConnectionModel.GetConnectionModelID(m_ConnectionModel);
        string partnerBlockId = partnerModel.SourceBlock?.ID ?? "PARTNER_BLOCK_NULL";

        Debug.Log($"[CV.OnAttached ENTRY] View: '{thisViewName}', Model: {thisModelConnId}. Trying to attach child block '{partnerBlockId}'.");

        if (m_SourceBlockView == null) { Debug.LogError($"[{thisViewName}.OnAttached] m_SourceBlockView is null!"); return; }

        //  Obtener la BlockView hija 
        m_TargetBlockView = m_SourceBlockView.WorkspaceView?.GetBlockView(partnerModel.SourceBlock);
        if (m_TargetBlockView == null)
        {
            Debug.LogError($"[{thisViewName}.OnAttached] Failed to get BlockView for child block '{partnerBlockId}'. Aborting attach.");
            return;
        }
        string targetViewName = m_TargetBlockView.gameObject.name;
        Debug.Log($"[{thisViewName}.OnAttached] Found Child BlockView: '{targetViewName}'.");

        // Determinar el padre visual- contenedor específico
        Transform visualParentTransform = m_SourceBlockView.GetNextStatementContainerTransform(); 
        if (visualParentTransform == null)
        {
            Debug.LogWarning($"[{thisViewName}.OnAttached] GetNextStatementContainerTransform returned null for '{m_SourceBlockView.name}'. Falling back to this.transform ({thisViewName}).");
            visualParentTransform = this.transform; 
        }
        string visualParentName = visualParentTransform.name;
        Debug.Log($"[{thisViewName}.OnAttached] Determined Visual Parent for '{targetViewName}' will be: '{visualParentName}'.");

        //  Reparentado 
        Debug.Log($"[{thisViewName}.OnAttached] Current parent of '{targetViewName}': '{m_TargetBlockView.transform.parent?.name ?? "NULL"}'. Setting parent to '{visualParentName}'...");
        m_TargetBlockView.transform.SetParent(visualParentTransform, true); 
        string actualNewParentName = m_TargetBlockView.transform.parent?.name ?? "NULL";
        if (actualNewParentName == visualParentName)
        {
            Debug.Log($"[{thisViewName}.OnAttached] Parent of '{targetViewName}' successfully set to '{actualNewParentName}'.");
        }
        else
        {
            Debug.LogError($"[{thisViewName}.OnAttached] FAILED to set parent for '{targetViewName}'. Expected '{visualParentName}', but got '{actualNewParentName}'!");
        }

        //  Posicionamiento 
        Vector2 targetAnchoredPos = CalculateTargetAnchoredPosition(m_TargetBlockView); 
        Debug.Log($"[{thisViewName}.OnAttached] Calculated Target AnchoredPosition for '{targetViewName}' (relative to '{visualParentName}'): {targetAnchoredPos.ToString("F2")}");

        // Antes de posicionar, hay que asegurar la configuración de RectTransform hija (Pivot/Anchor consistente)
        StandardizeChildRectTransformForConnection(m_TargetBlockView.GetRectTransform());

        m_TargetBlockView.GetRectTransform().anchoredPosition = targetAnchoredPos;
        // Verificar inmediatamente
        Vector2 actualAnchoredPos = m_TargetBlockView.GetRectTransform().anchoredPosition;
        Vector3 actualLocalPos = m_TargetBlockView.GetRectTransform().localPosition;
        Vector3 actualWorldPos = m_TargetBlockView.transform.position;
        Debug.Log($"[{thisViewName}.OnAttached] Set AnchoredPosition for '{targetViewName}'. Result: Anchored={actualAnchoredPos.ToString("F2")}, Local={actualLocalPos.ToString("F2")}, World={actualWorldPos.ToString("F2")}");
        if (Vector2.Distance(targetAnchoredPos, actualAnchoredPos) > 0.1f) // Margen pequeño por precisión float
        {
            Debug.LogWarning($"[{thisViewName}.OnAttached] AnchoredPosition discrepancy for '{targetViewName}'. Tried to set {targetAnchoredPos.ToString("F2")}, got {actualAnchoredPos.ToString("F2")}. Check RectTransform setup & LayoutGroups.");
        }

        //  Actualizar Layout del Padre
        Debug.Log($"[{thisViewName}.OnAttached] Requesting Layout Update for SourceBlockView: '{m_SourceBlockView.name}' (parent of this connection).");
       // m_SourceBlockView.UpdateLayout(); 
        LayoutRebuilder.MarkLayoutForRebuild(m_SourceBlockView.ViewTransform);

        Debug.Log($"[CV.OnAttached EXIT] View: '{thisViewName}' finished attaching '{targetViewName}'.");
    }

    /// <summary>
    /// Calcula la posición local (anchoredPosition) donde debe colocarse la childView
    /// </summary>
    /// <param name="childView"></param>
    /// <returns></returns>
    private Vector2 CalculateTargetAnchoredPosition(BlockView childView)
    {
       
        if (m_ConnectionModel.Type == EConnection.NextStatement)
        {

            float offsetX = BlockViewSettings.Instance.StatementConnectPointRect.position.x; 

        
            float offsetY = 0f; 

            Debug.LogWarning($"[CV.CalculateTargetAnchoredPos] Placeholder for Next->Prev! Returning ({offsetX:F2}, {offsetY:F2}). NEEDS PROPER IMPLEMENTATION.", gameObject);
            return new Vector2(offsetX, offsetY);
        }


        // TODO: Añadir lógica similar para conexiones InputValue -> OutputValue etc. si es necesario.

        Debug.LogError($"[CV.CalculateTargetAnchoredPos] Connection type {m_ConnectionModel.Type} not handled!", gameObject);
        return Vector2.zero; // Fallback
    }

    /// <summary>
    /// Asegura que el RectTransform del bloque hijo tenga la configuración
    /// </summary>
    /// <param name="childRect"></param>
    private void StandardizeChildRectTransformForConnection(RectTransform childRect)
    {
        // Ejemplo: Forzar Pivot y Anchors a TopLeft para consistencia
        childRect.pivot = new Vector2(0, 1);
        childRect.anchorMin = new Vector2(0, 1);
        childRect.anchorMax = new Vector2(0, 1);
        childRect.localScale = Vector3.one; 
        Vector3 currentLocalPos = childRect.localPosition;
        childRect.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, 0);

    }

    /// <summary>
    /// Ejecuta la lógica visual para desconectar un hijo visualmente.
    /// </summary>
    protected virtual void OnDetached()
    {
        if (m_TargetBlockView != null)
        {       m_SourceBlockView.RemoveChild(m_TargetBlockView);

                  m_TargetBlockView.SetOrphan();

            BlockView detachedView = m_TargetBlockView;
            m_TargetBlockView = null; 

                    m_SourceBlockView.UpdateLayout(m_SourceBlockView.XY);

        }
        if (m_SourceBlockView != null && m_SourceBlockView.ViewTransform != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(m_SourceBlockView.ViewTransform);
        }
        if (m_TargetBlockView != null && m_TargetBlockView.ViewTransform != null)
        { 
            LayoutRebuilder.MarkLayoutForRebuild(m_TargetBlockView.ViewTransform);
        }
    }

    /// <summary>
    /// Resalta la ConnectionView con un prefab de resaltado que imita la sombra de scratch en los bloques.
    /// Faltaría afinar está lógica para que represente en ella otros tipos de bloques como puede ser el de al hacer click en bandera verde
    /// </summary>
    /// <param name="active"></param>
    public void Highlight(bool active)
    {
        /* if ((m_HighlightInstance != null && m_HighlightInstance.activeSelf == active))
             return;*/

        string logPrefix = $"{System.DateTime.Now:HH:mm:ss.fff} [CV.Highlight '{gameObject.name}' ({m_ConnectionType})]";

        bool currentState = m_HighlightInstance != null && m_HighlightInstance.activeSelf;
        bool instanceExists = m_HighlightInstance != null;

        if (instanceExists && currentState == active)
        {
            Debug.Log($"{logPrefix} SKIPPING - Already in desired state (Instance active: {currentState}, Desired: {active}).");
            return;
        }

        //Debug.Log($"{logPrefix} ENTRY. Desired active: {active}. Instance exists: {instanceExists}. Current visual state: {currentState}.", m_HighlightInstance);

        if (active)
        {
            // if (m_HighlightInstance == null && m_HighlightPrefab != null)
            if (!instanceExists) // Crear si no existe
            {
                if (m_HighlightPrefab == null)
                {
                    Debug.LogWarning($"{logPrefix} No highlight prefab assigned. Cannot create highlight.", this.gameObject);

                    return;
                }

               // Debug.Log($"{logPrefix} Creating highlight instance from prefab '{m_HighlightPrefab.name}'.", this.gameObject);
                m_HighlightInstance = Instantiate(m_HighlightPrefab, this.transform/* this.ViewTransform*/); // Usar this.transform como padre inicial
                m_HighlightInstance.name = $"Highlight_{this.gameObject.name}";

                RectTransform highlightTrans = m_HighlightInstance.GetComponent<RectTransform>();
                if (highlightTrans == null)
                {
                    Debug.LogError($"{logPrefix} Highlight instance '{m_HighlightInstance.name}' is MISSING a RectTransform!", m_HighlightInstance);
                    Destroy(m_HighlightInstance);
                    m_HighlightInstance = null;
                    return;
                }
                float parentOffsetY = this.XY.y;

                PositionHighlightTransform(highlightTrans, parentOffsetY); 

                //Debug.Log($"{logPrefix} Instance CREATED and Positioned: '{m_HighlightInstance.name}'.", m_HighlightInstance);
            
            } 

            if (m_HighlightInstance != null) // Puede ser que la creación haya fallado arriba si highlightTrans era null
            {
                if (!m_HighlightInstance.activeSelf) // Solo activar si no está ya activo.
                {
                    m_HighlightInstance.SetActive(true);
                }
                //Debug.Log($"{logPrefix} Highlight (Instance: '{m_HighlightInstance.name}') is now ACTIVE. WorldPos: {m_HighlightInstance.transform.position.ToString("F2")}", m_HighlightInstance);
         
            }
            else
            {
                Debug.LogError($"{logPrefix} Cannot activate highlight, instance is NULL even after attempting creation.", this.gameObject);
            }

            m_HighlightInstance.SetActive(true);
       //     Debug.Log($"<color=yellow>[{gameObject.name}.Highlight]</color> Instance '{m_HighlightInstance.name}' ACTIVATED.", m_HighlightInstance);
        }

        else
        {
            if (instanceExists && m_HighlightInstance.activeSelf)
            {
                m_HighlightInstance.SetActive(false);
         //       Debug.Log($"{logPrefix} Post-SetActive(false): Instance.activeSelf is now: {m_HighlightInstance.activeSelf}.", m_HighlightInstance);

            }
            else
            {
                Debug.Log($"{logPrefix} Request to deactivate highlight, but no instance exists or was already null.");
            }
        }
    }

    /// <summary>
    /// Posiciona el transform del highlight relativo a esta ConnectionView.
    /// </summary>
    private void PositionHighlightTransform(RectTransform highlightTrans, float parentOffsetY)
    {
        string logPrefix = $"{System.DateTime.Now:HH:mm:ss.fff} [CV.PosHighlightCorrec '{gameObject.name}' ({m_ConnectionType})]";
        // Debug.Log($"{logPrefix} Attempting to position highlight '{highlightTrans.name}'. Parent: '{highlightTrans.parent.name}'.");

        highlightTrans.localScale = Vector3.one;
   
        highlightTrans.anchorMin = new Vector2(0, 1); // Top-Left del padre (ConnectionView)
        highlightTrans.anchorMax = new Vector2(0, 1); // Top-Left del padre
        highlightTrans.pivot = new Vector2(0, 1);     // Pivot Top-Left del propio highlight
        Vector2 targetAnchoredPos = Vector2.zero;    // Lo coloca en la esquina superior-izquierda del padre.
        highlightTrans.localRotation = Quaternion.identity; // Sin rotación por defecto


        //Se obtiene el RectTransfomr de esta ConnectionView (Padre del highlight)
        RectTransform connectionViewRect = GetRectTransformInternal(); 
        if (connectionViewRect == null)
        {
           // Debug.LogError($"{logPrefix} CRITICAL: Failed to get parent ConnectionView RectTransform. Highlight will use prefab's size: {highlightTrans.sizeDelta.ToString("F2")} and default position.", highlightTrans.gameObject);
            highlightTrans.anchoredPosition = targetAnchoredPos; // Intentar posicionar con valores por defecto.
            return; // No se puede continuar de forma fiable sin el tamaño del padre.
        }
        Vector2 connectionViewSize = connectionViewRect.rect.size; // El tamaño actual del ConnectionView.

        BlockViewSettings settings = BlockViewSettings.Instance;

        if (settings == null)
        {
            Debug.LogError($"{logPrefix} BlockViewSettings.Instance is NULL. Cannot use settings for offsets/sizes. Highlight may be incorrect.", this.gameObject);
        }

        // configuraciones específicas por tipo de conexión.
        switch (m_ConnectionType)
        {
            case EConnection.InputValue:
                highlightTrans.localRotation = Quaternion.Euler(0, 0, -90);
                highlightTrans.pivot = new Vector2(0.5f, 0); // Pivote Abajo-Centro del highlight
                highlightTrans.anchorMin = new Vector2(0, 0.5f); // Centro Izquierda de la ConnectionView
                highlightTrans.anchorMax = new Vector2(0, 0.5f);
                if (settings != null)
                {
                    targetAnchoredPos = new Vector2(settings.NotchWidth / 2f, 0); 
                }
            //    Debug.Log($"{logPrefix} InputValue. Using Prefab Size: {highlightTrans.sizeDelta.ToString("F2")}, TargetAP: {targetAnchoredPos.ToString("F2")}", highlightTrans.gameObject);
                break;

            case EConnection.OutputValue:
                highlightTrans.localRotation = Quaternion.Euler(0, 0, -90);
                highlightTrans.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro del highlight
                highlightTrans.anchorMin = new Vector2(1, 0.5f); // Centro Derecha de la ConnectionView
                highlightTrans.anchorMax = new Vector2(1, 0.5f);
                if (settings != null)
                {
                    //Similar a InputValue pero al otro lado
                    targetAnchoredPos = new Vector2(-settings.NotchWidth / 2f, 0); // Esto lo mueve a la izquierda
                }
              //  Debug.Log($"{logPrefix} OutputValue. Using Prefab Size: {highlightTrans.sizeDelta.ToString("F2")}, TargetAP: {targetAnchoredPos.ToString("F2")}", highlightTrans.gameObject);
                break;

            case EConnection.PrevStatement:

                targetAnchoredPos.x = -settings.BlockStartX; ;
                targetAnchoredPos.y = parentOffsetY * 2f - settings.NotchHeight;

                //Debug.Log($"{logPrefix} PrevStatement. TargetAP: {targetAnchoredPos.ToString("F2")}, Set SizeDelta to match ConnectionView: {highlightTrans.sizeDelta.ToString("F2")}", highlightTrans.gameObject);

                break;

            case EConnection.NextStatement:
                if (settings != null)
                {
                   targetAnchoredPos.x = -settings.BlockStartX;
                   targetAnchoredPos.y = settings.TabHeight;
                }

                //Debug.Log($"<color=#90EE90>[{gameObject.name}.Highlight]</color> Case NextStatement => Calculated Target=({targetAnchoredPos.x:F2},{targetAnchoredPos.y:F2})");

                if (Type == ViewType.ConnectionInput && settings != null) 
                {
                   
                    Debug.LogWarning($"{logPrefix} Pos: NextStatement INSIDE AN INPUT. Re-evaluar este offset. {targetAnchoredPos}");
                }
                //Debug.Log($"{logPrefix} NextStatement. TargetAP: {targetAnchoredPos.ToString("F2")}, Set SizeDelta to match ConnectionView: {highlightTrans.sizeDelta.ToString("F2")}", highlightTrans.gameObject);
                break;
            default:
                Debug.LogWarning($"{logPrefix} Connection type {m_ConnectionType} has no specific highlight sizing/anchoring. Using defaults. AP: {targetAnchoredPos.ToString("F2")}, Prefab SizeDelta: {highlightTrans.sizeDelta.ToString("F2")}", highlightTrans.gameObject); break;
        }

        highlightTrans.anchoredPosition = targetAnchoredPos;
        //Debug.Log($"{logPrefix} FINAL for '{highlightTrans.name}' -> Anchors: [{highlightTrans.anchorMin.ToString("F2")}, {highlightTrans.anchorMax.ToString("F2")}], Pivot: {highlightTrans.pivot.ToString("F2")}, AnchoredPos: {highlightTrans.anchoredPosition.ToString("F2")}, SizeDelta: {highlightTrans.sizeDelta.ToString("F2")}, LocalRotation: {highlightTrans.localRotation.eulerAngles.ToString("F1")}, WorldPos: {highlightTrans.transform.position.ToString("F3")}", highlightTrans.gameObject);
    }

    public void SetHighlight(bool show)
    {
        if (m_HighlightObject != null)
        {
            if (m_HighlightObject.activeSelf != show)
                m_HighlightObject.SetActive(show);
        }
        // else Debug.LogWarning("Highlight object not assigned or found.");
    }

    protected override void OnDestroy()
    {
        UnBindModel();
         Debug.Log($"[CV {gameObject?.name}] OnDestroy called."); 

        base.OnDestroy(); 
    }
    
    public  RectTransform GetRectTransform() 
    {
        if (m_RectTransform == null)
        {
            m_RectTransform = GetComponent<RectTransform>();
            if (m_RectTransform == null)
            {
                Debug.LogError($"ConnectionView ({gameObject.name}): CRITICAL - GetRectTransform() failed to find RectTransform!", this.gameObject);
            }
        }
        return m_RectTransform;
    }

    public bool SearchClosest(float searchLimitRadius,
                           ref ConnectionModel bestOverallCandidateConnection, // El mejor encontrado globalmente hasta ahora por cualquier conector
                           ref float currentSmallestRadiusFound, // El radio del mejor encontrado
                           ref ConnectionModel myDraggingConnectionTypeForBestCandidate, // Almacena cuál de los conectores (prev, output) encontró el mejor
                           ConnectionModel myOwnDraggingConnectionModel) // La conexión específica de este ConnectionView que está buscando
    {
        if (myOwnDraggingConnectionModel == null || myOwnDraggingConnectionModel.DBOpposite == null || this.SourceBlockView == null || this.SourceBlockView.InToolbox)
        {
            return false; // No se puede buscar
        }

        ConnectionModel locallyFoundWorkspaceConn; // La conexión encontrada en el WS (myOwnDraggingConnectionModel)
        float locallyFoundRadiusToWorkspaceConn;

        // busca en la DB de sus opuestos (NextStatement)
        myOwnDraggingConnectionModel.DBOpposite.SearchForClosest(
            myOwnDraggingConnectionModel, // Quién busca
            searchLimitRadius,             // Radio máximo
            Vector2.zero,                  // dxy, la posición ya está actualizada
            out locallyFoundWorkspaceConn,
            out locallyFoundRadiusToWorkspaceConn);

        if (locallyFoundWorkspaceConn != null && locallyFoundRadiusToWorkspaceConn < currentSmallestRadiusFound)
        {
            // Lo que encontré es mejor que lo que se había encontrado antes globalmente
            bestOverallCandidateConnection = locallyFoundWorkspaceConn;      // Actualiza el mejor global
            currentSmallestRadiusFound = locallyFoundRadiusToWorkspaceConn; // Actualiza el radio del mejor global
            myDraggingConnectionTypeForBestCandidate = myOwnDraggingConnectionModel; // Guarda que tipo de conexión es el que hizo el match
            return true;
        }
        return false; // No encontré nada o no era mejor
    }


}//Fin clase ConnectionView

