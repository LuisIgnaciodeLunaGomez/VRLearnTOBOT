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
 * Descripción:  Manejo de los valores dentro de los bloques
 * 
 */

using UnityEngine;
using UnityEngine.UI;

public class InputView : BaseView
{
    [SerializeField] private InputField m_InputField; // Campo de entrada de usuario
    [SerializeField] private bool m_AlignRight = false;
    public override ViewType Type
    {
        get { return ViewType.Input; }
    }

   
    public bool AlignRight
    {
        get { return m_AlignRight; }
        set { m_AlignRight = value; }
    }
    protected override Vector2 CalculateSize()
    {
        if (m_InputField == null)
            return new Vector2(80, 25); // Tamaño predeterminado para inputs

        return new Vector2(m_InputField.preferredWidth + 10, m_InputField.preferredHeight + 5);
    }

    public void BindInput(string placeholderText = "")
    {
        if (m_InputField == null)
        {
            GameObject inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(this.transform, false);
            m_InputField = inputGO.AddComponent<InputField>();

            Text placeholder = new GameObject("Placeholder").AddComponent<Text>();
            placeholder.transform.SetParent(m_InputField.transform, false);
            placeholder.text = placeholderText;
            placeholder.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholder.color = Color.gray;

            m_InputField.placeholder = placeholder;
            m_InputField.textComponent = placeholder;
        }
    }

    public ConnectionInputView GetConnectionView()
    {
        return Childs.Count > 0 ? Childs[Childs.Count - 1] as ConnectionInputView : null;
    }
}