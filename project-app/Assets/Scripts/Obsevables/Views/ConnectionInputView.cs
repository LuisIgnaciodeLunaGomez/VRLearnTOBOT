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

using UnityEngine;

public class ConnectionInputView : BaseView
{
    [SerializeField] private ConnectionInputViewType m_ConnectionInputViewType;
    //[SerializeField] private Image m_BgImage;
    [SerializeField] private Vector2 m_ImageMeshOffset;
    public override ViewType Type
    {
        get { return ViewType.ConnectionInput; }
    }

    public bool IsSlot
    {
        get { return m_ConnectionInputViewType == ConnectionInputViewType.ValueSlot; }
    }


    public  ConnectionInputViewType ConnectionIViewType
    {
        get { return m_ConnectionInputViewType; }
        set { m_ConnectionInputViewType = value; }
    }

    protected override Vector2 CalculateSize()
    {
        throw new System.NotImplementedException();
    }
}