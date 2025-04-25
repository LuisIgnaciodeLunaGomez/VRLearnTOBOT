/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using Newtonsoft.Json.Linq;
using System;
using UnityEngine; 

public class FieldButtonModel : FieldModel
{
  
    public string CallbackKey { get; protected set; }
    
    public FieldButtonModel(string fieldName, string buttonText = "Button", string callbackKey = null)
        : base(fieldName) 
    {
      
        this.mText = buttonText;
        this.CallbackKey = callbackKey;
    }


    [FieldCreator(FieldType = "field_button")]
    protected static FieldButtonModel CreateFromJson(JObject jObj)
    {
        string fieldName = jObj.JsonTryGetString("name");
        string buttonText = jObj.JsonTryGetString("text", "Click Me"); 
        string callbackKey = jObj.JsonTryGetString("callbackKey");     

        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogWarning($"Creating FieldButtonModel without a 'name' specified in JSON. Assigning a default or expecting logic based on callbackKey/text.");
            
        }

        return new FieldButtonModel(fieldName, buttonText, callbackKey);
    }

    public override bool IsEditable => false;
        
    public override string GetText()
    {
        return this.mText; 
    }

    public override void SetText(string newText)
    {
        if (this.mText != newText)
        {
            base.SetText(newText); 
        }
    }

    public override string GetValue()
    {
        return this.mText;
    }

    public override void SetValue(string newValue)
    {
        Debug.LogWarning($"SetValue called on FieldButtonModel '{Name}'. This is often ignored. Use SetText() to change the label if needed.");
    }


    public void InvokeClickAction()
    {
        Debug.Log($"FieldButtonModel '{Name}' InvokeClickAction called. CallbackKey: '{CallbackKey}'. Notifying SourceBlock (if attached)...");
       
    }
}//fin clase FieldBuutonModel