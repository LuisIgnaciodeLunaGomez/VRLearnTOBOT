
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
        // Hacemos la comprobación de seguridad para los Settings
        if (BlockViewSettings.Instance == null)
        {
            return new Vector2(150, 24); // Devolvemos un tamaño por defecto
        }

        if (mDropdown == null || mDropdown.captionText == null)
        {
            // Si el dropdown no está listo, devolvemos un tamaño mínimo
            return new Vector2(BlockViewSettings.Instance.MinUnitWidth * 5, BlockViewSettings.Instance.MinUnitHeight);
        }

        // El ancho preferido se basa en el texto más largo de todas las opciones para evitar que se corte
        float preferredWidth = 0;
        if (mDropdown.options.Count > 0)
        {
            foreach (var option in mDropdown.options)
            {
                preferredWidth = Mathf.Max(preferredWidth, mDropdown.captionText.GetPreferredValues(option.text).x);
            }
        }
        else
        {
            // Si no hay opciones, medimos un texto de fallback
            preferredWidth = mDropdown.captionText.GetPreferredValues("No procedures").x;
        }

        // Añadimos el ancho de la flechita del dropdown + un padding
        preferredWidth += BlockViewSettings.Instance.DropdownArrowWidth;
        preferredWidth += BlockViewSettings.Instance.FieldInputTextPadding.horizontal; // Padding horizontal general

        // La altura suele ser fija
        float preferredHeight = BlockViewSettings.Instance.DefaultInputFieldHeight; // Reutilizamos la altura de los input fields para consistencia

        // El tamaño final no puede ser menor que los mínimos absolutos.
        float finalWidth = Mathf.Max(preferredWidth, BlockViewSettings.Instance.MinUnitWidth * 5); // Un mínimo ancho para los desplegables
        float finalHeight = Mathf.Max(preferredHeight, BlockViewSettings.Instance.MinUnitHeight);

        return new Vector2(finalWidth, finalHeight);

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

    public override void UpdateLayout(Vector2 startPos)
    {
        this.XY = startPos;
        this.Size = CalculateSize();
    }

    public override Vector2 CalculateFieldSize()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FieldProcedureView