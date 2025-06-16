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
    private bool m_IsUIUpdate = false;     // Bandera para evitar que los eventos de la UI se disparen en un bucle
                                           //private LayoutElement m_LayoutElement;
    private Image m_BackgroundImage;

    public TMP_InputField inputFieldPublic { get { return m_InputField; } } 

   /* protected override void InitializeView()
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

    */
    public override void InitComponents()
    {
        base.InitComponents();
        m_InputField = GetComponent<TMP_InputField>();
        m_BackgroundImage = GetComponent<Image>(); // El fondo del input
    }

    // Vincula el modelo de datos.
    public override void BindModel(FieldModel model)
    {
        base.BindModel(model); // Guarda el modelo y añade observadores

        // Desregistra listeners antiguos para evitar duplicados
        m_InputField.onEndEdit.RemoveAllListeners();
        // Evento cuando el usuario TERMINA de editar

        m_InputField.onEndEdit.AddListener(HandleEndEdit);
        m_InputField?.onValueChanged.RemoveAllListeners(); // Limpiamos también este
        // Evento que se dispara MIENTRAS el usuario escribe
        m_InputField?.onValueChanged.AddListener(HandleValueChangedFromUI);

        // Asigna el valor inicial que viene del modelo
        if (m_FieldModel != null)
        {
            OnValueChanged(m_FieldModel.GetValue());
        }
    }


    protected override Vector2 CalculateSize()
    {
        var settings = BlockViewSettings.Instance;
        if (settings == null || m_InputField?.textComponent == null)
        {
            return new Vector2(50, 30); // Fallback robusto
        }

        // Si el campo de texto está vacío, medimos el placeholder. Si no, medimos el texto actual.
        TMP_Text textComponentToShow = m_InputField.textComponent;
        string textToMeasure = m_InputField.text;

        if (string.IsNullOrEmpty(textToMeasure) && m_InputField.placeholder is TMP_Text placeholderText)
        {
            textComponentToShow = placeholderText;
            textToMeasure = placeholderText.text;
        }

        // Si después de todo, no hay texto, devolvemos un tamaño mínimo.
        if (string.IsNullOrEmpty(textToMeasure))
        {
            return new Vector2(settings.MinUnitWidth, settings.DefaultInputFieldHeight);
        }

        // Medimos el texto.
        Vector2 preferredTextSize = textComponentToShow.GetPreferredValues(textToMeasure);

        // El tamaño del campo es el del texto + padding.
        float finalWidth = preferredTextSize.x + settings.FieldInputTextPadding.horizontal;

        // Damos un poco de espacio extra para el cursor y para que no se vea pegado.
        finalWidth += 15f;

        // Aplicamos los mínimos
        finalWidth = Mathf.Max(finalWidth, settings.MinUnitWidth);
        float finalHeight = settings.DefaultInputFieldHeight;

        return new Vector2(finalWidth, finalHeight);
    }

    // Se llama cuando el MODELO cambia.
    protected override void OnValueChanged(string newValue)
    {
        // Si el cambio viene de la UI, no hacemos nada para evitar un bucle infinito
        if (m_IsUIUpdate) return;

        if (m_InputField != null)
        {
            // Simplemente actualizamos el texto visual
            m_InputField.text = newValue ?? "";
        }
    }


    // Registra el listener para cuando el usuario TERMINA de editar
    protected override void RegisterInputListeners()
    {
        /*if (m_InputField != null)
        {
            m_InputField.onSelect.AddListener(HandleSelect);
            m_InputField.onEndEdit.AddListener(HandleEndEdit); 
            m_InputField.onDeselect.AddListener(HandleDeselect);
        }*/
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

    /*private void OnSelectInput(string currentVal)
    {
        m_isUserInput = true; 
    }*/
    /*private void OnDeselectInput(string finalVal)
    {
        m_isUserInput = false; 
        // HandleInputFieldEndEdit(finalVal);
    }*/

  /*  private void HandleSelect(string currentText)
    {
        m_IsUIUpdate = true; 
        //BlockDragController.Instance?.SetBlockInteraction(SourceBlockView, false);
    }*/

    private void HandleEndEdit(string finalValue)
    {
         m_IsUIUpdate = true;          // Le decimos al sistema que este cambio viene del usuario


        // Comprobar si el valor realmente cambió respecto al modelo antes de pedir update
        if (m_FieldModel != null && m_FieldModel.GetValue() != finalValue)
        {
            RequestModelUpdate(finalValue); // Pedimos al modelo que se actualice
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
           // m_InputField.onSelect.RemoveListener(OnSelectInput);
           // m_InputField.onDeselect.RemoveListener(OnDeselectInput);
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

    // Se llama CADA VEZ que el usuario teclea algo en el InputField.
    private void HandleValueChangedFromUI(string newText)
    {
        // Necesito que se recalcule el layout mientras escribimos.
        MarkDirty();
    }

    public override Vector2 CalculateFieldSize()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FieldTextInputView