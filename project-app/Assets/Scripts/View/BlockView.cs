/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 2.0.2
 * 
 * Descripción: Clase que representa un bloque visual en la interfaz de usuario premite la vinculación del modelo lógico con la UI
 * 
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]

public class BlockView : BaseView, IBeginDragHandler, IDragHandler, IEndDragHandler //TODO: Interesante mirar IPointerClickHandler
{

    [SerializeField] private List<Image> m_BgImages = new List<Image>();
    [Tooltip("Asigna aquí la Imagen principal que muestra el color del bloque")]
    [SerializeField] private Image m_PrimaryBackground; 
    private bool m_LayoutIsDirty = false;

    public override ViewType Type
    {
        get { return ViewType.Block; }
    }

    public string BlockType
    {
        get { return mBlock.Type; }
    }

    private BlockModel mBlock;
    public BlockModel Block { get { return mBlock; } }
    private bool m_IsInlineMode = false;
    public bool IsInlineMode => m_IsInlineMode;
    public bool InToolbox { get; set; }
    public bool IsDragging { get; set; } = true;
  
    private MemorySafeBlockObserver mBlockObserver;
    private CanvasGroup m_canvasGroup; 
    private Vector2 m_dragStartOffset; 
    private LayoutElement m_layoutElement;             
    private WorkSpaceView m_WorkspaceView;

    public WorkSpaceView workSpaceView { get; set; }
    protected override void InitializeView()
    {
        base.InitializeView();
    }
    public virtual void BindModel(BlockModel block, WorkSpaceView workspaceView)
    {
        if (mBlock == block) return;
        if (mBlock != null) UnBindModel();

        mBlock = block;
        m_WorkspaceView = workspaceView;

        if (m_WorkspaceView != null)
        {
            WorkSpaceView.Active.AddBlockView(this);
        }
        else
        {
            Debug.LogError($"BlockView ({BlockType}): BindView called with a NULL WorkSpaceView! Interactions may fail.", this);
        }

        mBlockObserver = new MemorySafeBlockObserver(this);
        mBlock.AddObserver(mBlockObserver);

        int inputIndex = 0;
        foreach (BaseView view in ChildViews)
        {
            if (view.Type == ViewType.Connection)
            {
                ConnectionView conView = view as ConnectionView;
                conView.BindModel(mBlock.GetFirstClassConnection(conView.ConnectionType), this);
            }
            else if (view.Type == ViewType.LineGroup)
            {
                LineGroupView groupView = view as LineGroupView;
                foreach (var inputView in groupView.ChildViews)
                {
                    ((InputView)inputView).BindModel(mBlock.InputList[inputIndex], this);
                    inputIndex++;
                }
            }
        }

        RegisterUIEvents();

    }
   
    
    public void NotifyLayoutDirty()
    {
        if (!m_LayoutIsDirty) 
        {
            // Debug.Log($"BlockView ({gameObject.name}): Marked as dirty.", this);
            m_LayoutIsDirty = true;
        }

    }

    public void UnBindModel()
    {
      if (mBlock == null) return;

        foreach (BaseView view in ChildViews)
        {
            if (view is ConnectionView conView)
            {
                conView.UnBindModel();
            }
            else if (view is LineGroupView groupView)
            {
                foreach (var childOfGroup in groupView.ChildViews)
                {
                     if (childOfGroup is InputView inputView)
                    {
                         inputView.UnBindModel();
                     }
                 }
             }
             else if (view is FieldView fieldView)
             {
                // fieldView.UnBindModel(); 
             }
        }

        WorkSpaceView.Active?.RemoveBlockView(this);
        mBlock.RemoveObserver(mBlockObserver);
        mBlock = null;
    }

    public void Dispose()
    {
        foreach (BaseView view in ChildViews )
        {
            if (view.Type == ViewType.Connection)
            {
                if (((ConnectionView)view).TargetBlockView != null)
                    ((ConnectionView)view).TargetBlockView.Dispose();
            }
            else if (view.Type == ViewType.LineGroup)
            {
                LineGroupView groupView = view as LineGroupView;
                foreach (var inputView in groupView.ChildViews )
                {
                    if (((InputView)inputView).HasConnection && ((InputView)inputView).GetConnectionView().TargetBlockView != null)
                        ((InputView)inputView).GetConnectionView().TargetBlockView.Dispose();
                }
            }
        }

        BlockModel model = mBlock;
        UnBindModel();
        if (this.gameObject != null)
        {
            Destroy(this.gameObject);
        }
        
        model?.Dispose();
    }

    #region UI Update

    public override Vector2 ChildStartXY
    {
        get
        {
            BlockViewSettings settings = BlockViewSettings.Instance;
            if (settings == null)
            {
                Debug.LogError("BlockViewSettings instance is null! Cannot calculate size correctly.");
                return Vector2.one * 50; 
            }
            if (ChildViews [0].Type == ViewType.Connection)
            {
                EConnection conType = ((ConnectionView)ChildViews [0]).ConnectionType;
                switch (conType)
                {
                    case EConnection.OutputValue:
                        return settings.ValueConnectPointRect.position;

                    case EConnection.PrevStatement:
                    case EConnection.NextStatement:
                        return settings.StatementConnectPointRect.position;
                }
            }
            return base.ChildStartXY;
        }
    }

    protected override Vector2 CalculateSize()
    {
        BlockViewSettings settings = BlockViewSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("BlockViewSettings instance is null! Cannot calculate size correctly.");
            return Vector2.one * 50; 
        }
        bool alignRight = false;

        Vector2 size = Vector2.zero;
        for (int i = 0; i < ChildViews .Count; i++)
        {
            LineGroupView groupView = ChildViews [i] as LineGroupView;
            if (groupView != null)
            {
                size.x = Mathf.Max(size.x, groupView.Size.x);
                size.y += groupView.Size.y;
                if (i < ChildViews .Count - 1)
                    size.y += settings.ContentSpace.y;

                if (((InputView)groupView.LastChild).AlignRight)
                    alignRight = true;
            }
        }

        List<Vector4> dimensions = new List<Vector4>();
        for (int i = 0; i < ChildViews .Count; i++)
        {
            LineGroupView groupView = ChildViews [i] as LineGroupView;
            if (groupView != null)
            {
                if (alignRight)
                    groupView.UpdateAlignRight(size.x);

                Vector2 drawSize = groupView.GetDrawSize();
                dimensions.Add(new Vector4(groupView.XY.x, groupView.XY.y - drawSize.y, groupView.XY.x + drawSize.x, groupView.XY.y));
            }
        }

        ((CustomMeshImage)m_BgImages[0]).SetDrawDimensions(dimensions.ToArray());
        return size;
    }

    protected internal override void OnXYUpdated()
    {
        if (InToolbox) return;

        mBlock.XY = XY;
        base.OnXYUpdated();
    }

    protected internal override void OnSizeUpdated()
    {
        ChildViews .ForEach(child =>
        {
            if (child.Type == ViewType.Connection)
            {
                child.OnXYUpdated();
            }
        });
    }

    
    
    
    public void BuildLayout()
    {
        BaseView startView = this.GetLineGroup(0).GetTopmostChild();
        startView.UpdateLayout(startView.HeaderXY);
    }

    
   
    public void AddBgImage(Image image)
    {
        if (image != null && !m_BgImages.Contains(image))
            m_BgImages.Add(image);
    }

   
    public void ChangeBgColor(Color color)
    {
        m_BgImages.RemoveAll(bg => bg == null);
        foreach (Image bg in m_BgImages)
        {
            bg.color = color;
        }
    }

    #endregion

    #region UI Interactions

    private void RegisterUIEvents()
    {
        var mutatorEntry = ViewTransform.Find("Mutator_entry");
        if (mutatorEntry != null)
        {
            mutatorEntry.GetComponent<Button>().onClick.AddListener(() =>
                DialogFactory.CreateMutatorDialog(mBlock)
            );
        }
    }

   
    public void SetOrphan()
    {
        if (InToolbox)
            InToolbox = false;
        ViewTransform.SetParent(WorkSpaceView.Active.CodingArea);
        ViewTransform.SetAsLastSibling();
    }

    private Vector2 mTouchOffset;
    private ConnectionModel mClosestConnection = null;
    private ConnectionModel mAttachingConnection = null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDragging || mBlock == null) 
        {
            eventData.pointerDrag = null; 
            return;
        }

        Debug.Log($"BeginDrag on: {BlockType} (InToolbox: {InToolbox})");

        if (WorkSpaceView.Active == null)
        {
            Debug.LogError("BlockView: WorkSpaceView.Active is null. Cannot begin drag.");
            eventData.pointerDrag = null;
            return;
        }

        mBlock.UnPlug(); 

        if (!InToolbox)
            SetOrphan();
        else
        {
           
            transform.SetParent(WorkSpaceView.Active.CodingArea, true); 
            transform.SetAsLastSibling();
            InToolbox = false; 
        }


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             transform.parent as RectTransform, 
             eventData.position,
             WorkSpaceView.Active.EventCamera, 
             out Vector2 localPointerPos);
        m_dragStartOffset = ViewTransform.anchoredPosition - localPointerPos;

        m_canvasGroup.blocksRaycasts = false; 

        mClosestConnection = null;
        mAttachingConnection  = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        WorkSpaceView activeWorkspaceView = WorkSpaceView.Active;

        if (!IsDragging || activeWorkspaceView == null || mBlock == null) return;
        
        BlockViewSettings settings = BlockViewSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("BlockViewSettings instance is null! Cannot calculate size correctly.");
           
        }

        Vector2 localPointerPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
           transform.parent as RectTransform,
            eventData.position,
            WorkSpaceView.Active.EventCamera,
            out localPointerPos);

        XY = localPointerPos + m_dragStartOffset;

        ConnectionModel oldClosest = mClosestConnection;
        mClosestConnection = null; 
        mAttachingConnection  = null;


        WorkSpaceModel workspaceModel = mBlock.Workspace;
        if (workspaceModel == null)
        {
            Debug.LogError("BlockModel has no associated WorkspaceModel!", this);
            workspaceModel = activeWorkspaceView.Workspace; 
            if (workspaceModel == null) return; 
        }

        float maxSearchRadius = settings.ConnectSearchRange; 

      
        List<ConnectionModel> draggableConnections = mBlock.GetDraggingConnections();

        ConnectionPair bestPairFound = null; 
        float minRadius = int.MaxValue;

        foreach (ConnectionModel myConn in draggableConnections)
        {
            BlockConnectionDB oppositeDB = workspaceModel.GetConnectionDB(myConn.OppositeType); // De ConnectionModel
            if (oppositeDB != null)
            {
                
                Vector2 dragOffset = Vector2.zero;

                oppositeDB.SearchForClosest(myConn, (int)maxSearchRadius, dragOffset, out ConnectionModel currentClosest, out float currentRadius);

                if (currentClosest != null && currentRadius < minRadius)
                {
                    minRadius = currentRadius;
                    bestPairFound = new ConnectionPair(myConn, currentClosest, currentRadius);
                }
            }
        }

        mClosestConnection = bestPairFound?.Neighbour;
        mAttachingConnection = bestPairFound?.Mine;

        if (oldClosest != null && (mClosestConnection == null || oldClosest != mClosestConnection))
        {
            ConnectionView oldClosestView = activeWorkspaceView.GetConnectionView(oldClosest);
            oldClosestView?.Highlight(false);
        }
        if (mClosestConnection != null && mClosestConnection != oldClosest)
        {
            ConnectionView closestView = activeWorkspaceView.GetConnectionView(mClosestConnection);
            closestView?.Highlight(true);
        }

        activeWorkspaceView.CheckTrashBin(this); 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return; 

        m_canvasGroup.blocksRaycasts = true; 

        if (mClosestConnection != null && mAttachingConnection != null)
        {
            
            mClosestConnection.FireUpdate(UpdateState.UnHighlight);
            mAttachingConnection.Connect(mClosestConnection);


            if (!mAttachingConnection.IsConnected)
            {
                Debug.LogWarning($"BlockView {BlockType}: Connection attempt failed between {mAttachingConnection .SourceBlock.Type} and {mClosestConnection.SourceBlock.Type}");
            }

        }
        else
        {
        
            if (mBlock != null && (mBlock.OutputConnection?.IsConnected == true || mBlock.PreviousConnection?.IsConnected == true))
            {
                Debug.LogWarning($"BlockView {BlockType}: EndDrag without connection, but model seems connected. Forcing UnPlug again.");
                mBlock.UnPlug();
            }
        }

        mClosestConnection = null;
        mAttachingConnection  = null;

        // TODO: Finalizar check de Papelera
        WorkSpaceView.Active.Toolbox?.FinishCheckBin(this);
    }



    public void OnPointerClick(PointerEventData eventData)
    {
        //todo: background outline
        /*if (!eventData.dragging && !InToolbox) 
            BlocklyUI.WorkspaceView.CloneBlockView(this, XYInCodingArea + BlockViewSettings.Get().BumpAwayOffset);*/
    }

    #endregion

    #region Block State Update

    
  
    protected void OnBlockUpdated(UpdateStates updateState)
    {
        switch (updateState)
        {
            case UpdateStates.Inputs:
                {
                    BlockViewBuilder.BuildInputViews(mBlock, this);

                    BuildLayout();

                    this.OnXYUpdated();
                    this.ChangeBgColor(m_BgImages[0].color);

                    break;
                }
        }
    }
   

    #endregion

    #region Child View Getter

    
    public ConnectionView GetConnectionView(EConnection connectionType)
    {
        int i = 0;
        while (i < ChildViews .Count)
        {
            ConnectionView view = ChildViews [i] as ConnectionView;
            if (view == null) break;
            if (view.ConnectionType == connectionType)
                return view;
            i++;
        }
        //Debug.LogFormat("<color=red>Can't find the {0} connection view in block view of {1}.</color>", connectionType, BlockType);
        return null;
    }

    
    public ConnectionView GetInputConnectionView(int inputIndex)
    {
        InputView inputView = GetInputView(inputIndex);
        if (inputView != null)
            return inputView.GetConnectionView();
        return null;
    }

    
 
    public LineGroupView GetLineGroup(int logicalIndex)
    {
       
        List<BaseView> children = this.ChildViews;

        Debug.Log($"GetLineGroup({logicalIndex}): Checking BlockView '{this.gameObject.name}' which has {children.Count} children in its ChildViews property.", this.gameObject);

        if (logicalIndex < 0)
        {
            Debug.LogError($"GetLineGroup: Invalid negative index ({logicalIndex}) requested.", this.gameObject);
            return null;
        }

        int currentLogicalIndex = 0;
        foreach (BaseView child in children) 
        {
            if (child == null)
            {
                Debug.LogWarning($"GetLineGroup({logicalIndex}): Found NULL entry in ChildViews! Skipping.", this.gameObject);
                continue; 
            }

            if (child is LineGroupView groupView) 
            {
                Debug.Log($" - Found LineGroupView at physical index (relative to loop): {children.IndexOf(child)}, Logical Index: {currentLogicalIndex}");
                if (currentLogicalIndex == logicalIndex)
                {
                    Debug.Log($"   ---> MATCH FOUND for logical index {logicalIndex}. Returning {groupView.name}");
                    return groupView;
                }
                currentLogicalIndex++;
            }
            else
            {
                Debug.Log($" - Child {child.name} is NOT a LineGroupView (Type: {child.GetType().Name})");
            }
        }
        Debug.LogWarning($"BlockView ({BlockType}): GetLineGroup({logicalIndex}) did NOT find the requested group. Total LineGroups found: {currentLogicalIndex}. Total children: {children.Count}.", this);


        return null;
    }

   
    public InputView GetInputView(int index)
    {
        int inputCounter = 0;
        int groupCounter = 0;
        while (groupCounter < ChildViews .Count)
        {
            LineGroupView view = ChildViews [groupCounter] as LineGroupView;
            groupCounter++;
            if (view == null) continue;

            if (inputCounter + view.ChildViews .Count > index)
                return view.ChildViews [index - inputCounter] as InputView;

            inputCounter += view.ChildViews .Count;
        }
        //Debug.LogFormat("<color=red>Can't find the {0}th input view in block view of {1}.</color>", index, BlockType);
        return null;
    }

    public List<InputView> GetInputViews()
    {
        List<InputView> inputViews = new List<InputView>();
        foreach (BaseView baseChildView in ChildViews)
        {
            if (baseChildView is LineGroupView lineGroupView)
            {
                if (lineGroupView.HasChildren)
                {
                    inputViews.AddRange(lineGroupView.ChildViews
                                                     .Select(v => v as InputView)
                                                     .Where(iv => iv != null));
                }
            }
            else if (baseChildView is InputView directInputView)
            {
                inputViews.Add(directInputView);
            }
        }
        return inputViews;
    }

    

    #endregion

   
    
    public void UpdateColor() 
    {
        ApplyBlockColor();
    }

    
    
    protected virtual void ApplyBlockColor()
    {
        if (mBlock == null) return;

        Color blockColor = Color.grey; 

        if (WorkSpaceView.Active != null && WorkSpaceView.Active.Toolbox != null)
        {
            blockColor = WorkSpaceView.Active.Toolbox.GetColorOfBlock(mBlock.Type);
        }
     

        if (m_PrimaryBackground != null)
        {
            m_PrimaryBackground.color = blockColor;
        }

        else
        {
            Debug.LogWarning($"No primary background image assigned to apply color on {gameObject.name}");
        }
    }

      
    public void QueueForceLayoutUpdate()
    {
        if (this != null && transform is RectTransform rt)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rt);
        }
        else if (this == null)
        {
            //Debug.LogWarning($"QueueForceLayoutUpdate called on a potentially destroyed BlockView.");
        }
        else
        {
            //Debug.LogWarning($"Transform on {gameObject.name} is not a RectTransform?");
        }
    }

   
    public ConnectionView FindConnectionView(ConnectionModel modelToFind)
    {
        if (modelToFind == null)
        {
            Debug.LogWarning($"BlockView ({Block?.Type ?? "Unknown"}): FindConnectionView called with a null model.");
            return null;
        }

        foreach (BaseView childView in ChildViews)
        {
            if (childView is ConnectionView connectionView)
            {
           
                if (connectionView.ConnectionModel == modelToFind) 
                {
                    return connectionView; 
                }
            }
        }

        foreach (BaseView childView in ChildViews)
        {
            if (childView is LineGroupView lineGroupView)
            {
                foreach (var potentialInputView in lineGroupView.ChildViews)
                {
                    if (potentialInputView is InputView inputView)
                    {
                        ConnectionView inputConnectionView = inputView.GetConnectionView(); 
                        if (inputConnectionView != null)
                        {
                            if (inputConnectionView.ConnectionModel == modelToFind) 
                            {
                                return inputConnectionView;
                            }
                        }
                    }
                }
            }
        }

      
        return null; 
    }

     
    public void InitiateDragFromExternal(PointerEventData eventData)
    {
        // Debug.Log($"InitiateDragFromExternal called on: {BlockType}");

        if (mBlock == null || !IsDragging)
        { 
            Debug.LogError($"BlockView ({gameObject.name}): Cannot initiate external drag - No model or not draggable.");
            return;
        }

        WorkSpaceView activeWorkspace = WorkSpaceView.Active;
        if (activeWorkspace == null) return;

        IsDragging = true;

        SetOrphan(); 

        if (m_canvasGroup != null) m_canvasGroup.blocksRaycasts = false;
        if (m_layoutElement != null) m_layoutElement.ignoreLayout = true;

        RectTransform parentRect = activeWorkspace.CodingArea;
        if (ViewTransform != null && parentRect != null) 
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                activeWorkspace.EventCamera,
                out Vector2 localPointerPos);
            m_dragStartOffset = ViewTransform.anchoredPosition - localPointerPos;
        }
        else
        {
            Debug.LogError($"InitiateDragFromExternal ({BlockType}): Failed to get ViewTransform or ParentRect for offset calculation.");
            m_dragStartOffset = Vector2.zero; 
        }

        mClosestConnection = null;
        mAttachingConnection = null;

    }

   
    public void HandleModelUpdate(BlockModel model, BlockUpdateType updateType)
    {
        if (model != mBlock || mBlock == null)
        {
            Debug.LogWarning($"BlockView ({gameObject?.name}): Received HandleModelUpdate for unexpected model or null model. Ignored.", this);
            return;
        }
        // Debug.Log($"BlockView ({mBlock.Type}): Handling update - {updateType}", this); // Opcional para depuración

        switch (updateType)
        {
            case BlockUpdateType.State_Disabled:
                SetVisualStateDisabled(model.Disabled); 
                break;
            case BlockUpdateType.State_Movable:
                this.IsDragging = model.Movable;
                break;
            
            case BlockUpdateType.State_Collapsed:
                SetVisualStateCollapsed(model.Collapsed); 
                MarkDirty();
                break;

            case BlockUpdateType.State_InputsInline:
                m_IsInlineMode = model.GetInputsInline(); 
                MarkDirty();
                break;

            case BlockUpdateType.Structure_Inputs:
                BlockViewBuilder.BuildInputViews(mBlock, this); 
                MarkDirty(); 
                break;
            case BlockUpdateType.Structure_Connections:
                
                MarkDirty();
                break;

            case BlockUpdateType.Value_Field:
             
                MarkDirty(); 
                break;

            case BlockUpdateType.Value_Variable:
               
                MarkDirty(); 
                break;

            case BlockUpdateType.Position_XY:
               
                if (m_ViewTransform != null && !IsDragging) 
                {
                    m_ViewTransform.anchoredPosition = model.XY; 
                }
                break;

            default:
                Debug.LogWarning($"BlockView ({mBlock.Type}): Unhandled update type: {updateType}", this);
                break;
        }
    }
    private void SetVisualStateDisabled(bool isDisabled)
    {
        if (m_canvasGroup == null) m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = isDisabled ? 0.6f : 1.0f; 
        }
    }
    private void SetVisualStateCollapsed(bool isCollapsed)
    {
       
        LineGroupView firstLineGroup = GetLineGroup(0); 

        for (int i = 0; i < ChildViews.Count; i++)
        {
            if (ChildViews[i] is LineGroupView groupView)
            {
                bool shouldBeActive = !isCollapsed || (i == 0); 
                groupView.gameObject.SetActive(shouldBeActive);
            }
        }
    }

  
    public virtual void BindView(BlockModel blockModel) 
    {
        if (mBlock == blockModel && mBlock != null) return; 
        if (blockModel == null)
        {
            Debug.LogError($"BlockView ({gameObject.name}): Attempted to BindView with a NULL model!", this);
            return;
        }
        if (mBlock != null) UnBindModel(); 

        mBlock = blockModel;
        if (WorkSpaceView.Active != null)
        {
            WorkSpaceView.Active.AddBlockView(this);
        }
        else
        {
            Debug.LogError($"BlockView ({BlockType}): Cannot find active WorkSpaceView during BindView!");
        }

        mBlockObserver = new MemorySafeBlockObserver(this); 
        mBlock.AddObserver(mBlockObserver);

        Debug.Log($"BlockView ({BlockType}): Binding Model ID {mBlock.ID}");

        int inputModelIndex = 0;
        foreach (BaseView childView in ChildViews) 
        {
            if (childView is ConnectionView conView)
            {
                ConnectionModel conModel = mBlock.GetFirstClassConnection(conView.ConnectionType);
                if (conModel != null)
                {
                    conView.BindModel(conModel, this); 
                }
                else
                {
                    Debug.LogWarning($"BlockView ({BlockType}): No model for ConnectionView {conView.ConnectionType}");
                }
            }
            else if (childView is LineGroupView groupView)
            {
                foreach (var viewInGroup in groupView.ChildViews)
                {
                    if (viewInGroup is InputView inputView)
                    {
                        if (inputModelIndex < mBlock.InputList.Count)
                        {
                            InputModel inputModel = mBlock.InputList[inputModelIndex];
                            if (inputModel != null)
                            {
                                inputView.BindModel(inputModel, this); 
                            }
                            else { Debug.LogError($"NULL InputModel at index {inputModelIndex}"); }
                            inputModelIndex++;
                        }
                        else
                        {
                            Debug.LogError($"View/Model mismatch: Too many InputViews (index {inputModelIndex}) for model {BlockType}");
                            break;
                        }
                    }
                }
            }
        }


        RegisterUIEvents(); 
        UpdateColor();      
        MarkDirty();        
        QueueForceLayoutUpdate(); 

        Debug.Log($"BlockView ({BlockType}): BindView completed.");
    }

}//fin de la clase BlockView

