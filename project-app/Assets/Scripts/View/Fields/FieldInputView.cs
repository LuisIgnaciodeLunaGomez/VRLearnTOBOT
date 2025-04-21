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
        if (m_InputField == null || m_InputField.textComponent == null)
            return BlockViewSettings.Get().MinUnitSize; 

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_InputField.textComponent.rectTransform);

        float preferredWidth = LayoutUtility.GetPreferredWidth(m_InputField.textComponent.rectTransform);
        float preferredHeight = LayoutUtility.GetPreferredHeight(m_InputField.textComponent.rectTransform);

        preferredWidth += 10; 
        preferredHeight += 5; 

        float finalWidth = Mathf.Max(preferredWidth, BlockViewSettings.Get().MinUnitSize.x);
        float finalHeight = Mathf.Max(preferredHeight, BlockViewSettings.Get().MinUnitSize.y);

       
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
}//Fin clase FielInputView