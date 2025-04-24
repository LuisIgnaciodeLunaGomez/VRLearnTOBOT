/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 04/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;


public sealed class FieldColour : FieldModel
{
    [FieldCreator(FieldType = "field_colour")]
    private static FieldColour CreateFromJson(JObject json)
    {
        // --- CORRECCIÓN AQUÍ ---
        string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                               ? json["name"].ToString()
                               : "FIELDNAME_DEFAULT"; // Nombre por defecto

        // Añadir una comprobación para "colour" también es buena idea
        string colourValue = (json["colour"] != null && json["colour"].Type == JTokenType.String)
                                ? json["colour"].ToString()
                                : "#FFFFFF"; // Color por defecto (Blanco) si falta o no es string

        FieldColour field = new FieldColour(fieldName, colourValue);

        // La lógica para "options" ya está bastante bien,
        // comprueba si no es null y lo trata como JArray
        if (json["options"] != null)
        {
            // Usar 'as' es seguro, será null si no es JArray
            JArray options = json["options"] as JArray;
            if (options != null) // Solo proceder si la conversión a JArray fue exitosa
            {
                // Crear la lista o array para las opciones de color
                List<string> colorOptionsList = new List<string>(options.Count);

                for (int i = 0; i < options.Count; i++)
                {
                    // Validar que cada elemento sea también un string antes de añadirlo
                    if (options[i] != null && options[i].Type == JTokenType.String)
                    {
                        colorOptionsList.Add((string)options[i]);
                    }
                    else
                    {
                        // Opcional: Loguear un aviso si una opción no es un string
                        Debug.LogWarning($"FieldColour.CreateFromJson: Option at index {i} for field '{fieldName}' is not a string. Skipping.");
                    }
                }
                // Asignar al campo miembro correspondiente (ajusta si mColorOptions es List<string>)
                field.mColorOptions = colorOptionsList.ToArray();
                // Si mColorOptions fuera List<string>: field.mColorOptions = colorOptionsList;
            }
            else
            {
                Debug.LogWarning($"FieldColour.CreateFromJson: 'options' field for '{fieldName}' exists but is not a JSON array.");
            }
        }
        // Si mColorOptions necesita inicializarse incluso si no hay opciones JSON:
        // else { field.mColorOptions = new string[0]; } // O = new List<string>();

        return field;
    }

    private string mColor;
    private string[] mColorOptions;

    private static string[] DEFAULT_COLOR_OPTIONS =
    {
            "#FFFFFF", "#000000", "#FF0000", "#00FF00", "#0000FF",
            "#FFEB04", "#00FFFF", "#FF00FF", "#808080", "#FF851B",
            
            //http://clrs.cc/
            "#7FDBFF", "#39CCCC", /*"#001F3F", "#85144B", "#B10DC9",*/ 
        };

    /// <summary>
    /// Class for a colour input field.
    /// </summary>
    /// <param name="fieldName">The unique name of the field, usually defined in json block.</param>
    /// <param name="color">The initial colour in '#rrggbb' format.</param>
    public FieldColour(string fieldName, string color) : base(fieldName)
    {
        mColorOptions = DEFAULT_COLOR_OPTIONS;
        mColor = color;
        //this.SetText(Field.NBSP + Field.NBSP + Field.NBSP);
    }

    /// <summary>
    /// Return the current colour.
    /// </summary>
    /// <returns>Current colour in '#rrggbb' format.</returns>
    public override string GetValue()
    {
        return mColor;
    }

    /// <summary>
    /// Set the colour.
    /// </summary>
    /// <param name="newValue">The new colour in '#rrggbb' format.</param>
    public override void SetValue(string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            // No change if null.
            return;
        }

        var oldValue = this.GetValue();
        if (string.Equals(oldValue.ToLower(), newValue.ToLower()))
            return;

        mColor = newValue;
        FireUpdate(mColor);
    }

    /// <summary>
    /// Get the text from this field.  Used when the block is collapsed.
    /// </summary>
    public override string GetText()
    {
        Regex rgx = new Regex(@"/^#(.)\1(.)\2(.)\3$/");
        Match match = rgx.Match(mColor);
        if (match.Success)
            return "#" + match.Value[1] + match.Value[2] + match.Value[3];
        return mColor;
    }

    /// <summary>
    /// Get the color options 
    /// </summary>
    public string[] GetOptions()
    {
        return mColorOptions;
    }
}
