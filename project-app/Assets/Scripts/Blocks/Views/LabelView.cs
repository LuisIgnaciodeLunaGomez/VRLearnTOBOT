/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 * 
 */

using TMPro;
using UnityEngine;

public class LabelView : BaseView
{
    [SerializeField] private TextMeshProUGUI m_TextComponent;
    public override ViewType Type => ViewType.Field;
    protected override Vector2 CalculateSize()
    {
        if (m_TextComponent == null) m_TextComponent = GetComponent<TextMeshProUGUI>();
        Vector2 preferredSize = m_TextComponent.GetPreferredValues();
        return new Vector2(preferredSize.x, preferredSize.y);
    }
}
