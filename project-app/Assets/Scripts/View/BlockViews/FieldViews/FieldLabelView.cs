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
public class FieldLabelView : FieldView
{
    private TextMeshProUGUI m_TextMeshPro;
    //private LayoutElement m_LayoutElement;

    public override void InitComponents()
    {
        base.InitComponents(); // Llama a la base si es necesario.
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();

        if (m_TextMeshPro == null)
        {
            Debug.LogError("FieldLabelView requiere un componente TextMeshProUGUI.");
        }
        else
        {
            // Configuraciones visuales por defecto
            m_TextMeshPro.fontSize = BlockViewSettings.Instance.DefaultFontSize;
            m_TextMeshPro.alignment = TextAlignmentOptions.Left;
            m_TextMeshPro.enableWordWrapping = false;
        }
    }

    public override void BindModel(FieldModel model)
    {
        base.BindModel(model); // Guarda el modelo en FieldView y añade el observador
        if (m_FieldModel != null)
        {
            // Asigna el texto inicial al bindeo
            OnValueChanged(m_FieldModel.GetValue());
        }
    }



    // Calcular tamaño basado en el texto
    protected override Vector2 CalculateSize()
    {
        if (m_TextMeshPro == null) return Vector2.zero;

        // Usa el texto actual para medir.
        Vector2 preferredSize = m_TextMeshPro.GetPreferredValues(m_TextMeshPro.text);

        // Sumamos el padding.
        preferredSize.x += BlockViewSettings.Instance.FieldInputTextPadding.horizontal;
        preferredSize.y += BlockViewSettings.Instance.FieldInputTextPadding.vertical;

        // Aseguramos tamaño mínimo.
        preferredSize.x = Mathf.Max(preferredSize.x, BlockViewSettings.Instance.MinUnitWidth);
        preferredSize.y = Mathf.Max(preferredSize.y, BlockViewSettings.Instance.MinUnitHeight);


      //  Debug.Log($"Field '{m_FieldModel.Name}': Texto '{m_TextMeshPro.text}', Ancho Calculado: {width}");
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

    // Este es el único método llamado por la cascada de layout manual.
    public override void UpdateLayout(Vector2 startPos)
    {
        this.XY = startPos;
        this.Size = CalculateSize();
    }

    /// Método público para establecer directamente el texto mostrado por esta vista.
    /// Útil para casos donde no hay un modelo completo (ej. placeholders, errores).

    public void SetDisplayText(string displayText)
    {
       
        OnValueChanged(displayText);
    }
    protected override void RegisterInputListeners()
    {
        // No hacer nada ya que no tiene listeners.
    }

    public override Vector2 CalculateFieldSize()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FieldLabelView