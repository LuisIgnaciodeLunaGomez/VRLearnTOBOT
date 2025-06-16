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
//[RequireComponent(typeof(LayoutElement))] 
public class FieldLabelView : FieldView
{
    private TextMeshProUGUI m_TextMeshPro;
   private LayoutElement m_LayoutElement;


    protected override void InitializeView()
    {
        base.InitializeView();
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        m_LayoutElement = GetComponent<LayoutElement>(); 

        if (m_LayoutElement != null)
        {
            m_LayoutElement.ignoreLayout = false;
        }
        if (m_TextMeshPro == null) Debug.LogError("FieldLabelView requires a TextMeshProUGUI component.");

        m_TextMeshPro.richText = false;
        m_TextMeshPro.overflowMode = TextOverflowModes.Overflow; 
        m_TextMeshPro.alignment = TextAlignmentOptions.Left; 
        //m_TextMeshPro.enableWordWrapping = false; 
        //m_TextMeshPro.fontSize = BlockViewSettings.Instance.DefaultFontSize;
        //m_TextMeshPro.color = BlockViewSettings.Instance.DefaultFieldColor;
        // m_LayoutElement.flexibleWidth = 0; 
        // m_LayoutElement.flexibleHeight = 0;
    }

    // Calcular tamaño basado en el texto
    protected override Vector2 CalculateSize()
    {
        // Hacemos comprobación de seguridad para los Settings
        if (BlockViewSettings.Instance == null) return new Vector2(50, 24); // Fallback

        // Si no hay texto o modelo, devolvemos un tamaño mínimo basado en las nuevas propiedades.
        if (m_TextMeshPro == null || m_FieldModel == null)
        {
            return new Vector2(BlockViewSettings.Instance.MinUnitWidth, BlockViewSettings.Instance.MinUnitHeight);
        }

        string textToShow = m_FieldModel.GetValue() ?? "";
        m_TextMeshPro.text = textToShow;
        Vector2 preferredSize = m_TextMeshPro.GetPreferredValues(textToShow);

        // Sumamos el padding de los campos para darles "aire" alrededor
        preferredSize.x += BlockViewSettings.Instance.FieldInputTextPadding.horizontal;
        preferredSize.y += BlockViewSettings.Instance.FieldInputTextPadding.vertical;

        // Nos aseguramos de que, incluso con padding, nunca sea más pequeño que el mínimo absoluto.
        preferredSize.x = Mathf.Max(preferredSize.x, BlockViewSettings.Instance.MinUnitWidth);
        preferredSize.y = Mathf.Max(preferredSize.y, BlockViewSettings.Instance.MinUnitHeight);

        // Si existe un LayoutElement, lo actualizamos. (Opcional)
        if (m_LayoutElement != null)
        {
            m_LayoutElement.preferredWidth = preferredSize.x;
            m_LayoutElement.preferredHeight = preferredSize.y;
        }

        // El debug que ya tenías está perfecto.
        Logger.Log($"Frame {Time.frameCount}:   <b>L-- [{GetType().Name}.CalculateSize]</b> en '{gameObject.name}'. Texto: '{m_TextMeshPro.text}'. Tamaño Calculado: {preferredSize.ToString("F2")}", gameObject);

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

                Size = CalculateSize();
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

    public override void UpdateLayout(Vector2 startPos)
    {
        // 1. Me posiciono donde me indica mi padre (el InputView).
        this.XY = startPos;

        // 2. Calculo mi propio tamaño basado en mi contenido (el texto).
        this.Size = CalculateSize();
        // Los 'Fields' no tienen hijos lógicos, así que la recursión se detiene aquí.
    }
}//Fin clase FieldLabelView