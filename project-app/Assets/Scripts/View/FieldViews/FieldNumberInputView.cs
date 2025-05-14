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

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FieldNumberInputView : FieldTextInputView 
{
    protected override void InitializeView()
    {
        base.InitializeView(); 
        if (inputFieldPublic != null)
        {
            inputFieldPublic.contentType = TMP_InputField.ContentType.DecimalNumber; 
        }
    }

    protected override Vector2 CalculateSize() { return base.CalculateSize(); }

    protected override void OnValueChanged(string newValue) { base.OnValueChanged(newValue); }

    protected override void RegisterInputListeners() { base.RegisterInputListeners(); }
}//Fin Clase FieldNumberInputView