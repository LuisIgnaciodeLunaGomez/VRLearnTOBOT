/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public sealed class FieldVariableModel : FieldDropdownModel
{
    
    private string m_DefaultVariableNameHint;

    [FieldCreator(FieldType = "field_variable")]
    private static FieldVariableModel CreateFromJson(JObject json)
    {
        string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                           ? json["name"].ToString()
                           : "FIELDNAME_VARIABLE"; 

        

        string initialVarNameHint = json["variable"]?.ToString() ?? "";


        return new FieldVariableModel(fieldName, initialVarNameHint);
    }


    public FieldVariableModel(string fieldName, string defaultVarNameHint = null)
        : base(fieldName) 
    {
        m_DefaultVariableNameHint = defaultVarNameHint ?? "DefaultVar"; 

        
        SetOptionsProvider(GenerateVariableOptions); 
    }
    public override string GetValue()
    {
        return base.GetValue();
    }


    public override void SetValue(string newValue)
    {
        if (SourceBlock?.Workspace == null)
        {
            Debug.LogWarning($"FieldVariable '{Name}': Cannot set value '{newValue}', SourceBlock or Workspace is null.");
            
            return;
        }

        WorkSpaceModel workspace = SourceBlock.Workspace;
        VariableModel variable = null;
        string variableIdToSet = null;

        
        variable = workspace.GetVariableById(newValue);
        if (variable != null)
        {
           
            variableIdToSet = variable.ID;
        }
        else
        {
           
            variable = workspace.GetVariable(newValue);
            if (variable != null)
            {
                variableIdToSet = variable.ID;
            }
            else
            {
              
                Debug.LogWarning($"FieldVariable '{Name}': Variable ID or Name '{newValue}' not found in workspace. Selection might be cleared.");
                variableIdToSet = null; 
            }
        }

        
        base.SetValue(variableIdToSet);
    }


    private List<FieldDropdownModel.FieldDropdownMenu> GenerateVariableOptions()
    {
        var options = new List<FieldDropdownModel.FieldDropdownMenu>();
        WorkSpaceModel workspace = SourceBlock?.Workspace; 

        if (workspace == null)
        {
            Debug.LogWarning($"FieldVariable '{Name}': Cannot generate options, SourceBlock or Workspace is null.");
            return options;
        }

        // List<VariableModel> varModels = workspace.GetAllVariables(); 
        List<VariableModel> varModels = workspace.GetVariablesOfType(""); 
        if (varModels != null)
        {
            varModels = new List<VariableModel>(varModels);
            varModels.Sort(VariableModel.CompareByName); 

            
            foreach (var variable in varModels)
            {
                options.Add(new FieldDropdownModel.FieldDropdownMenu(variable.Name, variable.ID));
            }
        }

        if (Define.FIELD_VARIABLE_ADD_MANIPULATION_OPTIONS) 
        {
            string currentVarId = base.GetValue(); 
            bool variableSelected = !string.IsNullOrEmpty(currentVarId) && workspace.GetVariableById(currentVarId) != null;

          
            if (variableSelected)
            {
                options.Add(new FieldDropdownModel.FieldDropdownMenu(I18n.Get(MsgDefine.RENAME_VARIABLE), Define.RENAME_VARIABLE_OPTION_VALUE)); 
                options.Add(new FieldDropdownModel.FieldDropdownMenu(I18n.Get(MsgDefine.DELETE_VARIABLE), Define.DELETE_VARIABLE_OPTION_VALUE)); 
            }
        }

    

        return options;
    }

    
    public override void OnItemSelected(int itemIndex)
    {
        var currentOptions = GetOptions();

        if (itemIndex < 0 || itemIndex >= currentOptions.Count)
        {
            Debug.LogError($"FieldVariable '{Name}': Invalid item index {itemIndex}.");
            return;
        }

        string selectedValue = currentOptions[itemIndex].Value;
        string selectedText = currentOptions[itemIndex].Text; 

        WorkSpaceModel workspace = SourceBlock?.Workspace;

     
        if (workspace != null && Define.FIELD_VARIABLE_ADD_MANIPULATION_OPTIONS)
        {
            if (selectedValue == Define.RENAME_VARIABLE_OPTION_VALUE)
            {
                string currentVarId = base.GetValue(); 
                if (!string.IsNullOrEmpty(currentVarId))
                {
                    Debug.Log($"FieldVariable '{Name}': Rename requested for variable ID '{currentVarId}'.");
                   
                    workspace.GetVariable(currentVarId)?.RequestRename();
                }
                return;
            }
            else if (selectedValue == Define.DELETE_VARIABLE_OPTION_VALUE)
            {
                string currentVarId = base.GetValue(); 
                if (!string.IsNullOrEmpty(currentVarId))
                {
                    Debug.Log($"FieldVariable '{Name}': Delete requested for variable ID '{currentVarId}'.");
                
                    workspace.GetVariable(currentVarId)?.RequestDelete();
                }
                return; 
            }
            /* else if (selectedValue == Define.NEW_VARIABLE_OPTION_VALUE)
            {
               // Trigger new variable UI
               return;
            } */
        }

       
        base.SetValue(selectedValue);
    }
    
    public static string GetVariableName(string variableId, WorkSpaceModel workspace)
    {
        if (string.IsNullOrEmpty(variableId) || workspace == null)
            return null;

        VariableModel variable = workspace.GetVariableById(variableId);
        return variable?.Name;
    }

}//Fin clase FieldVariableModel
