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
 * Versión: 2.0.0
 * 
 * Descripción: Esta clase se encarga de generar las imagenes de los bloques para su representación correcta
 * 
 */

using UnityEngine;
using UnityEngine.UI;      
using TMPro;           
using System.Collections.Generic;
using System;            
using System.Collections;
using System.Linq;


public class BlockListView : BaseToolbox
{
    [Header("UI Assignments")]
    private RectTransform m_categoryButtonContainer;
    private RectTransform m_blockTemplateScrollAreaContent;
    
    [SerializeField] private ScrollRect m_BlockTemplateScrollRect;
    [SerializeField] private TextMeshProUGUI m_CategoryTitleText;
    [SerializeField] private GameObject m_BinArea;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_CategoryButtonPrefab;
    private RectTransform m_BlockTemplateContainerRect;
    [SerializeField]
    private GameObject m_BlockViewPrefab;


    private CategoryController m_CategoryController;
    private string m_ActiveCategory = null;
    private bool isInitialized = false;

    protected WorkSpaceModel m_Workspace; 
    protected ToolboxConfig m_Config;
    protected WorkSpaceView m_WorkspaceView;

    new Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
    new List<BlockView> mVariableHelperViews = new List<BlockView>();
    new Dictionary<string, BlockView> mProcedureCallerViews = new Dictionary<string, BlockView>();

    private VariableObserver mVarObserver;
    private ProcedureObserver mProcObserver;

    public WorkSpaceView WorkspaceViewForFactory => m_WorkspaceView;

  //  protected Dictionary<string, GameObject> m_RootList = new Dictionary<string, GameObject>(); 
    protected Dictionary<string, Toggle> m_CategoryToggles = new Dictionary<string, Toggle>();

    public void InitializeToolbox(WorkSpaceModel workspace, ToolboxConfig config, WorkSpaceView workspaceView,
                                   RectTransform categoryButtonContainer, ScrollRect blockTemplateScrollRect)
    {
        if (isInitialized) return;

        Debug.Log("<color=lightblue>BlockListView: Initializing FULL Toolbox...</color>");

        m_Workspace = workspace;
        m_Config = config;
        m_WorkspaceView = workspaceView;
        
        m_categoryButtonContainer = categoryButtonContainer;
        m_BlockTemplateScrollRect = blockTemplateScrollRect;

        if (m_Workspace == null || m_Config == null )
        {
            Debug.LogError("BlockListView: Initialization failed due to missing references.");
            this.enabled = false;
            return;
        }

        if (m_BlockTemplateScrollRect == null)
        {
            Debug.LogError("BlockListView: Block Template ScrollRect is null!", this);
            this.enabled = false;
            return;
        }

        if (m_WorkspaceView == null) Debug.LogError("BlockListView InitializeToolbox: WorkspaceView is NULL!");
        if (m_categoryButtonContainer == null) Debug.LogError("BlockListView InitializeToolbox: Category Button Container is NULL!");
        if (m_BlockTemplateScrollRect == null) Debug.LogError("BlockListView InitializeToolbox: Block Template Scroll Rect is NULL!");

        if (m_BlockTemplateScrollRect.content == null)
        {
            Debug.Log("BlockListView: Creating Block Template Container dynamically...");
            m_BlockTemplateContainerRect = CreateAndConfigureBlockContainer(m_BlockTemplateScrollRect);
            m_BlockTemplateScrollRect.content = m_BlockTemplateContainerRect;
            m_BlockTemplateScrollRect.vertical = true; 
            m_BlockTemplateScrollRect.horizontal = false;
        }
        else
        {
            
            Debug.LogWarning("BlockListView: ScrollRect Content was pre-assigned. Using existing.", this);
            m_BlockTemplateContainerRect = m_BlockTemplateScrollRect.content as RectTransform;
        }

        
        if (m_BlockTemplateContainerRect == null)
        {
            Debug.LogError("BlockListView: Failed to assign or create Block Template Container!", this);
            this.enabled = false;
            return;
        }

       
       /* if (m_CategoryButtonPrefab == null) 
        {
            Debug.LogError("BlockListView: Missing Category Button Prefab! Assign it in the inspector.", this);
            this.enabled = false; 
        }*/

        if (m_BlockViewPrefab == null)
            m_BlockViewPrefab = Resources.Load<GameObject>("Prefabs/BlocksPrefab/Stack_block_grey"); 
        if (m_CategoryButtonPrefab == null)
            m_CategoryButtonPrefab = Resources.Load<GameObject>("Prefabs/CategoryButtonPrefab"); 

      
        if (m_Workspace != null)
        {
            mVarObserver = new VariableObserver(this);
            mProcObserver = new ProcedureObserver(this);
            m_Workspace.VariableMap.AddObserver(mVarObserver);
            m_Workspace.ProcedureDB.AddObserver(mProcObserver);
            Debug.Log("<color=lightblue>BlockListView: Observers registered.</color>");
        }
        else
        {
            Debug.LogWarning("BlockListView: WorkspaceModel is null, cannot register observers.");
        }

        isInitialized = true;

        if (m_Config != null)
        {
            Build(); 
        }
        else
        {
            Debug.LogWarning("BlockListView initialized but ToolboxConfig missing, Build() deferred.");
        }
    }

    /**
     * Descripción: Método que crea dinámicamente el GameObject que servirá como contenedor para las plantillas de bloques dentro del ScrollRect de la Toolbox.
     * Configura RectTransform, VerticalLayoutGroup y ContentSizeFitter.
     * @param scrollRectComponent: El componente ScrollRect al que se añadirá el contenedor.
     * @return: El RectTransform del contenedor creado.
     */
    private RectTransform CreateAndConfigureBlockContainer(ScrollRect scrollRectComponent)
    {
        GameObject containerGO = new GameObject("BlockContainer_Generated"); 
        RectTransform parentRect = scrollRectComponent.transform as RectTransform;
        containerGO.transform.SetParent(parentRect, false); 
        containerGO.layer = parentRect.gameObject.layer; 

        // RectTransform 
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);       // Arriba-Izquierda
        containerRect.anchorMax = new Vector2(1, 1);       // Arriba-Derecha (Stretch Horizontal)
        containerRect.pivot = new Vector2(0.5f, 1);     // Pivote Arriba-Centro
        containerRect.anchoredPosition = Vector2.zero; // Posición relativa al anchor
        containerRect.sizeDelta = new Vector2(0, 100);   // Ancho 0 (se estira), Altura inicial

        // Vertical Layout Group - VLG
        VerticalLayoutGroup layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(5, 5, 10, 5);  
        layoutGroup.spacing = 8f;                           
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;               // Hijos toman el ancho del contenedor
        layoutGroup.childControlHeight = false;              // Hijos definen su propia altura
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // Content Size Fitter 
        ContentSizeFitter fitter = containerGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; 
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Ajusta la altura al contenido

        // Image (Invisible)
        Image bgImage = containerGO.AddComponent<Image>();
        bgImage.color = Color.clear; // Transparente
        bgImage.raycastTarget = false; //No necesita interceptar clicks

        Debug.Log($"Dynamically created Block Container '{containerGO.name}'", containerGO);
        return containerRect;
    }

    protected override void Build()
    {
        if (!isInitialized) { Debug.LogWarning("BlockListView.Build called before initialization."); return; }
        if (m_Config == null || m_Config.BlockCategoryList == null || m_Config.BlockCategoryList.Count == 0)
        {
            Debug.LogError("BlockListView(BaseToolbox): Cannot Build, ToolboxConfig is missing or empty!");
            return;
        }
        Debug.Log($"BlockListView(BaseToolbox): Build started with {m_Config.BlockCategoryList.Count} categories.");
        BuildMenu();

        if (m_Config.BlockCategoryList.Count > 0)
        {
            StartCoroutine(SelectFirstCategoryAfterBuild());
        }
    }

    private IEnumerator SelectFirstCategoryAfterBuild()
    {
        yield return null; 
        if (m_Config != null && m_Config.BlockCategoryList.Count > 0)
        {
            string firstCategoryName = m_Config.BlockCategoryList[0].CategoryName;
            if (m_CategoryToggles.TryGetValue(firstCategoryName, out Toggle firstToggle))
            {
                if (firstToggle != null) 
                {
                    firstToggle.isOn = true; 
                }
                else
                {
                    Debug.LogWarning($"First category toggle '{firstCategoryName}' became null before selection.");
                    ShowBlockCategory(firstCategoryName, m_Config.GetBlockCategory(firstCategoryName)?.Color ?? Color.grey);
                }
            }
            else
            {
                Debug.LogError($"Could not find toggle for the first category '{firstCategoryName}' after building the menu.");
                ShowBlockCategory(firstCategoryName, m_Config.GetBlockCategory(firstCategoryName)?.Color ?? Color.grey);
            }
        }
    }
    protected virtual void BuildMenu()
    {
        Debug.Log($"BuildMenu: Category count: {m_Config.BlockCategoryList?.Count ?? 0}. Container valid: {m_categoryButtonContainer != null}");
        if (m_categoryButtonContainer == null) { Debug.LogError("Category Button container is null in BuildMenu!"); return; }

        ToggleGroup toggleGroup = m_categoryButtonContainer.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
        {
            Debug.LogWarning("Adding ToggleGroup to CategoryButtonContainer dynamically.");
            toggleGroup = m_categoryButtonContainer.gameObject.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false; 
        }

        ClearCategoryButtons(); 

        foreach (var category in m_Config.BlockCategoryList)
        {
            string categoryName = category.CategoryName;
            string displayName = I18n.Contains(categoryName) ? I18n.Get(categoryName) : categoryName;
            Color color = category.Color;

            GameObject buttonGO = CreateCategoryButtonUI(displayName, categoryName, color, toggleGroup);

            Toggle toggle = buttonGO.GetComponent<Toggle>(); 
            if (toggle != null)
            {
                string currentCategoryName = categoryName; 
                Color currentCategoryColor = color;       
                toggle.onValueChanged.AddListener((isOn) => {
                    if (isOn)
                    {
                        ShowBlockCategory(currentCategoryName, currentCategoryColor);
                    }
                });
                m_CategoryToggles[categoryName] = toggle;
            }
            else { Debug.LogError($"Failed to get Toggle component for category button '{categoryName}'!"); }

        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_categoryButtonContainer);
        StartCoroutine(DelayedLayoutRebuild(m_categoryButtonContainer)); 
    }

    private GameObject CreateCategoryButtonUI(string displayName, string categoryKey, Color color, ToggleGroup toggleGroup)
    {
        if (m_CategoryButtonPrefab == null)
        {
            Debug.LogError("Category Button Prefab is not loaded/assigned!");
            return null;
        }
        GameObject buttonGO = Instantiate(m_CategoryButtonPrefab, m_categoryButtonContainer);
        buttonGO.name = $"CategoryBtn_{categoryKey}";

        Image bgImage = buttonGO.GetComponent<Image>(); 
        Image iconImage = buttonGO.transform.Find("Icon")?.GetComponent<Image>();
        TextMeshProUGUI labelText = buttonGO.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

        Toggle toggle = buttonGO.GetComponent<Toggle>();
        if (toggle == null) toggle = buttonGO.AddComponent<Toggle>(); 
        toggle.group = toggleGroup;
        toggle.isOn = false; 

        if (labelText != null) labelText.text = displayName;
        else Debug.LogWarning($"Category Button Prefab missing 'Label' TextMeshProUGUI for {categoryKey}");

        if (bgImage != null)
        {
            bgImage.color = color;
            toggle.targetGraphic = bgImage; 
        }
        else { Debug.LogWarning($"Category Button Prefab missing background Image for {categoryKey}"); }

        ColorBlock cb = toggle.colors;
        cb.normalColor = Color.white * 0.8f; 
        cb.highlightedColor = Color.white;
        cb.pressedColor = Color.grey;
        cb.selectedColor = Color.white;    
        toggle.colors = cb;

        if (iconImage != null)
        {
            toggle.graphic = iconImage; 
            iconImage.color = Color.Lerp(color, Color.black, 0.3f);
            iconImage.raycastTarget = false;
        }
        else { Debug.LogWarning($"Category Button Prefab missing 'Icon' Image for {categoryKey}"); }

        return buttonGO;
    }

    public void ShowBlockCategory(string categoryName, Color categoryColor)
    {
        if (!isInitialized) { Debug.LogWarning("BlockListView not initialized. Cannot show category."); return; }
        if (string.IsNullOrEmpty(categoryName)) { Debug.LogError("ShowBlockCategory called with null or empty category name."); return; }
        if (m_BlockTemplateScrollRect == null) { Debug.LogError("BlockListView: m_BlockTemplateScrollRect is not assigned!"); return; }

        if (m_BlockTemplateContainerRect == null)
        {
            if (m_BlockTemplateScrollRect.content != null)
            {
                m_BlockTemplateContainerRect = m_BlockTemplateScrollRect.content as RectTransform;
                if (m_BlockTemplateContainerRect == null)
                {
                    Debug.LogError("ShowBlockCategory: ScrollRect content exists but is not a RectTransform!");
                    return;
                }
            }
            else
            {
                Debug.LogError("ShowBlockCategory: Both m_BlockTemplateContainerRect and ScrollRect.content are null!");
                return;
            }
        }

        if (categoryColor == default(Color))
        {
            ToolboxBlockCategory categoryConf = m_Config?.GetBlockCategory(categoryName);
            categoryColor = categoryConf?.Color ?? Color.grey;
        }

        Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Switching to category '{categoryName}'. Color: {categoryColor}");

        m_ActiveCategory = categoryName; 
        if (m_CategoryTitleText != null)
        {
            m_CategoryTitleText.text = I18n.Contains(categoryName) ? I18n.Get(categoryName) : categoryName;
        }

        PopulateContainer(categoryName, m_BlockTemplateContainerRect, categoryColor);

        StartCoroutine(DelayedLayoutRebuild(m_BlockTemplateContainerRect));
        ScrollRect scrollRectComponent = m_BlockTemplateScrollRect.GetComponent<ScrollRect>();
        if (scrollRectComponent != null)
        {
            StartCoroutine(DelayedScrollToTop(scrollRectComponent));
        }
        else
        {
            Debug.LogError("BlockListView: ScrollRect component missing!");
        }
    }

    /*
    private void ConfigureContainerLayout(GameObject containerGO)
    {
        VerticalLayoutGroup vlg = containerGO.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.spacing = 8f;                     
        vlg.childControlWidth = true;         
        vlg.childControlHeight = false;       
        vlg.childAlignment = TextAnchor.UpperLeft; 
        vlg.childForceExpandWidth = false;   
        vlg.childForceExpandHeight = false;  

        ContentSizeFitter csf = containerGO.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = containerGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; 
    }*/

    private void PopulateContainer(string categoryName, RectTransform containerRectTransform, Color categoryColor)
    {
        Debug.Log($"<color=#ADD8E6>BlockListView.PopulateContainer:</color> Populating container for '{categoryName}'...");

        foreach (Transform child in containerRectTransform) { Destroy(child.gameObject); }

        if (categoryName.Equals(Define.VARIABLE_CATEGORY_NAME))
        {
            BuildVariableBlocksInternal(containerRectTransform);
        }
        else if (categoryName.Equals(Define.PROCEDURE_CATEGORY_NAME))
        {
            BuildProcedureBlocksInternal(containerRectTransform);
        }
        else
        {
            BuildBlockViewsForStaticCategory(categoryName, containerRectTransform, categoryColor);
        }
    }
    private void BuildBlockViewsForStaticCategory(string categoryName, RectTransform containerRectTransform, Color categoryColor)
    {
        if (m_Config == null) { Debug.LogError("Cannot build static category blocks: ToolboxConfig is null."); return; }

        var categoryConfig = m_Config.GetBlockCategory(categoryName);
        if (categoryConfig == null || categoryConfig.BlockList == null)
        {
            Debug.LogWarning($"No category config or block list found for static category: {categoryName}");
            ShowEmptyMessage($"No blocks defined for '{categoryName}'.", containerRectTransform);
            return;
        }

        var blockTypes = categoryConfig.BlockList;
        Debug.Log($"BuildBlockViewsForStaticCategory '{categoryName}': {blockTypes.Count} types.");

        if (blockTypes.Count == 0)
        {
            ShowEmptyMessage($"Category '{I18n.Get(categoryName)}' is empty.", containerRectTransform);
            return;
        }

        foreach (string blockType in blockTypes)
        {
            if (string.IsNullOrEmpty(blockType)) continue; 
            BlockView view = NewBlockView(blockType, containerRectTransform);
            if (view != null)
            {
                view.ChangeBgColor(categoryColor); 
                if (view.transform.parent != containerRectTransform)
                {
                    view.transform.SetParent(containerRectTransform, false);
                }
            }
            else
            {
                Debug.LogWarning($"Failed to create BlockView for type '{blockType}' in category '{categoryName}'.");
            }
        }
    }

    private void BuildVariableBlocksInternal(RectTransform container)
    {
        if (container == null) { Debug.LogError("BuildVariableBlocksInternal: Target container is null!"); return; }
        if (m_Workspace == null) { Debug.LogError("BuildVariableBlocksInternal: Workspace is null!"); return; }


        GameObject createVarPrefab = BlockViewSettings.Get()?.PrefabBtnCreateVar;
        if (createVarPrefab != null)
        {
            GameObject btnGO = Instantiate(createVarPrefab, container);
            Text buttonText = btnGO.GetComponentInChildren<Text>();
            if (buttonText != null) buttonText.text = I18n.Get(MsgDefine.NEW_VARIABLE);
            else Debug.LogError("PrefabBtnCreateVar is missing Text component in children.", btnGO);

            Color categoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME); 
            Image buttonImage = btnGO.GetComponentInChildren<Image>();
            if (buttonImage != null) buttonImage.color = categoryColor;
            else Debug.LogWarning("PrefabBtnCreateVar missing Image component in children.");

            Button button = btnGO.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(() => DialogFactory.CreateDialog(DialogFactory.VARIABLE_NAME_DIALOG_NAME));
            else Debug.LogError("PrefabBtnCreateVar missing Button component.", btnGO);

            CanvasGroup cg = btnGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = btnGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true; 

        }
        else { Debug.LogWarning("PrefabBtnCreateVar not found in BlockViewSettings"); }

        CreateVariableHelperViewsInternal(container);

        mVariableGetterViews.Clear();
        List<VariableModel> allVars = m_Workspace.GetAllVariables();
        foreach (VariableModel variable in allVars)
        {
            CreateVariableGetterViewInternal(variable.Name, container);
        }
        Debug.Log($"BuildVariableBlocksInternal: Built helpers ({mVariableHelperViews.Count}) and getters ({mVariableGetterViews.Count})");
    }

    private void CreateVariableHelperViewsInternal(RectTransform container)
    {

        mVariableHelperViews.Clear();

        List<VariableModel> allVars = m_Workspace.GetAllVariables();
        if (allVars.Count == 0) return; 

        string firstVarName = allVars[0].Name; 

        List<string> helperBlockTypes = GetBlockTypesFromDefinitions(Define.VARIABLE_CATEGORY_NAME)
                                       ?.Where(type => type != Define.VARIABLE_GET_BLOCK_TYPE)
                                       .ToList() ?? new List<string>(); 

        if (helperBlockTypes.Count == 0)
        {
            //Debug.Log("No variable helper block types found in definitions.");
            return; 
        }


        Color variableCategoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME);

        foreach (string blockType in helperBlockTypes)
        {
            BlockModel block = BlockFactory.Instance.CreateBlock(m_Workspace, blockType); 
            if (block != null)
            {
                try { block.SetFieldValue("VAR", firstVarName); }
                catch (Exception e) { Debug.LogWarning($"Failed to set default variable '{firstVarName}' on helper block '{blockType}': {e.Message}"); }

                BlockView view = NewBlockView(blockType, container); 


                if (view != null)
                {
                    view.ChangeBgColor(variableCategoryColor);
                    mVariableHelperViews.Add(view);
                }
                else { block.Dispose(); } 
            }
            else { Debug.LogWarning($"Could not create block model for variable helper type {blockType}"); }

        }
    }

    private void CreateVariableGetterViewInternal(string varName, RectTransform container)
    {
        if (mVariableGetterViews.ContainsKey(varName))
        {
          
            return;
        }

        BlockModel block = BlockFactory.Instance.CreateBlock(m_Workspace, Define.VARIABLE_GET_BLOCK_TYPE);
        if (block == null) { Debug.LogError("Failed to create VARIABLE_GET_BLOCK_TYPE model"); return; }
        block.SetFieldValue("VAR", varName);

        BlockView view = NewBlockView(Define.VARIABLE_GET_BLOCK_TYPE, container); 

        if (view != null)
        {
            Color variableCategoryColor = BlockDataLoader.GetColorForCategoryPublic(Define.VARIABLE_CATEGORY_NAME);
            view.ChangeBgColor(variableCategoryColor);
            mVariableGetterViews[varName] = view;
        }
        else { block.Dispose(); } 
    }

    private void DeleteVariableGetterViewInternal(string varName)
    {
        if (mVariableGetterViews.TryGetValue(varName, out BlockView view))
        {
            mVariableGetterViews.Remove(varName); 
            if (view != null && view.gameObject != null) 
            {
                // Debug.Log($"Destroying variable getter view for {varName}");
                Destroy(view.gameObject);
            }
        }
    }

    protected void BuildProcedureBlocksInternal(RectTransform container)
    {
        if (container == null) { Debug.LogError("BuildProcedureBlocksInternal: Target container is null!"); return; }
        if (m_Workspace == null) { Debug.LogError("BuildProcedureBlocksInternal: Workspace is null!"); return; }


        List<string> blockTypes = GetBlockTypesFromDefinitions(Define.PROCEDURE_CATEGORY_NAME);

        if (blockTypes == null) return;

        foreach (string blockType in blockTypes)
        {
            if (blockType.Equals(Define.DEFINE_NO_RETURN_BLOCK_TYPE) ||
                 blockType.Equals(Define.DEFINE_WITH_RETURN_BLOCK_TYPE))
            {
                BlockView view = NewBlockView(blockType, container);
                if (view != null)
                {
                    view.ChangeBgColor(BlockDataLoader.GetColorForCategoryPublic(Define.PROCEDURE_CATEGORY_NAME));

                }
            }
        }

        mProcedureCallerViews.Clear();

        foreach (BlockModel procDefBlock in m_Workspace.ProcedureDB.GetDefinitionBlocks())
        {
            Procedure procedureInfo = null;
            if (procDefBlock.Mutator is ProcedureDefinitionMutator definitionMutator)
            {
                procedureInfo = definitionMutator.ProcedureInfo;
            }
            else if (procDefBlock.Mutator is ProcedureMutator baseProcMutator)
            {

                try
                {//TODO

                }
                catch
                {
                    //TODO
                }


                if (procedureInfo == null)
                {
                    BlockView associatedView = null;
                    if (m_WorkspaceView != null) 
                    {
                        associatedView = m_WorkspaceView.GetBlockView(procDefBlock);
                    }
                    Debug.LogError($"Could not retrieve ProcedureInfo from the Mutator of definition block {procDefBlock.Type} ({procDefBlock.ID})",
                                                 associatedView != null ? associatedView.gameObject : this.gameObject);
                    continue;
                }

                bool hasReturn = ProcedureDB.HasReturn(procDefBlock); 

                CreateProcedureCallerViewInternal(procedureInfo, hasReturn, container);
            }
            Debug.Log($"BuildProcedureBlocksInternal: Built defs and callers ({mProcedureCallerViews.Count})");

        }
    }

    private void CreateProcedureCallerViewInternal(Procedure procedureInfo, bool hasReturn, RectTransform container)
    {
        if (procedureInfo == null) { Debug.LogError("CreateProcedureCallerViewInternal: procedureInfo is null!"); return; }
        if (container == null) { Debug.LogError($"CreateProcedureCallerViewInternal: container is null for procedure {procedureInfo.Name}!"); return; }
        if (mProcedureCallerViews.ContainsKey(procedureInfo.Name)) return;


        string callBlockType = hasReturn ? Define.CALL_WITH_RETURN_BLOCK_TYPE : Define.CALL_NO_RETURN_BLOCK_TYPE;
        BlockModel block = BlockFactory.Instance.CreateBlock(m_Workspace, callBlockType); 
        if (block == null) { Debug.LogError($"Failed to create model for {callBlockType}"); return; }

        if (block.Mutator is ProcedureMutator callerMutator) { callerMutator.Mutate(procedureInfo); }
        else { block.SetFieldValue("NAME", procedureInfo.Name); } 

        BlockView view = NewBlockView(callBlockType, container);

        if (view != null)
        {
            view.ChangeBgColor(BlockDataLoader.GetColorForCategoryPublic(Define.PROCEDURE_CATEGORY_NAME));
            mProcedureCallerViews[procedureInfo.Name] = view;
        }
        else { block.Dispose(); } 
    }

    private void DeleteVariableHelperViewsInternal()
    {
        foreach (BlockView view in mVariableHelperViews)
        {
            if (view != null && view.gameObject != null)
            {
                // Debug.Log($"Destroying variable helper view {view.gameObject.name}");
                Destroy(view.gameObject);
            }
        }
        mVariableHelperViews.Clear(); 
    }

    private void DeleteProcedureCallerViewInternal(Procedure procedureInfo)
    {
        if (procedureInfo == null) { Debug.LogError("DeleteProcedureCallerViewInternal: procedureInfo is null!"); return; }
        if (mProcedureCallerViews.TryGetValue(procedureInfo.Name, out BlockView view))
        {
            mProcedureCallerViews.Remove(procedureInfo.Name);
            if (view != null && view.gameObject != null)
            {
                //Debug.Log($"Destroying procedure caller view for {procedureInfo.Name}");
                Destroy(view.gameObject);
            }
        }
    }

    public new void OnVariableUpdate(VariableUpdateData updateData)
    {
        if (!isInitialized || m_Workspace == null) return; 
        if (m_ActiveCategory != Define.VARIABLE_CATEGORY_NAME) return;

       /*if (!m_RootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out GameObject containerGO) || containerGO == null)
        {
            //Debug.LogWarning("Variable update received, but Variables container not found or invalid. Cannot update UI.");
            return;
        }*/

        RectTransform container = m_BlockTemplateContainerRect; //containerGO.GetComponent<RectTransform>();
        if (container == null)
        {
            Debug.LogWarning("Variable container GameObject exists, but missing RectTransform.");
            return;
        }

        //Debug.Log($"<color=orange>BlockListView.OnVariableUpdate: {updateData.Type} - {updateData.VarName} -> {updateData.NewVarName}</color> in container {container.name}");

        int variableCountBefore = m_Workspace.GetAllVariables().Count - (updateData.Type == VariableUpdateData.Create ? 1 : 0) + (updateData.Type == VariableUpdateData.Delete ? 1 : 0);
        int variableCountAfter = m_Workspace.GetAllVariables().Count;

        switch (updateData.Type)
        {
            case VariableUpdateData.Create:
                if (variableCountBefore == 0) 
                {
                    //Debug.Log("First variable created, building helper blocks.");
                    CreateVariableHelperViewsInternal(container); 
                }
                CreateVariableGetterViewInternal(updateData.VarName, container); 
                break;

            case VariableUpdateData.Delete:
                DeleteVariableGetterViewInternal(updateData.VarName); 
                if (variableCountAfter == 0) 
                {
                    //Debug.Log("Last variable deleted, removing helper blocks.");
                    DeleteVariableHelperViewsInternal(); 
                }
                else if (variableCountBefore > 0) 
                {
                    string remainingVarName = m_Workspace.GetAllVariables().FirstOrDefault()?.Name;
                    if (remainingVarName != null)
                    {
                        foreach (BlockView helperView in mVariableHelperViews)
                        {
                            if (helperView.Block != null && helperView.Block.GetFieldValue("VAR").Equals(updateData.VarName))
                            {
                               
                                //Debug.Log($"Updating helper block {helperView.Block.Type} from deleted var {updateData.VarName} to {remainingVarName}");
                                helperView.Block.SetFieldValue("VAR", remainingVarName);
                                FieldView fieldView = helperView.ChildViews.OfType<LineGroupView>().SelectMany(lg => lg.ChildViews.OfType<InputView>()).SelectMany(iv => iv.ChildViews.OfType<FieldView>()).FirstOrDefault(fv => fv.FieldModel != null && fv.FieldModel.Name == "VAR");
                                fieldView?.ForceUpdateDisplayFromModel();
                            }
                        }
                    }
                }
                break;

            case VariableUpdateData.Rename:
                if (mVariableGetterViews.TryGetValue(updateData.VarName, out BlockView viewToRename))
                {
                    mVariableGetterViews.Remove(updateData.VarName);
                    mVariableGetterViews[updateData.NewVarName] = viewToRename;
                    //Debug.Log($"BlockListView Cache: Renamed getter view entry {updateData.VarName} -> {updateData.NewVarName}");
                }
                else { Debug.LogWarning($"Rename: Could not find getter view for old name {updateData.VarName} in cache."); }
                break;
        }

        StartCoroutine(DelayedLayoutRebuild(container));
    }

    protected new void OnProcedureUpdate(ProcedureUpdateData updateData)
    {
        if (!isInitialized || m_Workspace == null) return; 
        if (m_ActiveCategory != Define.PROCEDURE_CATEGORY_NAME) return;

        /*if (!m_RootList.TryGetValue(Define.PROCEDURE_CATEGORY_NAME, out GameObject containerGO) || containerGO == null)
        {
            Debug.LogWarning("Procedure update received, but Procedures container not found or invalid. Cannot update UI.");
            return;
        }*/

        RectTransform container = m_BlockTemplateContainerRect; //containerGO.GetComponent<RectTransform>();
        if (container == null)
        {
            Debug.LogWarning("Procedure container GameObject exists, but missing RectTransform.");
            return;
        }

        //Debug.Log($"<color=purple>BlockListView.OnProcedureUpdate: {updateData.Type} - Info: {updateData.ProcedureInfo?.Name} -> NewInfo: {updateData.NewProcedureInfo?.Name}</color> in container {container.name}");


        switch (updateData.Type)
        {
            case ProcedureUpdateData.Add:
                if (updateData.ProcedureDefinitionBlock != null && updateData.ProcedureInfo != null)
                    CreateProcedureCallerViewInternal(updateData.ProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock), container); 
                else Debug.LogWarning("OnProcedureUpdate Add: Missing ProcedureInfo or DefinitionBlock");
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
                    CreateProcedureCallerViewInternal(updateData.NewProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock), container); 
                }
                else if (updateData.NewProcedureInfo == null) { Debug.LogWarning("Procedure mutate: NewProcedureInfo is null"); }
                else { Debug.LogWarning("Procedure mutate: ProcedureDefinitionBlock is null"); }

                break;
        }

        StartCoroutine(DelayedLayoutRebuild(container));
    }

    public  BlockView NewBlockView(string blockType, Transform parent = null, int index = -1)
    {
        if (m_Workspace == null) { Debug.LogError("NewBlockView: Workspace model is null, cannot create block template."); return null; }
        if (m_WorkspaceView == null) { Debug.LogError("NewBlockView: Workspace view is null, BlockViewFactory needs it."); return null; }

        BlockModel templateModel = BlockFactory.Instance.CreateBlock(m_Workspace, blockType);
        if (templateModel == null)
        {
            Debug.LogWarning($"Could not create template MODEL for type: {blockType}");
            return null;
        }
        m_Workspace.RemoveTopBlock(templateModel);

        BlockView view = BlockViewFactory.CreateView(templateModel, this); 
        if (view == null)
        {
            Debug.LogError($"BlockViewFactory failed for block type {blockType}");
            templateModel.Dispose(); 
            return null;
        }

        view.InToolbox = true;

        if (parent == null) parent = m_BlockTemplateScrollRect.content; 
        view.transform.SetParent(parent, false); 
        view.transform.localScale = Vector3.one; 

        if (index >= 0) view.transform.SetSiblingIndex(index);

        if (m_WorkspaceView != null)
        {
            ToolboxBlockDragger dragger = view.gameObject.GetComponent<ToolboxBlockDragger>();
            if (dragger == null) dragger = view.gameObject.AddComponent<ToolboxBlockDragger>();
            dragger.Init(m_WorkspaceView); 
        }
        else { Debug.LogError($"Cannot initialize ToolboxBlockDragger on block {blockType}, WorkspaceView reference is missing!", view.gameObject); }


        return view;
    }

    protected override List<BlockView> BuildVariableBlocks()
    {
        if (m_ActiveCategory != Define.VARIABLE_CATEGORY_NAME)
        {
            Debug.LogWarning("BuildVariableBlocks() called when Variable category not active. Returning potentially stale cache.");
        }

        List<BlockView> allVarViews = new List<BlockView>();
        allVarViews.AddRange(mVariableHelperViews);
        allVarViews.AddRange(mVariableGetterViews.Values);
        return allVarViews;
    }

    protected override List<BlockView> BuildProcedureBlocks()
    {
        if (m_ActiveCategory != Define.PROCEDURE_CATEGORY_NAME)
        {
            Debug.LogWarning("BuildProcedureBlocks() called when Procedure category not active. Returning potentially stale cache.");
        }

        return new List<BlockView>(mProcedureCallerViews.Values);
    }

    public override bool CheckBin(BlockView blockView)
    {
        if (m_WorkspaceView == null || m_WorkspaceView.RootCanvas == null) { Debug.LogWarning("CheckBin: WorkspaceView/Canvas not ready."); return false; }
        if (blockView == null || blockView.InToolbox) return false; 
        if (m_BinArea == null) { Debug.LogWarning("CheckBin: Bin Area not assigned."); return false; }

        Camera eventCamera = m_WorkspaceView.RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_WorkspaceView.RootCanvas.worldCamera;
      
        Vector2 screenPoint = Input.mousePosition;

        bool contains = RectTransformUtility.RectangleContainsScreenPoint(m_BinArea.transform as RectTransform, screenPoint, eventCamera);

        if (m_BinArea.activeSelf != contains) m_BinArea.SetActive(contains);

        return contains;
    }

    public override void FinishCheckBin(BlockView blockView)
    {
        if (m_BinArea == null || blockView == null) return;

        if (CheckBin(blockView))
        {
            Debug.Log($"Block {blockView.BlockType} ({blockView.Block?.ID}) dropped in Bin. Requesting delete via WorkspaceController.");
            WorkspaceController.Instance?.RequestDeleteBlock(blockView.Block);
         
        }

        m_BinArea.SetActive(false); 
    }

    private void ShowEmptyMessage(string message, RectTransform container)
    {
        if (container == null) { Debug.LogError("ShowEmptyMessage: container is null!"); return; }
        foreach (Transform child in container) { Destroy(child.gameObject); }

        GameObject messageGO = new GameObject("EmptyCategoryMessage");
        messageGO.transform.SetParent(container, false);
        TextMeshProUGUI text = messageGO.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.color = Color.grey;
        text.alignment = TextAlignmentOptions.Center;
        LayoutElement le = messageGO.AddComponent<LayoutElement>();
        le.minHeight = 50; 
    }

    private List<string> GetBlockTypesFromDefinitions(string categoryName)
    {
        var definitions = BlockFactory.Instance.GetAllBlockDefinitions();
        if (definitions == null) { Debug.LogError("GetBlockTypesFromDefinitions: Cannot access Block Definitions!"); return null; }

        List<string> types = new List<string>();
        foreach (KeyValuePair<string, BlockDefinition> pair in definitions)
        {
            string blockType = pair.Key;
            BlockDefinition definition = pair.Value;
            if (definition != null && string.Equals(definition.category, categoryName, StringComparison.OrdinalIgnoreCase))
            {
                types.Add(blockType);
            }
        }
        //if (types.Count == 0) { Debug.LogWarning($"GetBlockTypesFromDefinitions: No block types found for category '{categoryName}'.");}
        return types;
    }

    void OnDestroy()
    {
        if (m_Workspace != null)
        {
            if (mVarObserver != null) m_Workspace.VariableMap?.RemoveObserver(mVarObserver);
            if (mProcObserver != null) m_Workspace.ProcedureDB?.RemoveObserver(mProcObserver);
        }

        ClearCategoryButtons(); 
      /*  foreach (GameObject container in m_RootList.Values) 
        {
            if (container != null) Destroy(container);
        }
        m_RootList.Clear();*/
        mVariableGetterViews.Clear(); 
        mVariableHelperViews.Clear();
        mProcedureCallerViews.Clear();

        ClearBlockTemplates();
        
    }

    protected virtual void BuildBlockViewsForActiveCategory(RectTransform containerRectTransform)
    {
        if (string.IsNullOrEmpty(mActiveCategory) || !mRootList.ContainsKey(mActiveCategory)) return;

        Transform contentTrans = mRootList[mActiveCategory].transform; 
        var categoryConfig = mConfig.GetBlockCategory(mActiveCategory);
        var blockTypes = categoryConfig.BlockList;

        Debug.Log($"BuildBlockViewsForActiveCategory '{mActiveCategory}': {blockTypes.Count} types.");

        foreach (Transform child in contentTrans) { Destroy(child.gameObject); }

        if (blockTypes == null || blockTypes.Count == 0)
        {
            GameObject messageGO = new GameObject("EmptyCategoryMessage");
            messageGO.transform.SetParent(contentTrans, false);
            TextMeshProUGUI text = messageGO.AddComponent<TextMeshProUGUI>();
            text.text = $"No blocks found for category '{I18n.Get(mActiveCategory)}'."; 
            text.color = new Color(0.6f, 0.6f, 0.6f);
            text.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (string blockType in blockTypes)
        {
            try
            {
               
                BlockView view = NewBlockView(blockType,this, contentTrans); 
                if (view != null)
                {
                  
                    view.UpdateColor();
                }
                else
                {
                    Debug.LogWarning($"NewBlockView returned null for type: {blockType}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating BlockView in Toolbox for type '{blockType}': {e.Message}\n{e.StackTrace}");
            }
        }
       
    }

    protected void BuildVariableBlocks(RectTransform container)
    {
        if (container == null || mWorkspace == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        List<BlockView> views = BuildVariableBlocks(); 
        foreach (var view in views)
        {
            view.transform.SetParent(container, false);
        }
    }

    protected virtual void BuildProcedureBlocks(RectTransform container)
    {
        if (container == null || mWorkspace == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        List<BlockView> views = BuildProcedureBlocks(); 
        foreach (var view in views)
        {
            view.transform.SetParent(container, false); 
        }
    }

    // Corutinas para esperar al layout
    private IEnumerator DelayedLayoutRebuild(RectTransform rect)
    {
        yield return null;
        if (rect != null && rect.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

    }
 
    // Muestra los bloques para la categoría dada 
    public void ShowBlocksForCategory(string categoryName, BaseToolbox sourceToolbox,  Color categoryColor, List<string> blockTypes)
    {
        if (!isInitialized) { Debug.LogWarning("BlockListView not initialized."); return; }
        if (m_blockTemplateScrollAreaContent == null) { Debug.LogError("BlockModel Template container is null!"); return; }

        m_ActiveCategory = categoryName;
        if (m_CategoryTitleText != null) m_CategoryTitleText.text = categoryName; 

        ClearBlockTemplates(); 

        if (blockTypes == null || blockTypes.Count == 0)
        {
            Debug.Log($"No block types defined for category: {categoryName}");
            ShowEmptyMessage($"No blocks in '{categoryName}' category.");
            return;
        }

        Debug.Log($"Populating BlockListView for '{categoryName}' with {blockTypes.Count} block types.");

        WorkSpaceModel workspace = m_Workspace;
        if (workspace == null)
        {
            Debug.LogError("Cannot create block templates: UBlockly Workspace is not available.");
            ShowEmptyMessage("Error: Workspace unavailable.");
            return;
        }

        GameObject containerGO = CreateBlockTemplateContainer(categoryName); 

        foreach (string type in blockTypes)
        {
            try
            {
                BlockModel templateModel = BlockFactory.Instance.CreateBlock(workspace, type, $"template_{type}");
                if (templateModel == null)
                {
                    Debug.LogWarning($"Failed to create temporary model for template type: {type}");
                    continue;
                }
                workspace.RemoveTopBlock(templateModel);

                BlockView templateView = BlockViewFactory.CreateView(templateModel, this); 
                if (templateView == null)
                {
                    Debug.LogWarning($"BlockViewFactory failed to create view for template type: {type}");
                    templateModel.Dispose(false); 
                    continue;
                }

                templateView.transform.SetParent(containerGO.transform, false); 
                templateView.InToolbox = true; 
                templateView.gameObject.name = $"Template_{type}"; 

            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating template view for type '{type}': {e.Message}\n{e.StackTrace}");
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerGO.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_blockTemplateScrollAreaContent);
        StartCoroutine(DelayedLayoutRebuild(m_blockTemplateScrollAreaContent));
    }

    // Crear contenedor de layout para los bloques de una categoría (código existente OK)
    private GameObject CreateBlockTemplateContainer(string categoryName)
    {
        GameObject container = new GameObject($"BlockTemplateContainer_{categoryName}");
        RectTransform rt = container.AddComponent<RectTransform>();
        container.transform.SetParent(m_blockTemplateScrollAreaContent, false); 

        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1); // Centrado arriba
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero; // Que se ajuste al contenido

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10); 
        vlg.spacing = 10f;                       
        vlg.childControlWidth = true;            
        vlg.childControlHeight = false;           // Alto determinado por el bloque
        vlg.childAlignment = TextAnchor.UpperLeft; // Alinear arriba izquierda
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Crecer verticalmente
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Ancho controlado por VLG

        return container;
    }
    // Limpia solo los bloques
    private void ClearBlockTemplates()
    {
        foreach (GameObject container in mRootList.Values) 
        {
            if (container != null) Destroy(container);
        }
        mRootList.Clear(); 
        mActiveCategory = null; 

        if (m_BlockTemplateScrollRect?.content != null)
        {
            foreach (Transform child in m_BlockTemplateScrollRect.content)
            {
                if (!mRootList.ContainsValue(child.gameObject))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // Limpia el área de plantillas de bloque y muestra un mensaje 
    public void ShowEmptyMessage(string message)
    {
        if (!isInitialized) return;
        if (!string.IsNullOrEmpty(mActiveCategory) && mRootList.ContainsKey(mActiveCategory))
        {
            Transform container = mRootList[mActiveCategory].transform;
            foreach (Transform child in container) Destroy(child.gameObject);
            GameObject messageGO = new GameObject("EmptyCategoryMessage");
            messageGO.transform.SetParent(container, false);
            TextMeshProUGUI text = messageGO.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.color = Color.grey; 
        }
        else
        {
            
            Debug.LogWarning("ShowEmptyMessage called without active category/container.");
        }
        if (m_CategoryTitleText != null) m_CategoryTitleText.text = m_ActiveCategory ?? "Toolbox";
    }
    
    //Destruye todos los GameObjects hijos del contenedor de botones de categoría.

    private void ClearCategoryButtons()
    {
        if (m_categoryButtonContainer == null) return;
        foreach (Transform child in m_categoryButtonContainer) { Destroy(child.gameObject); }
        m_CategoryToggles.Clear();
    }
    private IEnumerator DelayedScrollToTop(ScrollRect scrollRect)
    {
        yield return null;
        // if (rect != null && rect.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        if (scrollRect != null && scrollRect.gameObject.activeInHierarchy)
            scrollRect.verticalNormalizedPosition = 1f;
    }


} // Fin de la clase BlockListView