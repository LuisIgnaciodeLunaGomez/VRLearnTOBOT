/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 * 
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class FieldDropdownView : FieldView
{
    [SerializeField] private TMP_Dropdown m_Dropdown;
    protected override void InitializeView()
    {
        base.InitializeView(); 
        if (m_Dropdown == null)
        {
            m_Dropdown = GetComponentInChildren<TMP_Dropdown>(); 
            if (m_Dropdown == null)
            {
                Debug.LogError($"FieldDropdownView ({gameObject.name}): TMP_Dropdown component not found or assigned.", this);
                return; 
            }
        }
        PopulateOptions();

     
    }

    protected override void RegisterInputListeners()
    {
        if (m_Dropdown != null)
        {
            m_Dropdown.onValueChanged.RemoveListener(HandleDropdownChange);
            m_Dropdown.onValueChanged.AddListener(HandleDropdownChange);
        }
        else
        {
            Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Cannot register listeners, TMP_Dropdown is null.", this);
        }
    }

    //  Calcular Tamaño
    protected override Vector2 CalculateSize()
    {
        
        if (BlockViewSettings.Instance != null) 
        {
            float preferredWidth = LayoutUtility.GetPreferredWidth(m_Dropdown.GetComponent<RectTransform>());
            if (preferredWidth <= 0)
            {
                        preferredWidth = BlockViewSettings.Instance.MinUnitSize.x * 4;
            }

            float height = BlockViewSettings.Instance.MinUnitSize.y; 

            return new Vector2(preferredWidth, height);

            // return BlockViewSettings.Instance.MinUnitSize * new Vector2(4, 1);
        }
        else
        {
            Debug.LogError("FieldDropdownView could not calculate size because BlockViewSettings.Instance is null.");
            return new Vector2(100, 20); 
        }
    }
    protected override void OnValueChanged(string newValue)
    {
        if (m_Dropdown == null || m_FieldModel == null || !gameObject.activeInHierarchy) return;


        if (m_FieldModel is FieldVariableModel)
        {
            string variableId = newValue; 
            if (string.IsNullOrEmpty(variableId))
            {       // m_Dropdown.SetValueWithoutNotify(0); // Opcional: seleccionar el primero por defecto
                Debug.LogWarning($"FieldDropdownView ({gameObject.name}): OnValueChanged called with empty or null variableId.");
                return;
            }

                  WorkSpaceModel workspace = WorkspaceView?.Workspace;
            if (workspace == null)
            {
                //  Debug.LogError("FieldDropdownView: Cannot find variable, Workspace is null!");
                return;
            }

            VariableModel targetVariable = workspace.GetVariableById(variableId);
            if (targetVariable == null)
            {
                Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Variable with ID '{variableId}' not found in workspace.");
                PopulateOptions();
                return;
            }

            for (int i = 0; i < m_Dropdown.options.Count; i++)
            {
                if (m_Dropdown.options[i].text == targetVariable.Name)
                {
                    m_Dropdown.SetValueWithoutNotify(i);
                    // Debug.Log($"Selected option {i} ({targetVariable.Name}) for variable ID {variableId}");
                    return;
                }
            }

            Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Could not find dropdown option corresponding to variable name: {targetVariable.Name} (ID: {variableId})");
            PopulateOptions(); 

        }
        else if (m_FieldModel is FieldDropdownModel dropdownModel)
        {
            var modelOptions = dropdownModel.GetOptions(); 
            for (int i = 0; i < modelOptions.Count && i < m_Dropdown.options.Count; i++) 
            {
                if (modelOptions[i].Value == newValue)
                {
                    m_Dropdown.SetValueWithoutNotify(i);
                    return;
                }
            }
            Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Could not find dropdown option corresponding to value: {newValue}");

        }
        else 
        {
            int index = m_Dropdown.options.FindIndex(option => option.text == newValue);
            if (index >= 0)
            {
                m_Dropdown.SetValueWithoutNotify(index);
            }
            else
            {
               
                // Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Could not find dropdown option with text: {newValue}");
            }
        }
        m_Dropdown.RefreshShownValue();
    }

    private void PopulateOptions()
    {
        if (m_Dropdown == null || m_FieldModel == null) return;

        m_Dropdown.ClearOptions();
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();

        if (m_FieldModel is FieldVariableModel variableField)
        {
            WorkSpaceModel workspace = WorkspaceView?.Workspace;
            if (workspace == null)
            {
                Debug.LogError($"FieldDropdownView ({gameObject.name}): Cannot populate variable dropdown, Workspace is null.");
                m_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Error: No Workspace") });
                m_Dropdown.interactable = false; 
                return;
            }
            m_Dropdown.interactable = true; 

            List<VariableModel> variables = workspace.GetAllVariables(); 

            variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)); 

            foreach (var variable in variables)
            {
                options.Add(new TMP_Dropdown.OptionData(variable.Name));
            }

            options.Add(new TMP_Dropdown.OptionData("---")); 
            options.Add(new TMP_Dropdown.OptionData("Renombrar variable..."));
            options.Add(new TMP_Dropdown.OptionData("Eliminar variable...")); 

            m_Dropdown.AddOptions(options);

            string currentVariableId = variableField.GetValue();
             VariableModel currentVar = workspace.GetVariableById(currentVariableId);
            int selectedIndex = -1;
            if (currentVar != null)
            {
                selectedIndex = options.FindIndex(opt => opt.text == currentVar.Name);
            }

            if (selectedIndex >= 0)
            {
                m_Dropdown.SetValueWithoutNotify(selectedIndex);
            }
            else
            {
                  m_Dropdown.SetValueWithoutNotify(0);
            }

        }
        else if (m_FieldModel is FieldDropdownModel dropdownModel)
        {
            var modelOptions = dropdownModel.GetOptions();
            if (modelOptions == null || modelOptions.Count == 0)
            {
                Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Dropdown model provided no options.");
                m_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("(No options)") });
                m_Dropdown.interactable = false;
                return;
            }
            m_Dropdown.interactable = true;

            foreach (var optionPair in modelOptions)
            {
                options.Add(new TMP_Dropdown.OptionData(optionPair.Value));
            }
            m_Dropdown.AddOptions(options);

            string currentValue = dropdownModel.GetValue();
            int selectedIndex = modelOptions.FindIndex(opt => opt.Value == currentValue);
            if (selectedIndex >= 0)
            {
                m_Dropdown.SetValueWithoutNotify(selectedIndex);
            }
            else
            {
                Debug.LogWarning($"FieldDropdownView ({gameObject.name}): Current model value '{currentValue}' not found in options. Selecting index 0.");
                if (options.Count > 0) m_Dropdown.SetValueWithoutNotify(0); 
            }

        }
        else
        {
            Debug.LogWarning($"PopulateOptions not fully implemented for Field Type: {m_FieldModel.GetType()} on {gameObject.name}");
            string currentValue = m_FieldModel.GetValue();
            options.Add(new TMP_Dropdown.OptionData(string.IsNullOrEmpty(currentValue) ? "(Value)" : currentValue));
            m_Dropdown.AddOptions(options);
            m_Dropdown.SetValueWithoutNotify(0);
            m_Dropdown.interactable = false; 
        }
        m_Dropdown.RefreshShownValue();

        MarkDirty(); 

    }
    private void HandleDropdownChange(int index)
    {
        if (m_Dropdown == null || m_FieldModel == null) return;
        if (index < 0 || index >= m_Dropdown.options.Count) return;

        string selectedDisplayText = m_Dropdown.options[index].text;
        string selectedValue = ""; 

        if (m_FieldModel is FieldVariableModel variableField)
        {
            WorkSpaceModel workspace = WorkspaceView?.Workspace;
            if (workspace == null) return;

            if (selectedDisplayText == "Renombrar variable...")
            {
                    string currentVarId = variableField.GetValue();
                VariableModel currentVar = workspace.GetVariableById(currentVarId);
                if (currentVar != null)
                {
                       Debug.Log($"Action: Request Rename UI for variable '{currentVar.Name}' (ID: {currentVar.ID})");
                }
                else
                {
                    Debug.LogWarning("Cannot rename: Current variable not found.");
                }
                OnValueChanged(variableField.GetValue()); 
                return; 

            }
            else if (selectedDisplayText == "Eliminar variable...")
            {
                string currentVarId = variableField.GetValue();
                VariableModel currentVar = workspace.GetVariableById(currentVarId);
                if (currentVar != null)
                {
                         Debug.Log($"Action: Request Delete Confirmation UI for variable '{currentVar.Name}' (ID: {currentVar.ID})");
                }
                else
                {
                    Debug.LogWarning("Cannot delete: Current variable not found.");
                }
                OnValueChanged(variableField.GetValue());
                return;
            }
            else if (selectedDisplayText == "---") 
            {
                OnValueChanged(variableField.GetValue());
                return;
            }

            VariableModel selectedVariable = workspace.GetVariable(selectedDisplayText); 
            if (selectedVariable != null)
            {
                selectedValue = selectedVariable.ID; 
            }
            else
            {
                Debug.LogError($"Selected variable name '{selectedDisplayText}' not found in workspace during HandleDropdownChange!");
                OnValueChanged(variableField.GetValue());
                return;
            }
        }
        else if (m_FieldModel is FieldDropdownModel dropdownModel)
        {
            var modelOptions = dropdownModel.GetOptions();
            if (index >= 0 && index < modelOptions.Count)
            {
                selectedValue = modelOptions[index].Value; 
            }
            else
            {
                Debug.LogError($"HandleDropdownChange index {index} out of range for model options.");
                OnValueChanged(m_FieldModel.GetValue());
                return;
            }

        }
        else
        {
            selectedValue = selectedDisplayText;
        }

         if (m_FieldModel.GetValue() != selectedValue)
        {
            RequestModelUpdate(selectedValue);
        }
        else
        {
            // Debug.Log("Dropdown selection didn't change the logical value.");
        }

    }
}//Fin clase FieldDropdownView