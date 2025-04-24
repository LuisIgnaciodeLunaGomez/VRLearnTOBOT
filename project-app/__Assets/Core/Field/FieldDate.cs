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

using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class FieldDate : FieldModel
{
    [FieldCreator(FieldType = "field_date")]
    private static FieldDate CreateFromJson(JObject json)
    {
        // --- Corrección para fieldName ---
        string fieldName = "FIELDNAME_DEFAULT"; // Valor por defecto
        // Verificar si la clave "name" existe, no es null, Y su tipo es String
        if (json["name"] != null && json["name"].Type == JTokenType.String)
        {
            fieldName = json["name"].ToString();
        }
        else if (json["name"] != null)
        {
            // Opcional: Añadir un warning si 'name' existe pero no es un string
            Debug.LogWarning($"FieldDate.CreateFromJson: El campo 'name' existe pero no es del tipo esperado String (Tipo: {json["name"].Type}). Usando nombre por defecto '{fieldName}'.");
        }
        // Si json["name"] es null o no existe, se mantiene el valor por defecto.

        // --- Corrección/Mejora para dateStr ---
        // Asumimos que JsonDataContainsKey verifica existencia y no nulidad (si no, añade json[key] != null)
        string dateStr = null;
        if (json.JsonDataContainsKey("date") && json["date"] != null) // Chequeo extra de null por si acaso
        {
            // Asegurarse de que el token sea realmente convertible a string
            // Para JValue (string, number, boolean) .ToString() suele funcionar bien.
            // Si pudiera ser un objeto o array, necesitarías más lógica. Asumimos JValue aquí.
            if (json["date"].Type == JTokenType.String ||
                json["date"].Type == JTokenType.Date || // Permitir formato ISODate
                json["date"].Type == JTokenType.Integer || // Podría ser timestamp? (necesitaría conversión)
                json["date"].Type == JTokenType.Float)     // Podría ser timestamp?
            {
                // Si es JTokenType.Date, .ToString() podría necesitar ajustes de formato.
                // Lo más seguro es extraer el valor específico si es Date.
                if (json["date"].Type == JTokenType.Date)
                {
                    // DateTime dtValue = (DateTime)json["date"]; // Extraer DateTime directamente
                    // dateStr = dtValue.ToString(DATE_FORMAT);
                    // --> Sin embargo, TryParseExact en el constructor es más robusto
                    dateStr = json["date"].Value<string>(); // Intenta obtener como string si es posible

                }
                else
                {
                    dateStr = json["date"].ToString();
                }

            }
            else
            {
                Debug.LogWarning($"FieldDate.CreateFromJson: El campo 'date' existe pero tiene un tipo inesperado ({json["date"].Type}). Se intentará usar un valor por defecto.");
                // dateStr permanecerá null
            }
        }
        // else: La clave 'date' no existe o es null, dateStr permanece null.


        // --- Manejo de dateStr nulo antes del constructor ---
        // El constructor necesita una cadena, aunque sea para fallar el TryParseExact y usar el default interno.
        // Podríamos usar una fecha por defecto aquí o dejar que el constructor lo haga.
        // Es más limpio dejar que el constructor maneje el parseo final y los defaults.
        // PERO el constructor original LANZABA EXCEPCIÓN si dateStr era null o inválido.
        // Necesitamos modificar el constructor O proporcionar un valor por defecto aquí.
        // --> Opción: Modificar constructor para que NO lance excepción y use default.

        if (dateStr == null)
        {
            // Si 'date' no se encontró o era de tipo incorrecto, generamos una fecha por defecto AHORA
            dateStr = DateTime.UtcNow.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
            Debug.LogWarning($"FieldDate.CreateFromJson: No se pudo obtener una cadena de fecha válida del JSON. Usando fecha actual por defecto: {dateStr}");
        }

        return new FieldDate(fieldName, dateStr);
    }

    private const string DATE_FORMAT = "yyyy-MM-dd";

    private DateTime mDate;
    public DateTime Date { get { return mDate; } }

    public FieldDate(string fieldName, string dateStr) : base(fieldName)
    {
        if (!DateTime.TryParseExact(dateStr, DATE_FORMAT, null, DateTimeStyles.None, out mDate))
            throw new Exception(
                String.Format("FieldDate: can\'t parse date string {0} to DateTime. Correct format is {1}.", dateStr, DATE_FORMAT));
    }
}
