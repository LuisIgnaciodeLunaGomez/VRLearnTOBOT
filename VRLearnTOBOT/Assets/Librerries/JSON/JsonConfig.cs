using UnityEngine;
using Newtonsoft.Json.Linq;
public static class JsonConig
{
    private static readonly Dictionary<JTokenType, string> TokenTypeDescriptions = new()
    {
        { JTokenType.Object, "El token es un objeto JSON." },
        { JTokenType.Array, "El token es un arreglo JSON." },
        { JTokenType.String, "El token es una cadena de texto." },
        { JTokenType.Integer, "El token es un número entero." },
        { JTokenType.Float, "El token es un número decimal." },
        { JTokenType.Boolean, "El token es un valor booleano." },
        { JTokenType.Null, "El token es null." },
        { JTokenType.Undefined, "El token es undefined." },
        { JTokenType.Date, "El token es una fecha." },
        { JTokenType.None, "El token no está inicializado." }
    };

/// <summary>
/// Verifica si el token es de uno o más tipos especificados.
/// </summary>
/// <param name="self"></param>
/// <param name="key"></param>
/// <returns></returns>
    public static bool JsonDataContainsKey(this JToken self, string key)
	{
        // Verificar si el token es null, undefined o no es un objeto JSON
        if (self.MatchesTypes(JTokenType.Null, JTokenType.Undefined, JTokenType.None) || !self.IsObject())
            return false;


        var JsonObject = self as JObject; //Convierte el objeto JToken en un JObject.
		return JsonObject.TryGetValue(key, out _); //Intenta obtener el valor asociado a la clave especificada. Si la clave existe, devuelve true, de lo contrario, devuelve false.

    }


    /// <summary>
    /// Devuelve una descripción del tipo de JToken.
    /// </summary>
    /// <param name="self">El JToken a analizar.</param>
    /// <returns>Una cadena que describe el tipo de JToken.</returns>
    public static string GetTokenTypeDescription(this JToken self)
    {
        if (self == null) return "El token es null.";

        return TokenTypeDescriptions.TryGetValue(self.Type, out var description)
            ? description
            : $"El token es de un tipo no identificado: {self.Type}.";
    }

}//fin clase JsonConfig

