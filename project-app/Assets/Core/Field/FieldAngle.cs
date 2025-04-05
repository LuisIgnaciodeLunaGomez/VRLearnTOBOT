
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
 * Versión: 1.0.2
 * 
 * Descripción: 
 */


using Newtonsoft.Json.Linq;
using System;
using UnityEngine;


public sealed class FieldAngle : FieldTextInputModel
{

    [FieldCreator(FieldType = "field_angle")]

    private static FieldAngle CreateFromJson(JObject json)
    {
        string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                               ? json["name"].ToString()
                               : "FIELDNAME_DEFAULT";

        // --- VALIDACIÓN ADICIONAL RECOMENDADA para angle ---
        // Asegurarse de que 'angle' existe y es convertible a string antes de pasarlo.
        // Usar un valor por defecto si falta o es de tipo incorrecto.
        string angleStr = "90"; // Valor por defecto si no se encuentra
        if (json["angle"] != null &&
           (json["angle"].Type == JTokenType.String || json["angle"].Type == JTokenType.Integer || json["angle"].Type == JTokenType.Float))
        {
            angleStr = json["angle"].ToString();
        }
        else if (json["angle"] != null)
        {
            // Opcional: Loguear si el tipo no es el esperado
            Debug.LogWarning($"FieldAngle.CreateFromJson: 'angle' tiene un tipo inesperado ({json["angle"].Type}), usando valor por defecto '{angleStr}'.");
        }


        FieldAngle field = new FieldAngle(fieldName, angleStr); // Usar el valor seguro

        if (json["gap"] != null)
        {
            // Permitir string, integer, o float para gap
            if (json["gap"].Type == JTokenType.String || json["gap"].Type == JTokenType.Integer || json["gap"].Type == JTokenType.Float)
            {
                try
                {
                    // Intentar crear el número, puede fallar si el string no es numérico
                    field.mGap = new Number(json["gap"].ToString());
                    if (field.mGap.IsNaN) // Chequear si la conversión falló dentro de Number
                    {
                        Debug.LogWarning($"FieldAngle.CreateFromJson: 'gap' no pudo convertirse a número válido ('{json["gap"].ToString()}'). Usando valor por defecto.");
                        field.mGap = Number.ZERO; // Revertir a defecto si la conversión falla
                    }
                }
                catch (System.Exception ex) // Captura errores en el constructor de Number
                {
                    Debug.LogError($"FieldAngle.CreateFromJson: Error creando Number para 'gap' ('{json["gap"].ToString()}'): {ex.Message}");
                    field.mGap = Number.ZERO; // Revertir a defecto en caso de error
                }
            }
            else
            {
                Debug.LogWarning($"FieldAngle.CreateFromJson: 'gap' tiene un tipo inesperado ({json["gap"].Type}), ignorando.");
                // mGap mantendrá su valor por defecto (Number.ZERO)
            }
        }


        return field;
    }



    private Number mAngleNumber;
    private Number mOriAngleNumber;

    /// <summary>
    /// gap between angles. eg. 0, 30, 60...
    /// </summary>
    private Number mGap;
    public Number Gap { get { return mGap; } }

    /// <summary>
    /// Class for an editable angle field.
    /// </summary>
    public FieldAngle(string fieldName) : this(fieldName, "0") { }

    /// <summary>
    /// Class for an editable angle field.
    /// </summary>
    public FieldAngle(string fieldName, string optValue) : base(fieldName)
    {
        mAngleNumber = new Number(!string.IsNullOrEmpty(optValue) ? optValue : "0");
        if (mAngleNumber.IsNaN) mAngleNumber = new Number(0);
        this.SetValue(mAngleNumber.ToString());

        mOriAngleNumber = mAngleNumber;
        mGap = new Number(0);
    }

    /// <summary>
    /// Class for an editable angle field.
    /// </summary>
    public FieldAngle(string fieldName, Number optValue) : base(fieldName)
    {
        mAngleNumber = optValue.IsNaN ? new Number(0) : optValue;
        this.SetValue(mAngleNumber.ToString());
    }

    protected override string ClassValidator(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        // 1. Parsear el texto de entrada a un Number inicial
        //    Usa una variable local temporal para no modificar mAngleNumber aún.
        Number inputNumber = new Number(text);
        if (inputNumber.IsNaN) // Validar NaN aquí
        {
            Debug.LogWarning($"FieldAngle.ClassValidator: Input text '{text}' is not a valid number.");
            return null; // Entrada inválida
        }


        // 2. Realizar cálculos creando NUEVOS Numbers para cada paso
        Number currentNumber = inputNumber; // Variable para los resultados intermedios

        try // Englobar cálculos que accedan a .Value
        {
            // Aplicar módulo 360
            float valueMod360 = currentNumber.Value % 360;
            currentNumber = new Number(valueMod360); // Crear nuevo Number

            // Asegurar que el ángulo esté en [0, 360)
            if (currentNumber.Value < 0)
            {
                currentNumber = new Number(currentNumber.Value + 360); // Crear nuevo Number
            }

            // 3. Aplicar el ajuste del 'gap' (intervalo) - más robusto
            //    Asegúrate que mGap y mOriAngleNumber NO son NaN antes de usarlos.
            if (!mGap.IsNaN && mGap.Value > 0 && !mOriAngleNumber.IsNaN)
            {
                // Calcular la diferencia angular más corta (maneja el cruce por 360)
                float diff = currentNumber.Value - mOriAngleNumber.Value;

                // Normalizar diferencia a +/- 180 grados
                while (diff <= -180) diff += 360;
                while (diff > 180) diff -= 360;

                // Calcular el "snap" al intervalo más cercano usando RoundToInt
                int interval = Mathf.RoundToInt(diff / mGap.Value);
                float snappedValue = mOriAngleNumber.Value + interval * mGap.Value;

                // Volver a normalizar el resultado final a [0, 360)
                snappedValue = (snappedValue % 360 + 360) % 360;

                currentNumber = new Number(snappedValue); // Crear nuevo Number con el valor ajustado
            }
            // Si mGap es 0, NaN o mOriAngleNumber es NaN, no se aplica el snap.

            // 4. Asignar el resultado final validado de vuelta a mAngleNumber
            //    Solo al final, cuando todos los cálculos son correctos.
            mAngleNumber = currentNumber;

            // 5. Devolver la representación de texto del Number final
            return mAngleNumber.ToString();

        }
        catch (InvalidOperationException ex)
        {
            // Esto captura el error si intentamos acceder a .Value en un NaN
            Debug.LogError($"FieldAngle.ClassValidator: Error during calculation (likely accessing .Value on NaN): {ex.Message}");
            // Puedes decidir qué devolver aquí, quizá el texto original o null.
            // Devolver null indica que la validación falló.
            return null;
        }
    }

}

