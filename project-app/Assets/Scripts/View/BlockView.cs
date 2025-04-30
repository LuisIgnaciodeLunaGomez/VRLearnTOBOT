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
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]

public class BlockView : BaseView, IBeginDragHandler, IDragHandler, IEndDragHandler //TODO: Interesante mirar IPointerClickHandler
{
    [SerializeField] private List<Image> m_BgImages = new List<Image>();
    [Tooltip("Asigna aquí la Imagen principal que muestra el color del bloque")]
    [SerializeField] private Image m_PrimaryBackground;
    private bool m_LayoutIsDirty = false;

    public override ViewType Type => ViewType.Block;

    public string BlockType => mBlock?.Type ?? "NULL_BLOCK_TYPE"; 

    private BlockModel mBlock;
    public BlockModel Block => mBlock;

    private bool m_IsInlineMode = false;
    public bool IsInlineMode => m_IsInlineMode;
    public bool InToolbox { get; set; } = false;
    public bool IsDragging { get; set; } = true;

    private MemorySafeBlockObserver mBlockObserver;
    private CanvasGroup m_canvasGroup;
    private Vector2 m_dragStartOffset;
    private LayoutElement m_layoutElement;
    private WorkSpaceView m_WorkspaceView;

    public WorkSpaceView WorkspaceView => m_WorkspaceView;
    protected override void InitializeView()
    {
        base.InitializeView();
    }

    /// <summary>
    /// Lleva a cabo el proceso de vinculación del modelo lógico con la vista.
    /// <param name="block">Modelo lógico del bloque.</param>
    /// <param name="workspaceView">Vista del espacio de trabajo.</param>
    /// </summary>
    public virtual void BindModel(BlockModel block, WorkSpaceView workspaceView)
    {
        if (mBlock == block && mBlock != null)
        {
            Debug.LogWarning($"BlockView ({BlockType}): BindModel called redundantly with the same model.", this.gameObject);
            return;

        }
        if (block == null)
        {
            Debug.LogError($"BlockView ({gameObject.name}): Attempted to BindModel with a NULL model!", this);
            return;
        }
        if (workspaceView == null)
        {
            Debug.LogError($"BlockView ({block.Type}/{gameObject.name}): Attempted to BindModel with a NULL WorkSpaceView!", this);
            //return;
        }
        if (mBlock != null) UnBindModel();

        mBlock = block;
        m_WorkspaceView = workspaceView;

        // Debug.Log($"BlockView ({BlockType}): Assigning WorkspaceView (InstanceID: {m_WorkspaceView?.GetInstanceID()})", this.gameObject);

        //Debug.Log($"BlockView ({BlockType}): BindModel START. Block Model ID: {block?.ID ?? "NULL"}. Workspace: {block?.Workspace?.Id ?? "NULL"}", this.gameObject);

        // Lógica para distinguir entre una template y un bloque normal 
        bool isTemplate = mBlock?.Workspace == null;

        if (!isTemplate) // no es una platnilla po lo que tiene que tener WS
        {
            Debug.Log($"BlockView ({BlockType}): Binding as REGULAR workspace block view.", this.gameObject);
            WorkSpaceView.Active?.AddBlockView(this); // Añadir a lista activa del workspace
            mBlockObserver = new MemorySafeBlockObserver(this); // Observer solo para modelos de workspace
            mBlock.AddObserver(mBlockObserver);
        }
        else // Si es una plantilla no tiene WS asociado
        {
           // Debug.Log($"BlockView ({BlockType}): Binding as TEMPLATE block view.", this.gameObject);
        }

      //  Debug.Log($"---> BlockView.BindModel ({this.gameObject.name}): About to process ChildViews. Count: {ChildViews?.Count ?? -1}");

        if (ChildViews != null)
        {
            foreach (var v in ChildViews)
            {
                Debug.Log($"    - ChildView Found: Name='{v.gameObject.name}', Type='{v.GetType().Name}'");
            }
        }

        int inputModelIndex = 0;
        // Limpiamos referencias antiguas (por si acaso es un re-bind)
        /*OutputConnectionView = null;
        PreviousConnectionView = null;
        NextConnectionView = null;
        mLineGroupViews.Clear();*/

        foreach (BaseView childView in ChildViews.Where(c => c != null))
        {
            // if (childView.Type == ViewType.Connection)

            //Proceso las conexiones hijas directas - OutputValue, PrevStatemnet y NextStatement
            if (childView is ConnectionView conView && !(conView is ConnectionInputView))
            {
                Debug.Log($"BlockView ({BlockType}): Found direct ConnectionView: {conView.gameObject.name} (Type:{conView.ConnectionType})", this.gameObject);
                //ConnectionView conView = childView as ConnectionView;
                //Obtengo el modelo conexión de BlockModel
                ConnectionModel conModel = mBlock.GetFirstClassConnection(conView.ConnectionType);

                //Verifico el modelo antes de bindear e informa de error solo si falla
                if (conModel != null)
                {
                    Debug.Log($" -> Found matching Model: {ConnectionModel.GetConnectionModelID(conModel)}. Binding...", conView.gameObject);
                    conView.BindModel(conModel, this);
                    /*    // Guarda la referencia específica (Output/Prev/Next)
                        if (conView.ConnectionType == EConnection.OutputValue) OutputConnectionView = conView;
                        else if (conView.ConnectionType == EConnection.PrevStatement) PreviousConnectionView = conView;
                        else if (conView.ConnectionType == EConnection.NextStatement) NextConnectionView = conView;*/
                }
                else
                {
                    // Log de Error solo si se esperaba una conexión que el modelo no tiene
                    Debug.LogError($"BlockView ({BlockType}): Failed to find matching ConnectionModel for direct ConnectionView of type {conView.ConnectionType} on BlockModel '{mBlock.Type}'. Binding view to NULL.", this.gameObject);
                    conView.BindModel(null, this); // Bindea a null si no hay modelo 
                }
            }

            //busco LineGroups

            //  else if (childView.Type == ViewType.LineGroup)
            else if (childView is LineGroupView groupView) //Si ChildeView es un LineGroup
            {
                Debug.Log($"BlockView ({BlockType}): Found LineGroupView: {groupView.gameObject.name}. Binding Inputs within...", groupView.gameObject);

                // mLineGroupViews.Add(groupView);

                for (int i = 0; i < groupView.transform.childCount; i++)
                {
                    Transform lineGroupChildTransform = groupView.transform.GetChild(i);
                    InputView inputViewVisual = lineGroupChildTransform.GetComponent<InputView>();

                    // SI es un InputView hijo DIRECTO del LineGroup
                    if (inputViewVisual != null)
                    {

                        InputModel correspondingInputModel = null;
                        if (inputModelIndex < mBlock.InputList.Count)
                        {
                           // Debug.Log($"--> BlockView mapping Visual Child Index {i} ('{inputViewVisual.gameObject.name}') to Logical Model Index {inputModelIndex}.");

                            correspondingInputModel = mBlock.InputList[inputModelIndex];

                            //Debug.Log($"--> BlockView mapping Visual Child Index {i} ('{inputViewVisual.gameObject.name}') to Logical Model Index {inputModelIndex}.");

                           // Debug.Log($"   -> LineGroup Child {i}: Found InputView '{inputViewVisual.gameObject.name}'. Attempting to bind to InputModel index {inputModelIndex} ('{correspondingInputModel?.Name ?? "NULL"}').");
                            inputViewVisual.BindModel(correspondingInputModel, this); // Delego el bindeo interno a InputView.BindModel
                            //inputModelIndex++; // Incrementar SOLO si se procesa un InputView
                        }
                        else
                        {
                            Debug.LogError($"   -> LineGroup Child {i}: Found InputView '{inputViewVisual.gameObject.name}' but NO corresponding InputModel at index {inputModelIndex} (Model has only {mBlock.InputList.Count} inputs). Binding view to NULL.", inputViewVisual.gameObject);
                            inputViewVisual.BindModel(null, this); // Bindea a null
                        }
                        inputModelIndex++; // Incremento el índice del modelo lógico
                    }
                    // else: Ignoro otros hijos DIRECTOS de LineGroup (e.g., separadores visuales, si los hubiera)
                    else
                    {
                        Debug.Log($"   -> LineGroup Child {i} ('{lineGroupChildTransform.name}'): Not an InputView. Skipping.");
                    }
                }
              

            }

         
           // Debug.Log($"---> BlockView.BindModel ({this.gameObject.name}): Finished processing ChildViews. Processed InputViews: {inputModelIndex}");

            //  Debug.Log($"BlockView ({BlockType}): Finished binding children.");
            if (!isTemplate && mBlock?.InputList != null && mBlock.InputList.Count != mBlock.InputList.Count)
            {
                Debug.LogError($"BlockView ({BlockType}): Unbound InputModels remaining: {mBlock.InputList.Count - inputModelIndex}. Mismatch in View/Model Input count.", this.gameObject);
            }
           // Debug.Log($"BlockView ({BlockType}): Finished binding children. Reviewing hierarchy...", this);
            ReviewBaseViewHierarchy(this, 0); //Depuración recursiva para la jerarquía BaseView dentro de este blockView padre

            RegisterUIEvents();
            UpdateColor();
            MarkDirty();
            QueueForceLayoutUpdate();

           // Debug.Log($"BlockView ({BlockType}): BindModel completed fully.");

        }

    }
    private void RecurseCheckChildBinds(BaseView view)
    {
        if (view == null) return;

        if (view is FieldView fv)
        {
            if (fv.FieldModel == null) Debug.LogError($"Check Bind Fail: FieldView {fv.gameObject.name} has NULL FieldModel", fv.gameObject);
        }
        else if (view is ConnectionInputView civ)
        {
            if (civ.ConnectionModel == null) Debug.LogError($"Check Bind Fail: ConnectionView {civ.gameObject.name} has NULL ConnectionModel", civ.gameObject);
        }
        else if (view is ConnectionView cv && !(view is ConnectionInputView)) // Chequeo para Output/Prev/Next
        {
            if (cv.ConnectionModel == null) Debug.LogError($"Check Bind Fail: ConnectionView {cv.gameObject.name} has NULL ConnectionModel", cv.gameObject);
        }
        else if (view is InputView iv) // Opcional: Chequear si el InputView mismo quedó nulo
        {
            if (iv.InputModel == null) Debug.LogError($"Check Bind Fail: InputView {iv.gameObject.name} has NULL InputModel", iv.gameObject);
        }
        if (view.HasChildren)
        {
            foreach (BaseView child in view.ChildViews.Where(c => c != null))
            {
                RecurseCheckChildBinds(child);
            }
        }
    }

    // Para recorrer la jerarquia lógica de BaseView e imprimirla con el padre visual
    private void ReviewBaseViewHierarchy(BaseView currentView, int indentLevel)
    {
        if (currentView == null) return;

        string indent = new string(' ', indentLevel * 2);
        string viewName = currentView.gameObject?.name ?? "NULL_VIEW_GO";
        string parentViewName = currentView.ParentView?.gameObject?.name ?? "NULL_LOGIC_PARENT";
        string visualParentName = currentView.ViewTransform?.parent?.gameObject?.name ?? "NULL_VISUAL_PARENT_GO";

        //Debug.Log($"{indent} Hierarchy Review: {viewName} (Type:{currentView.Type}, Active:{currentView.gameObject?.activeInHierarchy ?? false}) | Logic Parent: {parentViewName} | Visual Parent: {visualParentName}", currentView.gameObject);


        if (currentView.HasChildren)
        {
            foreach (BaseView child in currentView.ChildViews.Where(c => c != null))
            {
                ReviewBaseViewHierarchy(child, indentLevel + 1); // Llamo recursivamente para hijos logicos
            }
        }
        else if (currentView is BlockView blockViewWithChildren) // Solo para BlockViews que si deberian tener hijos
        {
            if (blockViewWithChildren.Block?.InputList?.Any() == true || blockViewWithChildren.Block?.NextConnection != null)
            {
                Debug.LogWarning($"{indent}   -> BlockView expected children based on BlockModel but HasChildren is false.", currentView.gameObject);
            }
        }
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
        Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Dispose called for {gameObject.name}. Initiating recursive dispose.", this.gameObject);

     
        List<BlockModel> childrenToDispose = new List<BlockModel>();

        // Bloque conectado al Next
        if (mBlock?.NextConnection?.TargetBlock != null)
        {
            childrenToDispose.Add(mBlock.NextConnection.TargetBlock);
        }

        // Bloques conectados a los Inputs (Value/Statement)
        if (mBlock?.InputList != null)
        {
            foreach (InputModel input in mBlock.InputList)
            {
                if (input?.Connection?.TargetBlock != null)
                {
                    childrenToDispose.Add(input.Connection.TargetBlock);
                }
            }
        }

        Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Found {childrenToDispose.Count} logical children to dispose.", this.gameObject);

        
        foreach (BlockModel childModel in childrenToDispose)
        {
            if (childModel != null)
            {
                BlockView childView = m_WorkspaceView?.GetBlockView(childModel);
                if (childView != null)
                {
                    Debug.Log($"  -> Recursively disposing child view: {childView.gameObject.name}", childView.gameObject);
                    childView.Dispose(); // La llamada recursiva se encarga de desbindar y destruir
                }
                else
                {
                    // Si no hay vista, al menos disponer del modelo lógico
                    Debug.Log($"  -> Disposing child model '{childModel.ID}' (no view found).");
                    childModel.Dispose();
                }
            }
        }


        //  Desvincular este bloque y destruir su GameObject
        Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Unbinding self ({gameObject.name}).", this.gameObject);
        BlockModel model = mBlock; // Guardar referencia al modelo para disponer al final
        UnBindModel(); // Desconectar observadores, quitar de WorkspaceView.mBlockViews

        if (this.gameObject != null)
        {
            Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Destroying GameObject {gameObject.name}.", this.gameObject);
            Destroy(this.gameObject); // Destruir el GameObject de esta vista
        }

        //limpieza final
        if (model != null)
        {
            Debug.Log($"BlockView ({model.Type}): Disposing BlockModel {model.ID}.", this.gameObject);
            model.Dispose(false); //false evita re-intentar destruir la vista
                                  
        }
        else
        {
            Debug.Log($"BlockView (Unknown Type): Dispose finished for view {gameObject?.name}, model was already null.", this.gameObject);
        }
        Debug.Log($"BlockView ({model?.Type ?? "Disposed"}): Dispose method finished for {gameObject?.name}.", this.gameObject);
      
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
     //   Debug.Log($"BlockView::OnXYUpdated calling base.OnXYUpdated().", this.gameObject);
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
        if (mBlock == null || !m_LayoutIsDirty) return;

        Debug.Log($"BlockView ({BlockType}): ---> Building Layout <---", this);

        if (gameObject.activeInHierarchy)

        {
            ManualLayoutRecursive(this.XY);
            
        }
        m_LayoutIsDirty = false;
        //BaseView startView = this.GetLineGroup(0).GetTopmostChild();
       // startView.UpdateLayout(startView.HeaderXY);
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

        if (!InToolbox && BlockDragController.Instance != null && this.Block != null && this.Block.Movable)
        {
            BlockDragController.Instance.StartDraggingBlockInternal(this, eventData);
        }
        else
        {
            eventData.pointerDrag = null; // No permitiremos drag si está en toolbox o no es movible
        }
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (BlockDragController.Instance != null && BlockDragController.Instance.IsDraggingBlock(this.Block))

        {        //if (!InToolbox) // Delegar si NO está en el toolbox
            BlockDragController.Instance?.HandleDrag(/*this,*/ eventData);
        }
   
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (BlockDragController.Instance != null && BlockDragController.Instance.IsDraggingBlock(this.Block))
        {
            //if (!InToolbox) // Delegar si NO está en el toolbox
            BlockDragController.Instance?.HandleEndDrag(/*this,*/ eventData);
        }
        /*else
        {
            Debug.Log($"BlockView ({BlockType}): OnEndDrag called but BlockDragController is null or not dragging this block.");
        }
        */
        else if (eventData.pointerDrag == this.gameObject && BlockDragController.Instance != null) // Fallback check si no pasaste por OnBeginDrag correcto
        {
            Debug.LogWarning($"BlockView.OnEndDrag: Controller's WasDraggingBlock returned false, but UGUI pointerDrag is this object. Forcing HandleEndDrag.", this.gameObject);
            // if (!InToolbox) // Delegar si NO está en el toolbox
            BlockDragController.Instance?.HandleEndDrag(/*this,*/ eventData);

        } }

    public void OnPointerClick(PointerEventData eventData)
    {
        //todo: background outline
        /*if (!eventData.dragging && !InToolbox) 
            BlocklyUI.WorkspaceView.CloneBlockView(this, XYInCodingArea + BlockViewSettings.Get().BumpAwayOffset);*/
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
           // Debug.LogWarning($"No primary background image assigned to apply color on {gameObject.name}");
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
              //  BlockViewBuilder.BuildInputViews(mBlock, this); 
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
   
    public static void EditorInitialDisplayUpdate(FieldView fieldView, FieldModel fieldModel, string errorText = null)
    {
        if (fieldView == null || fieldModel == null) return;

        string displayText = errorText ?? fieldModel.GetText() ?? $"[{fieldModel.GetType().Name}]";

        if (fieldView is FieldLabelView labelView)
        {
            labelView.SetDisplayText(displayText);
        }
        else if (fieldView is FieldInputView inputView)
        {
            inputView.SetDisplayText(displayText); 
        }
        else if (fieldView is FieldVariableView varView)
        {
            varView.SetDisplayText(displayText); 
        }
        else if (fieldView is FieldCheckboxView checkView)
        {
            
        }
        else
        {
            var textComp = fieldView.GetComponentInChildren<Text>();
            if (textComp != null) textComp.text = displayText;
            else
            {
                var tmpComp = fieldView.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpComp != null) tmpComp.text = displayText;
            }
        }
    }
}//fin de la clase BlockView

