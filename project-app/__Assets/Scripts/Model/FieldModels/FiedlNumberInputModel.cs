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


using UnityEngine;

public class FieldNumberInputModel : FieldModel 
{
    [FieldCreator(FieldType = "field_numberinput")] 
    private static FieldNumberInputModel CreateFromJson(Newtonsoft.Json.Linq.JObject json)
    {
        string fieldName = json.Value<string>("name") ?? "FIELD_DEFAULT_NAME";
        string defaultValue = json.Value<string>("value") ?? "0"; 
        
        return new FieldNumberInputModel(fieldName, defaultValue);
    }

    private Number mMin = Number.MinValue; // Valor mínimo permitido
    private Number mMax = Number.MaxValue; // Valor máximo permitido
    private bool mIntOnly = false; 

  

    
    // Constructor simple con valor por defecto "0".
    public FieldNumberInputModel(string name) : this(name, "0", Number.MinValue, Number.MaxValue, false) { }

   
    // Constructor con valor inicial.
    public FieldNumberInputModel(string name, string initialValue) : this(name, initialValue, Number.MinValue, Number.MaxValue, false) { }



    /**
     * Descripción: Constructor completo (similar a FieldNumber de UBlockly).
     * @param name Nombre del campo (debe ser único dentro del bloque).
     * @param initialValue Valor inicial del campo (string).
     * @param min Valor mínimo permitido (Number).
     * @param max Valor máximo permitido (Number).
     * @param intOnly Si es true, solo se permiten enteros (sin decimales).
     */
    public FieldNumberInputModel(string name, string initialValue, Number min, Number max, bool intOnly) : base(name)
    {
        mMin = min.IsNaN ? Number.MinValue : min;
        mMax = max.IsNaN ? Number.MaxValue : max;
        if (!mMin.IsNaN && !mMax.IsNaN && mMin > mMax)
        {
            Debug.LogWarning($"FieldNumberInputModel '{name}': Min value ({mMin}) cannot be greater than Max value ({mMax}). Swapping.");
            (mMin, mMax) = (mMax, mMin);
        }
        mIntOnly = intOnly;

     
        string validatedInitialValue = ValidateAndProcessValue(initialValue ?? "0");
        base.SetValue(validatedInitialValue); 

        if (this.mText == null)
        {
            this.mText = "0"; 
        }
    }

    // Obtiene el tipo de este campo.

    // public override string GetFieldType() => "field_numberinput";



    /** 
     * Descripción: Intenta establecer un nuevo valor para el campo, validándolo primero.
     * Actualiza el valor interno y notifica a los observadores si el valor validado es diferente al valor actual.
    * @param newValue El nuevo valor a establecer (string).
    */
    public override void SetValue(string newValue) 
    {
        if (newValue == null)
        {
            Debug.LogWarning($"FieldNumberInputModel '{Name}': SetValue called with null. Ignoring.");
            return;
        }

        string oldValue = this.mText; 

        string processedValue = ValidateAndProcessValue(newValue);

        if (processedValue == null)
        {
        
            // Debug.LogWarning($"FieldNumberInputModel: Value '{newValue}' rejected or resulted in no change for field '{Name}'.");
            return;
        }

      
        if (oldValue != processedValue)
        {
            
            base.SetText(processedValue); 
        }
       
    }


    /** 
     * Descripción: Valida, clampa y potencialmente convierte a entero el valor dado.
     * @param inputValue El valor de entrada a validar (string).
     * @return El valor procesado como string, o null si no es válido.
    */
    private string ValidateAndProcessValue(string inputValue)
    {
        if (!Number.TryParse(inputValue, out Number numValue))
        {
            
            // string cleanedInput = inputValue?.Replace(",", ""); 
            // if (cleanedInput == null || !Number.TryParse(cleanedInput, out numValue))
            //     return null;
            return null; 
        }

        if (numValue.IsNaN)
        {
            return null; 
        }

        if (mIntOnly)
        {
            numValue = new Number(Mathf.RoundToInt((float)numValue)); 
        }

        if (!numValue.IsNaN)
        {
            numValue = Number.Clamp(numValue, mMin, mMax);
        }

        return numValue.ToString();
    }

    /** 
     * Descripción: Obtiene el valor numérico actual del campo.
     * @return El valor como float, o 0 si no se puede convertir.
     */
    public float GetNumericValue()
    {

        return float.TryParse(this.mText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }

    /** 
     * Descripción: Obtiene el valor numérico actual del campo como un Number.
     * @return El valor como Number, o NaN si no se puede convertir.
    */
    public Number GetNumberValue()
    {
        if (Number.TryParse(this.mText, out Number result))
        {
            return result;
        }
        return Number.NaN;
    }

    public Number MinValue => mMin;
    public Number MaxValue => mMax;
    public bool IsIntOnly => mIntOnly;
} //fin clase FieldNumberInputModel


