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
//[RequireComponent(typeof(LayoutElement))]
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
        // No necesitamos el LayoutElement aquí, lo eliminamos

        if (m_InputField == null)
        {
            Debug.LogError("FieldTextInputView requiere un componente TMP_InputField.", this);
            return; // Salimos para evitar más errores
        }

        var settings = BlockViewSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("BlockViewSettings no están disponibles, no se pueden aplicar estilos al InputField.", this);
            return;
        }

        // Configuración del componente de texto interno
        if (m_InputField.textComponent != null)
        {
            m_InputField.textComponent.fontSize = settings.DefaultFontSize; // Usar nueva propiedad
            m_InputField.textComponent.color = settings.EditableFieldColor;
            m_InputField.textComponent.alignment = TextAlignmentOptions.Center;
        }

        // Configuración del fondo del InputField
        Image bg = m_InputField.GetComponent<Image>();
        if (bg != null) bg.color = settings.InputFieldBackground;
    }

    
    protected override Vector2 CalculateSize()
    {
        var settings = BlockViewSettings.Instance;
        if (settings == null) return new Vector2(50, 30); // Fallback

        Vector2 size = new Vector2(settings.DefaultInputFieldWidth, settings.DefaultInputFieldHeight);

        // Medimos el texto preferido para que el campo se ajuste
        string currentText = m_InputField?.text ?? m_FieldModel?.GetValue() ?? "10";
        Vector2 preferredTextSize = new Vector2(0, 0);
        if (m_InputField?.textComponent != null)
        {
            // Añadimos espacio extra para que no quede demasiado justo
            preferredTextSize = m_InputField.textComponent.GetPreferredValues(currentText + "XX");
        }

        // El tamaño final será el del texto más el padding, pero nunca menos que el mínimo por defecto.
        float finalWidth = preferredTextSize.x + settings.FieldInputTextPadding.horizontal;
        float finalHeight = preferredTextSize.y + settings.FieldInputTextPadding.vertical;

        size.x = Mathf.Max(finalWidth, settings.DefaultInputFieldWidth);
        size.y = Mathf.Max(finalHeight, settings.DefaultInputFieldHeight);

        // El debug que ya tenías está bien
        Debug.Log($"Frame {Time.frameCount}:   <b>L-- [{GetType().Name}.CalculateSize]</b> en '{gameObject.name}'. Texto: '{currentText}'. Tamaño Calculado: {size.ToString("F2")}", gameObject);

        return size;
    }

    // Actualiza el texto del InputField CUANDO EL MODELO CAMBIA
    protected override void OnValueChanged(string newValue)
    {
        if (m_InputField != null && !m_isUserInput) 
        {
            string textToShow = newValue ?? "";

            if (m_InputField.text != textToShow)
            {
                m_InputField.text = textToShow;

                Size = CalculateSize();
            }
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

    public override void UpdateLayout(Vector2 startPos)
    {
        // 1. Me posiciono donde me indica mi padre (el InputView).
        this.XY = startPos;

        // 2. Calculo mi propio tamaño basado en mi contenido.
        this.Size = CalculateSize();
    }
}//Fin clase FieldTextInputView