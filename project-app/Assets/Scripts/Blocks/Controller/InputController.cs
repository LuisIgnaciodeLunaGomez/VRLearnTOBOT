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
 * Descripción: Controlador para los Inputs de los bloques, se encarga de gestionar los cambios en los campos de los bloques.
 */

using UnityEngine;
public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    
    private WorkspaceController m_WorkspaceController;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    public void InitializeController(WorkspaceController workspaceController)
    {
        m_WorkspaceController = workspaceController;
        if (m_WorkspaceController == null)
            Debug.LogError("InputController: WorkspaceController reference is missing after initialization!");
        else
            Debug.Log("InputController Initialized.");
    }

    /**
     * Llamado por una FieldView o su subclase  cuando el usuario ha ingresado un nuevo valor.
     * @param ublocklyField El modelo UBlockly.Field que se intenta cambiar.
     * @param newValueFromUI El nuevo valor string ingresado por el usuario.
     */
    public void HandleFieldInputValueChange(FieldModel ublocklyField, string newValueFromUI)
    {
        if (ublocklyField == null) { Debug.LogWarning("InputController: Received input change for a null UBlockly.Field."); return; }

        string processedValue = newValueFromUI; 

        if (ublocklyField.GetValue() == processedValue) return;

        if (m_WorkspaceController == null || m_WorkspaceController.IsReadOnly())
        {
            Debug.LogWarning("InputController: Workspace read-only or controller missing. Change rejected.");
            RevertFieldView(ublocklyField); 
            return;
        }
        if (ublocklyField.SourceBlock != null && !ublocklyField.SourceBlock.Editable)
        {
            Debug.LogWarning($"InputController: BlockModel {ublocklyField.SourceBlock.ID} is not editable. Change rejected.");
            RevertFieldView(ublocklyField);
            return;
        }

        Debug.Log($"InputController: Requesting WC to set field '{ublocklyField.Name ?? "unnamed"}' to '{processedValue}'");
        bool success = m_WorkspaceController.RequestFieldSetValue(ublocklyField, processedValue);

           if (!success) 
        {
            Debug.LogWarning($"InputController: WorkspaceController reported failure setting value for field '{ublocklyField.Name ?? "unnamed"}'. Reverting view.");
            RevertFieldView(ublocklyField);
        }
      }

    /**
      * Llamado por FieldDropdownView cuando se selecciona una opción.
      * @param ublocklyDropdownField El modelo UBlockly.FieldDropdown.
      * @param selectedValue El *valor* lógico de la opción seleccionada (no el texto).
      */
    public void HandleFieldDropdownSelection(FieldDropdownModel ublocklyDropdownField, string selectedValue)
    {
        if (ublocklyDropdownField == null) return;
        if (m_WorkspaceController == null || m_WorkspaceController.IsReadOnly() || (ublocklyDropdownField.SourceBlock != null && !ublocklyDropdownField.SourceBlock.Editable))
        {
            RevertFieldView(ublocklyDropdownField); 
            return;
        }

        m_WorkspaceController.RequestFieldSetValue(ublocklyDropdownField, selectedValue);
    }

    /**
      * Llamado por FieldVariableView cuando se elige una variable diferente.
      * @param ublocklyVariableField El modelo UBlockly.FieldVariable.
      * @param newVariableName El *nombre* de la nueva variable seleccionada.
      */
    public void HandleFieldVariableSelection(FieldVariableModel ublocklyVariableField, string newVariableName)
    {
        if (ublocklyVariableField == null) return;
        if (m_WorkspaceController == null || m_WorkspaceController.IsReadOnly() || (ublocklyVariableField.SourceBlock != null && !ublocklyVariableField.SourceBlock.Editable))
        {
            RevertFieldView(ublocklyVariableField);
            return;
        }

          m_WorkspaceController.RequestFieldVariableChange(ublocklyVariableField, newVariableName);
    }

    /**
     * Busca la vista UBlockly.UGUI.FieldView correspondiente a un modelo UBlockly.Field
     * y le pide que actualice su display desde el valor actual del modelo.
     */
    private void RevertFieldView(FieldModel Field)
    {
        if (Field == null) return;

        WorkSpaceView WorkSpaceView = WorkSpaceView.Active;
        if (WorkSpaceView == null) { Debug.LogError("RevertFieldView: Cannot find UBlockly WorkspaceView!"); return; }

        BlockView parentBlockView = WorkSpaceView.GetBlockView(Field.SourceBlock);
        if (parentBlockView == null) { Debug.LogWarning($"RevertFieldView: Could not find BlockView for parent block {Field.SourceBlock?.ID}"); return; }

        FieldView fieldViewToRevert = null;
        foreach (var inputView in parentBlockView.GetComponentsInChildren<InputView>(true))
        {
            foreach (var fieldView in inputView.GetComponentsInChildren<FieldView>(true))
            {
                if (fieldView.FieldModel == Field)
                {
                    fieldViewToRevert = fieldView;
                    break; 
                }
            }
            if (fieldViewToRevert != null) break;
        }


        if (fieldViewToRevert != null)
        {
            Debug.Log($"InputController: Reverting FieldView for '{Field.Name ?? "unnamed"}' to model value '{Field.GetValue()}'.");
                     fieldViewToRevert.ForceUpdateDisplayFromModel(); 
        }
        else
        {
            Debug.LogWarning($"RevertFieldView: Could not find the corresponding FieldView UI component for field '{Field.Name ?? "unnamed"}' on block {Field.SourceBlock?.ID}");
        }
    }
} //Fin inputController


