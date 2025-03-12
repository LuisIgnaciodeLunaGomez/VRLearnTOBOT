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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputView : BaseView
{
    [SerializeField] private TMP_InputField m_InputField; // Campo de entrada de usuario

    public override ViewType Type => ViewType.Input;
    protected override Vector2 CalculateSize()
    {
        if (m_InputField == null) m_InputField = GetComponent<TMP_InputField>();
        Vector2 preferredSize = m_InputField.textComponent.GetPreferredValues();
        return new Vector2(Mathf.Max(preferredSize.x, 50f), preferredSize.y); // Mínimo de 50 para inputs
    }


    public ConnectionInputView GetConnectionView()
    {
        return Childs.Count > 0 ? Childs[Childs.Count - 1] as ConnectionInputView : null;
    }
}