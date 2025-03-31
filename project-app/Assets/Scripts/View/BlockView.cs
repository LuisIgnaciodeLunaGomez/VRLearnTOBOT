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
using System.Collections;

[RequireComponent(typeof(CanvasGroup))] 
[RequireComponent(typeof(LayoutElement))] 

public class BlockView : BaseView, IBeginDragHandler, IDragHandler, IEndDragHandler 
{
    
    public override ViewType Type => ViewType.Block;
    private BlockModel m_BlockModel;
    public BlockModel BlockModel => m_BlockModel;


    private Image m_BackgroundImage;
    private LayoutElement m_LayoutElement;
    private CanvasGroup m_CanvasGroup;
    private BlockDragController m_DragController; 

   
    public bool IsDraggable { get;  set; } = true;
    public bool IsTemplate { get; set; } = false;

    
    private Vector2 m_DragOffset; // Offset LOCAL dentro del bloque
    private bool m_IsDragging = false; // Flag interno para saber si se está arrastrando 
    private bool m_needsLayoutUpdate = false;

 
    public override void InitializeView()
    {
        base.InitializeView(); 
        m_BackgroundImage = GetComponent<Image>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null) m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Configuraciones por defecto
        m_LayoutElement.ignoreLayout = false;
        if (m_BackgroundImage == null) Debug.LogWarning($"BlockView ({gameObject.name}) is missing an Image.");

        if (m_DragController == null) 
        {
            m_DragController = FindFirstObjectByType<BlockDragController>(); 
            if (m_DragController == null)
            {
                Debug.LogError("BlockView could not find BlockDragController!");
            }
        }

    }

   
    public void Setup(BlockDragController dragController )
    {
        m_DragController = dragController;
    }

   
    public void BindModel(BlockModel blockModel)
    {
        if (m_BlockModel == blockModel) return;
        UnbindModel();

        m_BlockModel = blockModel;
        if (m_BlockModel == null)
        {
            if (IsTemplate)
            {
              
                QueueForceLayoutUpdate();
            }
            return;
        }

        gameObject.name = $"BlockView_{m_BlockModel.Type}_{m_BlockModel.ID.Substring(0, 4)}";

        // Suscribirse a Eventos del Modelo
        m_BlockModel.OnUpdate += HandleModelUpdate;

        //ENLAZAR Vistas Hijas Existentes  con el Modelo
        if (!BindChildViewsRecursive()) 
        {
            Debug.LogError($"BlockView.BindModel ({m_BlockModel.Type}): Failed to bind child views due to structure mismatch. Rebuilding.");
            RebuildContent(); 
            return; 
        }

        // Actualizar Estado Visual Inicial y Posición
        UpdateVisualState();
        XY = m_BlockModel.XY; // Sincronizar posición visual con la lógica inicial
        SetColor(m_BlockModel.Definition.color); // Asegurar color correcto

        //Layout Inicial
        QueueForceLayoutUpdate(); 
    }

    //  Reacción a Cambios del Modelo 
    internal void HandleModelUpdate(BlockModel model, BlockUpdateType updateType)
    {
        if (model != m_BlockModel || this == null || !gameObject.activeInHierarchy) return;

       
        switch (updateType)
        {
            // Reconstrucción compelta del contenido visual
            case BlockUpdateType.Structure_Inputs:
            case BlockUpdateType.Structure_Connections:
                RebuildContent();
                break;

            // Actualizar apariencia
            case BlockUpdateType.State_Disabled:
            case BlockUpdateType.State_Movable:
            case BlockUpdateType.State_Deletable:
            case BlockUpdateType.State_Editable:
            case BlockUpdateType.State_Shadow:
                UpdateVisualState(); // 
                if (updateType == BlockUpdateType.State_Shadow) SetColorBasedOnModel(); // Recalcula color si cambia a/de sombra
                break;
            case BlockUpdateType.State_Collapsed:
                UpdateCollapsedState(); 
                break;

            // Cambio de posición lógica 
            case BlockUpdateType.Position_XY:
                if (!m_IsDragging && XY != model.XY) // Evitar bucle si yo inicié el cambio
                {
                    XY = model.XY; // Actualizar posición visual
                    
                }
                break;

            // Cambios de valor (Field/Variable)
            case BlockUpdateType.Value_Field:
            case BlockUpdateType.Value_Variable:
                QueueForceLayoutUpdate();
                break;
        }
    }

    // Reconstruye TODO el contenido interno 
    private void RebuildContent()
    {
        Debug.Log($"BlockView {m_BlockModel?.ID}: Rebuilding content...");
        //Desvincular modelos hijos
        UnbindChildViewsRecursive();

        // Destruir GameObjects hijos (vistas internas)
        foreach (var child in ChildViews.ToList())
        {
            RemoveChildView(child);
            Destroy(child.gameObject);
        }
        ChildViews.Clear();

        //  Llamar al Builder para recrear la estructura visual
        if (m_BlockModel?.Definition != null)
            BlockViewBuilder.BuildBlockViewContent(this.gameObject, m_BlockModel.Definition);

        // Re-enlazar los nuevos hijos creados por el Builder
        BindChildViewsRecursive();

        //Forzar Layout
        QueueForceLayoutUpdate();
    }

    public void UnbindModel()
    {
        if (m_BlockModel != null)
        {
            m_BlockModel.OnUpdate -= HandleModelUpdate;
            UnbindChildViewsRecursive(); //
            m_BlockModel = null;
        }
    }

   
    protected override Vector2 CalculateSize()
    {
        var lineGroups = ChildViews.OfType<LineGroupView>();
        float contentWidth = 0;
        float contentHeight = 0;
        if (lineGroups.Any())
        {
            contentWidth = lineGroups.Max(lg => lg.Size.x);
            contentHeight = lineGroups.Sum(lg => lg.Size.y) + Mathf.Max(0, lineGroups.Count() - 1) * BlockViewSettings.ContentSpace.y;
        }

        // Añadir espacio para conexiones principales visuales
        float topPadding = BlockViewSettings.BlockTopPadding;
        float bottomPadding = BlockViewSettings.BlockBottomPadding; 
        float sidePadding = BlockViewSettings.BlockSidePadding;
        float outputNotchWidth = 0;

        if (m_BlockModel?.PreviousConnection != null) topPadding = Mathf.Max(topPadding, BlockViewSettings.ConnectionHeight);
        if (m_BlockModel?.NextConnection != null) bottomPadding = Mathf.Max(bottomPadding, BlockViewSettings.ConnectionHeight);
        if (m_BlockModel?.OutputConnection != null)
        {
            // Añadir espacio a la izquierda para la conexión de output
            sidePadding = Mathf.Max(sidePadding, BlockViewSettings.OutputConnectionWidth);
            outputNotchWidth = BlockViewSettings.OutputConnectionWidth; 
            topPadding = Mathf.Max(topPadding, BlockViewSettings.BlockTopPaddingOutput); 
            bottomPadding = Mathf.Max(bottomPadding, BlockViewSettings.BlockBottomPaddingOutput);
        }

        float totalWidth = Mathf.Max(contentWidth + sidePadding * 2, BlockViewSettings.MinBlockSize.x);
        float totalHeight = Mathf.Max(contentHeight + topPadding + bottomPadding, BlockViewSettings.MinBlockSize.y);

        //Ajustar 'contentWidth' si la conexión Output influye
        //totalWidth = Mathf.Max(contentWidth + sidePadding + outputNotchWidth, BlockViewSettings.MinBlockSize.x);


        // Actualizar LayoutElement
        if (m_LayoutElement != null)
        {
            m_LayoutElement.preferredWidth = totalWidth;
            m_LayoutElement.preferredHeight = totalHeight;
        }
        return new Vector2(totalWidth, totalHeight);
    }

    private bool m_NeedsLayoutUpdate = false;
    public void QueueForceLayoutUpdate()
    {
        if (!m_NeedsLayoutUpdate) 
        {
            m_NeedsLayoutUpdate = true; LayoutRebuilder.MarkLayoutForRebuild(ViewTransform);
        } 
    } 

    // Posición donde empieza el primer hijo a primera LineGroup o una Connection
    public override Vector2 ChildStartXY
    {
        get
        {
            // Si tiene Previous, el primer LineGroup empieza debajo de la muesca
            if (m_BlockModel?.PreviousConnection != null)
            {
                return new Vector2(BlockViewSettings.BlockSidePadding, // Margen izquierdo
                                    -BlockViewSettings.ConnectionHeight); // Debajo de la conexión prev
            }
            // Si tiene Output, los inputs empiezan más a la derecha
            else if (m_BlockModel?.OutputConnection != null)
            {
                return new Vector2(BlockViewSettings.OutputConnectionWidth, // Después de la conexión output
                                   -BlockViewSettings.BlockTopPadding); // Margen superior
            }
            // Bloque sin Prev ni Output 
            else
            {
                return new Vector2(BlockViewSettings.BlockSidePadding,
                                    -BlockViewSettings.BlockTopPadding);
            }
        }
    }

    // Cuando el BlockView se mueve, actualiza el modelo lógico y las posiciones lógicas de las conexiones
    protected internal override void OnXYUpdated()
    {
        // base.OnXYUpdated(); 
        if (m_BlockModel != null && !IsTemplate) // Solo actualiza el modelo si no es una plantilla
        {
            
            WorkspaceController workspaceController = WorkspaceController.Instance; 
            if (workspaceController != null)
            {
                workspaceController.RequestBlockMove(m_BlockModel, this.XY);
            }
            else
            {
                Debug.LogError("WorkspaceController not found in BlockView.OnXYUpdated!");
                
            }
        }
     
        foreach (var conView in GetAllConnectionViewsInChildren())
        {
            conView.OnXYUpdated();
        }
    }

    // Cuando el tamaño cambia, actualizar layout o background
    protected internal override void OnSizeUpdated()
    {
        base.OnSizeUpdated();
        // Ajustar el tamaño de la imagen de fondo si es necesario
        if (m_BackgroundImage != null)
        {
            // Si la imagen es 'Sliced', RectTransform maneja el tamaño.
            // Si es 'Simple', escalar o ajustar UVs.
           
            m_BackgroundImage.rectTransform.sizeDelta = this.Size; 
        }
        // Forzar redibujo del layout padre 
        // ParentView?.UpdateLayout();
    }

    //Enlazar vistas hijas recursivamente
    private bool BindChildViewsRecursive()
    {
        if (m_BlockModel == null) return false; // No hay modelo para enlazar

        int inputModelIndex = 0;
        bool success = true;

        // Obtener vistas hijas directas  -Connections y LineGroups

        List<BaseView> directChildren = ChildViews.ToList(); 

        foreach (BaseView childView in directChildren)
        {
            
            if (childView is ConnectionView conView && !(childView is ConnectionInputView)) 
            {
                ConnectionModel modelToBind = null;
                switch (conView.ConnectionType)
                {
                    case EConnection.OutputValue: modelToBind = m_BlockModel.OutputConnection; break;
                    case EConnection.PrevStatement: modelToBind = m_BlockModel.PreviousConnection; break;
                    case EConnection.NextStatement: modelToBind = m_BlockModel.NextConnection; break;
                }

                if (modelToBind != null)
                {
                    conView.BindModel(modelToBind);
                }
                else
                {
                   
                    if (HasConnectionSlot(conView.ConnectionType))
                    { 
                        // Comprueba si la definición *debería* tenerla
                        Debug.LogError($"Bind Error ({m_BlockModel.Type}): Found {conView.ConnectionType} View but no matching Model!");
                        success = false;
                    }
                    // Si la definición no la tiene, esta vista es errónea 
                     childView.gameObject.SetActive(false); 
                }
            }
            // Es un LineGroupView
            else if (childView is LineGroupView lineGroup)
            {
                // Recorrer sus hijos (InputViews)
                foreach (BaseView inputChild in lineGroup.ChildViews)
                {
                    if (inputChild is InputView inputView)
                    {
                        if (inputModelIndex < m_BlockModel.InputList.Count)
                        {
                            // Enlazar InputView con el InputModel correspondiente
                            inputView.BindModel(m_BlockModel.InputList[inputModelIndex]);
                            // BindModel de InputView se encarga de enlazar sus FieldViews y ConnectionInputView internas
                            inputModelIndex++;
                        }
                        else
                        {
                            Debug.LogError($"Bind Error ({m_BlockModel.Type}): Found more InputViews than InputModels.");
                            inputView.gameObject.SetActive(false); // Ocultar extra
                            success = false;
                        }
                    }
                    else { Debug.LogWarning($"LineGroup contains unexpected child type: {inputChild.GetType()}"); }
                }
            }
            else { Debug.LogWarning($"BlockView contains unexpected child type: {childView.GetType()}"); }
        }

        // Comprobación final: ¿Se enlazaron todos los InputModels?
        if (inputModelIndex < m_BlockModel.InputList.Count)
        {
            Debug.LogError($"Bind Error ({m_BlockModel.Type}): More InputModels than InputViews were found/bound.");
            success = false;
        }

        return success;
    }

    // Desvincular
    private void UnbindChildViewsRecursive()
    {
        foreach (BaseView childView in ChildViews)
        {
            if (childView is ConnectionView conView) conView.UnbindModel();
            else if (childView is InputView inputView) inputView.UnbindModel(); // InputView desvincula sus FieldViews
            else if (childView is LineGroupView lineGroup)
            { 
                foreach (BaseView inputChild in lineGroup.ChildViews)
                {
                    if (inputChild is InputView iv) iv.UnbindModel();
                }
            }
        }
    }

    //  Actualización de Estado Visual 

    private void UpdateVisualState() 
    {
        if (m_BlockModel == null)
        {
            // Estado default para plantillas
            IsDraggable = IsTemplate ? true : false;
            if (m_CanvasGroup != null) { m_CanvasGroup.alpha = 1f; m_CanvasGroup.interactable = true; }
            
            return;
        }

        IsDraggable = m_BlockModel.Movable;
        float targetAlpha = (m_BlockModel.Disabled || !m_BlockModel.Editable) ? 0.6f : 1.0f;
        if (m_CanvasGroup != null) m_CanvasGroup.alpha = targetAlpha;
        if (m_CanvasGroup != null) m_CanvasGroup.interactable = m_BlockModel.Editable && !m_BlockModel.Disabled;

        SetColorBasedOnModel(); 

    }

    private void UpdateCollapsedState()
    {
        if (m_BlockModel == null) return;
        // TODO: Lógica para ocultar/mostrar inputs/next block, cambiar texto, etc.
        Debug.LogWarning($"Collapse/Expand not implemented for BlockView {m_BlockModel.Type}");
        QueueForceLayoutUpdate();
    }

    public void SetColor(Color color)
    {
        if (m_BackgroundImage != null) m_BackgroundImage.color = color;
        
    }

    void LateUpdate()
    {
        if (m_needsLayoutUpdate)
        {
            m_needsLayoutUpdate = false;
            Debug.Log($"<color=lightblue>Performing Layout for {m_BlockModel.Type}...</color>");
            PerformLayoutDownwards(); // Inicia el layout descendente
                                      // Forzar actualización de componentes UI de Unity si se usan
            LayoutRebuilder.ForceRebuildLayoutImmediate(ViewTransform);
        }
    }
    // Drag & Drop 
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDraggable) { eventData.pointerDrag = null; return; }
        m_IsDragging = true;

        
        if (m_DragController != null)
        {
            if (IsTemplate)
            {
                // Template: Pide al drag controller que inicie el proceso de clonado
                // Se pasa la definición o el tipo y la posición inicial del puntero
                BlockDefinition definition = m_BlockModel?.Definition ?? BlockDataLoader.GetDefinition(gameObject.name.Replace("Template_", ""));
                if (definition != null)
                    m_DragController.StartDraggingTemplate(this, definition, eventData);
                else { eventData.pointerDrag = null; Debug.LogError("Cannot start drag: Template definition not found."); }
              
                return; 
            }
            else
            {
                // Bloque Real-Calcular offset local e iniciar drag
                if (m_CanvasGroup != null) m_CanvasGroup.blocksRaycasts = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(ViewTransform, eventData.position, eventData.pressEventCamera, out m_DragOffset);
                m_DragController.StartDraggingBlock(this, eventData); 
            }
        }
        else { eventData.pointerDrag = null; Debug.LogError("BlockDragController not found!"); }

    }

    // Coroutine para esperar y pasar el drag
    /*private IEnumerator PassDragToNewView(BlockModel newModel, PointerEventData eventData)
    {
        BlockView newView = null;
        float timeout = 0.5f; // Esperar max 0.5 seg
        float elapsed = 0f;
        while (newView == null && elapsed < timeout)
        {
            newView = WorkSpaceView.Instance?.GetBlockView(newModel); // Intentar encontrar la nueva vista
            if (newView == null) yield return null; // Esperar al siguiente frame
            elapsed += Time.deltaTime;
        }

        if (newView != null)
        {
            Debug.Log($"Passing drag to new view: {newView.gameObject.name}");
            eventData.pointerDrag = newView.gameObject;
            ExecuteEvents.Execute(newView.gameObject, eventData, ExecuteEvents.beginDragHandler);
        }
        else
        {
            Debug.LogError("Failed to find new BlockView for cloned model to pass drag!");
            eventData.pointerDrag = null;
            // ¿Qué hacer con el modelo clonado si no se encontró la vista? Podría necesitar limpiarse.
            WorkspaceController.Instance?.CancelPendingClone(newModel); // Necesitas método en WC
        }
    }*/


    public void OnDrag(PointerEventData eventData)
    {
        if (!m_IsDragging || IsTemplate) return; 
        m_DragController?.UpdateDragging(this, eventData, m_DragOffset); 
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (!m_IsDragging) return; 
        if (!IsTemplate && m_CanvasGroup != null) m_CanvasGroup.blocksRaycasts = true; 
        m_IsDragging = false;
      
        m_DragController?.StopDragging(this, eventData);
    }

    // obtener posición en coordenadas del Workspace
    /*private Vector2 GetPointerPosInWorkspace(PointerEventData eventData)
    {
        RectTransform workspaceAreaRect = WorkSpaceView.Instance?.CodingArea;
        Canvas workspaceCanvas = WorkSpaceView.Instance?.GetComponentInParent<Canvas>();
        Vector2 localPos = Vector2.zero;
        if (workspaceAreaRect != null && workspaceCanvas != null)
        {
            // Usa la cámara del canvas raíz si existe, sino null (Screen Space Overlay)
            Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(workspaceAreaRect, eventData.position, workspaceCanvas.worldCamera, out localPos);
        }
        return localPos;
    }
    */
 
    // Encuentra ConnectionView hija -Principal o de Input
    public ConnectionView FindConnectionView(ConnectionModel modelToFind)
    {
        if (modelToFind == null) return null;
        return GetAllConnectionViewsInChildren().FirstOrDefault(cv => cv.ConnectionModel == modelToFind);
    }

    // Encuentra FieldView hija
    public FieldView FindFieldView(FieldModel modelToFind)
    {
        if (modelToFind == null) return null;
        foreach (var lineGroup in ChildViews.OfType<LineGroupView>())
        {
            foreach (var inputView in lineGroup.ChildViews.OfType<InputView>())
            {
                foreach (var fieldView in inputView.ChildViews.OfType<FieldView>())
                {
                    if (fieldView.FieldModel == modelToFind) return fieldView; 
                }
            }
        }
        return null;
    }

    //encontrar ConnectionViews o FieldViews específicas
    public IEnumerable<ConnectionView> GetAllConnectionViewsInChildren()
    {
        List<ConnectionView> views = new List<ConnectionView>();
        foreach (var child in ChildViews)
        {
            if (child is ConnectionView cv) views.Add(cv);
            else if (child is LineGroupView lgv)
            {
                foreach (var inputChild in lgv.ChildViews)
                {
                    if (inputChild is InputView iv)
                    {
                        if (iv.GetConnectionView() != null) views.Add(iv.GetConnectionView());
                    }
                }
            }
        }
        return views;
    }

    // --- Limpieza 
    public new void OnDestroy()
    {
        UnbindModel();
        
    }


    // Llama a UpdateLayout 
    private void ForceLayoutCalculationNow()
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        try
        {
            
            BaseView header = GetHeaderView(); 
            if (header != null) header.PerformLayoutDownwards();
            else PerformLayoutDownwards();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during ForceLayoutCalculationNow for {gameObject.name}: {e.Message}\n{e.StackTrace}");
        }
    }

   
    public BaseView GetHeaderView()
    {
        BaseView header = this;
        while (header.PreviousView != null && header.ParentView == this.ParentView) // Busca el primer hermano en la misma línea/padre
        {
            header = header.PreviousView;
        }
        return header;
    }


    // saber si la definición tiene una conexión específica
    private bool HasConnectionSlot(EConnection type)
    {
        if (m_BlockModel?.Definition == null) return false;
        switch (type)
        {
            case EConnection.OutputValue: return m_BlockModel.Definition.hasOutput;
            case EConnection.PrevStatement: return m_BlockModel.Definition.hasPreviousStatement;
            case EConnection.NextStatement: return m_BlockModel.Definition.hasNextStatement;
            default: return false; 
        }
    }

    private void ConfigureForTemplate()
    {
      
        string potentialType = gameObject.name.Replace("Template_", "");
        BlockDefinition definition = BlockDataLoader.GetDefinition(potentialType);
        if (definition != null) SetColor(definition.color); else SetColor(Color.grey);
    }
    private void SetColorBasedOnModel()
    {
        if (m_BlockModel == null) return;
        Color targetColor = m_BlockModel.IsShadow ? BlockViewSettings.ShadowColor : (m_BlockModel.Definition?.color ?? Color.grey);
        SetColor(targetColor);
    }

    // obtener primer hijo de contenido real
    private BaseView GetFirstContentChild()
    {
        return ChildViews.FirstOrDefault(v => v is ConnectionView || v is LineGroupView);
    }

    //  para Revertir UI desde InputController
    public void ForceUpdateDisplayFromModel()
    {
        // Forzar actualización de los fields hijos
        foreach (var fieldView in GetComponentsInChildren<FieldView>(true))
        { 
            if (fieldView.FieldModel != null)
                fieldView.ForceUpdateDisplayFromModel(); // 
        }
        
        QueueForceLayoutUpdate();
    }
}
