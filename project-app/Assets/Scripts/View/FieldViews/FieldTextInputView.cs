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
 * Versión: 1.0.1
 * 
 * Descripción: 
 */

using UnityEngine;
using TMPro;
using UnityEngine.UI; 

[RequireComponent(typeof(TMP_InputField))]
[RequireComponent(typeof(LayoutElement))]
public class FieldTextInputView : FieldView
{
    private TMP_InputField m_InputField;
    private bool m_isUserInput = false; 
    private bool m_IsUIUpdate = false; 
    private LayoutElement m_LayoutElement;

    public TMP_InputField inputFieldPublic { get { return m_InputField; } }
    protected override void InitializeView()
    {
        base.InitializeView();
        m_InputField = GetComponent<TMP_InputField>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_LayoutElement.ignoreLayout = false;

        if (m_InputField == null) Debug.LogError("FieldTextInputView requires a TMP_InputField component.");

        m_InputField.contentType = TMP_InputField.ContentType.Standard; // Default
        
        if (m_InputField.textComponent != null)
        {
            m_InputField.textComponent.fontSize = BlockViewSettings.Instance.DefaultFontSize;
            m_InputField.textComponent.color = BlockViewSettings.Instance.EditableFieldColor; 
            m_InputField.textComponent.alignment = TextAlignmentOptions.Center; // Centrado en inputs
        }
        else
        {
            Debug.LogError($"InputField {gameObject.name} has no TextComponent assigned!");
        }
        Image bg = m_InputField.GetComponent<Image>(); 
        if (bg != null) bg.color = BlockViewSettings.Instance.InputFieldBackground;
    }

    
    protected override Vector2 CalculateSize()
    {
        //if (m_InputField == null || m_FieldModel == null) return BlockViewSettings.Get().MinUnitSize * 2; 

        
       // if (bg != null) bg.color = BlockViewSettings.Instance.InputFieldBackground;
        Vector2 size = new Vector2(BlockViewSettings.Instance.DefaultInputFieldWidth, BlockViewSettings.Instance.DefaultInputFieldHeight);

        string currentText = m_InputField?.text ?? m_FieldModel?.GetValue() ?? "";
        Vector2 preferredSize = BlockViewSettings.Instance.MinUnitSize * 2; 
        if (m_InputField?.textComponent != null)
            preferredSize = m_InputField.textComponent.GetPreferredValues(currentText + "XX"); 

        preferredSize.x += BlockViewSettings.Instance.FieldHorizontalPadding * 4; 
        preferredSize.y += BlockViewSettings.Instance.FieldVerticalPadding * 2;

        size.x = Mathf.Max(preferredSize.x, BlockViewSettings.Instance.DefaultInputFieldWidth);
        size.y = Mathf.Max(preferredSize.y, BlockViewSettings.Instance.DefaultInputFieldHeight);


        m_LayoutElement.preferredWidth = size.x;
        m_LayoutElement.preferredHeight = size.y;
        return size;
    }

    // Actualiza el texto del InputField CUANDO EL MODELO CAMBIA
    protected override void OnValueChanged(string newValue)
    {
        if (m_InputField != null && !m_isUserInput) 
        {
            m_InputField.text = newValue ?? "";
            
            // Vector2 newSize = CalculateSize();
            // Size = newSize;
            // ParentView?.UpdateLayout();
        }
    }

    // Registra el listener para cuando el usuario TERMINA de editar
    protected override void RegisterInputListeners()
    {
        if (m_InputField != null)
        {
            m_InputField.onSelect.AddListener(HandleSelect);
            m_InputField.onEndEdit.AddListener(HandleEndEdit); 
            m_InputField.onDeselect.AddListener(HandleDeselect);
        }
    }

    // Listener para onEndEdit
    private void HandleInputFieldEndEdit(string finalValue)
    {
        m_isUserInput = false; 
        RequestModelUpdate(finalValue);
    }

    
    // private void HandleInputFieldValueChanged(string currentValue)
    // {
    //     m_isUserInput = true; // Marca que el cambio viene de la UI
    //     RequestModelUpdate(currentValue);
    //     // NO llames a LayoutRebuilder aquí, espera la respuesta del modelo
    //     m_isUserInput = false; // Resetear (podría ser problemático si hay latencia)
    // }

    private void OnSelectInput(string currentVal)
    {
        m_isUserInput = true; 
    }
    private void OnDeselectInput(string finalVal)
    {
        m_isUserInput = false; 
        // HandleInputFieldEndEdit(finalVal);
    }

    private void HandleSelect(string currentText)
    {
        m_IsUIUpdate = true; 
        //BlockDragController.Instance?.SetBlockInteraction(SourceBlockView, false);
    }

    private void HandleEndEdit(string finalValue)
    {
        // m_IsUIUpdate = false; 

        // Comprobar si el valor realmente cambió respecto al modelo antes de pedir update
        if (m_FieldModel != null && m_FieldModel.GetValue() != finalValue)
        {
            RequestModelUpdate(finalValue); // Llama al InputController
        }
        m_IsUIUpdate = false; // Resetear flag después de procesar
    }

    private void HandleDeselect(string finalValue)
    {
       
        if (m_IsUIUpdate) 
        {
          
            // HandleEndEdit(finalValue); 
            m_IsUIUpdate = false; // Resetear aquí
        }
        // BlockDragController.Instance?.SetBlockInteraction(SourceBlockView, true);
    }
    public new void OnDestroy()
    {
        if (m_InputField != null)
        {
            m_InputField.onEndEdit.RemoveListener(HandleInputFieldEndEdit);
            // m_InputField.onValueChanged.RemoveListener(HandleInputFieldValueChanged);
            m_InputField.onSelect.RemoveListener(OnSelectInput);
            m_InputField.onDeselect.RemoveListener(OnDeselectInput);
        }
        base.OnDestroy();
    }

}//Fin clase FieldTextInputView