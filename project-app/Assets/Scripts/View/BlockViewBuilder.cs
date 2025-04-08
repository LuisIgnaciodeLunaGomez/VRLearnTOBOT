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
 * Versión: 2.0.0
 * 
 * Descripción: Clase que representa un bloque visual en la interfaz de usuario premite la vinculación del modelo lógico con la UI
 * 
 */
using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using System;
using System.Linq;
using UBlockly;

public static class BlockViewBuilder
{

    private static Vector2 BLOCK_PIVOT = new Vector2(0, 1); //pivot top-left

    private static Vector2 BLOCK_ANCHOR = new Vector2(0, 1); //anchor top-left

    static void UniformRectTransform(RectTransform rectTrans)
    {
        rectTrans.pivot = BLOCK_PIVOT;
        rectTrans.anchorMin = rectTrans.anchorMax = BLOCK_ANCHOR;
        rectTrans.anchoredPosition3D = Vector3.zero;
        rectTrans.localScale = Vector3.one;
        rectTrans.localRotation = Quaternion.identity;
    }

    static T AddViewComponent<T>(GameObject viewObj) where T : BaseView
    {
        Debug.Log($"AddViewComponent: Attempting to get/add {typeof(T).Name} on {viewObj.name}", viewObj);
        T view = viewObj.GetComponent<T>();
        if (view == null)
        {
            Debug.Log($" - Component {typeof(T).Name} not found, calling AddComponent.");

            view = viewObj.AddComponent<T>();

            if (view == null)
            { 
                Debug.LogError($"   - AddComponent<{typeof(T).Name}> FAILED on {viewObj.name}!");
            }
            else
            {
                Debug.Log($"   - AddComponent<{typeof(T).Name}> SUCCEEDED.");
            }
        }
        else
        {
            Debug.Log($" - Component {typeof(T).Name} found via GetComponent.");
        }
     //view.InitComponents();
    return view;
    }

    public static GameObject BuildBlockView(BlockModel block)
    {
        GameObject blockPrefab = BlockViewSettings.Get().PrefabRoot;
        if (block.OutputConnection != null)
            blockPrefab = BlockViewSettings.Get().PrefabRootOutput;
        else if (block.PreviousConnection != null && block.NextConnection != null)
            blockPrefab = BlockViewSettings.Get().PrefabRootPrevNext;
        else if (block.PreviousConnection != null)
            blockPrefab = BlockViewSettings.Get().PrefabRootPrev;
        else if (block.NextConnection != null)
            blockPrefab = BlockViewSettings.Get().PrefabRootNext;

        GameObject blockObj = GameObject.Instantiate(blockPrefab);
        blockObj.name = "Block_" + block.Type;

        Debug.Log($"Building BlockView for {block.Type}. Prefab used: {blockPrefab.name}. Instance: {blockObj.name}"); 

        RectTransform blockTrans = blockObj.GetComponent<RectTransform>();
        UniformRectTransform(blockTrans);

        BlockView blockView = AddViewComponent<BlockView>(blockObj);

        blockView.AddBgImage(blockObj.GetComponent<Image>());

        int lineGroupsFound = 0;

        Transform mutatorEntry = null;
        foreach (Transform child in blockTrans)
        {
            Debug.Log($" - Checking child: {child.name}");
            string childName = child.name.ToLower();
            if (childName.StartsWith("connection"))
            {
                //connection node views
                ConnectionView conView = AddViewComponent<ConnectionView>(child.gameObject);
                blockView.AddChildView(conView, 0);

                if (childName.EndsWith("output")) conView.ConnectionType = EConnection.OutputValue;
                else if (childName.EndsWith("prev")) conView.ConnectionType = EConnection.PrevStatement;
                else if (childName.EndsWith("next")) conView.ConnectionType = EConnection.NextStatement;

                //connection node view background color
                Image image = child.GetComponent<Image>();
                if (image != null) blockView.AddBgImage(image);
            }
            else if (childName.Equals("linegroup"))
            {
                Debug.Log($"   - FOUND CHILD 'LineGroup'! Adding LineGroupView component...");
                UniformRectTransform(child as RectTransform);
                LineGroupView groupView = AddViewComponent<LineGroupView>(child.gameObject);
                if (groupView != null)
                {
                    blockView.AddChildView(groupView); 
                    lineGroupsFound++;
                    Debug.Log($"   - Added LineGroupView {groupView.GetInstanceID()} to BlockView. Current ChildViews count: {blockView.ChildViews.Count}"); 
                }
                else { Debug.LogError("    - FAILED to add LineGroupView component!", child.gameObject); }
            }
            else if (childName.Equals("mutator_entry"))
            {
                mutatorEntry = child;
            }
        }

        Debug.Log($"Finished checking children for {block.Type}. Found and added {lineGroupsFound} LineGroups. BlockView now has {blockView.ChildViews.Count} children in its list.");


        Debug.Log($"Calling BuildInputViews for {block.Type}. BlockView has {blockView.ChildViews.Count} child views.");

        BuildInputViews(block, blockView);
        blockView.BuildLayout();
        blockView.ChangeBgColor(Color.blue);
        blockView.BindView(block);

        return blockObj;
    }

    public static LineGroupView BuildNewLineGroup(BlockView blockView)
    {
        GameObject groupObj = new GameObject("LineGroup");
        RectTransform groupTrans = groupObj.AddComponent<RectTransform>();
        groupTrans.SetParent(blockView.transform);
        UniformRectTransform(groupTrans);

        LineGroupView groupView = AddViewComponent<LineGroupView>(groupObj);
        blockView.AddChildView(groupView);

        return groupView;
    }

    
    public static void BuildInputViews(BlockModel block, BlockView blockView)
    {
        bool inputsInline = block.GetInputsInline();
        LineGroupView groupView = blockView.GetLineGroup(0);

        if (groupView == null)
        {
            Debug.LogError($"CRITICAL: Initial LineGroup (index 0) not found in BlockView '{blockView.BlockType}'. Cannot build inputs.", blockView.gameObject);
      
            return; 
        }
        List<InputView> oldInputViews = blockView.GetInputViews();

        foreach (InputView view in oldInputViews)
        {
            if (!block.InputList.Contains(view.InputModel))
            {
                view.UnBindModel();
                GameObject.DestroyImmediate(view.gameObject);
            }
        }

      
        for (int i = 0; i < block.InputList.Count; i++)
        {
            InputModel input = block.InputList[i];
            bool useNewLineGroup = false; 

            if (inputsInline)
            {
                if (input.Type == EConnection.NextStatement)
                { 
                    useNewLineGroup = true;
                }
            }
               else if (input.Type == EConnection.NextStatement)
            {
                useNewLineGroup = true;
            }

            if (useNewLineGroup && i > 0) 
            {
                groupView = blockView.GetLineGroup(blockView.ChildViews.OfType<LineGroupView>().Count()); 
                if (groupView == null) groupView = BuildNewLineGroup(blockView);
            }

            if (groupView == null)
            {
                Debug.LogError($"BuildInputViews: Failed to get/create LineGroupView for input index {i}. Skipping.", blockView.gameObject);
                continue;
            }

            if (/* needBuild */ true)
            { 
                InputView inputView = BuildInputView(input, groupView, blockView); 
                if (inputView != null)
                {
                    groupView.AddChildView(inputView); 
                }
            }
        }
        for (int i = blockView.ChildViews .Count - 1; i >= 0; i--)
        {
            BaseView view = blockView.ChildViews [i];
            if (view.Type == ViewType.LineGroup && view.ChildViews .Count == 0)
                GameObject.DestroyImmediate(view.gameObject);
        }
    }

    public static InputView BuildInputView(InputModel input, LineGroupView groupView, BlockView blockView)
    {
        GameObject inputPrefab;
        ConnectionInputViewType viewType;
        if (input.Type == EConnection.NextStatement)
        {
            inputPrefab = BlockViewSettings.Get().PrefabInputStatement;
            viewType = ConnectionInputViewType.Statement;
        }
        else if (input.SourceBlock.InputList.Count > 1 && input.SourceBlock.GetInputsInline())
        {
            inputPrefab = BlockViewSettings.Get().PrefabInputValueSlot;
            viewType = ConnectionInputViewType.ValueSlot;
        }
        else
        {
            inputPrefab = BlockViewSettings.Get().PrefabInputValue;
            viewType = ConnectionInputViewType.Value;

            Debug.Log($"Input '{input.Name}': Connection is real. Determined viewType: {viewType}");
        }

        GameObject inputObj = GameObject.Instantiate(inputPrefab);

        if (inputObj == null)
        {
            Debug.Log("Error en el inputObj");
        }
        inputObj.name = "Input_" + (!string.IsNullOrEmpty(input.Name) ? input.Name : "");
        
        RectTransform inputTrans = inputObj.GetComponent<RectTransform>();

        if (groupView == null)
        {
            Debug.LogError($"BuildInputView received NULL groupView for input '{input.Name}'! Aborting.", blockView.gameObject);
            GameObject.DestroyImmediate(inputObj); return null;
        }
        if (groupView.transform == null)
        { 
            Debug.LogError($"BuildInputView: groupView for input '{input.Name}' has a NULL TRANSFORM! Aborting.", groupView.gameObject);
            GameObject.DestroyImmediate(inputObj); return null;
        }
        if (groupView.gameObject == null)
        {
            Debug.LogError($"BuildInputView: groupView.gameObject is NULL for input '{input.Name}'! Likely destroyed. Aborting.", blockView.gameObject);
        }
        inputTrans.SetParent(groupView.transform, false);
        UniformRectTransform(inputTrans);

        Transform conInputTrans = inputTrans.GetChild(0);

        InputView inputView = AddViewComponent<InputView>(inputObj);
        inputView.AlignRight = input.Align == EAlign.Right;

        List<FieldModel> fields = input.FieldRow;
        foreach (FieldModel field in fields)
        {
            FieldView fieldView = BuildFieldView(field);
            inputView.AddChildView(fieldView);
            RectTransform fieldTrans = fieldView.GetComponent<RectTransform>();
            UniformRectTransform(fieldTrans);
        }
        Debug.Log($"Checking Input Type: {input.Name} is type {input.Type}. Is Dummy/None? {input.Type == EConnection.DummyInput || input.Type == EConnection.None}");
        if (input.Type == EConnection.DummyInput || input.Type == EConnection.None)
        {
            if (conInputTrans != null) 

                GameObject.DestroyImmediate(conInputTrans.gameObject);

            else Debug.LogWarning($"Dummy Input '{input.Name}': Child at index 0 (connection point) not found to destroy.");

        }
        else
        {
            Debug.Log($"Input '{input.Name}': Handling REAL connection. conInputTrans is null? {conInputTrans == null}", (conInputTrans != null ? conInputTrans.gameObject : null));
            if (conInputTrans == null) { 
                //TODO: Añadir Error Log, Destroy(inputObj), return null
            }

            ConnectionInputView conInputView = AddViewComponent<ConnectionInputView>(conInputTrans.gameObject); 
            if (conInputView == null) {
                //TODO:Añadir Error Log, Destroy(inputObj), return null
            }

            conInputView.ConnectionType = input.Type; 

            

            inputView.AddChildView(conInputView); 

          
            if (conInputView.BgImage == null) 
            {
                Debug.LogError($"ConnectionInputView for Input '{input.Name}' on Block '{blockView.BlockType}' has a NULL BgImage! Check prefab '{inputPrefab.name}' child '{conInputTrans?.name}'.", inputObj);
                GameObject.DestroyImmediate(inputObj); 
                return null;
            }
            else if (conInputView.BgImage.gameObject == null)
            {
                Debug.LogError($"Input '{input.Name}': BgImage component reference exists, BUT ITS GAMEOBJECT IS NULL! Likely destroyed. Check Dummy Input logic / Prefab Awake.", conInputView.gameObject);
                if (inputObj != null) GameObject.DestroyImmediate(inputObj); return null;
            }
            else
            {
                conInputView.BgImage.raycastTarget = false; 

               
                    blockView.AddBgImage(conInputView.BgImage);
            }
        }

        return inputView;
    }

    public static FieldView BuildFieldView(FieldModel field)
    {
        FieldView fieldView = null;
        GameObject fieldObj = null;
        GameObject prefabToInstantiate = null;

        Type viewTypeToAdd = null;
        bool wasHandled = true;

        Type fieldType = field.GetType();

        if (fieldType == typeof(FieldLabelModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldLabel;
            viewTypeToAdd = typeof(FieldLabelView);
        }
        else if (fieldType == typeof(FieldTextInputModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldInput;
            viewTypeToAdd = typeof(FieldInputView);
        }
        else if (fieldType == typeof(FieldVariableModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldVariable;
            viewTypeToAdd = typeof(FieldVariableView);
        }
        /*else if (fieldType == typeof(FieldColour))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldColor;
            viewTypeToAdd = typeof(FieldColorView);
            if (prefabToInstantiate == null) Debug.LogError("BuildFieldView: Missing 'PrefabFieldColor' in BlockViewSettings!");
        }*/
        else if (fieldType == typeof(FieldImageModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldImage;
            viewTypeToAdd = typeof(FieldImageView);
        }
        else if (fieldType == typeof(FieldCheckboxModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldCheckbox;
            viewTypeToAdd = typeof(FieldCheckboxView);
        }
        else if (fieldType == typeof(FieldNumberModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldInput; 
            viewTypeToAdd = typeof(FieldInputView); 
        }
       /* else if (fieldType == typeof(FieldDropdownModel))
        {
            
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldDropdown;
            viewTypeToAdd = typeof(FieldDropdownView);
            if (prefabToInstantiate == null) Debug.LogError("BuildFieldView: Missing 'PrefabFieldDropdown' in BlockViewSettings!");
        }*/
        else if (fieldType == typeof(FieldButtonModel))
        {
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldButton;
            viewTypeToAdd = typeof(FieldButtonView);
        }
        else 
        {
            wasHandled = false;
            Debug.LogWarning($"BuildFieldView: Unhandled field type: {fieldType.Name}. Attempting fallback label.");
            prefabToInstantiate = BlockViewSettings.Get().PrefabFieldLabel; 
        }

        bool creationSuccessful = false;
        if (prefabToInstantiate != null)
        {
            fieldObj = GameObject.Instantiate(prefabToInstantiate);
            if (fieldObj != null)
            {
                if (viewTypeToAdd != null) 
                {
                    
                    if (viewTypeToAdd == typeof(FieldLabelView)) fieldView = AddViewComponent<FieldLabelView>(fieldObj);
                    else if (viewTypeToAdd == typeof(FieldInputView)) fieldView = AddViewComponent<FieldInputView>(fieldObj);
                    else if (viewTypeToAdd == typeof(FieldVariableView)) fieldView = AddViewComponent<FieldVariableView>(fieldObj);
                    else if (viewTypeToAdd == typeof(FieldColorView)) fieldView = AddViewComponent<FieldColorView>(fieldObj);
                    else if (viewTypeToAdd == typeof(FieldImageView)) fieldView = AddViewComponent<FieldImageView>(fieldObj);
                    else if (viewTypeToAdd == typeof(FieldCheckboxView)) fieldView = AddViewComponent<FieldCheckboxView>(fieldObj);
                  
                    else if (viewTypeToAdd == typeof(FieldDropdownView)) fieldView = AddViewComponent<FieldDropdownView>(fieldObj);
                    //else if (viewTypeToAdd == typeof(FieldButtonView)) fieldView = AddViewComponent<FieldButtonView>(fieldObj);
                    else
                    {
                        Debug.LogError($"BuildFieldView: Unexpected viewTypeToAdd '{viewTypeToAdd.Name}' even after selecting prefab. Cannot add component.", fieldObj);
                    }

                    if (fieldView != null)
                    {
                        creationSuccessful = true; 
                    }
                    else
                    {
                        Debug.LogError($"BuildFieldView: AddViewComponent failed for type '{viewTypeToAdd.Name}' on prefab '{prefabToInstantiate.name}'. Destroying object.", fieldObj);
                        GameObject.DestroyImmediate(fieldObj);
                        fieldObj = null;
                    }
                }
            }
            else
            {
                Debug.LogError($"BuildFieldView: Failed to Instantiate prefab '{prefabToInstantiate.name}'.");
            }
        }
        else
        {
            Debug.LogError($"BuildFieldView: Cannot create view for {fieldType.Name} because selected prefab was null (Check Settings or unhandled type).");
        }


        if (!creationSuccessful)
        {
            Debug.LogError($"BuildFieldView failed for field '{field.Name}' (Type: {fieldType.Name}). Creating Error Label.");

            
            GameObject errorPrefab = BlockViewSettings.Get().PrefabFieldLabel;
            if (errorPrefab != null)
            {
                fieldObj = GameObject.Instantiate(errorPrefab);
                fieldView = AddViewComponent<FieldLabelView>(fieldObj); 

                if (fieldView != null && fieldObj != null)
                {
                    fieldObj.name = "Field_ERROR_" + fieldType.Name; 
                    ((FieldLabelView)fieldView).SetDisplayText($"[ERR:{fieldType.Name}]"); 

                    CanvasGroup cg = fieldView.GetComponent<CanvasGroup>() ?? fieldView.gameObject.AddComponent<CanvasGroup>();
                    cg.interactable = false;
                    wasHandled = false; 
                    Debug.LogWarning("-> Successfully created fallback error label.");
                }
                else
                {
                    Debug.LogError("CRITICAL: Failed even to create the error label fallback VIEW/OBJECT!");
                    if (fieldObj != null) GameObject.DestroyImmediate(fieldObj); 
                    return null; 
                }
            }
            else
            {
                Debug.LogError("CRITICAL: PrefabFieldLabel is MISSING. Cannot create fallback label.");
                return null;
            }
        }

        // if(fieldView != null) fieldView.BindModel(field); 

        return fieldView; 
    }


}//fin clase BlockViewBuiler