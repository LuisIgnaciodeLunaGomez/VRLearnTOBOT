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

public class FieldButtonView : FieldView 
{
    [Header("UI References")]
    [SerializeField] protected Button m_Button; 
    [SerializeField] protected TextMeshProUGUI m_Label; 

    protected FieldModel FieldButtonModel => FieldModel;

    protected override void Awake() 
    {
        base.Awake();
        if (m_Button == null)
        {
            m_Button = GetComponentInChildren<Button>();
            if (m_Button == null) Debug.LogError($"FieldButtonView ({gameObject.name}): Button component not found!", this);
        }
        if (m_Label == null)
        {
            m_Label = GetComponentInChildren<TextMeshProUGUI>();
            if (m_Label == null)
            {
                var textComp = GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    Debug.LogWarning($"FieldButtonView ({gameObject.name}): Found legacy Text component, but TextMeshProUGUI is preferred.", this);
                }
                else
                {
                    Debug.LogWarning($"FieldButtonView ({gameObject.name}): Text component (TMP or legacy) not found.", this);
                }
            }

        }
    }

    protected  void OnBindModel() 
    {
        if (m_Button == null || FieldButtonModel == null) return;

        if (m_Label != null)
        {
            m_Label.text = FieldButtonModel.GetValue();
        }

            m_Button.interactable = FieldModel.IsEditable; 

        m_Button.onClick.RemoveListener(OnButtonClick);
        m_Button.onClick.AddListener(OnButtonClick);

        // TODO: Observar cambios en el MODELO si el texto o estado puede cambiar dinámicamente
    }

    protected void OnUnBindModel() 
    {
        if (m_Button != null)
        {
            m_Button.onClick.RemoveListener(OnButtonClick);
        }
        // TODO: Desregistrar observador del modelo si se usó
    }

   
    protected virtual void OnButtonClick()
    {
        if (FieldButtonModel == null) return;

        Debug.Log($"FieldButton clicked: {FieldButtonModel.Name} on block {SourceBlock?.Type}");

           BlockView parentBlockView = SourceBlockView; 

       
        /* if (FieldButtonModel.Name == "add_item_button") {
             SourceBlock?.GetComponent<MySpecificBlockLogic>()?.AddItem();
        } */
    }

    protected override Vector2 CalculateSize()
    {
        if (m_Button == null)
        {
            // Si el botón no existe, no podemos calcular nada. Devolvemos el tamaño mínimo.
            if (BlockViewSettings.Instance != null)
            {
                return new Vector2(BlockViewSettings.Instance.MinUnitWidth, BlockViewSettings.Instance.MinUnitHeight);
            }
            return new Vector2(30, 24); // Fallback
        }

        // Usamos los tamaños mínimos como base.
        float preferredWidth = BlockViewSettings.Instance.MinUnitWidth;
        float preferredHeight = BlockViewSettings.Instance.MinUnitHeight;

        // Si el botón tiene una etiqueta de texto, calculamos su tamaño preferido.
        if (m_Label != null && !string.IsNullOrEmpty(m_Label.text))
        {
            // Forzamos al sistema de UI a recalcular el tamaño del texto para obtener una medida fiable.
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Label.rectTransform);
            preferredWidth = m_Label.GetPreferredValues().x;
            preferredHeight = m_Label.GetPreferredValues().y;

            // Añadimos un padding visual para que el botón no quede pegado al texto.
            // Usaremos el padding genérico de los campos de texto como referencia.
            preferredWidth += BlockViewSettings.Instance.FieldInputTextPadding.horizontal;
            preferredHeight += BlockViewSettings.Instance.FieldInputTextPadding.vertical;
        }

        // El tamaño final del botón será el mayor entre el tamaño de su texto y el mínimo permitido.
        float finalWidth = Mathf.Max(preferredWidth, BlockViewSettings.Instance.MinUnitWidth);
        float finalHeight = Mathf.Max(preferredHeight, BlockViewSettings.Instance.MinUnitHeight);

        return new Vector2(finalWidth, finalHeight);
    }

    protected override void OnValueChanged(string newValue)
    {
        throw new System.NotImplementedException();
    }

    protected override void RegisterInputListeners()
    {
        throw new System.NotImplementedException();
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
}//Fin clase FieldButtonView