
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
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class BaseToolbox : MonoBehaviour
{
    /// <summary>
    /// the current displayed block category
    /// </summary>
    protected string mActiveCategory;

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
    protected BlockView NewBlockView(string blockType, BaseToolbox sourceToolbox, Transform parent = null) 
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
                
                if (parent != mHiddenCache) 
                {
                    ToolboxBlockDragger dragger = view.GetComponent<ToolboxBlockDragger>();
                    if (dragger == null) dragger = view.gameObject.AddComponent<ToolboxBlockDragger>();
                    dragger.Init(this.WorkspaceView); 
                }

            }
            return view;
        }
        catch (Exception e) { Debug.LogWarning(e); return null; }
    }

    /// <summary>
    /// Create a new block view in toolbox 
    /// </summary>
    protected BlockView NewBlockView(BlockModel block, BaseToolbox sourceToolbox, Transform parent, int index = -1)
    {
        mWorkspace.RemoveTopBlock(block);

        BlockView view = BlockViewFactory.CreateView(block, sourceToolbox);
        view.InToolbox = true;
        view.ViewTransform.SetParent(parent, false);

        if (index >= 0)
            view.ViewTransform.SetSiblingIndex(index);

        GameObject maskObj = new GameObject("ToolboxMask");
        maskObj.transform.SetParent(view.ViewTransform, false);
        RectTransform maskTrans = maskObj.AddComponent<RectTransform>();
        maskTrans.sizeDelta = view.Size;
        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = new Color(1, 1, 1, 0);
        UIEventListener.Get(maskObj).onBeginDrag = data => PickBlockView(data, view);
        if (!BlockViewSettings.Get().MaskedInToolbox)
            maskTrans.SetAsFirstSibling();

        return view;
    }

    protected void PickBlockView(PointerEventData data, BlockView blockView)
    {
        Vector3 localPos = WorkspaceView.CodingArea.InverseTransformPoint(blockView.ViewTransform.position);

       
        BlockView newBlockView = WorkspaceView.CloneBlockView(blockView,this, new Vector2(localPos.x, localPos.y));
        newBlockView.OnBeginDrag(data);

      
        data.pointerDrag = newBlockView.gameObject;

        OnPickBlockView();
    }

    /// <summary>
    /// Get the category name for block view
    /// </summary>
    public string GetCategoryNameOfBlockView(BlockView view)
    {
        foreach (var category in mConfig.BlockCategoryList)
        {
            foreach (string type in category.BlockList)
            {
                if (string.Equals(view.BlockType, type))
                {
                    return category.CategoryName;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get the background color for block view
    /// </summary>
    public Color GetColorOfBlockView(BlockView view)
    {
        if (view == null)
        {
            Debug.LogWarning("GetColorOfBlockView called with null view.");
            return Color.white; 
        }
       
        return GetColorOfBlock(view.BlockType);
    }

    #region Variables

    protected Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
    protected List<BlockView> mVariableHelperViews = new List<BlockView>();

    protected virtual List<BlockView> BuildVariableBlocks()
    {
       
        List<BlockView> createdViews = new List<BlockView>(); 
        if (mWorkspace == null) return createdViews; 

        var variables = mWorkspace.GetAllVariables(); 
        if (variables.Count > 0)
        {
            
            BlockView getBlockView = NewBlockView(Define.VARIABLE_GET_BLOCK_TYPE,this, mHiddenCache); 
            if (getBlockView != null)
            {
                createdViews.Add(getBlockView);
               
            }
            else { Debug.LogWarning("Failed to create base view for VARIABLE_GET_BLOCK_TYPE"); }

            BlockView setBlockView = NewBlockView(Define.VARIABLE_SET_BLOCK_TYPE,this, mHiddenCache); 
            if (setBlockView != null)
            {
                createdViews.Add(setBlockView);
                // setBlockView.transform.SetParent(null);
            }
            else { Debug.LogWarning("Failed to create base view for VARIABLE_SET_BLOCK_TYPE"); }

        }
        else // No hay variables
        {
            // BuildButton(Define.CREATE_VARIABLE_TITLE);
        }



        return createdViews; 
    }

    protected void CreateVariableGetterView(string varName)
    {
        if (mVariableGetterViews.ContainsKey(varName))
            return;

        GameObject parentObj;
        if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
            return;

        BlockModel block = mWorkspace.NewBlock(Define.VARIABLE_GET_BLOCK_TYPE);
        block.SetFieldValue("VAR", varName);
        BlockView view = NewBlockView(block,this, parentObj.transform);
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

    protected void CreateVariableHelperViews()
    {
        GameObject parentObj;
        if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
            return;

        string varName = mWorkspace.GetAllVariables()[0].Name;
        List<string> blockTypes = mConfig.GetBlockCategory(Define.VARIABLE_CATEGORY_NAME).BlockList;
        foreach (string blockType in blockTypes)
        {
            if (!blockType.Equals(Define.VARIABLE_GET_BLOCK_TYPE))
            {
                BlockModel block = mWorkspace.NewBlock(blockType);
                block.SetFieldValue("VAR", varName);
                BlockView view = NewBlockView(block,this, parentObj.transform);
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
                    if (mVariableHelperViews.Count == 0)
                        CreateVariableHelperViews();
                    CreateVariableGetterView(updateData.VarName);
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
    }

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
