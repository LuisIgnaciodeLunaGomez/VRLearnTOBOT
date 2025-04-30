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
 * Descripción:  Manejo de las conexiones entre bloques
 * 
 */

using System;
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
    public BlockView TargetBlockView => m_TargetBlockView;


    protected BlockView m_SourceBlockView;
    private GameObject m_HighlightObject; 

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
            Debug.Log($"ConnectionView ({gameObject.name}): InitializeView found AND assigned BgImage: {m_BgImage.gameObject.name} (InstanceID: {m_BgImage.GetInstanceID()})", gameObject);
        }
        else
        {
            m_BgImage = null; 
            if (foundImage == null)
            {
                Debug.LogError($"ConnectionView ({gameObject.name}): Standard Image component NOT found on self! Check prefab.", gameObject);
            }
            else
            { 
                Debug.LogError($"ConnectionView ({gameObject.name}): Found Image component but its GameObject is NULL! Possible corruption or timing issue?", gameObject);
            }
        }
    }

    public virtual void BindModel(ConnectionModel connectionModel, BlockView sourceBlockView)
    {
        Debug.Log($"ConnectionView ({gameObject.name}): BindModel START. Model ID received: {ConnectionModel.GetConnectionModelID(connectionModel)}, SourceView: {sourceBlockView?.gameObject.name}", this.gameObject);

        if (m_ConnectionModel == connectionModel && m_SourceBlockView == sourceBlockView && m_ConnectionModel != null) return;

        if (m_ConnectionModel != null) UnBindModel();

        Debug.Log($"ConnectionView ({gameObject.name}): Assigning m_ConnectionModel.", this.gameObject);

        m_SourceBlockView = sourceBlockView;
        m_ConnectionModel = connectionModel;

        Debug.Log($"ConnectionView ({gameObject.name}): m_ConnectionModel assigned: {ConnectionModel.GetConnectionModelID(m_ConnectionModel)}.", this.gameObject);

        if (m_ConnectionModel == null)
        {
            Debug.Log($"ConnectionView ('{gameObject.name}'): Bound with NULL model. No observers.", this.gameObject);
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

      /*  if (connectionModel.Type != this.ConnectionType)
            throw new ArgumentException($"ConnectionView type mismatch! View is {ConnectionType}, Model is {connectionModel.Type}", nameof(connectionModel));
      */
       // if (m_ConnectionModel.SourceBlock == null && m_SourceBlockView?.Block != null) m_ConnectionModel.SourceBlock = m_SourceBlockView.Block;
        //else if (m_ConnectionModel.SourceBlock != m_SourceBlockView?.Block) Debug.LogError($"ConnectionView on {m_SourceBlockView?.name} bound to model from a different block!", this);

        m_Observer = new MemorySafeConnectionObserver(this);
        m_ConnectionModel.AddObserver(m_Observer);

        if (m_ConnectionModel.IsConnected && m_ConnectionModel.TargetConnection != null)
        {
            OnConnectStateUpdated(m_ConnectionModel.IsSuperior ?
                                   UpdateState.Connected :
                                   UpdateState.AcceptConnection);
        }
        OnXYUpdated();

        Debug.Log($"ConnectionView ({gameObject.name}): BindModel END. m_ConnectionModel is {(m_ConnectionModel == null ? "NULL" : "Assigned")}.", this.gameObject);
    }

    public virtual void UnBindModel()
    {
        if (m_ConnectionModel == null) return;

        if (m_Observer != null)
        {
            m_ConnectionModel.RemoveObserver(m_Observer);
            m_Observer = null; 
        }
        if (m_TargetBlockView != null)
        {
            if (m_ConnectionModel.IsSuperior)
            {
                OnDetached();
            }
            else
            {
                m_TargetBlockView = null;
            }
        }
        Highlight(false); 
        m_ConnectionModel = null;
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
        Debug.Log($"OnXYUpdated START for {gameObject.name}. InToolbox: {m_SourceBlockView?.InToolbox}. Current World Pos: {ViewTransform?.position}", this.gameObject);
        // if (m_ConnectionModel == null || m_SourceBlockView?.WorkSpaceView == null) // Necesitamos WorkspaceView
        //     return;
        if (m_SourceBlockView != null && m_SourceBlockView.InToolbox)
        {
            Debug.Log($"OnXYUpdated para ConnectionView de Plantilla '{gameObject.name}'. Saltando actualización de DB.");

            return;
        }

        if (m_ConnectionModel == null) { Debug.LogError("OnXYUpdated: m_ConnectionModel is NULL!", this); return; }
        if (m_SourceBlockView == null) { Debug.LogError("OnXYUpdated: m_SourceBlockView is NULL!", this); return; }
        if (m_SourceBlockView.WorkspaceView == null) { Debug.LogError($"OnXYUpdated: m_SourceBlockView '{m_SourceBlockView.gameObject.name}' has NULL WorkSpaceView!", this); return; }

        Debug.Log($"OnXYUpdated START para {gameObject.name}. InToolbox: {m_SourceBlockView?.InToolbox}. Current World Pos: {ViewTransform?.position}", this.gameObject);

        WorkSpaceView workspaceView = m_SourceBlockView.WorkspaceView;

        if (workspaceView == null)
        {
            Debug.LogError($"OnXYUpdated: m_SourceBlockView '{m_SourceBlockView.gameObject.name}' HAS NULL WorkspaceView property! Check BindModel chain.", this);
            return; 
        }

        RectTransform codingArea = workspaceView.CodingArea;
        Canvas canvas = workspaceView.RootCanvas;

        if (codingArea == null) { Debug.LogError($"OnXYUpdated ({m_ConnectionModel.Type} on {m_SourceBlockView.BlockType}): CodingArea is NULL in WorkspaceView '{workspaceView.gameObject.name}'!", this); return; }
        if (canvas == null) { Debug.LogError($"OnXYUpdated ({m_ConnectionModel.Type} on {m_SourceBlockView.BlockType}): RootCanvas is NULL in WorkspaceView '{workspaceView.gameObject.name}'!", this); return; }

        BlockConnectionDB dbBefore = m_ConnectionModel.DB;
        bool wasInDB = m_ConnectionModel.InDB;

        if (dbBefore != null && wasInDB)
        {
            // Intenta remover antes de actualizar la ubicación si estaba en una DB válida
            try { dbBefore.RemoveConnection(m_ConnectionModel); Debug.Log($"  Removed {gameObject.name} from DB before updating location."); }
            catch (Exception e) { Debug.LogError($"  Failed to remove {gameObject.name} from DB: {e.Message}"); m_ConnectionModel.InDB = false; } // Corregir InDB si la remocion falló
        }
       /* else if (wasInDB && dbBefore == null) 
        { //TODO
          
        }*/
        Vector2 screenPoint;
        Camera eventCamera = workspaceView.EventCamera;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) eventCamera = null;
        screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, ViewTransform.position);

        Vector2 oldLocation = m_ConnectionModel.Location; 
        m_ConnectionModel.Location = workspaceView.ScreenPointToWorkspaceLogicalPosition(screenPoint, eventCamera);

        Debug.Log($"  Calculated ScreenPoint: {screenPoint}, New Logical Location: ({m_ConnectionModel.Location.x:F2}, {m_ConnectionModel.Location.y:F2})");
        if (oldLocation != m_ConnectionModel.Location && m_ConnectionModel.Location == Vector2.zero && screenPoint != Vector2.zero)
        {
            Debug.LogWarning($"  ScreenPointToWorkspaceLogicalPosition might be returning zero for non-zero ScreenPoint!", this.gameObject);
        }

        BlockConnectionDB dbAfter = m_ConnectionModel.DB; 
        if (dbAfter == null && m_SourceBlockView.InToolbox)
        { Debug.Log($"  Cannot add {gameObject.name} to DB - ConnectionModel.DB is NULL after update! - Lógico Plantilla");
            return;
        }
        else if (m_ConnectionModel.Hidden) { Debug.Log($"  Skipped adding {gameObject.name} to DB - Hidden flag is TRUE."); }
        else if (m_ConnectionModel.IsConnected) { Debug.Log($"  Skipped adding {gameObject.name} to DB - IsConnected flag is TRUE."); } // Solo añado desconectados y visibles a la DB
        else if (!m_ConnectionModel.InDB) // <- Intento añadir si no estaba ya y no está oculta/conectada
        {
            try
            {
                dbAfter.AddConnection(m_ConnectionModel);
                Debug.Log($"  ADDED {gameObject.name} to DB! Type: {m_ConnectionModel.Type}"); 

                if (!m_ConnectionModel.InDB) { Debug.LogError("  db.AddConnection failed to set InDB=true!"); }
            }
            catch (Exception e)
            {
                Debug.LogError($"  EXCEPTION when ADDING {gameObject.name} to DB: {e.Message}", this.gameObject);
                m_ConnectionModel.InDB = false; // Asegurar estado correcto
            }
        }
        else { Debug.Log($"  {gameObject.name} is already in DB ({m_ConnectionModel.Location}), no need to re-add."); }

        if (m_ConnectionModel.IsSuperior && m_TargetBlockView != null)
        {
            Debug.Log($"  Propagating OnXYUpdated to Target Block View {m_TargetBlockView.Block.Type}.", m_TargetBlockView.gameObject);
            m_TargetBlockView.OnXYUpdated();
        }
    }

    internal void OnConnectStateUpdated(UpdateState updateState) 
    {
        if (m_ConnectionModel == null) return; 

        switch (updateState)
        {
            case UpdateState.Connected:
                if (!m_ConnectionModel.IsSuperior) throw new InvalidOperationException("Only Superior can receive 'Connected'");
                OnAttached();
                break;

            case UpdateState.AcceptConnection:
                if (m_ConnectionModel.IsSuperior) throw new InvalidOperationException("Only Inferior can receive 'AcceptConnection'");
                m_TargetBlockView = m_SourceBlockView.WorkspaceView?.GetBlockView(m_ConnectionModel.TargetBlock);
                break;

            case UpdateState.Disconnected:
                if (!m_ConnectionModel.IsSuperior) throw new InvalidOperationException("Only Superior can receive 'Disconnected'");
                OnDetached();
                break;

            case UpdateState.CancelConnection:
                if (m_ConnectionModel.IsSuperior) throw new InvalidOperationException("Only Inferior can receive 'CancelConnection'");
                m_TargetBlockView = null;
                m_SourceBlockView.SetOrphan();
                break;


            case UpdateState.BumpedAway:
                if (m_ConnectionModel.IsSuperior) throw new InvalidOperationException("Only Inferior can receive 'BumpedAway'");
                if (BlockViewSettings.Instance != null)
                    m_SourceBlockView.XY += BlockViewSettings.Instance.BumpAwayOffset;
                else
                    Debug.LogWarning("BlockViewSettings not available for BumpAwayOffset");
                break;

            case UpdateState.Highlight:
                Highlight(true);
                break;

            case UpdateState.UnHighlight:
                Highlight(false);
                break;
        }
    }

    protected virtual void OnAttached()
    {
        if (m_ConnectionModel.TargetConnection == null) return; 

        if (m_SourceBlockView == null)
        {
            Debug.LogError("OnAttached: m_SourceBlockView is null! Cannot proceed.");
            return;
        }

        m_TargetBlockView = m_SourceBlockView.WorkspaceView?.GetBlockView(m_ConnectionModel.TargetBlock);

        if (m_TargetBlockView != null)
        {
            m_SourceBlockView.AddChild(m_TargetBlockView);
            // m_TargetBlockView.XY = this.ChildStartXY; 
            m_SourceBlockView.UpdateLayout(m_SourceBlockView.XY); 
        }
        else
        {
            Debug.LogError($"OnAttached: Could not find BlockView for target block {m_ConnectionModel.TargetBlock?.ID}");
        }
    }
    protected virtual void OnDetached()
    {
        if (m_TargetBlockView != null)
        {       m_SourceBlockView.RemoveChild(m_TargetBlockView);

                  m_TargetBlockView.SetOrphan();

            BlockView detachedView = m_TargetBlockView;
            m_TargetBlockView = null; 

                    m_SourceBlockView.UpdateLayout(m_SourceBlockView.XY);

        }
    }

    public void Highlight(bool active)
    {
        if ((m_HighlightInstance != null && m_HighlightInstance.activeSelf == active))
            return;


        if (active)
        {
            if (m_HighlightInstance == null && m_HighlightPrefab != null)
            {
                m_HighlightInstance = Instantiate(m_HighlightPrefab, this.ViewTransform); 
                RectTransform highlightTrans = m_HighlightInstance.GetComponent<RectTransform>();

                highlightTrans.localScale = Vector3.one; 
                highlightTrans.localPosition = Vector3.zero; 

                if (ConnectionType == EConnection.InputValue)
                {
                    highlightTrans.localRotation = Quaternion.Euler(0, 0, -90);
                    highlightTrans.pivot = new Vector2(0.5f, 0); // Pivote Abajo-Centro
                    highlightTrans.anchorMin = new Vector2(0, 0.5f); // Centro Izquierda del padre
                    highlightTrans.anchorMax = new Vector2(0, 0.5f);
                       highlightTrans.anchoredPosition = new Vector2(BlockViewSettings.Instance.NotchWidth / 2f, 0); // Centrado en X

                }
                else if (ConnectionType == EConnection.OutputValue)
                {
                    // Ajustes para Tab Derecho (vertical)
                    highlightTrans.localRotation = Quaternion.Euler(0, 0, -90);
                    highlightTrans.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro
                    highlightTrans.anchorMin = new Vector2(1, 0.5f); // Centro Derecha del padre
                    highlightTrans.anchorMax = new Vector2(1, 0.5f);
                    highlightTrans.anchoredPosition = new Vector2(-BlockViewSettings.Instance.NotchWidth / 2f, 0); // Centrado en X

                }
                else if (ConnectionType == EConnection.NextStatement && Type == ViewType.ConnectionInput) 
                {
                    highlightTrans.localRotation = Quaternion.identity; // Sin rotación
                    highlightTrans.pivot = new Vector2(0.5f, 0); // Pivote Abajo-Centro
                    highlightTrans.anchorMin = new Vector2(0.5f, 0); // Abajo-Centro del padre
                    highlightTrans.anchorMax = new Vector2(0.5f, 0);
                    highlightTrans.anchoredPosition = new Vector2(0, BlockViewSettings.Instance.NotchHeight / 2f); // Centrado Y

                }
                else 
                {
                    // Previous: Notch Superior, Next: Tab Inferior (ambos horizontales)
                    highlightTrans.localRotation = Quaternion.identity;
                    if (ConnectionType == EConnection.PrevStatement)
                    {
                        highlightTrans.pivot = new Vector2(0.5f, 1); // Pivote Arriba-Centro
                        highlightTrans.anchorMin = new Vector2(0.5f, 1); // Arriba-Centro padre
                        highlightTrans.anchorMax = new Vector2(0.5f, 1);
                        highlightTrans.anchoredPosition = new Vector2(0, -BlockViewSettings.Instance.NotchHeight / 2f);
                    }
                    else
                    { // NextStatement
                        highlightTrans.pivot = new Vector2(0.5f, 0); // Pivote Abajo-Centro
                        highlightTrans.anchorMin = new Vector2(0.5f, 0); // Abajo-Centro padre
                        highlightTrans.anchorMax = new Vector2(0.5f, 0);
                        highlightTrans.anchoredPosition = new Vector2(0, BlockViewSettings.Instance.NotchHeight / 2f);
                    }
                }


            }
            if (m_HighlightInstance != null) m_HighlightInstance.SetActive(true);

        }
        else
        {
            if (m_HighlightInstance != null)
                m_HighlightInstance.SetActive(false);
        }
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

    public new void OnDestroy()
    {
        UnBindModel(); 
        base.OnDestroy(); 
    }
}//Fin clase ConnectionView

