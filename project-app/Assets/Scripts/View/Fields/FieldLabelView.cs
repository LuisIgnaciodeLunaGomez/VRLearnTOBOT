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
 * Versión: 1.0.2
 * 
 * Descripción: 
 */

using UnityEngine;
using TMPro; 
using UnityEngine.UI; 

[RequireComponent(typeof(TextMeshProUGUI))] 
[RequireComponent(typeof(LayoutElement))] 
public class FieldLabelView : FieldView
{
    private TextMeshProUGUI m_TextMeshPro;
    private LayoutElement m_LayoutElement;


    protected override void InitializeView()
    {
        base.InitializeView();
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_LayoutElement.ignoreLayout = false; 

        if (m_TextMeshPro == null) Debug.LogError("FieldLabelView requires a TextMeshProUGUI component.");

        m_TextMeshPro.richText = false;
        m_TextMeshPro.overflowMode = TextOverflowModes.Overflow; 
        m_TextMeshPro.alignment = TextAlignmentOptions.Left; 
        //m_TextMeshPro.enableWordWrapping = false; 
        m_TextMeshPro.fontSize = BlockViewSettings.Instance.DefaultFontSize;
        m_TextMeshPro.color = BlockViewSettings.Instance.DefaultFieldColor;
        // m_LayoutElement.flexibleWidth = 0; 
        // m_LayoutElement.flexibleHeight = 0;
    }

    // Calcular tamaño basado en el texto
    protected override Vector2 CalculateSize()
    {
        if (m_TextMeshPro == null || m_FieldModel == null) return BlockViewSettings.Instance.MinUnitSize;

        string textToShow = m_FieldModel.GetValue() ?? "";
        m_TextMeshPro.text = textToShow;
        Vector2 preferredSize = m_TextMeshPro.GetPreferredValues(textToShow);

        preferredSize.x += BlockViewSettings.Instance.FieldHorizontalPadding * 2;
        preferredSize.y += BlockViewSettings.Instance.FieldVerticalPadding * 2;

        preferredSize.x = Mathf.Max(preferredSize.x, BlockViewSettings.Instance.MinUnitWidth);
        preferredSize.y = Mathf.Max(preferredSize.y, BlockViewSettings.Instance.MinUnitHeight);

          m_LayoutElement.preferredWidth = preferredSize.x;
        m_LayoutElement.preferredHeight = preferredSize.y;


        return preferredSize; 
    }

    // Actualiza el texto del componente TMP
    protected override void OnValueChanged(string newValue)
    {
        if (m_TextMeshPro != null)
        {
            string textToShow = newValue ?? "";
            if (m_TextMeshPro.text != textToShow)
            {
                m_TextMeshPro.text = textToShow;
                 if (gameObject.activeInHierarchy)
                {
                    Vector2 newSize = CalculateSize();
                    Size = newSize; 
                    ParentView?.UpdateLayout(Vector2.zero); 
                }
            }
        }
    }

  
    /// Método público para establecer directamente el texto mostrado por esta vista.
    /// Útil para casos donde no hay un modelo completo (ej. placeholders, errores).
   
    public void SetDisplayText(string displayText)
    {
       
        OnValueChanged(displayText);
    }
    protected override void RegisterInputListeners()
    {
        // No hacer nada
    }
}//Fin clase FieldLabelView