
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseToolbox : MonoBehaviour
{
    /// <summary>
    /// the current displayed block category
    /// </summary>
    protected string mActiveCategory;

    private BlockListView m_OwningBlockListView; //Referencia a instancia qeu lo va a usar
    /// <summary>
    /// root objects of block views for different category
    /// </summary>
    protected Dictionary<string, GameObject> mRootList = new Dictionary<string, GameObject>();

    /// <summary>
    /// different toggle item for different block category
    /// </summary>
    protected Dictionary<string, Toggle> mMenuList = new Dictionary<string, Toggle>();

    protected WorkSpaceModel mWorkspace;
    protected ToolboxConfig mConfig;
    protected Transform mHiddenCache;
    protected WorkSpaceView WorkspaceView => WorkSpaceView.Active;

    private WorkSpaceModel m_WorkpaceModel => mWorkspace; //Referencia a la instancia del workspace

    protected abstract void Build();
    protected virtual void OnPickBlockView() { }
    public void Init(WorkSpaceModel workspace, ToolboxConfig config)
    {
        mWorkspace = workspace;
        mConfig = config;

        Build();

       // mWorkspace.VariableMap.AddObserver(new VariableObserver(this));
      //  mWorkspace.ProcedureDB.AddObserver(new ProcedureObserver(this));
    }

    public void Clean()
    {
        mActiveCategory = null;

        foreach (GameObject obj in mRootList.Values)
        {
            GameObject.Destroy(obj);
        }
        mRootList.Clear();

        foreach (Toggle toggle in mMenuList.Values)
        {
            GameObject.Destroy(toggle.gameObject);
        }
        mMenuList.Clear();
    }

    /// <summary>
    /// Create a new block view in toolbox 
    /// </summary>
    protected BlockView NewBlockView(string blockType, BlockListView sourceToolbox, Transform parent = null) 
    {
        if (mWorkspace == null) return null;
        if (parent == null) parent = mHiddenCache; 

        try
        {
            BlockModel block = BlockFactory.Instance.CreateBlock(mWorkspace, blockType);
            if (block == null) return null;
            mWorkspace.RemoveTopBlock(block); 

            BlockView view = BlockViewFactory.CreateView(block, sourceToolbox); 
            if (view != null)
            {
                view.InToolbox = true;
                view.BuildLayout();
                
               /* if (parent != mHiddenCache) 
                {
                    ToolboxBlockDragger dragger = view.GetComponent<ToolboxBlockDragger>();
                    if (dragger == null) dragger = view.gameObject.AddComponent<ToolboxBlockDragger>();
                    dragger.Init(this.WorkspaceView); 
                }*/

            }
            return view;
        }
        catch (Exception e) { Debug.LogWarning(e); return null; }
    }

    /// <summary>
    /// Create a new block view in toolbox 
    /// </summary>
    protected BlockView NewBlockView(BlockModel block, BlockListView sourceToolbox, Transform parent, int index = -1)
    {
        if (block == null)
        {
            Debug.LogError("BaseToolbox.NewBlockView(BlockModel): Received null block model.");
            return null;
        }
        if (sourceToolbox == null)
        {
            Debug.LogError($"BaseToolbox.NewBlockView(BlockModel {block.Type}): Received null sourceToolbox (BlockListView).");
            return null;
        }
        if (parent == null)
        {
            Debug.LogWarning($"BaseToolbox.NewBlockView(BlockModel {block.Type}): Parent transform is null. Using hidden cache if available.");
            parent = mHiddenCache ?? sourceToolbox.transform; 
        }
        mWorkspace.RemoveTopBlock(block);

        BlockView view = BlockViewFactory.CreateView(block, sourceToolbox);

        if (view == null)
        {
            Debug.LogError($"BaseToolbox.NewBlockView(BlockModel): BlockViewFactory failed for block {block.Type}/{block.ID}");
            
            return null;
        }
        view.InToolbox = true;
        view.ViewTransform.SetParent(parent, false);

        if (index >= 0)
            view.ViewTransform.SetSiblingIndex(index);

        GameObject dragTriggerGO = new GameObject($"DragTrigger_{block.Type}_{block.ID}");
        dragTriggerGO.transform.SetParent(view.transform, false); // Hijo del BlockView

        // Configuramos RectTransform del trigger para cubrir la vista
        RectTransform triggerRect = dragTriggerGO.AddComponent<RectTransform>();
        triggerRect.anchorMin = Vector2.zero;
        triggerRect.anchorMax = Vector2.one;
        triggerRect.offsetMin = Vector2.zero;
        triggerRect.offsetMax = Vector2.zero;
        triggerRect.localScale = Vector3.one;

        // Añadimos imagen transparente para capturar eventos
        Image triggerImage = dragTriggerGO.AddComponent<Image>();
        triggerImage.color = Color.clear;
        triggerImage.raycastTarget = true;

        // Añadimos el componente que inicia el drag
        BlockTemplateDragSource dragSource = Utilidades.GetOrAddComponent<BlockTemplateDragSource>(dragTriggerGO);
        dragSource.TemplateBlockView = view;       //  vista plantilla
        dragSource.SourceToolbox = sourceToolbox; //  BlockListView que lo contiene

        // Forzamos cálculo inicial de layout 
        if (view.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.ViewTransform);
        }
        return view;
    }
 


    #region Variables

    protected Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
    protected List<BlockView> mVariableHelperViews = new List<BlockView>();

    protected void CreateVariableGetterView(string varName, BlockListView specificListView)
    {
        if (mVariableGetterViews.ContainsKey(varName))
            return;

        if (m_OwningBlockListView == null) // << Necesitamos el dueño
        {
            Debug.LogError("BaseToolbox: Owning BlockListView not set!");
            return;
        }
        GameObject parentObj;
        if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
            return;

        BlockModel block = mWorkspace.NewBlock(Define.VARIABLE_GET_BLOCK_TYPE);
        block.SetFieldValue("VAR", varName);
        BlockView view = NewBlockView(block, specificListView, parentObj.transform);
        mVariableGetterViews[varName] = view;
    }

    protected void DeleteVariableGetterView(string varName)
    {
        BlockView view;
        mVariableGetterViews.TryGetValue(varName, out view);
        if (view != null)
        {
            mVariableGetterViews.Remove(varName);
            view.Dispose();
        }
    }
    
    protected void CreateVariableHelperViews(BlockListView specificListView)
    {
        GameObject parentObj;
        if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
            return;

       // if (m_WorkpaceModel == null || m_WorkpaceModel.VariableMap.Count == 0) return; //Chequeamos si hay variables
        string varName = mWorkspace.GetAllVariables()[0].Name;
        List<string> blockTypes = mConfig.GetBlockCategory(Define.VARIABLE_CATEGORY_NAME).BlockList;
        if (blockTypes == null)
        {
            Debug.LogError($"CreateVariableHelperViews: Could not get BlockList for category '{Define.VARIABLE_CATEGORY_NAME}'.");
            return;
        }
        foreach (string blockType in blockTypes)
        {
            if (!blockType.Equals(Define.VARIABLE_GET_BLOCK_TYPE))
            {
                BlockModel block = mWorkspace.NewBlock(blockType);
                if (block == null)
                {
                    Debug.LogWarning($"CreateVariableHelperViews: Failed to create BlockModel for type '{blockType}'.");
                    continue; 
                }
                block.SetFieldValue("VAR", varName);
                BlockView view = NewBlockView(block, specificListView, parentObj.transform);
                mVariableHelperViews.Add(view);
            }
        }
    }

    protected void DeleteVariableHelperViews()
    {
        foreach (BlockView view in mVariableHelperViews)
        {
            view.Dispose();
        }
        mVariableHelperViews.Clear();
    }

    protected void OnVariableUpdate(VariableUpdateData updateData)
    {
        switch (updateData.Type)
        {
            case VariableUpdateData.Create:
                {
                   // if (mVariableHelperViews.Count == 0) CreateVariableHelperViews();
                  //  CreateVariableGetterView(updateData.VarName);
                    break;
                }
            case VariableUpdateData.Delete:
                {
                    DeleteVariableGetterView(updateData.VarName);

                    List<VariableModel> allVars = mWorkspace.GetAllVariables();
                    if (allVars.Count == 0)
                    {
                        DeleteVariableHelperViews();
                    }
                    else
                    {
                        foreach (BlockView view in mVariableHelperViews)
                        {
                            if (view.Block.GetFieldValue("VAR").Equals(updateData.VarName))
                            {
                                view.Block.SetFieldValue("VAR", allVars[0].Name);
                            }
                        }
                    }
                    break;
                }
            case VariableUpdateData.Rename:
                {
                    BlockView view;
                    mVariableGetterViews.TryGetValue(updateData.VarName, out view);
                    if (view != null)
                    {
                        mVariableGetterViews.Remove(updateData.VarName);
                        mVariableGetterViews[updateData.NewVarName] = view;
                    }
                    break;
                }
        }
    }

    private class VariableObserver : IObserver<VariableUpdateData>
    {
        private BaseToolbox mToolbox;

        public VariableObserver(BaseToolbox toolbox)
        {
            mToolbox = toolbox;
        }

        public void OnUpdated(object subject, VariableUpdateData args)
        {
            if (mToolbox == null || mToolbox.transform == null)
                ((Observable<VariableUpdateData>)subject).RemoveObserver(this);
            else mToolbox.OnVariableUpdate(args);
        }
    }
    #endregion

    #region Procedures

    protected Dictionary<string, BlockView> mProcedureCallerViews = new Dictionary<string, BlockView>();

    protected virtual List<BlockView> BuildProcedureBlocks() 
    {
        List<BlockView> createdViews = new List<BlockView>();
        if (mWorkspace == null)
        {
            Debug.LogWarning("BuildProcedureBlocks called but Workspace is null.");
            return createdViews;
        }
        if (mWorkspace.ProcedureDB == null)
        {
            Debug.LogError("Workspace.ProcedureDB is null!");
            return createdViews;
        }
        List<BlockModel> allDefinitions = mWorkspace.ProcedureDB.GetDefinitionBlocks();

        if (allDefinitions == null || allDefinitions.Count == 0)
        {
           
            return createdViews;
        }

        return createdViews; 
    } 
    /*
    protected void CreateProcedureCallerView(Procedure procedureInfo, bool hasReturn)
    {
        if (mProcedureCallerViews.ContainsKey(procedureInfo.Name))
            return;

        GameObject parentObj;
        if (!mRootList.TryGetValue(Define.PROCEDURE_CATEGORY_NAME, out parentObj))
            return;

        string blockType = hasReturn ? Define.CALL_WITH_RETURN_BLOCK_TYPE : Define.CALL_NO_RETURN_BLOCK_TYPE;
        BlockModel block = mWorkspace.NewBlock(blockType);
        block.SetFieldValue("NAME", procedureInfo.Name);
        BlockView view = NewBlockView(block,this, parentObj.transform);
        mProcedureCallerViews[procedureInfo.Name] = view;
    }*/

    protected void DeleteProcedureCallerView(Procedure procedureInfo)
    {
        BlockView view;
        mProcedureCallerViews.TryGetValue(procedureInfo.Name, out view);
        if (view != null)
        {
            mProcedureCallerViews.Remove(procedureInfo.Name);
            view.Dispose();
        }
    }
    /*
    public void OnProcedureUpdate(ProcedureUpdateData updateData)
    {
        switch (updateData.Type)
        {
            case ProcedureUpdateData.Add:
                {
                    CreateProcedureCallerView(updateData.ProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock));
                    break;
                }
            case ProcedureUpdateData.Remove:
                {
                    DeleteProcedureCallerView(updateData.ProcedureInfo);
                    break;
                }
            case ProcedureUpdateData.Mutate:
                {
                    BlockView view;
                    if (mProcedureCallerViews.TryGetValue(updateData.ProcedureInfo.Name, out view))
                    {
                        if (!updateData.ProcedureInfo.Name.Equals(updateData.NewProcedureInfo.Name))
                        {
                            mProcedureCallerViews.Remove(updateData.ProcedureInfo.Name);
                            mProcedureCallerViews[updateData.NewProcedureInfo.Name] = view;
                        }
                        ((ProcedureMutator)view.Block.Mutator).Mutate(updateData.NewProcedureInfo);
                    }
                    break;
                }
        }
    }
    */

    #endregion

    #region Bin

    /// <summary>
    /// Check the block view is over the bin area, preparing dropped in bin
    /// </summary>
    public abstract bool CheckBin(BlockView blockView);

    /// <summary>
    /// Finish the check. 
    /// If the block view is over bin, drop it. 
    /// </summary>
    public abstract void FinishCheckBin(BlockView blockView);

    #endregion

    #region Monobehavior calls

    private void Update()
    {
        // UpdatePickedBlockView();
    }

    #endregion


    public virtual Color GetColorOfBlock(string blockType)
    {

        if (mConfig != null)
        {
           
            ToolboxBlockCategory category = mConfig.GetBlockCategoryByType(blockType);
            if (category != null)
            {
                return category.Color; 
            }
        }
        Debug.LogWarning($"Could not determine color for block type {blockType}. Using default.");
        return Color.gray;
    }
}//fin clase BaseToolBox
