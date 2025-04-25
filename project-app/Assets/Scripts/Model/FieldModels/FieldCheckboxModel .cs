/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 03/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using Newtonsoft.Json.Linq;
using System;

public class FieldCheckboxModel : FieldModel 
{
    private bool m_IsChecked;

  
    [FieldCreator(FieldType = "field_checkbox")]
    private static FieldCheckboxModel CreateFromJson(JObject json)
    {
        string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                           ? json["name"].ToString()
                           : "FIELDNAME_DEFAULT";

  
        string initialStateStr = (json["checked"] != null && json["checked"].Type == JTokenType.String) 
                                  ? json["checked"].ToString().ToUpperInvariant() 
                                  : "FALSE"; 

        return new FieldCheckboxModel(fieldName, initialStateStr == "TRUE" ? "TRUE" : "FALSE"); 
    }

 
    public FieldCheckboxModel(string fieldName, string initialState = "FALSE") : base(fieldName) 
    {
        m_IsChecked = string.Equals(initialState, "TRUE", StringComparison.OrdinalIgnoreCase);
        mText = GetValue();
    }

    public bool IsChecked
    {
        get { return m_IsChecked; }
        set
        {
            if (m_IsChecked != value)
            {
                m_IsChecked = value;
                string standardizedValue = GetValue();
                if (mText != standardizedValue)
                {
                    mText = standardizedValue; 
                }

                FireUpdate(standardizedValue); 

                SourceBlock?.OnModelChange(this);
            }
        }
    }

 
    public override string GetValue()
    {
        return m_IsChecked ? "TRUE" : "FALSE";
    }

 
    public override void SetValue(string newValue)
    {
      
        IsChecked = string.Equals(newValue, "TRUE", StringComparison.OrdinalIgnoreCase);
    }


}//fin clase FieldCheckboxModel
