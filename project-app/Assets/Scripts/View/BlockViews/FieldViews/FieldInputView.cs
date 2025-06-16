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

public class FieldInputView : FieldView 
{
    [Header("UI References")]
    [SerializeField] protected TMP_InputField m_InputField; 
    protected BlockView m_ParentBlockView;
    public TMP_InputField InputField => m_InputField;

    protected FieldTextInputModel FieldInputModel => FieldModel as FieldTextInputModel; 

    protected override void Awake()
    {
        base.Awake(); 
        if (m_InputField == null)
        {
            m_InputField = GetComponentInChildren<TMP_InputField>(); 
            if (m_InputField == null)
                Debug.LogError($"FieldInputView ({gameObject.name}): TMP_InputField component not found or assigned!", this);
        }
    }

    protected  void OnBindModel()
    {
        if (m_InputField == null) return; 

        if (FieldInputModel == null)
        {
            Debug.LogError($"FieldInputView ({gameObject.name}): Model is not a FieldTextInputModel! (Type: {FieldModel?.GetType()})", this);
            m_InputField.text = "[MODEL ERROR]";
            m_InputField.interactable = false;
            return;
        }

        m_InputField.text = FieldInputModel.GetValue();
        m_InputField.interactable = FieldModel.IsEditable; 

        m_InputField.onValueChanged.RemoveListener(OnUiValueChanged); 
        m_InputField.onValueChanged.AddListener(OnUiValueChanged);

        m_InputField.onSubmit.RemoveListener(OnUiSubmit);
        m_InputField.onSubmit.AddListener(OnUiSubmit);

        // FieldInputModel.AddObserver(OnModelValueChanged);
    }

    protected  void OnUnBindModel() 
    {
        if (m_InputField != null)
        {
            m_InputField.onValueChanged.RemoveListener(OnUiValueChanged);
            m_InputField.onSubmit.RemoveListener(OnUiSubmit);
        }
        // FieldInputModel?.RemoveObserver(OnModelValueChanged);
    }

    protected virtual void OnUiValueChanged(string newValue)
    {
        if (FieldInputModel != null && FieldInputModel.GetValue() != newValue)
        {
            FieldInputModel.SetValue(newValue); 

            m_ParentBlockView?.QueueForceLayoutUpdate();
        }
    }

    // Llamado cuando el usuario presiona Enter o deselecciona el campo
    protected virtual void OnUiSubmit(string finalValue)
    {
          OnUiValueChanged(finalValue);
       }

    /*
    protected virtual void OnModelValueChanged()
    {
        if (m_InputField != null && FieldInputModel != null && m_InputField.text != FieldInputModel.GetValue())
        {
            m_InputField.text = FieldInputModel.GetValue(); // Actualizar la UI desde el modelo
        }
    }
    */

    protected override Vector2 CalculateSize()
    {
        // Hacemos comprobación de seguridad
        if (BlockViewSettings.Instance == null) return new Vector2(50, 30); // Fallback

        // Si el InputField no existe, usamos los mínimos definidos
        if (m_InputField == null || m_InputField.textComponent == null)
        {
            return new Vector2(BlockViewSettings.Instance.MinUnitWidth, BlockViewSettings.Instance.MinUnitHeight);
        }

        // Medimos el texto que tiene para saber el tamaño preferido
        string currentText = m_InputField.text ?? "";
        Vector2 preferredSize = m_InputField.textComponent.GetPreferredValues(currentText);

        // Añadimos el padding de los campos de texto
        preferredSize.x += BlockViewSettings.Instance.FieldInputTextPadding.horizontal;
        preferredSize.y += BlockViewSettings.Instance.FieldInputTextPadding.vertical;

        // El tamaño final será el mayor entre el del texto con padding, o el tamaño por defecto
        float finalWidth = Mathf.Max(preferredSize.x, BlockViewSettings.Instance.DefaultInputFieldWidth);
        float finalHeight = Mathf.Max(preferredSize.y, BlockViewSettings.Instance.DefaultInputFieldHeight);

        return new Vector2(finalWidth, finalHeight);
    }

    public void CloseKeyboard()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (m_InputField != null && m_InputField.touchScreenKeyboard != null)
        {
            m_InputField.touchScreenKeyboard.active = false;
        }
#endif
    }

    protected override void OnValueChanged(string newValue)
    {
       // throw new System.NotImplementedException();
    }

   /* protected override void RegisterInputListeners()
    {
        throw new System.NotImplementedException();
    }*/

    public void SetDisplayText(string text)
    {
        if (m_InputField == null)
        {
            m_InputField = GetComponentInChildren<TMP_InputField>();
            if (m_InputField == null)
            {
                Debug.LogError($"SetDisplayText ({gameObject.name}): TMP_InputField component not found or assigned!", this);
                return; // Salir si no se encuentra
            }
        }
        m_InputField.text = text;
        
        // m_InputField.ForceLabelUpdate(); 
        // QueueForceLayoutUpdate(); 
    }

    protected override void RegisterInputListeners()
    {
       // throw new System.NotImplementedException();
    }

    public override void UpdateLayout(Vector2 startPos)
    {
        // 1. Me posiciono donde me indica mi padre (el InputView).
        this.XY = startPos;

        // 2. Calculo mi propio tamaño basado en mi contenido.
        this.Size = CalculateSize();
    }

    public override Vector2 CalculateFieldSize()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FielInputView