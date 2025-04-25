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
        if (m_Button == null) return BlockViewSettings.Get().MinUnitSize;

        float preferredWidth = BlockViewSettings.Get().MinUnitSize.x;
        float preferredHeight = BlockViewSettings.Get().MinUnitSize.y;

        // Calcular tamaño basado en el texto, si existe
        if (m_Label != null && !string.IsNullOrEmpty(m_Label.text))
        {
            // Forzar actualización para obtener el tamaño preferido correcto del texto
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Label.rectTransform);
            preferredWidth = LayoutUtility.GetPreferredWidth(m_Label.rectTransform);
            preferredHeight = LayoutUtility.GetPreferredHeight(m_Label.rectTransform);

            // Añadir padding/márgenes del botón
            preferredWidth += 20;  
            preferredHeight += 10; 
        }

        // Usar el mayor entre el tamaño calculado y el mínimo del Settings
        float finalWidth = Mathf.Max(preferredWidth, BlockViewSettings.Get().MinUnitSize.x);
        float finalHeight = Mathf.Max(preferredHeight, BlockViewSettings.Get().MinUnitSize.y);

        // Considerar el tamaño mínimo del propio componente Button si lo tiene configurado
        var layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            finalWidth = Mathf.Max(finalWidth, layoutElement.minWidth);
            finalHeight = Mathf.Max(finalHeight, layoutElement.minHeight);
        }


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
}//Fin clase FieldButtonView