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

using UnityEngine.Diagnostics;


public class FieldTextInputModel : FieldModel
{
    /*[FieldCreator(FieldType = "field_input")]
    private static FieldTextInputModel CreateFromJson(JObject json)
    {
        string fieldName = json["name"].IsString() ? json["name"].ToString() : "FIELDNAME_DEFAULT";
        var text = json["text"].IsString() ? Utils.ReplaceMessageReferences(json["text"].ToString()) : "";
        return new FieldTextInputModel(fieldName, text);
    }

    /// <summary>
    /// Empty constructor for inheritance use
    /// </summary>
    protected FieldTextInputModel(string fieldName) : base(fieldName) { }

    /// <summary>
    /// Class for an editable text field.
    /// </summary>
    /// <param name="fieldName">The unique name of the field, usually defined in json block.</param>
    /// <param name="text">The default text in the field</param>
    public FieldTextInputModel(string fieldName, string text) : base(fieldName)
    {
        this.SetValue(text);
    }

    public override void SetValue(string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            return;

        if (SourceBlock != null)
        {
            string validated = CallValidator(newValue);
            if (validated != null)
                newValue = validated;
        }

        base.SetValue(newValue);
    }*/
    public FieldTextInputModel(string fieldName) : base(fieldName)
    {
    }
} //Fin clase FieldTextInputModel
