
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/042025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */


using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using System.Linq; 

[RequireComponent(typeof(TMP_Dropdown))]
public class FieldProcedureView : FieldView 
{
    protected TMP_Dropdown mDropdown;

    private WorkSpaceModel mWorkspace => WorkspaceView?.Workspace; 

    protected override void InitializeView() 
    {
        base.InitializeView(); 

        mDropdown = GetComponentInChildren<TMP_Dropdown>(); 
        if (mDropdown == null)
        {
            Debug.LogError("FieldProcedureView necesita un componente TMP_Dropdown!", this);
            enabled = false; 
            return;
        }

        mDropdown.ClearOptions();
        mDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

    }

    public override void BindModel(FieldModel fieldModel)
    {
        base.BindModel(fieldModel); 

        if (m_FieldModel != null) 
        {
            UpdateDropdownOptions();
            UpdateDropdownValue();   
        }
    }

    public override void UnbindModel()
    {
        if (mDropdown != null) mDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        base.UnbindModel(); 
    }


    
    //Puebla las opciones del desplegable con los nombres de procedimientos válidos.
    protected virtual void UpdateDropdownOptions()
    {
        if (mDropdown == null || mWorkspace == null || mWorkspace.ProcedureDB == null) return;

        mDropdown.ClearOptions();
        List<string> procedureNames = GetAvailableProcedures();

        if (procedureNames.Count > 0)
        {
            // Crear opciones TMP
            List<TMP_Dropdown.OptionData> options = procedureNames.Select(name => new TMP_Dropdown.OptionData(name)).ToList();
            mDropdown.AddOptions(options);
            mDropdown.interactable = true;
        }
        else
        {
            // No hay procedimientos, mostrar mensaje y desactivar
            mDropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("No procedures") });
            mDropdown.interactable = false;
        }
    }

    
    /// Selecciona la opción en el desplegable que coincide con el valor del modelo.
    
    protected virtual void UpdateDropdownValue()
    {
        if (mDropdown == null || mWorkspace == null || mWorkspace.ProcedureDB == null) return;

        // Guardar el valor actual para intentar restaurarlo
        string previousValue = mDropdown.options.Count > mDropdown.value ? mDropdown.options[mDropdown.value].text : null;

        mDropdown.ClearOptions();
        List<string> procedureNames = GetAvailableProcedures();

        if (procedureNames.Count > 0)
        {
            // Crear opciones TMP
            List<TMP_Dropdown.OptionData> options = procedureNames.Select(name => new TMP_Dropdown.OptionData(name)).ToList();
            mDropdown.AddOptions(options);
            mDropdown.interactable = true;
        }
        else
        {
            // No hay procedimientos, mostrar mensaje y desactivar
            mDropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("No procedures") }); 
            mDropdown.interactable = false;
        }
        UpdateDropdownValue();
    }
    

    // Callback cuando el usuario selecciona un valor diferente en el desplegable.
    // Actualiza el modelo FieldModel.
    protected virtual void OnDropdownValueChanged(int index)
    {
        if (mDropdown == null || m_FieldModel == null || index < 0 || index >= mDropdown.options.Count) return;

        if (!mDropdown.interactable || mDropdown.options[index].text == "No procedures") return; 

        string selectedName = mDropdown.options[index].text;

        RequestModelUpdate(selectedName);
    }
    

    // Obtiene la lista de nombres de procedimientos disponibles, filtrando según
    // si el bloque que contiene este campo es una llamada con o sin retorno.
  
    protected virtual List<string> GetAvailableProcedures()
    {
        if (mWorkspace == null || mWorkspace.ProcedureDB == null) return new List<string>();

        BlockView parentBlock = this.GetComponentInParent<BlockView>(); 
        bool needsReturn = parentBlock?.Block?.Type == Define.CALL_WITH_RETURN_BLOCK_TYPE;

        List<BlockModel> allDefinitions = mWorkspace.ProcedureDB.GetDefinitionBlocks();
        List<string> names = allDefinitions
            .Where(defBlock => ProcedureDB.HasReturn(defBlock) == needsReturn) 
            .Select(defBlock => ProcedureDB.GetProcedureName(defBlock)) 
            .Where(name => !string.IsNullOrEmpty(name)) 
            .OrderBy(name => name) 
            .ToList();

        return names;
    }

    protected override Vector2 CalculateSize()
    {
        if (mDropdown == null)
            InitializeView();
        if (mDropdown == null) return BlockViewSettings.Instance.MinUnitSize; 


        RectTransform dropdownRect = mDropdown.GetComponent<RectTransform>();
        float width = dropdownRect.rect.width;
        float height = dropdownRect.rect.height;

        if (width <= 0) width = BlockViewSettings.Instance.MinUnitSize.x * 2; 
        if (height <= 0) height = BlockViewSettings.Instance.MinUnitSize.y; 
        // width += BlockViewSettings.Instance.InternalPadding.x * 2;
        // height += BlockViewSettings.Instance.InternalPadding.y * 2;

        width += (BlockViewSettings.Instance.ContentMargin?.left ?? 0) + (BlockViewSettings.Instance.ContentMargin?.right ?? 0);
        height += (BlockViewSettings.Instance.ContentMargin?.top ?? 0) + (BlockViewSettings.Instance.ContentMargin?.bottom ?? 0);

        return new Vector2(width, height);

    }

    protected override void OnValueChanged(string newValue)
    {
        UpdateDropdownValue();
    }

    protected override void RegisterInputListeners()
    {
       // throw new System.NotImplementedException();
    }

    public void UpdateValue(string procedureName)
    {
        if (mDropdown == null || m_FieldModel == null)
        {
            Debug.LogWarning($"FieldProcedureView ({this.name}): Dropdown or FieldModel is null, cannot UpdateValue.");
            return;
        }

        int index = mDropdown.options.FindIndex(option => option.text == procedureName);

        if (index >= 0)
        {
            m_FieldModel.SetValue(procedureName); 

            mDropdown.SetValueWithoutNotify(index);
            mDropdown.RefreshShownValue(); 
            Debug.Log($"FieldProcedureView: Value updated to '{procedureName}' (index {index})");
        }
        else
        {
            
            Debug.LogWarning($"FieldProcedureView ({this.name}): Procedure name '{procedureName}' not found in dropdown options.", this);
           
        }
    }
}//Fin clase FieldProcedureView