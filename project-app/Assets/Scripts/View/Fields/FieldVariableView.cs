/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 03/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using System.Collections.Generic; 
using System.Linq;

public class FieldVariableView : FieldView 
{
    [Header("UI References (Assign in Prefab)")]
    [SerializeField] protected TextMeshProUGUI m_VariableNameText; 
    [SerializeField] protected Button m_DropdownButton;        
    [SerializeField] protected Image m_DropdownArrow;         

    protected FieldVariableModel FieldVariableModel => FieldModel as FieldVariableModel; 

    private List<Dropdown.OptionData> m_VariableOptions = new List<Dropdown.OptionData>();
    protected override void Awake() 
    {
        base.Awake();
        if (m_VariableNameText == null) m_VariableNameText = GetComponentInChildren<TextMeshProUGUI>();
        if (m_DropdownButton == null) m_DropdownButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(); 
        if (m_DropdownArrow == null)
        {
            Transform arrow = transform.Find("Arrow");
            if (arrow != null) m_DropdownArrow = arrow.GetComponent<Image>();
        }

        if (m_VariableNameText == null) Debug.LogError("FieldVariableView: TextMeshProUGUI component not found!", this);
        if (m_DropdownButton == null) Debug.LogError("FieldVariableView: Button component not found!", this);
    }

    protected  void OnBindModel()
    {
        if (m_VariableNameText == null || m_DropdownButton == null || FieldVariableModel == null)
        {
            if (m_VariableNameText != null) m_VariableNameText.text = "[BIND ERROR]";
            if (m_DropdownButton != null) m_DropdownButton.interactable = false;
            if (FieldVariableModel == null) Debug.LogError($"FieldVariableView ({gameObject.name}): Model is not a FieldVariableModel!", this);
            return;
        }

        UpdateText();

        m_DropdownButton.interactable = FieldModel.IsEditable;

        m_DropdownButton.onClick.RemoveListener(OnDropdownClicked); 
        m_DropdownButton.onClick.AddListener(OnDropdownClicked);

        if (m_DropdownArrow != null) m_DropdownArrow.enabled = FieldModel.IsEditable;

        
        // FieldVariableModel.AddObserver(UpdateText);
    }

    protected  void OnUnBindModel()
    {
        if (m_DropdownButton != null)
        {
            m_DropdownButton.onClick.RemoveListener(OnDropdownClicked);
        }
       
        // FieldVariableModel?.RemoveObserver(UpdateText);
    }

    protected virtual void UpdateText()
    {
        if (m_VariableNameText == null || FieldVariableModel == null) return;

        string variableNameToDisplay = "[Select Variable]";


        string currentVarId = FieldVariableModel.GetValue();

        WorkSpaceModel workspace = FieldVariableModel.SourceBlock?.Workspace;

        if (!string.IsNullOrEmpty(currentVarId) && workspace != null)
        {
            string lookedUpName = FieldVariableModel.GetVariableName(currentVarId, workspace);

            if (!string.IsNullOrEmpty(lookedUpName))
            {
                variableNameToDisplay = lookedUpName;
            }
            else
            {
                Debug.LogWarning($"FieldVariableView: Variable con ID '{currentVarId}' no encontrada en el workspace para el campo '{FieldVariableModel.Name}'. Mostrando ID como fallback.");
                variableNameToDisplay = $"[{currentVarId}]";
                // FieldVariableModel.SetValue(null); // Limpiar la selección inválida
            }
        }
        else if (string.IsNullOrEmpty(currentVarId))
        {

            variableNameToDisplay = "[Select Variable]";
        }
        else
        {
            Debug.LogError($"FieldVariableView: Workspace es null para el campo '{FieldVariableModel.Name}'. No se puede obtener el nombre de la variable.");
            variableNameToDisplay = "[Error: No Workspace]";
        }


        m_VariableNameText.text = variableNameToDisplay;
        MarkDirty();
    }


 
    protected virtual void OnDropdownClicked()
    {
        if (FieldVariableModel == null || FieldModel.SourceBlock?.Workspace == null)
        {
            Debug.LogError("Cannot show variable dropdown: Missing Model or Workspace reference.", this);
            return;
        }

        WorkSpaceModel workspace = FieldModel.SourceBlock.Workspace; 
        List<VariableModel> variables = workspace.GetAllVariables(); 
        string currentVarId = FieldVariableModel.GetValue(); 

        string currentVariableName = "[None Selected]"; 
        if (!string.IsNullOrEmpty(currentVarId)) 
        {
            string lookedUpName = FieldVariableModel.GetVariableName(currentVarId, workspace);
            if (!string.IsNullOrEmpty(lookedUpName))
            {
                currentVariableName = lookedUpName; 
            }
            else
            {
                currentVariableName = $"[ID: {currentVarId} - Not Found]";
            }
        }

     
        Debug.Log($"--- Select Variable (Current: {currentVariableName}) ---"); 
        m_VariableOptions.Clear();
        int selectedIndex = -1;
        for (int i = 0; i < variables.Count; i++)
        {
            VariableModel vm = variables[i];
            string displayText = vm.Name; 
            Debug.Log($"{i}: {displayText} (ID: {vm.ID})");
          
            if (vm.ID == currentVarId) selectedIndex = i; 
        }
        Debug.Log("---------------------------------");
      
        // if (Define.FIELD_VARIABLE_ADD_MANIPULATION_OPTIONS) { // Si se habilitan las opciones especiales
        //     if (!string.IsNullOrEmpty(currentVarId)) { // Solo si hay variable seleccionada
        //         Debug.Log($"R: {I18n.Get(MsgDefine.RENAME_VARIABLE)}...");
        //         Debug.Log($"D: {I18n.Get(MsgDefine.DELETE_VARIABLE)}...");
        //     }
        //     // Debug.Log($"N: {I18n.Get(MsgDefine.NEW_VARIABLE)}...");
        // }

   
        if (variables.Count > 0)
        {
            VariableModel selectedVar = variables[0];
            if (selectedVar.ID != currentVarId)
            {
                Debug.Log($"Simulating selection: Setting variable to '{selectedVar.Name}' (ID: {selectedVar.ID})");
                FieldVariableModel.SetValue(selectedVar.ID);
                UpdateText();
            }
        }

        // ShowActualDropdownUI(variables, currentVarId, OnVariableSelected);
    }

    private void OnVariableSelected(VariableModel selectedVariable) 
    {
        if (FieldVariableModel != null && selectedVariable != null)
        {
            if (FieldVariableModel.GetValue() != selectedVariable.ID)
            {
                FieldVariableModel.SetValue(selectedVariable.ID); 
                UpdateText(); 
            }
        }
        else if (selectedVariable == null) 
        {
            // HandleSpecialVariableActions();
        }
    }

    protected override Vector2 CalculateSize()
    {
        if (m_VariableNameText == null) return BlockViewSettings.Get().MinUnitSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_VariableNameText.rectTransform);
        float textWidth = LayoutUtility.GetPreferredWidth(m_VariableNameText.rectTransform);
        float textHeight = LayoutUtility.GetPreferredHeight(m_VariableNameText.rectTransform);

        float buttonWidth = 0;
        if (m_DropdownButton != null && m_DropdownButton.gameObject.activeSelf)
        {
            buttonWidth = BlockViewSettings.Get().DropdownArrowWidth;
        }

        float totalWidth = textWidth + buttonWidth + BlockViewSettings.Get().ContentSpace.x * 2; // Padding interno
        float totalHeight = textHeight + BlockViewSettings.Get().ContentSpace.y * 2; // Padding interno


        // Considerar mínimos
        totalWidth = Mathf.Max(totalWidth, BlockViewSettings.Get().MinUnitSize.x);
        totalHeight = Mathf.Max(totalHeight, BlockViewSettings.Get().MinUnitSize.y);

        return new Vector2(totalWidth, totalHeight);
    }

    protected override void OnValueChanged(string newValue)
    {
        throw new System.NotImplementedException();
    }

    protected override void RegisterInputListeners()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FieldVariableView
