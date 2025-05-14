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
 * Versión: 2.0.3
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

    [Tooltip("Asigna aquí el GameObject (con RectTransform) que actúa como padre visual de los bloques conectados al NextStatement.")]
    [SerializeField] private RectTransform m_nextStatementContainer;
    private BlockModel mBlock;
    public BlockModel Block => mBlock;

    private bool m_IsInlineMode = false;
    public bool IsInlineMode => m_IsInlineMode;
    public bool InToolbox { get; set; } = false;
    public bool IsDragging { get; set; } = true;

    //private Vector2 m_CalculatedContentSize;

    private MemorySafeBlockObserver mBlockObserver;
    private CanvasGroup m_canvasGroup;
    private Vector2 m_dragStartOffset;
    private LayoutElement m_layoutElement;
    private WorkSpaceView m_WorkspaceView;

    private RectTransform m_RectTransform;

    public RectTransform GetRectTransform() 
    {
        if (m_RectTransform == null)
        {
            Debug.LogWarning($"BlockView ({gameObject.name}): GetRectTransform() found m_RectTransform as null. Attempting to GetComponent.", this.gameObject);
            m_RectTransform = GetComponent<RectTransform>();
            if (m_RectTransform == null)
            {
                Debug.LogError($"BlockView ({gameObject.name}): CRITICAL - GetRectTransform() failed to find RectTransform after GetComponent attempt!", this.gameObject);
            }
        }
        return m_RectTransform;
    }

    public WorkSpaceView WorkspaceView => m_WorkspaceView;
    protected override void InitializeView()
    {
        base.InitializeView();

        m_canvasGroup = GetComponent<CanvasGroup>(); 
        m_layoutElement = GetComponent<LayoutElement>();
        m_RectTransform = GetComponent<RectTransform>();

        if (m_PrimaryBackground != null && !m_BgImages.Contains(m_PrimaryBackground))
        {
            m_BgImages.Insert(0, m_PrimaryBackground);
        }
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
       m_WorkspaceView = this.InToolbox ? null : workspaceView;

        // Debug.Log($"BlockView ({BlockType}): Assigning WorkspaceView (InstanceID: {m_WorkspaceView?.GetInstanceID()})", this.gameObject);

        //Debug.Log($"BlockView ({BlockType}): BindModel START. Block Model ID: {block?.ID ?? "NULL"}. Workspace: {block?.Workspace?.Id ?? "NULL"}", this.gameObject);

        // Lógica para distinguir entre una template y un bloque normal 
        bool isTemplate = mBlock?.Workspace == null;

        if (!isTemplate) // no es una platnilla po lo que tiene que tener WS
        {
           // Debug.Log($"BlockView ({BlockType}): Binding as REGULAR workspace block view.", this.gameObject);
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
               // Debug.Log($"    - ChildView Found: Name='{v.gameObject.name}', Type='{v.GetType().Name}'");
            }
        }

        int inputModelIndex = 0;

        foreach (BaseView childView in ChildViews.Where(c => c != null))
        {
            // if (childView.Type == ViewType.Connection)

            //Proceso las conexiones hijas directas - OutputValue, PrevStatemnet y NextStatement
            if (childView is ConnectionView conView && !(conView is ConnectionInputView))
            {
              //  Debug.Log($"BlockView ({BlockType}): Found direct ConnectionView: {conView.gameObject.name} (Type:{conView.ConnectionType})", this.gameObject);
                //ConnectionView conView = childView as ConnectionView;
                //Obtengo el modelo conexión de BlockModel
                ConnectionModel conModel = mBlock.GetFirstClassConnection(conView.ConnectionType);

                //Verifico el modelo antes de bindear e informa de error solo si falla
                if (conModel != null)
                {
                   // Debug.Log($" -> Found matching Model: {ConnectionModel.GetConnectionModelID(conModel)}. Binding...", conView.gameObject);
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
               // Debug.Log($"BlockView ({BlockType}): Found LineGroupView: {groupView.gameObject.name}. Binding Inputs within...", groupView.gameObject);

                // mLineGroupViews.Add(groupView);

                for (int i = 0; i < groupView.transform.childCount; i++)
                {
                   // Transform lineGroupChildTransform = groupView.transform.GetChild(i);
                   // InputView inputViewVisual = lineGroupChildTransform.GetComponent<InputView>();
                    InputView inputViewVisual = groupView.transform.GetChild(i).GetComponent<InputView>();

                    // SI es un InputView hijo DIRECTO del LineGroup
                    if (inputViewVisual != null)
                    {

                        InputModel correspondingInputModel = null;
                        if (inputModelIndex < mBlock.InputList.Count)
                        {
                            // Debug.Log($"--> BlockView mapping Visual Child Index {i} ('{inputViewVisual.gameObject.name}') to Logical Model Index {inputModelIndex}.");

                            //  correspondingInputModel = mBlock.InputList[inputModelIndex];

                            //Debug.Log($"--> BlockView mapping Visual Child Index {i} ('{inputViewVisual.gameObject.name}') to Logical Model Index {inputModelIndex}.");

                            // Debug.Log($"   -> LineGroup Child {i}: Found InputView '{inputViewVisual.gameObject.name}'. Attempting to bind to InputModel index {inputModelIndex} ('{correspondingInputModel?.Name ?? "NULL"}').");
                            // inputViewVisual.BindModel(correspondingInputModel, this); // Delego el bindeo interno a InputView.BindModel
                            //inputModelIndex++; // Incrementar SOLO si se procesa un InputView

                            inputViewVisual.BindModel(mBlock.InputList[inputModelIndex], this);
                        }
                        else
                        {
                            Debug.LogError($"BlockView({BlockType}): InputModel index mismatch for InputView '{inputViewVisual.name}'. Expected index {inputModelIndex} out of bounds ({mBlock.InputList.Count} inputs).");
                            //    Debug.LogError($"   -> LineGroup Child {i}: Found InputView '{inputViewVisual.gameObject.name}' but NO corresponding InputModel at index {inputModelIndex} (Model has only {mBlock.InputList.Count} inputs). Binding view to NULL.", inputViewVisual.gameObject);
                            inputViewVisual.BindModel(null, this); // Bindea a null
                        }
                        inputModelIndex++; // Incremento el índice del modelo lógico
                    }
                    // else: Ignoro otros hijos DIRECTOS de LineGroup (e.g., separadores visuales, si los hubiera)
                    /*else
                    {
                        Debug.Log($"   -> LineGroup Child {i} ('{lineGroupChildTransform.name}'): Not an InputView. Skipping.");
                    }*/
                }
              
            }
         
           // Debug.Log($"---> BlockView.BindModel ({this.gameObject.name}): Finished processing ChildViews. Processed InputViews: {inputModelIndex}");

            //  Debug.Log($"BlockView ({BlockType}): Finished binding children.");
           /* if (!isTemplate && mBlock?.InputList != null && mBlock.InputList.Count != mBlock.InputList.Count)
            {
                Debug.LogError($"BlockView ({BlockType}): Unbound InputModels remaining: {mBlock.InputList.Count - inputModelIndex}. Mismatch in View/Model Input count.", this.gameObject);
            }*/
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
        if (mBlockObserver != null)
        {
            mBlock.RemoveObserver(mBlockObserver);

            mBlockObserver=null;
                
        }

        mBlock = null;
        m_WorkspaceView = null;
        m_LayoutIsDirty = false; // Resetear estado
    }

    public void Dispose()
    {
        //Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Dispose called for {gameObject.name}. Initiating recursive dispose.", this.gameObject);
        string logPrefix = $"[BV.Dispose '{gameObject?.name}' ({mBlock?.ID ?? "NO_MODEL"})]";

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
                   
                    Debug.Log($"  -> Disposing child model '{childModel.ID}' (no view found).");
                    childModel.Dispose();
                }
            }
        }

        //  Desvincular este bloque y destruir su GameObject
       // Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Unbinding self ({gameObject.name}).", this.gameObject);
        BlockModel model = mBlock;

        Debug.Log($"{logPrefix} Unbinding self first.");
        UnBindModel(); 

        if (this.gameObject != null)
        {
            Debug.Log($"BlockView ({mBlock?.Type ?? "Disposing..."}): Destroying GameObject {gameObject.name}.", this.gameObject);
            Destroy(this.gameObject); 
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
            
            return BlockViewSettings.Instance?.StatementConnectPointRect.position ?? Vector2.zero;

        }
    }

    protected override Vector2 CalculateSize()
    {
        string logPrefix = $"[BV.CalculateSize '{gameObject.name}']";
        if (m_RectTransform == null) { /* LogError y devolver tamaño default */ return BlockViewSettings.Instance?.DefaultBlockSize ?? Vector2.one * 50; }
        BlockViewSettings settings = BlockViewSettings.Instance;
        if (settings == null) { /* LogError */ return Vector2.one * 50; }

        Vector2 contentSize = Vector2.zero; // Tamaño interno (Fields, Inputs sin bloques)
        float maxHeight = 0f;
        float currentY = 0f; // Trackea la posición Y mientras calcula

        // Muesca Superior (Prev)
        currentY -= settings.InternalPadding.y; // Padding superior
        bool hasPrev = mBlock?.PreviousConnection != null;
        if (hasPrev)
        {
            currentY -= settings.NotchHeight;
            // El Ancho lo dictan los LineGroups, la muesca solo añade altura
        }

        // Contenido Interno (LineGroups)
        float maxLineWidth = 0f;
        float totalLineGroupHeight = 0f;
        int lineGroupIndex = 0;

        // Importante: Primero asegurar que todos los hijos (LineGroup, Input, Field) han calculado su propio tamaño
        foreach (var child in ChildViews.OfType<BaseView>())
        { // Usar ChildViews de BaseView
            if (child is LineGroupView lgv)
            {
                // Forzar que calcule su tamaño interno y el de sus hijos
                // Este UpdateLayout debe calcular el .Size de LineGroupView correctamente
                lgv.UpdateLayout(Vector2.zero);
                maxLineWidth = Mathf.Max(maxLineWidth, lgv.Size.x);
                totalLineGroupHeight += lgv.Size.y;
                if (lineGroupIndex > 0) totalLineGroupHeight += settings.ContentSpace.y;
                lineGroupIndex++;
            }
            else if (child is InputView iv)
            { // Podría haber Inputs directos?
                iv.UpdateLayout(Vector2.zero); // Asegurar que el input sabe su tamaño
                maxLineWidth = Mathf.Max(maxLineWidth, iv.Size.x);
                totalLineGroupHeight += iv.Size.y; // Añadir si es hijo directo
                // Falta espaciado aquí si hay mezcla... el modelo debería ser consistente.
            }
            // Añadir más tipos si son hijos directos que contribuyen al tamaño (raro)
        }
        contentSize.x = maxLineWidth;
        contentSize.y = totalLineGroupHeight;
        currentY -= contentSize.y; // Restar altura del contenido

        // Debug.Log($"{logPrefix} Internal Content Size Calculated: W={contentSize.x:F2}, H={contentSize.y:F2}");

        // Pestaña Inferior (Next)
        currentY -= settings.InternalPadding.y; // Padding inferior
        bool hasNext = mBlock?.NextConnection != null;
        if (hasNext)
        {
            currentY -= settings.TabHeight;
            // El Ancho se ajusta si es necesario, pero no por el TAB en sí
        }

        // Calcular Tamaño Final (con Paddings y Mínimos)
        float finalWidth = contentSize.x + settings.InternalPadding.x + settings.InternalPadding.x; // Paddings laterales
        // Ver si necesita ser más ancho por la indentación del bloque SIGUIENTE (si está conectado Y TENEMOS CONTENEDOR)
        if (hasNext && mBlock.NextConnection.IsConnected && m_nextStatementContainer != null)
        {
            // Obtener el ancho PREFERIDO del contenedor (que tiene los bloques siguientes)
            float nextStackWidth = LayoutUtility.GetPreferredWidth(m_nextStatementContainer);
            // Si GetPreferredWidth no funciona, intentar con sizeDelta si está actualizado
            if (nextStackWidth <= 0) nextStackWidth = m_nextStatementContainer.sizeDelta.x;

            float requiredWidthForNext = settings.StatementIndent + nextStackWidth + settings.InternalPadding.x; // Indent + Stack + Padding Derecho
            finalWidth = Mathf.Max(finalWidth, requiredWidthForNext);
            // Debug.Log($"{logPrefix} Width adjustment for Next Stack. Child Container Width: {nextStackWidth:F2}. Required Width: {requiredWidthForNext:F2}. Final Width: {finalWidth:F2}");
        }

        float finalHeight = Mathf.Abs(currentY); // Altura total es la Y negativa final

        // Aplicar Mínimos
        finalWidth = Mathf.Max(finalWidth, settings.MinBlockWidth);
        finalHeight = Mathf.Max(finalHeight, settings.MinBlockHeight);

        Vector2 finalCalculatedSize = new Vector2(finalWidth, finalHeight);
        // Debug.Log($"{logPrefix} FINAL Calculated Size = {finalCalculatedSize.ToString("F2")}");
        return finalCalculatedSize;
    }


    public override void UpdateLayout(Vector2 position = default) // Permite que BaseView controle XY y Size
    {
        Vector2 previousSize = this.Size;
         string logPrefix = $"[BV.UpdateLayout '{gameObject.name}']";
         Debug.Log($"{logPrefix} ENTRY. Current XY: {this.XY.ToString("F2")}, Size: {this.Size.ToString("F2")}");

        // Llama a la implementación de BaseView que calculará el tamaño (llamando a nuestro CalculateSize override)
        // y asignará XY y Size internamente.
        base.UpdateLayout(position);

        // Ahora this.Size debería tener el tamaño calculado correcto.
        // Aplícalo al RectTransform.
        if (m_RectTransform != null)
        {
            if (m_RectTransform.sizeDelta != this.Size)
            {
                //  Debug.Log($"{logPrefix} Applying Size {this.Size.ToString("F2")} to RectTransform sizeDelta.");
                m_RectTransform.sizeDelta = this.Size;
            }
        }
        else { /* Log Error */ }

        // Forzar actualización de hijos o Layout Groups INTERNOS
        ForceInternalLayoutUpdate();

        if (this.Size != previousSize && ViewTransform.parent != null) // PreviousSize debe ser guardado 
        {
            LayoutRebuilder.MarkLayoutForRebuild(ViewTransform.parent as RectTransform);
            // Debug.Log($"{logPrefix} Size changed. Marked parent '{ViewTransform.parent.name}' for rebuild.");
        }

        // Debug.Log($"{logPrefix} EXIT. Final XY: {this.XY.ToString("F2")}, Size: {this.Size.ToString("F2")}");
        m_LayoutIsDirty = false; // Marcar como limpio después de actualizar
    }

    // OnXYUpdated (Mantener, llama a base)
    protected internal override void OnXYUpdated()
    {
        if (InToolbox) return;

        // Actualizar modelo si NO esta siendo arrastrados por el usuario
        if (mBlock != null /* && !IsCurrentlyBeingDraggedByUser() */ )

        { // <-- Necesitas una forma de saber si este XY viene de un drag o de un layout
            if (mBlock.XY != this.XY)
            {
                // Debug.Log($"[BV.OnXYUpdated '{gameObject.name}'] Model XY was {mBlock.XY.ToString("F2")}, View XY is {this.XY.ToString("F2")}. Updating Model.");
              //  mBlock.XY = this.XY;
            }
        }
        //base.OnXYUpdated(); // Llama a la lógica de BaseView (actualizar DBs de conexión)
    

        if (mBlock != null && !IsDragging) // Si se está arrastrando, BlockDragController lo maneja
        {
            if (Mathf.Abs(mBlock.XY.x - XY.x) > 0.01f || Mathf.Abs(mBlock.XY.y - XY.y) > 0.01f)
            {
                // Debug.Log($"[BV.OnXYUpdated MODEL UPDATE for '{gameObject.name}'] From View XY:{XY} To Model OldXY:{mBlock.XY}. ISDRAGGING: {IsDragging}");
                mBlock.XY = XY;
            }
        }

        // PROPAGAR A LAS CONNECTION VIEWS HIJAS para que actualicen la DB.
        // Esto debe suceder incluso si el bloque es arrastrado por el usuario.
        if (ChildViews != null) // Asegurarse que ChildViews está disponible.
        {
            foreach (var child in ChildViews.OfType<ConnectionView>()) // Solo para conexiones directas o todas
            {
                
                if (child.gameObject.activeInHierarchy && child.ConnectionModel != null && !child.SourceBlockView.InToolbox)
                {
                    // Debug.Log($"    [BV.OnXYUpdated->ChildCV] Calling OnXYUpdated for ConnectionView '{child.gameObject.name}' of block '{this.gameObject.name}'.");
                    child.OnXYUpdated(); // <<< Esto actualiza el ConnectionModel.Location y su posición en ConnectionDB
                }
            }
            // Si también hay LineGroups que contienen ConnectionViews (ConnectionInputView)
            foreach (var lineGroup in ChildViews.OfType<LineGroupView>())
            {
                foreach (var inputView in lineGroup.ChildViews.OfType<InputView>())
                {
                    ConnectionInputView connInputView = inputView.GetConnectionView();
                    if (connInputView != null && connInputView.gameObject.activeInHierarchy && connInputView.ConnectionModel != null && !connInputView.SourceBlockView.InToolbox)
                    {
                        // Debug.Log($"    [BV.OnXYUpdated->ChildInputCV] Calling OnXYUpdated for ConnectionInputView '{connInputView.gameObject.name}' of block '{this.gameObject.name}'.");
                        connInputView.OnXYUpdated();
                    }
                }
            }
        }
        // Debug.Log($"BaseView.OnXYUpdated calling base (original): {gameObject.name}"); // No veo un base.OnXYUpdated en tu BaseView original
        // Llama a OnXYUpdated de los hijos directos de BaseView si fuera necesario (ya lo hace ChildViews.ForEach de uBlockly).
        // Lo importante es que LAS CONEXIONES de ESTE bloque actualicen su posición en la DB.
    }

    protected internal override void OnSizeUpdated()
    {
        base.OnSizeUpdated(); // Llamar a Base por si hace algo
                              // Asegurar que las posiciones de las conexiones se actualizan si el tamaño cambia
        foreach (var connectionView in ChildViews.OfType<ConnectionView>())
        {
            connectionView.OnXYUpdated();
        }
        if (m_nextStatementContainer != null)
        { // Si el contenedor está separado
            LayoutRebuilder.MarkLayoutForRebuild(m_nextStatementContainer);
        }
    }

    // MarkDirty (Helper, mantener)
    public override void MarkDirty()
    {
        if (!m_LayoutIsDirty && gameObject.activeInHierarchy) // Solo marcar si está activo
        {
            // Debug.Log($"[BV.MarkDirty '{gameObject.name}']");
            m_LayoutIsDirty = true;
            
            QueueForceLayoutUpdate();
        }
    }

    /// <summary>
    /// Busca y retorna el RectTransform que actúa como contenedor visual
    /// para los bloques conectados al NextStatement de este bloque.
    /// Idealmente, asignado vía Inspector.
    /// </summary>
    public RectTransform GetNextStatementContainerTransform()
    {
        string logPrefix = $"[BV.GetNextStmtContainer '{gameObject.name}']";

        if (m_nextStatementContainer == null)
        {
            Logger.LogError($"{logPrefix} CRITICAL: m_nextStatementContainer (SerializedField) is NULL for BlockView '{BlockType}'. Check prefab assignment in Inspector. Attempting fallback...", this.gameObject);

            // Fallback 1: Intentar obtener la ConnectionView de NextStatement y su RectTransform
            ConnectionView nextConnView = GetConnectionView(EConnection.NextStatement); // Tu método existente
            if (nextConnView != null)
            {
                RectTransform connViewRect = nextConnView.GetRectTransform(); // Asumiendo que GetRectTransform() de ConnectionView devuelve RectTransform
                if (connViewRect != null)
                {
                    Logger.LogWarning($"{logPrefix} Fallback 1: Using RectTransform of NextConnectionView '{nextConnView.gameObject.name}'. This is NOT ideal.", this.gameObject);
                    return connViewRect;
                }
                else
                {
                    Logger.LogWarning($"{logPrefix} Fallback 1 FAILED: NextConnectionView '{nextConnView.gameObject.name}' does not have a valid RectTransform.", this.gameObject);
                }
            }
            else
            {
                Logger.LogWarning($"{logPrefix} Fallback 1 FAILED: No NextConnectionView found for fallback.", this.gameObject);
            }

            // Fallback 2: Usar el propio RectTransform del BlockView (aún menos ideal, pero es un RectTransform)
            RectTransform selfRectTransform = GetRectTransform(); // Tu método GetRectTransform() de BlockView
            if (selfRectTransform != null)
            {
                Logger.LogWarning($"{logPrefix} Fallback 2: Using self RectTransform '{selfRectTransform.name}'. This is LIKELY INCORRECT for layout.", this.gameObject);
                return selfRectTransform;
            }

            // Fallback Final: Si NADA funcionó, devuelve null e imprime error crítico.
            Logger.LogError($"{logPrefix} Fallback FINAL FAILED: Cannot obtain any valid RectTransform. Returning NULL.", this.gameObject);
            return null;
        }

       
        // Logger.Log($"{logPrefix} Returning assigned m_nextStatementContainer: {m_nextStatementContainer.name}", this.gameObject);
        return m_nextStatementContainer;
    }
    /// <summary>
    /// Función auxiliar para forzar el recálculo del layout de los elementos internos.
    /// Puede ser simplemente marcar este RectTransform para rebuild, o algo más específico si gestionas layout manualmente.
    /// </summary>
    protected virtual void ForceInternalLayoutUpdate()
    {
        if (m_RectTransform != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
            
            if (m_nextStatementContainer != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(m_nextStatementContainer);
            }
            foreach (var lineGroup in ChildViews.OfType<LineGroupView>())
            {
                LayoutRebuilder.MarkLayoutForRebuild(lineGroup.GetRectTransform());
            }
        }
    }


    protected /*override*/ Vector2 _CalculateSize()
    {
        if (m_RectTransform == null)
        {
            Debug.LogError($"BlockView ({BlockType}) CalculateSize: m_RectTransform is NULL at the beginning! Re-assigning.", gameObject);
            m_RectTransform = GetComponent<RectTransform>();
            if (m_RectTransform == null)
            {
                Debug.LogError($"BlockView ({BlockType}) CalculateSize: FAILED to re-assign m_RectTransform. Returning default size.", gameObject);
                return BlockViewSettings.Instance?.DefaultBlockSize ?? new Vector2(100, 30);
            }
        }
        BlockViewSettings settings = BlockViewSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("BlockViewSettings instance is null! Cannot calculate size correctly.");
            return Vector2.one * 50;
        }

        //  CALCULAR EL TAMAÑO DEL CONTENIDO INTERNO DE ESTE BLOQUE (LineGroups)
        Vector2 currentBlockDirectContentSize = Vector2.zero; // Tamaño solo de los LineGroups directos
        bool alignRight = false;
        float accumulatedLineGroupHeight = 0f;
        float maxLineGroupWidth = 0f;

        // actualiza el layout de todos los LineGroupView hijos para que sus .Size sean correctos
        foreach (BaseView childView in ChildViews)
        {
            if (childView is LineGroupView groupView)
            {
                // Suponiendo que UpdateLayout establece groupView.XY y calcula su tamaño
                groupView.UpdateLayout(Vector2.zero); // El offset real se aplicará al generar dimensions
            }
        }

        // Ahora suma los tamaños de los LineGroup
        int lineGroupCount = 0;
        foreach (BaseView childView in ChildViews)
        {
            if (childView is LineGroupView groupView)
            {
                maxLineGroupWidth = Mathf.Max(maxLineGroupWidth, groupView.Size.x);
                accumulatedLineGroupHeight += groupView.Size.y;
                lineGroupCount++;
                if (lineGroupCount > 1) // Si no es el primer LineGroup, añade espacio
                {
                    accumulatedLineGroupHeight += settings.ContentSpace.y;
                }

                InputView lastInputView = groupView.LastChild as InputView;
                if (lastInputView != null && lastInputView.AlignRight)
                {
                    alignRight = true;
                }
            }
        }
        currentBlockDirectContentSize.x = maxLineGroupWidth;
        currentBlockDirectContentSize.y = accumulatedLineGroupHeight;

        Debug.Log($"BlockView ({BlockType}) - Initial Direct Content Size: {currentBlockDirectContentSize}");


        //  PASO 2: GESTIONAR EL NEXTCONNECTION 
        ConnectionModel nextConnectionModel = mBlock?.NextConnection;
        BlockView nextBlockActualView = null; // El BlockView del bloque SIGUIENTE si está conectado
        float heightOfFollowingStack = 0f;  // Altura de TODA la pila que cuelga del nextBlockActualView
        float widthOfFollowingStack = 0f;   // Ancho MÁXIMO de la pila que cuelga del nextBlockActualView (para ajustar el padre)
        bool hasNextConnectionDefined = nextConnectionModel != null; // Si este bloque TIENE un conector Next (aunque no esté conectado)
        bool isNextActuallyConnected = hasNextConnectionDefined && nextConnectionModel.IsConnected && nextConnectionModel.TargetBlock != null;

        if (isNextActuallyConnected)
        {
            Debug.Log($"BlockView ({BlockType}) - NextConnection IS CONNECTED. Getting next block's view and size...");
            ConnectionView localNextConnView = GetConnectionView(EConnection.NextStatement);
            if (localNextConnView != null)
            {
                nextBlockActualView = localNextConnView.TargetBlockView;
            }

            if (nextBlockActualView != null)
            {
                if (nextBlockActualView.GetRectTransform() != null)
                {
                    // Forzar que el bloque siguiente y toda su pila calculen su tamaño.
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(nextBlockActualView.GetRectTransform());
                    heightOfFollowingStack = nextBlockActualView.Size.y; 
                    widthOfFollowingStack = nextBlockActualView.Size.x;  
                    Debug.Log($"  -> Next Block View '{nextBlockActualView.name}' Size AFTER ForceRebuild: ({widthOfFollowingStack:F2}, {heightOfFollowingStack:F2})");
                }
                else
                {
                    Debug.LogError($"BlockView ({BlockType}): Next block '{nextBlockActualView.name}' has null RectTransform!", nextBlockActualView.gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"BlockView ({BlockType}): NextConnection Model is connected, but TargetBlockView not found in its ConnectionView.", this.gameObject);
            }
        }

        //  PASO 3: CALCULAR EL TAMAÑO TOTAL DE ESTE BLOQUE ---
        Vector2 finalCalculatedSize = Vector2.zero;

        //  ancho del contenido directo de este bloque
        finalCalculatedSize.x = currentBlockDirectContentSize.x;
        //  altura del contenido directo de este bloque
        finalCalculatedSize.y = currentBlockDirectContentSize.y;

        // padding interno general
        finalCalculatedSize.x += settings.InternalPadding.x + settings.InternalPadding.x;
        finalCalculatedSize.y += settings.InternalPadding.y + settings.InternalPadding.y;

        // Si hay un NextConnection (conectado o no), la forma del bloque lo incluye (pestaña/muesca).
        if (hasNextConnectionDefined)
        {
            // La pestaña "C" (o muesca para el prev) del propio bloque añade a su altura.
            finalCalculatedSize.y += settings.TabHeight; 
            Debug.Log($"  Added TabHeight ({settings.TabHeight}) for NextConnection visual shape. Y is now {finalCalculatedSize.y}");

            if (isNextActuallyConnected && heightOfFollowingStack > 0)
            {
                // Añadir la altura de la pila que le sigue
                finalCalculatedSize.y += heightOfFollowingStack;
                Debug.Log($"  Added HeightOfFollowingStack ({heightOfFollowingStack}). Y is now {finalCalculatedSize.y}");

                // Asegurar que el ancho de este bloque acomode el bloque siguiente si está indentado
                float requiredWidthForNext = widthOfFollowingStack + settings.StatementIndent + settings.InternalPadding.x; // Ancho del hijo + indent + padding derecho de este bloque
                finalCalculatedSize.x = Mathf.Max(finalCalculatedSize.x, requiredWidthForNext);
                Debug.Log($"  Adjusted width for next stack. X is now {finalCalculatedSize.x} (required: {requiredWidthForNext})");
            }
        }

        // Asegurar tamaño mínimo
        finalCalculatedSize.x = Mathf.Max(finalCalculatedSize.x, settings.MinBlockWidth);
        finalCalculatedSize.y = Mathf.Max(finalCalculatedSize.y, settings.MinBlockHeight);

        Debug.Log($"BlockView ({BlockType}) - Intermediate Final Calculated Size: {finalCalculatedSize}");


        //  GENERAR LA LISTA 'dimensions' PARA CustomMeshImage 
      
        List<Vector4> dimensions = new List<Vector4>();
        Vector2 currentDrawOffset = Vector2.zero; //  empezamos desde (0,0) local para las 'dimensions'

        // Muesca Previous (Superior)
        currentDrawOffset.y -= settings.InternalPadding.y; // Empezar debajo del padding superior
        if (mBlock.PreviousConnection != null)
        {
            dimensions.Add(new Vector4(
                settings.InternalPadding.x + settings.StatementIndent, // x0
                currentDrawOffset.y,                                       // y0 (top de la muesca)
                settings.InternalPadding.x + settings.StatementIndent + settings.StatementConnectorVisualWidth, // x1
                currentDrawOffset.y - settings.NotchHeight                 // y1 (bottom de la muesca)
            ));
            currentDrawOffset.y -= settings.NotchHeight;
            Debug.Log($"   Added PrevNotch. Offset.y = {currentDrawOffset.y}. Dimensions Count: {dimensions.Count}");
        }

        // Contenido (LineGroups)
        float lineGroupStartX = settings.InternalPadding.x;
        float runningHeightForLineGroups = 0; // Acumulador para la altura del contenido de los LineGroups

        // Primero calculamos la altura total de todos los LineGroups más sus espacios

        if (currentBlockDirectContentSize.y > 0)
        {
            dimensions.Add(new Vector4(
                lineGroupStartX,                    // x0
                currentDrawOffset.y,                // y0 (donde terminó la muesca previa, o el top)
                lineGroupStartX + currentBlockDirectContentSize.x, // x1 (ancho del contenido directo)
                currentDrawOffset.y - currentBlockDirectContentSize.y // y1 (altura total de linegroups + spaces)
            ));
            currentDrawOffset.y -= currentBlockDirectContentSize.y;
            Debug.Log($"   Added MainContent. Offset.y = {currentDrawOffset.y}. ContentSize=({currentBlockDirectContentSize.x}, {currentBlockDirectContentSize.y}). Dimensions Count: {dimensions.Count}");
        }

        // Muesca/Pestaña Next (Inferior)
        currentDrawOffset.y -= settings.InternalPadding.y; // Espacio antes de la pestaña Next
        if (hasNextConnectionDefined)
        {
            dimensions.Add(new Vector4(
                settings.InternalPadding.x + settings.StatementIndent, // x0
                currentDrawOffset.y,                                       // y0 (top de la pestaña)
                settings.InternalPadding.x + settings.StatementIndent + settings.StatementConnectorVisualWidth, // x1
                currentDrawOffset.y - settings.TabHeight                   // y1 (bottom de la pestaña)
            ));
            currentDrawOffset.y -= settings.TabHeight; // Esta es la altura de la forma de la pestaña en sí
            Debug.Log($"   Added NextNotch/Tab. Offset.y = {currentDrawOffset.y}. Dimensions Count: {dimensions.Count}");
        }

        Debug.Log($"BlockView ({BlockType}) - FINISHED DIMENSIONS LIST GENERATION. Total Dimensions: {dimensions.Count}");
        if (m_BgImages != null && m_BgImages.Count > 0 && m_BgImages[0] is CustomMeshImage customImage)
        {
           
            customImage.SetDrawDimensions(dimensions.ToArray());
        }
        else if (m_PrimaryBackground is CustomMeshImage mainCustomImageBG) // Fallback si m_BgImages[0] no es, pero PrimaryBackground sí
        {
            mainCustomImageBG.SetDrawDimensions(dimensions.ToArray());
        }

        // Esto lo hace BaseView.UpdateLayout()
        // LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);

        Debug.LogWarning($"BlockView ({BlockType}) CalculateSize FINISHED. Returning finalCalculatedSize = {finalCalculatedSize}. (InternalContentSize was {currentBlockDirectContentSize})", this.gameObject);
        return finalCalculatedSize;
    }
    
    public void BuildLayout()
    {
        if (mBlock == null || !m_LayoutIsDirty) return;

        Debug.Log($"BlockView ({BlockType}): ---> Building Layout <---", this);

        if (gameObject.activeInHierarchy)

        {
            ManualLayoutRecursive(this.XY);
            //ApplyVisualAppearance(); //<----- vamos a ver si funciona esto para la sombra
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
   
    public void SetOrphan(bool preserveWorldPosition = false)
    {
        /* if (InToolbox)
             InToolbox = false;
         ViewTransform.SetParent(WorkSpaceView.Active.CodingArea);
         ViewTransform.SetAsLastSibling();*/
        string logPrefix = $"[BlockView.SetOrphan '{gameObject.name}']";

        // Obtener la referencia necesaria (CodingArea Transform)
        if (WorkspaceView == null)
        {
            Logger.LogError($"{logPrefix} Cannot set orphan because WorkspaceView reference is null!", this);
            return;
        }

        RectTransform codingAreaRectTransform = WorkspaceView.CodingArea;

        if (codingAreaRectTransform == null)
        {
            Logger.LogError($"{logPrefix} Cannot set orphan because WorkspaceView.CodingArea reference is null!", this.WorkspaceView.gameObject);
            return;
        }

        //Lógica de Reparentado
        RectTransform currentRectTransform = GetRectTransform(); // RectTransform de este bloque
        if (currentRectTransform == null)
        {
            Logger.LogError($"{logPrefix} Cannot set orphan because this block does not have a RectTransform!", this);
            return;
        }

        Logger.Log($"{logPrefix} Requesting reparent to '{codingAreaRectTransform.name}'. Preserve World Position: {preserveWorldPosition}", this);

        if (preserveWorldPosition)
        {
            // Esto es MUCHO más simple y fiable que SetParent(true)
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;
            Vector3 localScale = transform.localScale; 

            transform.SetParent(codingAreaRectTransform, true); 
                                                            // transform.position = worldPos;
                                                            // transform.rotation = worldRot;
                                                            // transform.localScale = Vector3.one; // O 'localScale' si es relevante para la escala en CodingArea.
        }
        else
        {
            transform.SetParent(codingAreaRectTransform, false);
            ViewTransform.anchoredPosition = Vector2.zero;
        }

        currentRectTransform.SetAsLastSibling();
       // this.m_ParentView = null;
        Logger.Log($"{logPrefix} Successfully reparented to '{codingAreaRectTransform.name}'. Preserve World Position: {preserveWorldPosition}. Block is now visually orphan.", this);
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

        {        //if (!InToolbox) 
            BlockDragController.Instance?.HandleDrag(/*this,*/ eventData);
        }
   
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (BlockDragController.Instance != null && BlockDragController.Instance.IsDraggingBlock(this.Block))
        {
            //if (!InToolbox) 
            BlockDragController.Instance?.HandleEndDrag(/*this,*/ eventData);
        }
        /*else
        {
            Debug.Log($"BlockView ({BlockType}): OnEndDrag called but BlockDragController is null or not dragging this block.");
        }
        */
        else if (eventData.pointerDrag == this.gameObject && BlockDragController.Instance != null)
        {
            Debug.LogWarning($"BlockView.OnEndDrag: Controller's WasDraggingBlock returned false, but UGUI pointerDrag is this object. Forcing HandleEndDrag.", this.gameObject);
            // if (!InToolbox) 
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

    private float GetStackHeightBelow(BlockView startBlock)
    {
        Debug.Log($"---> GetStackHeightBelow called for {startBlock.name}");
        float totalHeight = 0;
        BlockView current = startBlock;
        int safetyBreak = 0; // Para evitar bucles infinitos

        while (current != null && safetyBreak < 100) // Límite de seguridad
        {
            
            Vector2 currentSize = current.Size; 

          
            Debug.Log($"      - Processing block in stack: {current.name}, Reported Size: {currentSize}");

            if (currentSize.y <= 0)
            {
                Debug.LogWarning($"      !!! Block '{current.name}' reported Zero or Negative height ({currentSize.y}). Using default estimate.", current.gameObject);
                totalHeight += (BlockViewSettings.Instance?.DefaultBlockSize.y ?? 30f);
            }
            else
            {
                totalHeight += currentSize.y;
            }

            ConnectionModel nextConn = current.mBlock?.NextConnection;
            if (nextConn != null && nextConn.IsConnected && nextConn.TargetBlock != null)
            {
                // totalHeight += (BlockViewSettings.Instance?.StatementIndent ?? 5f); // O TabHeight?

                current = current.GetChildViewForConnection(nextConn.TargetConnection) as BlockView; // Encuentra el siguiente BlockView en la pila
                if (current == null) Debug.Log($"      - Next connection of {current.name} is connected but GetChildViewForConnection failed.");

            }
            else
            {
                current = null; // Fin de la pila
            }
            safetyBreak++;
        }
        if (safetyBreak >= 100) Debug.LogError("!!!! GetStackHeightBelow hit safety break !!!! Possible infinite loop detected.");


        Debug.Log($"<--- GetStackHeightBelow for {startBlock.name} finished. Total Height calculated: {totalHeight}");
        return totalHeight;
    }

    public BaseView GetChildViewForConnection(ConnectionModel targetConnection)
    {
     
        foreach (var connView in GetComponentsInChildren<ConnectionView>(true)) // Busca en hijos
        {
            if (connView.ConnectionModel != null && connView.ConnectionModel.TargetConnection == targetConnection)
            {
                return connView.TargetBlockView;
            }
        }
        return null;
    }


    /// <summary>
    /// Busca la InputView que posee la ConnectionView especificada.
    /// Esto es útil cuando una ConnectionView (por ejemplo, de un InputValue)
    /// necesita saber a qué InputView pertenece.
    /// </summary>
    public InputView GetInputViewForConnectionView(ConnectionView connView)
    {
        if (connView == null) return null;

        // Recorre todos los InputView hijos (directos o dentro de LineGroups)
        // y compara sus ConnectionView.
        List<InputView> allInputViews = GetInputViews(); 
        foreach (var inputView in allInputViews)
        {
            if (inputView.GetConnectionView() == connView)
            {
                // Debug.Log($"BlockView ({gameObject.name}): Found InputView '{inputView.gameObject.name}' for ConnectionView '{connView.gameObject.name}'.", this);
                return inputView;
            }
        }
        // Debug.LogWarning($"BlockView ({gameObject.name}): Could not find InputView for ConnectionView '{connView.gameObject.name}'.", this);
        return null;
    }
}//fin de la clase BlockView

