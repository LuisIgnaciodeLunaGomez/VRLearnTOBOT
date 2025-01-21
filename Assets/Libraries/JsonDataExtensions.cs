//Requiere instalar el paquete NGUT Newtonsoft.Json Install-Package Newtonsoft.Json instalar también en Unity com.unity.nuget.newtonsoft-json

using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;


    public static class JsonDataExtensions
    {
        /// <summary>
        /// Verifica si un JToken es nulo, indefinido o no tiene valor.
        /// </summary>
        /// <param name="self">El JToken a analizar.</param>
        /// <returns>True si el JToken es null, undefined o no tiene valor; de lo contrario, false.</returns>
        public static bool IsNullOrUndefined(this JToken self)
        {
            return self == null || self.Type == JTokenType.Null || self.Type == JTokenType.Undefined || self.Type == JTokenType.None;
        }

        /// <summary>
        /// Verifica si el JToken no es un objeto JSON.
        /// </summary>
        /// <param name="self">El JToken a analizar.</param>
        /// <returns>True si el JToken no es un objeto JSON; de lo contrario, false.</returns>
        public static bool IsNotObject(this JToken self)
        {
            return self == null || self.Type != JTokenType.Object;
        }

        /// <summary>
        /// Verifica si un JToken contiene una clave específica.
        /// </summary>
        /// <param name="self">El JToken (debe ser un objeto).</param>
        /// <param name="key">La clave a buscar.</param>
        /// <returns>True si la clave existe; de lo contrario, false.</returns>
        public static bool JsonDataContainsKey(this JToken self, string key)
        {
            if (self.IsNullOrUndefined() || self.IsNotObject())
                return false;

            var jsonObject = self as JObject;
            return jsonObject != null && jsonObject.TryGetValue(key, out _);
        }

        /// <summary>
        /// Devuelve el tipo de un JToken como una cadena descriptiva.
        /// </summary>
        /// <param name="self">El JToken a analizar.</param>
        /// <returns>Una descripción del tipo de JToken.</returns>
        public static string GetTokenTypeDescription(this JToken self)
        {
            if (self == null)
                return "El token es null.";

            return self.Type switch
            {
                JTokenType.Object => "El token es un objeto JSON.",
                JTokenType.Array => "El token es un arreglo JSON.",
                JTokenType.String => "El token es una cadena de texto.",
                JTokenType.Integer => "El token es un número entero.",
                JTokenType.Float => "El token es un número decimal.",
                JTokenType.Boolean => "El token es un valor booleano.",
                JTokenType.Null => "El token es null.",
                JTokenType.Undefined => "El token es undefined.",
                JTokenType.Date => "El token es una fecha.",
                JTokenType.None => "El token no está inicializado.",
                _ => $"El token es de un tipo no identificado: {self.Type}."
            };
    }

        /// <summary>
        /// Comprueba si un JToken coincide con uno de los tipos proporcionados.
        /// </summary>
        /// <param name="self">El JToken a analizar.</param>
        /// <param name="types">Tipos de JToken a verificar.</param>
        /// <returns>True si el JToken coincide con uno de los tipos; de lo contrario, false.</returns>
        public static bool MatchesTypes(this JToken self, params JTokenType[] types)
        {
            if (self == null) return false;
            return types.Contains(self.Type);
        }
    }

