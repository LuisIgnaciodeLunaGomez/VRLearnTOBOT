/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de generar la vista de los bloques de la zona de categorías
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class BlockScrollListView : BaseToolbox
{
    private Transform m_blockContainer; // Contenedor donde instanciar los bloques (asignado por UICanvasManager)
    [SerializeField] private GameObject m_blockViewPrefab; // Prefab base para crear vistas de bloque
    private UICanvasView m_uiManager; // Referencia al manager para comunicación inversa si es necesaria
    private WorkSpaceView m_workspaceView; // Referencia a la vista del área de código (para Drag & Drop)
    private WorkSpaceModel m_currentWorkspace; // El modelo principal (para BlockFactory si es necesario)

    private string m_currentCategory = null;
    private List<GameObject> m_templateBlocks = new List<GameObject>(); // Mantener lista de GOs de plantilla

    //Diccionarios para Variables/Procedimientos
    protected new Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
    protected new List<BlockView> mVariableHelperViews = new List<BlockView>(); // Para Setters/Changers
    protected new Dictionary<string, BlockView> mProcedureCallerViews = new Dictionary<string, BlockView>();

    //Observadores
    private VariableObserver mVarObserver;
    private ProcedureObserver mProcObserver;

    /**
     * Descripción: Inicializa el Toolbox con las referencias necesarias.
     * @param blockContainer: Contenedor donde se instanciarán los bloques.
     * @param blockPrefab: Prefab de bloque (opcional, se puede cargar desde recursos).
     * @param manager: Referencia al UICanvasManager.
     * @param workspace: Referencia al modelo del área de trabajo.
     * @param workspaceView: Referencia a la vista del área de trabajo.
     */
    public void InitializeToolbox(Transform blockContainer, /*GameObject blockPrefab,*/ UICanvasView manager, WorkSpaceModel workspace, WorkSpaceView workspaceView)
    {
        Debug.Log("<color=lightblue>BlockScrollList: Initializing references...</color>");
        m_blockContainer = blockContainer;
        m_uiManager = manager;
        m_currentWorkspace = workspace;
        m_workspaceView = workspaceView;

        // Cargar prefab fijo Assets/Resources/Prefabs/BlocksPrefab/Stack_block_grey.prefab
        m_blockViewPrefab = Resources.Load<GameObject>("Prefabs/BlocksPrefab/Stack_block_grey"); 
        Debug.Log($"<color=lightblue>BlockScrollList: BlockViewPrefab loaded: {m_blockViewPrefab != null}</color>");
        if (m_blockViewPrefab == null)
        {
            Debug.LogError("----> FAILED TO LOAD BlockViewPrefab FROM RESOURCES! Check Path: Resources/Prefabs/BlocksPrefab <----");
        }
        // Validaciones
        if (m_blockContainer == null) Debug.LogError("BlockContainer is NULL");
        if (m_blockViewPrefab == null) Debug.LogError("BlockViewPrefab is NULL");
        if (m_uiManager == null) Debug.LogError("UIManager is NULL");
        if (m_currentWorkspace == null) Debug.LogError("WorkspaceModel is NULL");
        if (m_workspaceView == null) Debug.LogError("WorkspaceView is NULL");

        // Registrar observadores 
        if (m_currentWorkspace != null)
        {
            mVarObserver = new VariableObserver(this);
            mProcObserver = new ProcedureObserver(this);
            m_currentWorkspace.VariableMap.AddObserver(mVarObserver);
            m_currentWorkspace.ProcedureDB.AddObserver(mProcObserver);
            Debug.Log("<color=lightblue>BlockScrollList: Observers registered.</color>");
        }
    }

    // Método principal que ahora muestra bloques como plantillas
    public void ShowBlockCategory(string categoryName, Color categoryColor)
    {
        if (m_currentCategory == categoryName)
        {
            //Debug.Log($"Category '{categoryName}' already displayed.");
            //return; // Opcional: No recargar si es la misma categoría
        }
        m_currentCategory = categoryName;
        Debug.Log($"<color=lightblue>BlockScrollList(Toolbox): Showing category '{categoryName}'</color>");

        
        ClearTemplateBlocks();

        if (m_currentWorkspace == null || m_blockContainer == null || m_blockViewPrefab == null)
        {
            Debug.LogError("BlockScrollList: Cannot show category, dependencies missing.");
            return;
        }

       
        switch (categoryName)
        {
            case Define.VARIABLE_CATEGORY_NAME:
                BuildVariableBlocksInternal();
                break;

            case Define.PROCEDURE_CATEGORY_NAME: 
                BuildProcedureBlocksInternal();
                break;

            default:
                // Categoría estándar
                List<string> blockTypes = GetBlockTypesFromDefinitions(categoryName);
                if (blockTypes != null)
                {
                    foreach (string blockType in blockTypes)
                    {
                        // Crear modelo temporal desconectado
                        BlockModel templateModel = BlockFactory.Instance.CreateBlock(null, blockType);
                        if (templateModel != null)
                        {
                            // Crear vista y añadirla
                            BlockView view = NewBlockViewInternal(templateModel, m_blockContainer);
                            if (view != null)
                            {
                                m_templateBlocks.Add(view.gameObject); // Añadir a la lista general
                            }
                            else
                            {
                                templateModel.Dispose(); // Limpiar modelo si vista falló
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Could not create template model for type: {blockType}");
                        }
                    }
                }
                break;
        }

        //  Actualizar Layout 
        LayoutRebuilder.MarkLayoutForRebuild(m_blockContainer as RectTransform);
        StartCoroutine(ForceScrollRectUpdate()); 
    }

    // Corutina para forzar actualización del ScrollRect después de un frame
    private IEnumerator<WaitForEndOfFrame> ForceScrollRectUpdate()
    {
        yield return new WaitForEndOfFrame(); // Espera a que el layout calcule
        LayoutRebuilder.MarkLayoutForRebuild(m_blockContainer as RectTransform);
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>(); 
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // Scroll arriba
    }

    // Método Interno para Crear Vistas 
    private BlockView NewBlockViewInternal(BlockModel block, Transform parent, int index = -1)
    {
   
        BlockView view = BlockViewFactory.CreateView(block);
        if (view == null)
        {
            Debug.LogError($"BlockViewFactory failed for block type {block.Type}");
            return null;
        }

        view.InToolbox = true;
        view.transform.localScale = Vector3.one; 
        if (index >= 0)
            view.transform.SetSiblingIndex(index);

        // Añadir Dragger
        ToolboxBlockDragger dragger = view.gameObject.GetComponent<ToolboxBlockDragger>();
        if (dragger == null) dragger = view.gameObject.AddComponent<ToolboxBlockDragger>();

        
        if (m_workspaceView != null)
        {
            dragger.Init(m_workspaceView);
        }
        else
        {
            Debug.LogError($"Cannot initialize ToolboxBlockDragger on block {block.Type}, WorkspaceView reference is missing!", view.gameObject);
            
        }
        return view;
    }

    //  Limpieza 
    private void ClearTemplateBlocks()
    {
        Debug.Log($"Clearing {m_templateBlocks.Count + mVariableGetterViews.Count + mVariableHelperViews.Count + mProcedureCallerViews.Count} previous template blocks.");
        // Limpiar bloques generales
        foreach (GameObject blockGO in m_templateBlocks) { if (blockGO != null) Destroy(blockGO); }
        m_templateBlocks.Clear();

        // Limpiar bloques de Variables
        foreach (BlockView view in mVariableGetterViews.Values) { if (view != null && view.gameObject != null) Destroy(view.gameObject); }
        mVariableGetterViews.Clear();
        foreach (BlockView view in mVariableHelperViews) { if (view != null && view.gameObject != null) Destroy(view.gameObject); }
        mVariableHelperViews.Clear();

        // Limpiar bloques de Procedimientos
        foreach (BlockView view in mProcedureCallerViews.Values) { if (view != null && view.gameObject != null) Destroy(view.gameObject); }
        mProcedureCallerViews.Clear();

        // Destruir botón "Crear Variable" 
       /* Button createVarButton = m_blockContainer.GetComponentInChildren<Button>(); 
        if (createVarButton != null && createVarButton.name.Contains("CreateVarButton")) 
        {
            Destroy(createVarButton.gameObject);
        }*/

    }

    #region implementación BaseToolBox 

    // Implementación de Manejadores de Variables 
    private void BuildVariableBlocksInternal()
    {
        // 1. Botón Crear Variable (Adaptar Prefab si es necesario)
        GameObject createVarPrefab = BlockViewSettings.Get()?.PrefabBtnCreateVar; // Obtener prefab desde settings
        if (createVarPrefab != null)
        {
            GameObject btnGO = Instantiate(createVarPrefab, m_blockContainer);
            btnGO.name = "CreateVarButton_Instance";
            Text buttonText = btnGO.GetComponentInChildren<Text>(); // Guarda la referencia
            if (buttonText != null)
                buttonText.text = I18n.Get(MsgDefine.NEW_VARIABLE);
            else Debug.LogError("PrefabBtnCreateVar is missing Text component in children.", btnGO);
            // Asignar color y acción
            Color categoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME);
            Image buttonImage = btnGO.GetComponentInChildren<Image>();
            if (buttonImage != null)
                buttonImage.color = categoryColor; // Asignar color
            else Debug.LogError("PrefabBtnCreateVar is missing Image component in children.", btnGO);

            Button button = btnGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => {
                   
                    DialogFactory.CreateDialog(DialogFactory.VARIABLE_NAME_DIALOG_NAME);
                });
            }
            else Debug.LogError("PrefabBtnCreateVar is missing Button component.", btnGO);

        }
        else { Debug.LogWarning("PrefabBtnCreateVar not found in BlockViewSettings"); }


        List<VariableModel> allVars = m_currentWorkspace.GetAllVariables();
        if (allVars.Count == 0) return; // No hay variables, solo mostrar el botón crear

        
        if (mVariableHelperViews.Count == 0) 
            CreateVariableHelperViewsInternal();

        
        mVariableGetterViews.Clear(); 
        foreach (VariableModel variable in allVars)
        {
            CreateVariableGetterViewInternal(variable.Name);
        }
    }

    private void CreateVariableHelperViewsInternal()
    {
        foreach (BlockView view in mVariableHelperViews) { if (view != null && view.gameObject != null) Destroy(view.gameObject); }
        mVariableHelperViews.Clear();

        if (m_currentWorkspace.GetAllVariables().Count == 0) return; 
        string firstVarName = m_currentWorkspace.GetAllVariables()[0].Name; 

        List<string> blockTypes = BlockDataLoader.GetDefinitionsForCategory(Define.VARIABLE_CATEGORY_NAME) 
                                  .Select(def => def.type)
                                  .ToList();
        if (blockTypes == null || blockTypes.Count == 0) return;

        Color variableCategoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME);

        foreach (string blockType in blockTypes)
        {
            if (!string.Equals(blockType, Define.VARIABLE_GET_BLOCK_TYPE))
            {
                BlockModel block = BlockFactory.Instance.CreateBlock(null, blockType);
                if (block != null)
                {
                    try { block.SetFieldValue("VAR", firstVarName); }
                    catch { /*TOIDO*/ }

                    BlockView view = NewBlockViewInternal(block, m_blockContainer);
                    if (view != null)
                    {
                        // 
                        view.ChangeBgColor(variableCategoryColor);
                        mVariableHelperViews.Add(view);
                    }
                    else block.Dispose();
                }
            }
        }
    }
    private void CreateVariableGetterViewInternal(string varName)
    {
        if (mVariableGetterViews.ContainsKey(varName))
        {
            return;
        }


        BlockModel block = BlockFactory.Instance.CreateBlock(null, Define.VARIABLE_GET_BLOCK_TYPE);
        if (block == null) { Debug.LogError("Failed to create VARIABLE_GET_BLOCK_TYPE model"); return; }
        block.SetFieldValue("VAR", varName);

        BlockView view = NewBlockViewInternal(block, m_blockContainer);
        if (view != null)
        {
            Color variableCategoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME);
            view.ChangeBgColor(variableCategoryColor);
            mVariableGetterViews[varName] = view;
        }
        else block.Dispose();
    }

    private void DeleteVariableGetterViewInternal(string varName)
    {
        if (mVariableGetterViews.TryGetValue(varName, out BlockView view))
        {
            mVariableGetterViews.Remove(varName);
            if (view != null && view.gameObject != null) Destroy(view.gameObject);
        }
    }
    private void DeleteVariableHelperViewsInternal()
    {
        foreach (BlockView view in mVariableHelperViews) { if (view != null && view.gameObject != null) Destroy(view.gameObject); }
        mVariableHelperViews.Clear();
    }

    // Método que se llama desde el Observer
    protected new void OnVariableUpdate(VariableUpdateData updateData)
    {
        if (m_currentCategory != Define.VARIABLE_CATEGORY_NAME) return;

        Debug.Log($"<color=orange>OnVariableUpdate: {updateData.Type} - {updateData.VarName} -> {updateData.NewVarName}</color>");

        switch (updateData.Type)
        {
            case VariableUpdateData.Create:
                if (mVariableHelperViews.Count == 0 && m_currentWorkspace.GetAllVariables().Count == 1)
                    CreateVariableHelperViewsInternal();
                CreateVariableGetterViewInternal(updateData.VarName);
                break;

            case VariableUpdateData.Delete:
                DeleteVariableGetterViewInternal(updateData.VarName);
                if (m_currentWorkspace.GetAllVariables().Count == 0)
                    DeleteVariableHelperViewsInternal();
                else
                {
                    string remainingVarName = m_currentWorkspace.GetAllVariables()[0].Name;
                    foreach (BlockView views in mVariableHelperViews)
                    {
                        if (views.Block.GetFieldValue("VAR").Equals(updateData.VarName))
                        {
                            views.Block.SetFieldValue("VAR", remainingVarName);
                        }
                    }
                }
                break;

            case VariableUpdateData.Rename:
                if (mVariableGetterViews.TryGetValue(updateData.VarName, out BlockView view))
                {
                  
                    mVariableGetterViews.Remove(updateData.VarName);
                    mVariableGetterViews[updateData.NewVarName] = view;
                    Debug.Log($"Renamed view in dictionary: {updateData.VarName} -> {updateData.NewVarName}");
                }
                else { Debug.LogWarning($"Rename: Could not find getter view for old name {updateData.VarName}"); }
                break;
        }
      
        LayoutRebuilder.MarkLayoutForRebuild(m_blockContainer as RectTransform);
    }

    //Implementación de Manejadores de Procedimientos 
    
    protected void BuildProcedureBlocksInternal()
    {
        List<string> blockTypes = GetBlockTypesFromDefinitions(Define.PROCEDURE_CATEGORY_NAME);
        if (blockTypes == null) return;

        // Crea bloques de Definición
        foreach (string blockType in blockTypes)
        {
            // Crea solo los bloques Define y IfReturn
            if (blockType.Equals(Define.DEFINE_NO_RETURN_BLOCK_TYPE) ||
                 blockType.Equals(Define.DEFINE_WITH_RETURN_BLOCK_TYPE))
                
            {
                BlockModel block = BlockFactory.Instance.CreateBlock(null, blockType);
                if (block != null)
                {
                    BlockView view = NewBlockViewInternal(block, m_blockContainer);
                    if (view != null) m_templateBlocks.Add(view.gameObject); else block.Dispose(); // Añadir a lista general
                }
            }
        }

        // Crea bloques de Llamada para cada procedimiento existente
        mProcedureCallerViews.Clear();
        foreach (BlockModel procDefBlock in m_currentWorkspace.ProcedureDB.GetDefinitionBlocks())
        {
            if (procDefBlock.Mutator is ProcedureDefinitionMutator mutator)
            {
                CreateProcedureCallerViewInternal(mutator.ProcedureInfo, ProcedureDB.HasReturn(procDefBlock));
            }
        }
    }
    private void CreateProcedureCallerViewInternal(Procedure procedureInfo, bool hasReturn)
    {
        if (procedureInfo == null) { Debug.LogError("CreateProcedureCallerViewInternal: procedureInfo is null!"); return; }
        if (mProcedureCallerViews.ContainsKey(procedureInfo.Name)) return; 

        string callBlockType = hasReturn ? Define.CALL_WITH_RETURN_BLOCK_TYPE : Define.CALL_NO_RETURN_BLOCK_TYPE;
        BlockModel block = BlockFactory.Instance.CreateBlock(null, callBlockType);
        if (block == null) { Debug.LogError($"Failed to create model for {callBlockType}"); return; }

        // Configurar el bloque de llamada
        if (block.Mutator is ProcedureMutator callerMutator)
        {
            // Mutate el bloque de llamada para que coincida con la definición
            callerMutator.Mutate(procedureInfo); // Configura argumentos, nombre, etc.
        }
        else
        {
            // Fallback si no hay mutator 
            block.SetFieldValue("NAME", procedureInfo.Name);
        }

        BlockView view = NewBlockViewInternal(block, m_blockContainer);
        if (view != null) mProcedureCallerViews[procedureInfo.Name] = view; else block.Dispose();
    }
    private void DeleteProcedureCallerViewInternal(Procedure procedureInfo)
    {
        if (procedureInfo == null) { Debug.LogError("DeleteProcedureCallerViewInternal: procedureInfo is null!"); return; }
        if (mProcedureCallerViews.TryGetValue(procedureInfo.Name, out BlockView view))
        {
            mProcedureCallerViews.Remove(procedureInfo.Name);
            if (view != null && view.gameObject != null) Destroy(view.gameObject);
        }
    }
    // Método que se llama desde el Observer
    protected new void OnProcedureUpdate(ProcedureUpdateData updateData)
    {
       
        if (m_currentCategory != Define.PROCEDURE_CATEGORY_NAME) return;
        Debug.Log($"<color=purple>OnProcedureUpdate: {updateData.Type} - {updateData.ProcedureInfo?.Name} -> {updateData.NewProcedureInfo?.Name}</color>");

        switch (updateData.Type)
        {
            case ProcedureUpdateData.Add:
                if (updateData.ProcedureDefinitionBlock != null && updateData.ProcedureInfo != null)
                    CreateProcedureCallerViewInternal(updateData.ProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock));
                else Debug.LogWarning("OnProcedureUpdate Add: Missing data");
                break;
            case ProcedureUpdateData.Remove:
                if (updateData.ProcedureInfo != null)
                    DeleteProcedureCallerViewInternal(updateData.ProcedureInfo);
                else Debug.LogWarning("OnProcedureUpdate Remove: Missing ProcedureInfo");
                break;
            case ProcedureUpdateData.Mutate:
                if (updateData.ProcedureInfo != null) DeleteProcedureCallerViewInternal(updateData.ProcedureInfo);
                if (updateData.NewProcedureInfo != null && updateData.ProcedureDefinitionBlock != null)
                {
                    CreateProcedureCallerViewInternal(updateData.NewProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock));
                }
               
                break;
        }
        LayoutRebuilder.MarkLayoutForRebuild(m_blockContainer as RectTransform);
    }


    //   obtener tipos de bloque
    private List<string> GetBlockTypesFromDefinitions(string categoryName)
    {
        var definitions = BlockFactory.Instance.GetAllBlockDefinitions();
        if (definitions == null) { Debug.LogError("GetBlockTypesFromDefinitions: Cannot access Block Definitions!"); return null; }

        if (definitions == null)
        {
            Debug.LogError("GetBlockTypesFromDefinitions: Could not retrieve block definitions from BlockFactory!");
            return new List<string>(); 
        }

        List<string> types = new List<string>();

        foreach (KeyValuePair<string, BlockDefinition> pair in definitions)
        {
            string blockType = pair.Key;
            BlockDefinition definition = pair.Value; 

           
            if (definition != null)
            {
                if (string.Equals(definition.category, categoryName, System.StringComparison.OrdinalIgnoreCase))
                {
                    types.Add(blockType);
                }
            }
        }

        if (types.Count == 0)
        {
            Debug.LogWarning($"GetBlockTypesFromDefinitions: No block types found for category '{categoryName}'. Check definitions and category names.");
        }

        return types;
    }

    protected override void Build()
    {
        throw new System.NotImplementedException();
    }

    public override bool CheckBin(BlockView blockView)
    {
        throw new System.NotImplementedException();
    }

    public override void FinishCheckBin(BlockView blockView)
    {
        throw new System.NotImplementedException();
    }


    #endregion

    #region Observadores

    //Clases Observadoras Internas  
    private class VariableObserver : IObserver<VariableUpdateData>
    {
        private BlockScrollListView mToolboxRef; 
        public VariableObserver(BlockScrollListView toolbox) { mToolboxRef = toolbox; }
        public void OnUpdated(object subject, VariableUpdateData args)
        {
            if (mToolboxRef == null || mToolboxRef.gameObject == null)
                ((Observable<VariableUpdateData>)subject).RemoveObserver(this); 
            else
                mToolboxRef.OnVariableUpdate(args);
        }
    }
    private class ProcedureObserver : IObserver<ProcedureUpdateData>
    {
        private BlockScrollListView mToolboxRef; 
        public ProcedureObserver(BlockScrollListView toolbox) { mToolboxRef = toolbox; }
        public void OnUpdated(object subject, ProcedureUpdateData args)
        {
            if (mToolboxRef == null || mToolboxRef.gameObject == null)
                ((Observable<ProcedureUpdateData>)subject).RemoveObserver(this);
            else
                mToolboxRef.OnProcedureUpdate(args);
        }
    }
    #endregion

}//fin clase BlockScrollListView