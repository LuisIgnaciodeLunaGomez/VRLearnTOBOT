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

using Newtonsoft.Json.Linq;

public static class JsonExtensions
{
    public static bool JsonDataContainsKey(this JObject json, string key)
    {        return json != null && json.TryGetValue(key, out JToken token) && token != null && token.Type != JTokenType.Null;
    }
  
    /// Safely tries to get a string value from a JObject property.
   
    public static string JsonTryGetString(this JObject jobj, string propertyName, string defaultValue = null)
    {
        if (jobj == null || !jobj.TryGetValue(propertyName, System.StringComparison.OrdinalIgnoreCase, out JToken token))
        {
            return defaultValue;
        }

        return token?.ToString() ?? defaultValue;
    }

   
    /// Safely tries to get an integer value from a JObject property.
    
    public static int JsonTryGetInt(this JObject jobj, string propertyName, int defaultValue = 0)
    {
        if (jobj != null && jobj.TryGetValue(propertyName, System.StringComparison.OrdinalIgnoreCase, out JToken token))
        {
            if (token != null && int.TryParse(token.ToString(), out int result))
            {
                return result;
            }
        }
        return defaultValue;
    }

 
    /// Safely tries to get a boolean value from a JObject property.
    
    public static bool JsonTryGetBool(this JObject jobj, string propertyName, bool defaultValue = false)
    {
        if (jobj != null && jobj.TryGetValue(propertyName, System.StringComparison.OrdinalIgnoreCase, out JToken token))
        {
            if (token != null && bool.TryParse(token.ToString(), out bool result))
            {
                return result;
            }
            string lowerVal = token?.ToString().ToLowerInvariant();
            if (lowerVal == "true" || lowerVal == "1") return true;
            if (lowerVal == "false" || lowerVal == "0") return false;
        }
        return defaultValue;
    }
}//fin clase JsonExtension