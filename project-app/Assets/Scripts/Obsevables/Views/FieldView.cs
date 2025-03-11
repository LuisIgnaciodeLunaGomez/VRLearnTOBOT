/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 10/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  Manejo de etiquetas y valores de los bloques
 * 
 */

using UnityEngine;
using UnityEngine.UI;

public class FieldView : BaseView
{
    [SerializeField] private Text m_Text; // Componente de texto para mostrar contenido

    public override ViewType Type
    {
        get { return ViewType.Field; }
    }

    public string FieldText
    {
        get { return m_Text != null ? m_Text.text : ""; }
        set { if (m_Text != null) m_Text.text = value; }
    }

    protected override Vector2 CalculateSize()
    {
        if (m_Text == null)
            return new Vector2(100, 20); // Tamaño por defecto

        Vector2 size = new Vector2(m_Text.preferredWidth + 10, m_Text.preferredHeight + 5);
        return size;
    }

    public void BindText(string text)
    {
        if (m_Text == null)
        {
            GameObject textGO = new GameObject("FieldText");
            textGO.transform.SetParent(this.transform, false);
            m_Text = textGO.AddComponent<Text>();
            m_Text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            m_Text.fontSize = 14;
        }

        FieldText = text;
    }
}