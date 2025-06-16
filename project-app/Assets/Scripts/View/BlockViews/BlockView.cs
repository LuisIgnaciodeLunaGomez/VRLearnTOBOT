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
 * Versión: 2.0.4
 * 
 * Descripción: Clase que representa un bloque visual en la interfaz de usuario premite la vinculación del modelo lógico con la UI
 * 
 */

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(CanvasGroup))]
//[RequireComponent(typeof(LayoutElement))]

public class BlockView : BaseView, IBeginDragHandler, IDragHandler, IEndDragHandler //TODO: Interesante mirar IPointerClickHandler
{
    [SerializeField] private List<Image> m_BgImages = new List<Image>();
    [Tooltip("Asigna aquí la Imagen principal que muestra el color del bloque")]
    [SerializeField] private Image m_PrimaryBackground;

     private bool m_LayoutIsDirty = true;// Empieza sucio para el primer layout
    private bool m_IsExecutingLayout = false; // Flag para evitar recursividad infinita en UpdateLayout
    public bool LayoutISDirty => m_LayoutIsDirty; // Propiedad para acceder al estado de suciedad del layout
    // private bool m_LayoutIsDirty = false;
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
        if (mBlock == block && mBlock != null) return;
        if (block == null)
        {
            Debug.LogError($"BlockView ({gameObject.name}): Intento de BindModel con un modelo NULL.", this);
            return;
        }

        if (mBlock != null) UnBindModel();

        mBlock = block;
        m_WorkspaceView = this.InToolbox ? null : workspaceView;

        if (!InToolbox && m_WorkspaceView != null)
        {
            m_WorkspaceView.AddBlockView(this);
            mBlockObserver = new MemorySafeBlockObserver(this);
            mBlock.AddObserver(mBlockObserver);
        }

        // --- LÓGICA DE BINDEO CORREGIDA Y DEFINITIVA ---

        // 1. Recolecta todas las vistas con nombre.
        var viewDictionary = GetComponentsInChildren<BaseView>(true)
            .Where(v => v != this && !string.IsNullOrEmpty(v.DefinitionName))
            .ToDictionary(v => v.DefinitionName, v => v);

        // 2. Bindea las conexiones de primer nivel EXPLÍCITAMENTE.
        // Conexión PREVIA
        if (mBlock.PreviousConnection != null)
        {
            if (viewDictionary.TryGetValue("PREVIOUSSTATEMENT", out BaseView prevView) && prevView is ConnectionView prevConnView)
            {
                prevConnView.BindModel(mBlock.PreviousConnection, this);
            }
        }

        // Conexión SIGUIENTE
        if (mBlock.NextConnection != null)
        {
            if (viewDictionary.TryGetValue("NEXTSTATEMENT", out BaseView nextView) && nextView is ConnectionView nextConnView)
            {
                nextConnView.BindModel(mBlock.NextConnection, this);
            }
        }

        // Conexión de SALIDA (Output)
        if (mBlock.OutputConnection != null)
        {
            if (viewDictionary.TryGetValue("OUTPUT", out BaseView outputView) && outputView is ConnectionView outputConnView)
            {
                outputConnView.BindModel(mBlock.OutputConnection, this);
            }
        }

        // 3. Bindea los inputs y sus componentes internos.
        foreach (InputModel inputModel in mBlock.InputList)
        {
            // Bindea los CAMPOS
            foreach (FieldModel fieldModel in inputModel.FieldRow)
            {
                if (fieldModel != null && viewDictionary.TryGetValue(fieldModel.Name, out BaseView foundFieldView) && foundFieldView is FieldView fieldView)
                {
                    fieldView.BindModel(fieldModel);
                }
            }
            // Bindea la CONEXIÓN del input
            if (inputModel.Connection != null && viewDictionary.TryGetValue(inputModel.Name, out BaseView foundConnView) && foundConnView is ConnectionView connView)
            {
                connView.BindModel(inputModel.Connection, this);
            }
        }


        UpdateColor();
        MarkDirty();
        Debug.Log($"<color=lime><b>✔ BINDING COMPLETO para '{gameObject.name}'.</b></color>", gameObject);
    }

    /*
    private void BindFirstClassConnection(Dictionary<string, BaseView> viewDict, ConnectionModel model)
    {
        if (model == null) return;
        string definitionName = model.Type.ToString().ToUpper();
        if (viewDict.TryGetValue(definitionName, out BaseView view) && view is ConnectionView connView)
        {
            connView.BindModel(model, this);
        }
    }
    */


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
            // Usar los valores de padding definidos en el Settings.
            // Y añadimos una comprobación de seguridad por si no se encuentra el asset.
            if (BlockViewSettings.Instance != null)
            {
                return new Vector2(BlockViewSettings.Instance.BlockInternalPadding.left,
                                 -BlockViewSettings.Instance.BlockInternalPadding.top); // Negativo porque Y va hacia abajo
            }

            // Si no hay settings, usamos un valor por defecto seguro.
            return new Vector2(10, -10);
        }
    }

   

    protected override Vector2 CalculateSize()
    {
        var settings = BlockViewSettings.Instance;

        // El tamaño del contenido empieza con los paddings.
        float contentWidth = BlockViewSettings.Instance.BlockInternalPadding.left + BlockViewSettings.Instance.BlockInternalPadding.right;
        float contentHeight = BlockViewSettings.Instance.BlockInternalPadding.top + BlockViewSettings.Instance.BlockInternalPadding.bottom;

        float maxChildWidth = 0;
        float totalChildHeight = 0;

        var activeLineGroups = ChildViews.OfType<LineGroupView>()
                                         .Where(lg => lg != null && lg.gameObject.activeSelf)
                                         .ToList();

        if (activeLineGroups.Count > 0)
        {
            foreach (var lineGroup in activeLineGroups)
            {
                totalChildHeight += lineGroup.Height;
                maxChildWidth = Mathf.Max(maxChildWidth, lineGroup.Width);
            }
            // Añadir el espaciado entre los line groups.
            totalChildHeight += (activeLineGroups.Count - 1) * settings.VerticalLineSpacing;
        }

        contentWidth += maxChildWidth;
        contentHeight += totalChildHeight;

        // El tamaño final no puede ser menor que el mínimo definido.
        float finalWidth = Mathf.Max(contentWidth, settings.MinBlockSize.x);
        float finalHeight = Mathf.Max(contentHeight, settings.MinBlockSize.y);

        return new Vector2(finalWidth, finalHeight);
    }


    public override void UpdateLayout(Vector2 startPos)
    {
        // 1. Me posiciono donde me han dicho (o en mi posición actual si soy la raíz).
        this.XY = startPos;

        // 2. Organizo a mis hijos (los LineGroups) verticalmente.
        if (HasChildren)
        {
            // La posición local de inicio para el primer hijo, teniendo en cuenta el padding del bloque.
            Vector2 currentChildPos = new Vector2(
                     BlockViewSettings.Instance.BlockInternalPadding.left,
                      -BlockViewSettings.Instance.BlockInternalPadding.top); // Negativo porque Y va hacia abajo

            Debug.Log($"<color=yellow><b>[BlockView.UpdateLayout]</b></color> en '{gameObject.name}': Empezando a posicionar LineGroups. Posición inicial de hijos: {currentChildPos:F2}", gameObject);

            foreach (LineGroupView lineGroup in ChildViews.OfType<LineGroupView>().Where(lg => lg != null && lg.gameObject.activeSelf))
            {
                lineGroup.UpdateLayout(currentChildPos);

                // El siguiente LineGroup se posiciona debajo del anterior.
                currentChildPos.y -= lineGroup.Height + BlockViewSettings.Instance.VerticalLineSpacing;

                Debug.Log($"    - LineGroup '{lineGroup.gameObject.name}' posicionado. Tamaño: {lineGroup.Size:F2}. Próximo empezará en Y={currentChildPos.y:F2}", gameObject);

            }
        }

        // 3. Finalmente, con todos los hijos en su sitio, calculo mi propio tamaño.
        this.Size = CalculateSize();

        Debug.Log($"<color=yellow><b>[BlockView.UpdateLayout]</b></color> en '{gameObject.name}': Layout completado. Mi tamaño final es {this.Size:F2}", gameObject);


    }

    /// <summary>
    ///     Método para actualizar la malla (Mesh) y la apariencia visual del bloque.
    /// </summary>

    public void ApplyVisualAppearance()
    {
        var customImage = m_PrimaryBackground as CustomMeshImage;
        if (customImage == null) return;

        Vector2 finalBlockSize = this.Size;
        if (finalBlockSize.x <= 0 || finalBlockSize.y <= 0) return;

        // Dibujamos un único rectángulo que abarca todo el bloque
        Vector4 blockDimension = new Vector4(
            0,                         // X min
            -finalBlockSize.y,         // Y min (es negativo)
            finalBlockSize.x,          // X max
            0                          // Y max
        );

        customImage.SetDrawDimensions(new Vector4[] { blockDimension });
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
        base.OnSizeUpdated();

        // Iteramos por TODAS las ConnectionView hijas.
        var allConnections = GetComponentsInChildren<ConnectionView>();

        foreach (var connectionView in allConnections)
        {
            // AÑADIMOS ESTA CONDICIÓN CRÍTICA:
            // Solo llamamos a OnXYUpdated SI la conexión TIENE UN MODELO ASIGNADO.
            if (connectionView.ConnectionModel != null)
            {
                connectionView.OnXYUpdated();
            }
        }

        if (m_nextStatementContainer != null)
        {
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
    /*
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
    }*/

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
                totalHeight += (BlockViewSettings.Instance?.MinBlockSize.y ?? 30f);
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

    private void LateUpdate()
    {

        // Solo el BlockView raíz de una jerarquía (el que no tiene padre) debe iniciar el layout
        if (m_LayoutIsDirty && !m_IsExecutingLayout && this.ParentView == null)
        {

            // <<< DEBUG >>>
            Debug.LogWarning($"<color=yellow>--- INICIO LAYOUT COMPLETO (FRAME {Time.frameCount}) ---</color> en '{gameObject.name}' porque m_LayoutIsDirty era true.");

            m_IsExecutingLayout = true; // Bloqueamos para evitar re-entrada

            try
            {
                // La magia empieza aquí. Llamamos a UpdateLayout desde la raíz.
                UpdateLayout(this.XY);
                ApplyVisualAppearance(); // Aplicamos la malla 9-slice al final
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error catastrófico durante el ciclo de layout en '{gameObject.name}': {ex}", gameObject);
            }
            finally
            {
                m_LayoutIsDirty = false;
                m_IsExecutingLayout = false; // Desbloqueamos
                Debug.Log($"<color=cyan>--- FRAME {Time.frameCount}: FIN Layout para '{gameObject.name}' ---</color>", gameObject);
            }

        }
    }



}//fin de la clase BlockView

