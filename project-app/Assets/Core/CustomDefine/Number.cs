/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 24/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Archivo que contiene los enumeradores utilizados en la aplicación
 */
using System;
using UnityEngine;

/// <summary>
/// struct for number type, instead of int, float, double...
/// we use number in blockly, like number type in dynamic languages like javascript.
/// </summary>
public struct Number : IEquatable<Number>
{
    // Usar un Epsilon constante para las comparaciones de igualdad
    
    private const float Epsilon = 1e-6f;

    private readonly float mValue;

    public static readonly Number ZERO = new Number(0);

    public float Value
    {
        get
        {
            if (IsNaN)
            {
                // Decide qué hacer: ¿lanzar excepción o devolver float.NaN?
                // Lanzar excepción es más seguro para detectar usos incorrectos.
                throw new InvalidOperationException("Cannot access Value property when Number is NaN.");
                // Alternativa: return float.NaN; // Pero puede ocultar errores
            }
            return mValue;
        }
    }
    public bool IsNaN { get; private set; } 
    

    // --- Constructores ---


    public Number(int intValue)
    {
        IsNaN = false;
        mValue = intValue;
    }

    public Number(float floatValue)
    {
        // Considerar si float.NaN debe representarse como IsNaN=true
        if (float.IsNaN(floatValue))
        {
            IsNaN = true;
            mValue = float.NaN; // O 0, según preferencia
        }
        else
        {
            IsNaN = false;
            mValue = floatValue;
        }
    }

    public Number(double doubleValue)
    {
        if (double.IsNaN(doubleValue))
        {
            IsNaN = true;
            mValue = float.NaN;
        }
        // Considerar límites de float
        else if (doubleValue > float.MaxValue)
        {
            IsNaN = false; // ¿O NaN? Decide el comportamiento en overflow
            mValue = float.MaxValue;
            Debug.LogWarning($"Number(double): Value {doubleValue} clamped to float.MaxValue.");
        }
        else if (doubleValue < float.MinValue)
        {
            IsNaN = false; // ¿O NaN?
            mValue = float.MinValue;
            Debug.LogWarning($"Number(double): Value {doubleValue} clamped to float.MinValue.");
        }
        else
        {
            IsNaN = false;
            mValue = (float)doubleValue;
        }
    }

    public Number(string strValue)
    {
        // Usar TryParse para mantener la lógica centralizada
        // TryParse maneja la asignación de NaN si falla
        Number.TryParse(strValue, out this);
        // Opcional: Loguear si el resultado es NaN después del parseo
        if (this.IsNaN)
        {
            Debug.LogWarning($"Number(string): Could not parse '{strValue}' as a valid number, or it resulted in NaN.");
        }
    }

    // Propiedad estática para NaN (Singleton)
    public static readonly Number NaN = new Number(float.NaN);

    // Propiedades estáticas para Min/Max Value
    public static Number MinValue => new Number(float.MinValue);
    public static Number MaxValue => new Number(float.MaxValue);


    /// <summary>
    /// Tries to parse a string into a Number.
    /// Uses CultureInfo.InvariantCulture for parsing.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The parsed Number if successful, otherwise Number.NaN.</param>
    /// <returns>True if parsing was successful (result is not NaN), false otherwise.</returns>
    public static bool TryParse(string s, out Number result)
    {
        // Usar configuración InvariantCulture para consistencia (punto decimal)
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
        {
            // El constructor Number(float) maneja el caso float.IsNaN(floatValue)
            result = new Number(floatValue);
            return true;
        }
        else
        {
            result = Number.NaN;
            return false;
        }
    }

    // --- Métodos de Igualdad y HashCode ---
    public override string ToString()
    {
        // Devolver "NaN" si es NaN, de lo contrario el valor
        return IsNaN ? "NaN" : mValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // 1. Sobrescritura de Object.Equals(object obj)
    public override bool Equals(object obj)
    {
        // Verificar null y si el tipo es compatible
        if (obj is Number other)
        {
            // Delegar a la implementación IEquatable<Number>
            return Equals(other);
        }
        return false;
    }

    // 2. Implementación de IEquatable<Number>.Equals(Number other)
    public bool Equals(Number other)
    {
        // Dos NaN se consideran iguales en este contexto? O seguir el estándar float?
        // Opción A: Seguir el estándar (NaN != NaN) -> La comparación Math.Abs fallará para NaN.
        // Opción B: Considerar NaN == NaN
        if (this.IsNaN && other.IsNaN)
        {
            return true; // Decisión: consideramos dos Number.NaN como iguales
        }
        if (this.IsNaN || other.IsNaN)
        {
            return false; // Si uno es NaN y el otro no, no son iguales
        }

        // Si ninguno es NaN, comparar los valores con el épsilon
        return Math.Abs(this.Value - other.Value) < Epsilon;
    }

    // 3. Sobrescritura de Object.GetHashCode()
    public override int GetHashCode()
    {
        // Debe ser consistente con Equals.
        // Si dos NaNs son iguales (según nuestra Equals), deben tener el mismo hash code.
        if (IsNaN)
        {
            return 0; // O cualquier otro valor constante para NaN, como float.NaN.GetHashCode()
        }
        // Si no es NaN, basar el hash en el Value
        return Value.GetHashCode();
    }

  
    public static Number operator +(Number a, Number b)
    {
        return new Number(a.Value + b.Value);
    }

    public static Number operator -(Number a, Number b)
    {
        return new Number(a.Value - b.Value);
    }

    public static Number operator -(Number a)
    {
        return new Number(-a.Value);
    }

    public static Number operator *(Number a, Number b)
    {
        return new Number(a.Value * b.Value);
    }

    public static Number operator /(Number a, Number b)
    {
        return new Number(a.Value / b.Value);
    }

    public static Number operator %(Number a, Number b)
    {
        return new Number(a.Value % b.Value);
    }

    public static bool operator ==(Number a, Number b)
    {
        return Math.Abs(a.Value - b.Value) < 9.99999943962493E-11;
    }

    public static bool operator !=(Number a, Number b)
    {
        return Math.Abs(a.Value - b.Value) >= 9.99999943962493E-11;
    }

    public static bool operator <(Number a, Number b)
    {
        return a.Value < b.Value;
    }

    public static bool operator >(Number a, Number b)
    {
        return a.Value > b.Value;
    }

    public static bool operator <=(Number a, Number b)
    {
        return a.Value <= b.Value;
    }

    public static bool operator >=(Number a, Number b)
    {
        return a.Value >= b.Value;
    }

    public static Number Clamp(Number value, Number min, Number max)
    {
        if (value.IsNaN || min.IsNaN || max.IsNaN) return Number.NaN;
        // Usa el operador relacional sobrecargado que ya maneja NaN
        if (min > max)
        {
            Debug.LogError("Clamp static: min value cannot be greater than max value.");
            return Number.NaN; // Devuelve NaN si los límites son inválidos
        }
        // Usa los valores internos para Mathf.Clamp ya que hemos validado que no son NaN
        float clampedFloat = Mathf.Clamp(value.mValue, min.mValue, max.mValue);
        return new Number(clampedFloat); // Devuelve una NUEVA instancia
    }

    /// <summary>
    /// Defines an explicit conversion from a Number to a float.
    /// Returns float.NaN if the Number is NaN.
    /// </summary>
    public static explicit operator float(Number number)
    {
        if (number.IsNaN) return float.NaN;
        // Accede al campo readonly directamente aquí porque es seguro dentro de la propia struct.
        return number.mValue;
    }

    /// <summary>
    /// Defines an explicit conversion from a Number to an int.
    /// Throws InvalidCastException if the Number is NaN.
    /// Uses Convert.ToInt32 for rounding/truncation behavior.
    /// </summary>
    public static explicit operator int(Number number)
    {
        if (number.IsNaN) throw new InvalidCastException("Cannot cast Number.NaN to int.");
        // Accede a mValue de forma segura después de comprobar IsNaN
        return Convert.ToInt32(number.mValue);
    }

    /// <summary>
    /// Defines an explicit conversion from a Number to a double.
    /// Returns double.NaN if the Number is NaN.
    /// </summary>
    public static explicit operator double(Number number)
    {
        if (number.IsNaN) return double.NaN;
        // Accede a mValue, la conversión a double es segura.
        return number.mValue;
    }
}
