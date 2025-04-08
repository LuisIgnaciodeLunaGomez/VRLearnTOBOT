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


public class BlockListView : BaseToolbox
{
    private RectTransform m_categoryButtonContainer;
    private RectTransform m_blockTemplateScrollAreaContent;

    [Tooltip("Texto (TextMeshPro) para mostrar el nombre de la categoría activa.")]
    private TextMeshProUGUI m_categoryTitleText;
    private GameObject m_categoryButtonPrefab; 
    private CategoryController m_categoryController;
    private string m_ActiveCategory = null;
    private bool isInitialized = false;

    protected WorkSpaceModel m_Workspace; 
    protected ToolboxConfig m_Config;

    protected WorkSpaceView m_WorkspaceView;
    public WorkSpaceView WorkspaceViewForFactory => m_WorkspaceView;

    protected Dictionary<string, GameObject> m_RootList = new Dictionary<string, GameObject>(); 
    protected Dictionary<string, Toggle> m_MenuList = new Dictionary<string, Toggle>(); 

    [Header("UI Assignments (BlockListView)")]
    [SerializeField] private ScrollRect m_blockTemplateScrollRect;
    [SerializeField] private GameObject m_blockTemplateContainerPrefab; 
    [SerializeField] private GameObject m_BinArea; 
                                                   

    public void InitializeView(CategoryController categoryController, RectTransform categoryButtonContainer, ScrollRect blockTemplateScrollRectParam) 
    {
        if (isInitialized) return;
        m_categoryController = categoryController; 
        m_categoryButtonContainer = categoryButtonContainer;
        m_blockTemplateScrollRect = blockTemplateScrollRectParam; 
        m_blockTemplateScrollAreaContent = m_blockTemplateScrollRect?.content;
                                                                               
        if (m_blockTemplateScrollAreaContent == null) Debug.LogError("ScrollRect provided has no 'Content' RectTransform assigned!");

        isInitialized = true;
        Debug.Log($"BlockListView PRE-INITIALIZED.");
    }

    protected override void Build()
    {
        if (mConfig == null || mConfig.BlockCategoryList == null || mConfig.BlockCategoryList.Count == 0)
        {
            Debug.LogError("BlockListView(BaseToolbox): Cannot Build, ToolboxConfig is missing or empty!");
            return;
        }
        Debug.Log($"BlockListView(BaseToolbox): Build started with {mConfig.BlockCategoryList.Count} categories.");
        BuildMenu(); 

        if (mConfig.BlockCategoryList.Count > 0)
        {
            ShowBlockCategory(mConfig.BlockCategoryList[0].CategoryName);
        }
    }

    protected virtual void BuildMenu() 
    {
        Debug.Log($"BuildMenu: Category count: {mConfig.BlockCategoryList?.Count ?? 0}. Container valid: {m_categoryButtonContainer != null}");
        if (m_categoryButtonContainer == null) { Debug.LogError("Button container is null in BuildMenu!"); return; }

        ClearCategoryButtons();

        foreach (var category in mConfig.BlockCategoryList)
        {
            string categoryName = category.CategoryName;
            string displayName = I18n.Contains(categoryName) ? I18n.Get(categoryName) : categoryName;
            Color color = category.Color; 

            GameObject buttonGO = CreateCategoryButtonUI(displayName, categoryName, color, /* OnClick: */(catName) => {
                ShowBlockCategory(catName);
            });

            Toggle toggle = buttonGO.GetComponent<Toggle>();
            if (toggle == null) toggle = buttonGO.AddComponent<Toggle>();
            toggle.isOn = false; 
            toggle.onValueChanged.AddListener((isSelected) => {
                if (isSelected) ShowBlockCategory(categoryName);
                // else HideBlockCategory(categoryName); 
            });
            mMenuList[categoryName] = toggle; 
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_categoryButtonContainer);
        StartCoroutine(DelayedLayoutRebuild(m_categoryButtonContainer));
    }

    public void ShowBlockCategory(string categoryName)
    {
        Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Showing category '{categoryName}'. Previously active: '{mActiveCategory}'");

        if (!isInitialized)
        {
            Debug.LogWarning("BlockListView not initialized. Cannot show category.");
            return;
        }

        if (string.Equals(categoryName, mActiveCategory))
        {
            // Debug.Log($"Category '{categoryName}' is already active.");
            return;
        }

        if (m_blockTemplateScrollRect == null)
        {
            Debug.LogError("BlockListView: m_blockTemplateScrollRect (reference to the ScrollRect GameObject/Transform) is not assigned!");
            return;
        }
        ScrollRect scrollRectComponent = m_blockTemplateScrollRect.GetComponent<ScrollRect>();
        if (scrollRectComponent == null)
        {
            Debug.LogError("BlockListView: The GameObject assigned to m_blockTemplateScrollRect does not have a ScrollRect component!", this);
            return;
        }

        if (!string.IsNullOrEmpty(mActiveCategory) && mRootList.TryGetValue(mActiveCategory, out GameObject oldContainer))
        {
            if (oldContainer != null) 
                oldContainer.SetActive(false);
            // Debug.Log($"Deactivated previous container for '{mActiveCategory}'.");
        }

        mActiveCategory = categoryName;
        if (m_categoryTitleText != null)
            m_categoryTitleText.text = I18n.Contains(categoryName) ? I18n.Get(categoryName) : categoryName;

        if (!m_blockTemplateScrollRect.gameObject.activeSelf)
            m_blockTemplateScrollRect.gameObject.SetActive(true);

        GameObject activeContentContainerGO = null; 
        RectTransform activeContentContainerRect = null;
        bool needsPopulation = true; 

        if (mRootList.TryGetValue(categoryName, out GameObject existingContainerGO))
        {
            if (existingContainerGO != null && existingContainerGO.GetComponent<RectTransform>() != null)
            {
                activeContentContainerGO = existingContainerGO;
                activeContentContainerRect = activeContentContainerGO.GetComponent<RectTransform>();

                if (activeContentContainerGO.transform.parent != m_blockTemplateScrollRect.transform) 
                {
                    activeContentContainerGO.transform.SetParent(m_blockTemplateScrollRect.transform, false);
                    Debug.LogWarning($"Re-parented existing container '{categoryName}' to ScrollRect transform.");
                }

                bool isDynamicCategory = categoryName.Equals(Define.VARIABLE_CATEGORY_NAME) || categoryName.Equals(Define.PROCEDURE_CATEGORY_NAME);
                if (!isDynamicCategory)
                {
                    needsPopulation = false; 
                    Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Re-activated existing container for '{categoryName}'. No population needed.");
                }
                else
                {
                    Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Re-activated existing container for dynamic category '{categoryName}'. Repopulating...");

                    foreach (Transform child in activeContentContainerRect) Destroy(child.gameObject);
                }

               
                activeContentContainerGO.SetActive(true);
                scrollRectComponent.content = activeContentContainerRect;
            }
            else
            {
                Debug.LogWarning($"Container for category '{categoryName}' was in mRootList but is invalid. Recreating.");
                if (existingContainerGO != null) Destroy(existingContainerGO); 
                mRootList.Remove(categoryName);
            }
        }

        
        if (activeContentContainerGO == null)
        {
            if (m_blockTemplateContainerPrefab == null)
            {
                Debug.LogError("BlockListView: m_blockTemplateContainerPrefab is not assigned!");
                return; 
            }

            activeContentContainerGO = Instantiate(m_blockTemplateContainerPrefab, m_blockTemplateScrollRect.transform); 
            activeContentContainerGO.name = "BlockContent_" + categoryName;
            activeContentContainerRect = activeContentContainerGO.GetComponent<RectTransform>();

            if (activeContentContainerRect == null)
            {
                Debug.LogError("Instantiated container prefab is missing RectTransform!", activeContentContainerGO);
                Destroy(activeContentContainerGO);
                return; 
            }

            mRootList[categoryName] = activeContentContainerGO; 
            scrollRectComponent.content = activeContentContainerRect; 

            Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Created new container '{activeContentContainerGO.name}'.");
            needsPopulation = true; 
        }

        if (needsPopulation && activeContentContainerRect != null)
        {
            Debug.Log($"<color=#ADD8E6>BlockListView.ShowBlockCategory:</color> Populating container for '{categoryName}'...");
            if (categoryName.Equals(Define.VARIABLE_CATEGORY_NAME))
            {
                BuildVariableBlocks(activeContentContainerRect);
            }
            else if (categoryName.Equals(Define.PROCEDURE_CATEGORY_NAME))
            {
                BuildProcedureBlocks(activeContentContainerRect);
            }
            else
            {
                BuildBlockViewsForActiveCategory(activeContentContainerRect);
            }
        }

        if (activeContentContainerRect != null)
        {
            StartCoroutine(DelayedLayoutRebuild(activeContentContainerRect));
            StartCoroutine(DelayedScrollToTop(scrollRectComponent));
        }
        else
        {
            Debug.LogError($"Failed to obtain a valid activeContentContainerRect for category '{categoryName}'. Layout/Scroll skipped.");
        }
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

    private IEnumerator DelayedScrollToTop(ScrollRect scrollRect)
    {
        yield return null; 
        yield return null; 
        if (scrollRect != null && scrollRect.gameObject.activeInHierarchy)
            scrollRect.verticalNormalizedPosition = 1f;
    }
    public override bool CheckBin(BlockView blockView)
    {
        if (WorkSpaceView.Active == null || WorkSpaceView.Active.RootCanvas == null) 
        {
            Debug.LogWarning("CheckBin called before WorkSpaceView or its RootCanvas is ready.");
            return false;
        }
        if (blockView.InToolbox) return false; 
        if (m_BinArea == null) return false; 

        Canvas rootCanvas = WorkSpaceView.Active.RootCanvas;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        bool contains = RectTransformUtility.RectangleContainsScreenPoint(
       m_BinArea.transform as RectTransform,
       Input.mousePosition,
       eventCamera); 

        m_BinArea.SetActive(contains); 

        return contains;
    }

    public override void FinishCheckBin(BlockView blockView)
    {
        if (m_BinArea == null) return;

        if (CheckBin(blockView)) 
        {
            Debug.Log($"BlockModel {blockView.Block.ID} dropped in Bin. Disposing...");
            blockView.Dispose();
        }

        m_BinArea.SetActive(false);
    }
 

    // Muestra los bloques para la categoría dada (Llamado por CategoryController)
    public void ShowBlocksForCategory(string categoryName, BaseToolbox sourceToolbox,  Color categoryColor, List<string> blockTypes)
    {
        if (!isInitialized) { Debug.LogWarning("BlockListView not initialized."); return; }
        if (m_blockTemplateScrollAreaContent == null) { Debug.LogError("BlockModel Template container is null!"); return; }

        m_ActiveCategory = categoryName;
        if (m_categoryTitleText != null) m_categoryTitleText.text = categoryName; 

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

    //Destruye todos los GameObjects hijos del contenedor de botones de categoría.
    private void ClearCategoryButtons()
    {
        if (m_categoryButtonContainer == null) return;
        foreach (Transform child in m_categoryButtonContainer)
            Destroy(child.gameObject);
        mMenuList.Clear();
    }

    // Limpia solo los bloques, mantiene el contenedor padre (scroll content)
    private void ClearBlockTemplates()
    {
        foreach (GameObject container in mRootList.Values) 
        {
            if (container != null) Destroy(container);
        }
        mRootList.Clear(); 
        mActiveCategory = null; 

        if (m_blockTemplateScrollRect?.content != null)
        {
            foreach (Transform child in m_blockTemplateScrollRect.content)
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
        if (m_categoryTitleText != null) m_categoryTitleText.text = m_ActiveCategory ?? "Toolbox";
    }

    // Crear el botón de categoría UI
    private GameObject CreateCategoryButtonUI(string displayName, string categoryKey, Color color, Action<string> onClickCallback)
    {
        GameObject buttonGO = new GameObject($"CategoryBtn_{categoryKey}");
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonGO.transform.SetParent(m_categoryButtonContainer, false); 
        buttonRect.localScale = Vector3.one;

        Image bgImage = buttonGO.AddComponent<Image>();
        bgImage.color = color; 
        bgImage.raycastTarget = true;

        LayoutElement buttonLayout = buttonGO.AddComponent<LayoutElement>();
        buttonLayout.minHeight = 30; 

        GameObject iconGO = new GameObject("Icon");
        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconGO.transform.SetParent(buttonGO.transform, false); 
        iconRect.localScale = Vector3.one;

        
        Image iconImage = iconGO.AddComponent<Image>();
        
        Sprite iconSprite = Resources.Load<Sprite>($"Icons/category_{categoryKey}"); 
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }
        else
        {
            iconImage.sprite = Resources.Load<Sprite>("Sprites/UI/default_category_icon"); 
            iconImage.color = Color.white; 
        }
        iconImage.preserveAspect = true;

        GameObject textGO = new GameObject("Label");
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textGO.transform.SetParent(buttonGO.transform, false);
        textRect.localScale = Vector3.one;

        TextMeshProUGUI labelText = textGO.AddComponent<TextMeshProUGUI>();
        labelText.text = displayName; 
        labelText.fontSize = 14;   
        labelText.color = Color.black; 
        labelText.alignment = TextAlignmentOptions.Left;
 
      
        HorizontalLayoutGroup hlg = buttonGO.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = buttonGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(5, 5, 2, 2);
        hlg.spacing = 4f;                       
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true; 
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        LayoutElement iconLayout = iconGO.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 20; 
        iconLayout.preferredHeight = 20;
        LayoutElement textLayout = textGO.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1; 

        Button buttonComponent = buttonGO.GetComponent<Button>();
        if (buttonComponent == null) buttonComponent = buttonGO.AddComponent<Button>(); 

        Toggle toggle = buttonGO.GetComponent<Toggle>();
        if (toggle == null) toggle = buttonGO.AddComponent<Toggle>();

        toggle.interactable = true;
    
        toggle.targetGraphic = bgImage;
        
        if (iconImage != null) 
        {
            toggle.graphic = iconImage; 
            ColorBlock cb = toggle.colors;
            cb.normalColor = Color.white; 
            cb.selectedColor = Color.yellow; 
            toggle.colors = cb;
        }
        else
        {
            toggle.graphic = null; 
        }

        ToggleGroup toggleGroup = m_categoryButtonContainer.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
        {
  
            Debug.LogWarning("Adding ToggleGroup to CategoryButtonContainer dynamically inside button creation.");
            toggleGroup = m_categoryButtonContainer.gameObject.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = true; 
        }
        toggle.group = toggleGroup;

        toggle.onValueChanged.RemoveAllListeners(); 
        string currentCategoryKey = categoryKey;
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) 
            {
             
                Debug.Log($"Category Toggle ON: {currentCategoryKey}");
                onClickCallback?.Invoke(currentCategoryKey); 
            }
           
        });

        return buttonGO;
    }
    void OnDestroy()
    {
        ClearBlockTemplates();
        ClearCategoryButtons();
    }

} // Fin de la clase BlockListView