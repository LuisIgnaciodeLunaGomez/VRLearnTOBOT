/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonView : MonoBehaviour
{
    [SerializeField] private Image m_Background;
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private Button m_Button;

    private string m_CategoryName;
    private Action<string> m_OnClickCallback;

    public void Setup(string categoryName, Color color, Action<string> onClickCallback)
    {
        m_CategoryName = categoryName;
        m_OnClickCallback = onClickCallback;

        if (m_NameText != null) m_NameText.text = categoryName;
        if (m_Background != null) m_Background.color = color;

        m_Button.onClick.RemoveAllListeners();
        m_Button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        Debug.Log($"Category Button Clicked: {m_CategoryName}");
        m_OnClickCallback?.Invoke(m_CategoryName); 
    }
    void OnDestroy()
    {
        m_Button?.onClick.RemoveAllListeners(); 
    }
}//Fin Clase CategoryButtonView