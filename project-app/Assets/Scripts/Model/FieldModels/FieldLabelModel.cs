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
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using Newtonsoft.Json.Linq;
using UnityEngine;


public class FieldLabelModel : FieldModel
{
    [FieldCreator(FieldType = "field_label")]
    private static FieldLabelModel CreateFromJson(JObject json)
    {
        string fieldName = "FIELDNAME_DEFAULT"; 
        JToken nameToken = json?["name"];
       
        if (nameToken != null && nameToken.Type == JTokenType.String)
        {
            fieldName = nameToken.ToString();
        }

        string text = "";
      
        JToken textToken = json?["text"];
        if (textToken != null && textToken.Type == JTokenType.String)
        {
            text = Utilidades.ReplaceMessageReferences(textToken.ToString());
        }

        return new FieldLabelModel(fieldName, text);
    }

    public FieldLabelModel(string fieldName, string text) : base(fieldName)
    {
        this.SetValue(text);
    }
    public FieldLabelModel(string fieldName) : base(fieldName)
    {
    }


}//Fin clase FieldLabelModel