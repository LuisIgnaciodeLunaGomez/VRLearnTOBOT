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

using Newtonsoft.Json.Linq;
using UnityEngine;

public class FieldImageModel : FieldModel {
    [FieldCreator(FieldType = "field_image")]
    private static FieldImageModel CreateFromJson(JObject json)
    {
        string fieldName = "FIELDNAME_DEFAULT"; 
        if (json.TryGetValue("name", System.StringComparison.OrdinalIgnoreCase, out JToken nameToken) && nameToken.Type == JTokenType.String)
        {
            fieldName = nameToken.ToString();
        }
        else if (nameToken != null) 
        {
            Debug.LogWarning($"FieldImageModel.CreateFromJson: 'name' property for a field_image is present but not a string ('{nameToken}'). Using default '{fieldName}'.");
        }

        string imageSrc = Define.FIELD_IMAGE_SRC_DEFAULT;
        if (json.TryGetValue("src", System.StringComparison.OrdinalIgnoreCase, out JToken srcToken) && srcToken.Type == JTokenType.String)
        {
            imageSrc = srcToken.ToString();
            if (string.IsNullOrEmpty(imageSrc)) 
            {
                Debug.LogWarning($"FieldImageModel.CreateFromJson: 'src' property for field '{fieldName}' is an empty string. Using default '{Define.FIELD_IMAGE_SRC_DEFAULT}'.");
                imageSrc = Define.FIELD_IMAGE_SRC_DEFAULT;
            }
        }
        else if (srcToken != null) 
        {
            Debug.LogWarning($"FieldImageModel.CreateFromJson: 'src' property for field '{fieldName}' is present but not a string ('{srcToken}'). Using default '{Define.FIELD_IMAGE_SRC_DEFAULT}'.");
        }

        float finalWidth = Define.FIELD_IMAGE_WIDTH_DEFAULT; 
        if (json.TryGetValue("width", System.StringComparison.OrdinalIgnoreCase, out JToken widthToken) && widthToken != null)
        {
           
            if (Number.TryParse(widthToken.ToString(), out Number parsedWidth) && !parsedWidth.IsNaN && parsedWidth.Value > 0)
            {
                finalWidth = (float)parsedWidth.Value; 
            }
            else
            {
                Debug.LogWarning($"FieldImageModel.CreateFromJson: Could not parse 'width' or value is not positive ('{widthToken}') for field '{fieldName}'. Using default {finalWidth}.");
            }
        }

        float finalHeight = Define.FIELD_IMAGE_HEIGHT_DEFAULT; 
        if (json.TryGetValue("height", System.StringComparison.OrdinalIgnoreCase, out JToken heightToken) && heightToken != null)
        {
            if (Number.TryParse(heightToken.ToString(), out Number parsedHeight) && !parsedHeight.IsNaN && parsedHeight.Value > 0)
            {
                finalHeight = (float)parsedHeight.Value; 
            }
            else
            {
                Debug.LogWarning($"FieldImageModel.CreateFromJson: Could not parse 'height' or value is not positive ('{heightToken}') for field '{fieldName}'. Using default {finalHeight}.");
            }
        }

        string alt = null; 
        if (json.TryGetValue("alt", System.StringComparison.OrdinalIgnoreCase, out JToken altToken) && altToken.Type == JTokenType.String)
        {
            alt = altToken.ToString();
        }
        else if (altToken != null) 
        {
            Debug.LogWarning($"FieldImageModel.CreateFromJson: 'alt' property for field '{fieldName}' is present but not a string ('{altToken}'). Ignoring.");
        }

    
        return new FieldImageModel(fieldName, imageSrc, new Vector2(finalWidth, finalHeight), alt);
    }

    private Vector2 mSize;
    public Vector2 Size { get { return mSize; } } 

   
    public FieldImageModel(string fieldName, string imageSrc, Vector2 imageSize, string optAlt = null) : base(fieldName)
    {
        this.mText = !string.IsNullOrEmpty(optAlt) ? optAlt : "";

        if (string.IsNullOrEmpty(imageSrc))
        {
            Debug.LogWarning($"FieldImageModel Constructor: Received null or empty imageSrc for field '{fieldName}'. Using default '{Define.FIELD_IMAGE_SRC_DEFAULT}'.");
            imageSrc = Define.FIELD_IMAGE_SRC_DEFAULT;
        }
        this.SetValue(imageSrc);

        mSize = imageSize;

        if (mSize.x <= 0)
        {
            Debug.LogWarning($"FieldImageModel Constructor: Received non-positive width ({mSize.x}) for field '{fieldName}'. Setting to default {Define.FIELD_IMAGE_WIDTH_DEFAULT}.");
            mSize.x = Define.FIELD_IMAGE_WIDTH_DEFAULT;
        }
        if (mSize.y <= 0)
        {
            Debug.LogWarning($"FieldImageModel Constructor: Received non-positive height ({mSize.y}) for field '{fieldName}'. Setting to default {Define.FIELD_IMAGE_HEIGHT_DEFAULT}.");
            mSize.y = Define.FIELD_IMAGE_HEIGHT_DEFAULT;
        }

        IsImage = true; 
    }

}//Fin de la clase FieldImageModel
