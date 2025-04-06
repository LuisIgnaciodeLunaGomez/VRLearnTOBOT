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
    {
        // Verifica si el objeto no es nulo, la clave existe,
        // y el valor asociado a la clave no es nulo ni del tipo JTokenType.Null
        return json != null && json.TryGetValue(key, out JToken token) && token != null && token.Type != JTokenType.Null;
    }
}