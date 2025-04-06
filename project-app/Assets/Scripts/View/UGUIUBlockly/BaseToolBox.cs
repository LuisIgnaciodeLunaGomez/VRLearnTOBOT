
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

        mWorkspace.VariableMap.AddObserver(new VariableObserver(this));
        mWorkspace.ProcedureDB.AddObserver(new ProcedureObserver(this));
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
    protected BlockView NewBlockView(string blockType, Transform parent = null) // Aceptar padre opcional
    {
        if (mWorkspace == null) return null;
        if (parent == null) parent = mHiddenCache; // Usar caché si no se especifica padre

        try
        {
            BlockModel block = BlockFactory.Instance.CreateBlock(mWorkspace, blockType);
            if (block == null) return null;
            mWorkspace.RemoveTopBlock(block); // No es un top block aún

            BlockView view = BlockViewFactory.CreateView(block); 
            if (view != null)
            {
                view.InToolbox = true;
                view.BuildLayout();
                // No añadir ToolboxBlockDragger aquí si se hace en el método que llama?
                // O añadirlo aquí SIEMPRE que se crea para el toolbox? Añadirlo aquí es más seguro:
                if (parent != mHiddenCache) // Solo añade dragger si el padre es el contenedor real
                {
                    ToolboxBlockDragger dragger = view.GetComponent<ToolboxBlockDragger>();
                    if (dragger == null) dragger = view.gameObject.AddComponent<ToolboxBlockDragger>();
                    dragger.Init(this.WorkspaceView); // Asume que tienes WorkspaceView accesible
                }

            }
            return view;
        }
        catch (Exception e) { Debug.LogWarning(e); return null; }
    }

    // Método auxiliar potencial para botones genéricos
    /* protected virtual void BuildButton(string buttonText, Action onClickAction)
       {
           Transform parentContainer = null;
           // Determinar el contenedor correcto (¿Variable o Procedure?)
           if(mActiveCategory == Define.VARIABLE_CATEGORY_NAME)
                 mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentContainer);
             else if (mActiveCategory == Define.PROCEDURE_CATEGORY_NAME)
                 mRootList.TryGetValue(Define.PROCEDURE_CATEGORY_NAME, out parentContainer);

           if(parentContainer != null) {
                 // Instanciar prefab de botón o crearlo programáticamente
                GameObject buttonGO = new GameObject("ToolboxButton_" + buttonText);
                 // ... Añadir RectTransform, Image, Button, TextMeshPro ...
                buttonGO.transform.SetParent(parentContainer, false);
                 // ... Configurar texto y listener: button.onClick.AddListener(() => onClickAction?.Invoke()); ...
             }
        }
    */
    /// <summary>
    /// Create a new block view in toolbox 
    /// </summary>
    protected BlockView NewBlockView(BlockModel block, Transform parent, int index = -1)
    {
        mWorkspace.RemoveTopBlock(block);

        BlockView view = BlockViewFactory.CreateView(block);
        view.InToolbox = true;
        view.ViewTransform.SetParent(parent, false);

        if (index >= 0)
            view.ViewTransform.SetSiblingIndex(index);

        //add mask
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
        // compute the local position of the block view in coding area
        Vector3 localPos = WorkspaceView.CodingArea.InverseTransformPoint(blockView.ViewTransform.position);

        // clone a new block view for coding area
        BlockView newBlockView = WorkspaceView.CloneBlockView(blockView, new Vector2(localPos.x, localPos.y));
        newBlockView.OnBeginDrag(data);

        //change the dragging object as the newly created blockview 
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
        foreach (var category in mConfig.BlockCategoryList)
        {
            foreach (string type in category.BlockList)
            {
                if (string.Equals(view.BlockType, type))
                {
                    return category.Color;
                }
            }
        }
        return Color.white;
    }

    #region Variables

    protected Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
    protected List<BlockView> mVariableHelperViews = new List<BlockView>();

    protected virtual List<BlockView> BuildVariableBlocks()
    {
        /* Transform parent = mRootList[Define.VARIABLE_CATEGORY_NAME].transform;

         //build createVar button
         GameObject obj = GameObject.Instantiate(BlockViewSettings.Get().PrefabBtnCreateVar);

         obj.GetComponentInChildren<Text>().text = I18n.Get(MsgDefine.NEW_VARIABLE);

         obj.transform.SetParent(parent, false);
         obj.GetComponentInChildren<Image>().color = mConfig.GetBlockCategory(Define.VARIABLE_CATEGORY_NAME).Color;
         obj.GetComponent<Button>().onClick.AddListener(() =>
         {
             DialogFactory.CreateDialog("variable_name");
         });

         List<VariableModel> allVars = mWorkspace.GetAllVariables();
         if (allVars.Count == 0) return;

         CreateVariableHelperViews();

         //list all variable getter views
         foreach (VariableModel variable in mWorkspace.GetAllVariables())
         {
             CreateVariableGetterView(variable.Name);
         }*/

        List<BlockView> createdViews = new List<BlockView>(); // Lista para devolver
        if (mWorkspace == null) return createdViews; // Devolver lista vacía si no hay workspace

        // -- Lógica para crear bloques 'Get Variable' --
        var variables = mWorkspace.GetAllVariables(); // Obtiene TODAS las variables del workspace
        if (variables.Count > 0)
        {
            // Crear bloque 'Get' GENÉRICO una vez (si todos usan el mismo prefab base)
            BlockView getBlockView = NewBlockView(Define.VARIABLE_GET_BLOCK_TYPE, mHiddenCache); // Usa un padre temporal oculto
            if (getBlockView != null)
            {
                createdViews.Add(getBlockView);
                // Configuración adicional si es necesaria aquí? O se hace después?
                // getBlockView.transform.SetParent(null); // Desemparentar de mHiddenCache por ahora
            }
            else { Debug.LogWarning("Failed to create base view for VARIABLE_GET_BLOCK_TYPE"); }

            // -- Crear bloques 'Set Variable' (si tu lenguaje los tiene como bloques separados) --
            BlockView setBlockView = NewBlockView(Define.VARIABLE_SET_BLOCK_TYPE, mHiddenCache); // Crea vista SET base
            if (setBlockView != null)
            {
                createdViews.Add(setBlockView);
                // setBlockView.transform.SetParent(null);
            }
            else { Debug.LogWarning("Failed to create base view for VARIABLE_SET_BLOCK_TYPE"); }

            // -- Crear botón "Create Variable" (si no se maneja distinto) --
            // Esto puede ser un botón UI normal, no necesariamente un BlockView
            // BuildButton(Define.CREATE_VARIABLE_TITLE); // Método auxiliar que necesita el contenedor real
        }
        else // No hay variables
        {
            // BuildButton(Define.CREATE_VARIABLE_TITLE);
        }



        return createdViews; // Devolver la lista de vistas creadas (sin padre final asignado
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
        BlockView view = NewBlockView(block, parentObj.transform);
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
                BlockView view = NewBlockView(block, parentObj.transform);
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

                    //change variable helper view
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



    protected virtual List<BlockView> BuildProcedureBlocks() // Cambiado de void a List<BlockView>
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

        // Obtener TODOS los bloques de definición
        // Asegúrate que GetDefinitionBlocks() exista y sea público en tu ProcedureDB
        List<BlockModel> allDefinitions = mWorkspace.ProcedureDB.GetDefinitionBlocks();

        if (allDefinitions == null || allDefinitions.Count == 0)
        {
            //Debug.Log("No procedure definitions found.");
            // Aún podrías querer construir el botón "Make a Block" aquí
            // BuildButton(Define.CREATE_PROCEDURE_TITLE);
            return createdViews;
        }


        // --- Filtrar y Crear bloque 'Call No Return' ---
        /* if (proceduresNoReturn.Count > 0)
         {
             BlockView callNoReturnView = NewBlockViewFromModel( // Usa la versión que toma modelo
                  BlockFactory.Instance.CreateBlock(mWorkspace, Define.CALL_NO_RETURN_BLOCK_TYPE),
                  buildParent,
                  false);

             if (callNoReturnView != null)
             {
                 string procedureName = proceduresNoReturn[0].GetFieldValue("NAME");
                 if (!string.IsNullOrEmpty(procedureName))
                 {
                     // Establecer valor en el MODELO del clon primero
                     callNoReturnView.Block.SetFieldValue("NAME", procedureName);

                     // AHORA, buscar la VISTA manualmente
                     FieldView targetFieldView = FindFieldViewByName(callNoReturnView, "NAME");

                     if (targetFieldView is FieldProcedureView procedureFieldView) // Asegúrate que el cast es correcto
                     {
                         procedureFieldView.UpdateValue(procedureName); // Actualiza la vista
                         createdViews.Add(callNoReturnView); // Añadir SOLO si todo funcionó
                     }
                     else
                     {
                         Debug.LogWarning($"Could not find or cast FieldView 'NAME' in {Define.CALL_NO_RETURN_BLOCK_TYPE} template.");
                         callNoReturnView.Dispose(); // Limpia si no se pudo configurar
                     }
                 }
                 else
                 {
                     Debug.LogWarning($"Procedure definition block {proceduresNoReturn[0].ID} has no value in field 'NAME'");
                     callNoReturnView.Dispose();
                 }
             }
             else { Debug.LogWarning($"Failed to create base view for {Define.CALL_NO_RETURN_BLOCK_TYPE}"); }

         }*/
        // --- Filtrar y Crear bloque 'Call With Return' ---
        // Usar LINQ para filtrar los bloques que SÍ tienen retorno
        /* if (proceduresWithReturn.Count > 0)
         {
             BlockView callWithReturnView = NewBlockView(Define.CALL_WITH_RETURN_BLOCK_TYPE, mHiddenCache);
             if (callWithReturnView != null)
             {
                 createdViews.Add(callWithReturnView);
                 var fieldView = callWithReturnView.GetFieldView("NAME");
                 if (fieldView is FieldProcedureView procedureField)
                 {
                     procedureField.UpdateValue(proceduresWithReturn[0].Name);
                 }
                 else if (fieldView != null)
                 {
                     Debug.LogWarning($"Field 'NAME' in {Define.CALL_WITH_RETURN_BLOCK_TYPE} template is not a FieldProcedureView (Type: {fieldView.GetType()}). Cannot set initial value.");
                 }
             }
             else { Debug.LogWarning($"Failed to create base view for {Define.CALL_WITH_RETURN_BLOCK_TYPE}"); }
         }*/

        // --- Botón "Make a Block" (Create Procedure) ---
        // La lógica para añadir el botón iría aquí, si es necesario construirlo dinámicamente
        // por ejemplo, usando otro método auxiliar BuildButton().
        // BuildButton(Define.CREATE_PROCEDURE_TITLE);


        return createdViews; // Devuelve las VISTAS creadas (callNoReturnView, callWithReturnView)
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
        BlockView view = NewBlockView(block, parentObj.transform);
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
                    //mutate the caller prototype view
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
        //            UpdatePickedBlockView();
    }

    #endregion


    public virtual Color GetColorOfBlock(string blockType)
    {

        if (mConfig != null)
        {
            // Usamos el método que añadimos a ToolboxConfig
            ToolboxBlockCategory category = mConfig.GetBlockCategoryByType(blockType);
            if (category != null)
            {
                return category.Color; // Devuelve el color de la categoría encontrada
            }
        }
           
        
        // Fallback color
        Debug.LogWarning($"Could not determine color for block type {blockType}. Using default.");
        return Color.gray;
    }
}
