/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;


    /// <summary>
    /// boolean, number, string
    /// </summary>
    public struct DataStruct
    {
        public Define.EDataType Type;

        private bool mBooleanValue;
        public bool BooleanValue
        {
            get
            {
                if (this.Type != Define.EDataType.Boolean)
                    throw new Exception("try to GET a boolean value from a not-boolean data");
                return mBooleanValue;
            }
            set
            {
                if (this.Type != Define.EDataType.Boolean)
                    throw new Exception("try to SET a boolean value from a not-boolean data");
                mBooleanValue = value;
            }
        }

        private Number mNumberValue;
        public Number NumberValue
        {
            get
            {
                if (this.Type != Define.EDataType.Number)
                    throw new Exception("try to GET a number value from a not-number data");
                return mNumberValue;
            }
            set
            {
                if (this.Type != Define.EDataType.Number)
                    throw new Exception("try to SET a number value from a not-number data");
                mNumberValue = value;
            }
        }

        private string mStringValue;
        public string StringValue
        {
            get
            {
                if (this.Type != Define.EDataType.String)
                    throw new Exception("try to GET a string value from a not-string data");
                return mStringValue;
            }
            set
            {
                if (this.Type != Define.EDataType.String)
                    throw new Exception("try to SET a string value from a not-string data");
                mStringValue = value;
            }
        }

        private ArrayList mListValue;
        public ArrayList ListValue
        {
            get
            {
                if (this.Type != Define.EDataType.List)
                    throw new Exception("try to GET a list value from a not-list data");
                return mListValue;
            }
            set
            {
                if (this.Type != Define.EDataType.List)
                    throw new Exception("try to SET a list value from a not-list data");
                mListValue = value;
            }
        }

        public DataStruct(bool booleanValue)
        {
            this.Type = Define.EDataType.Boolean;
            this.mBooleanValue = booleanValue;
            this.mNumberValue = Number.NaN;
            this.mStringValue = null;
            this.mListValue = null;
        }

        public DataStruct(Number numberValue)
        {
            this.Type = Define.EDataType.Number;
            this.mBooleanValue = false;
            this.mNumberValue = numberValue;
            this.mStringValue = null;
            this.mListValue = null;
        }

        public DataStruct(int intValue)
        {
            this.Type = Define.EDataType.Number;
            this.mBooleanValue = false;
            this.mNumberValue = new Number(intValue);
            this.mStringValue = null;
            this.mListValue = null;
        }
        
        public DataStruct(float floatValue)
        {
            this.Type = Define.EDataType.Number;
            this.mBooleanValue = false;
            this.mNumberValue = new Number(floatValue);
            this.mStringValue = null;
            this.mListValue = null;
        }
        
        public DataStruct(double doubleValue)
        {
            this.Type = Define.EDataType.Number;
            this.mBooleanValue = false;
            this.mNumberValue = new Number(doubleValue);
            this.mStringValue = null;
            this.mListValue = null;
        }

        public DataStruct(string stringValue)
        {
            this.Type = Define.EDataType.String;
            this.mBooleanValue = false;
            this.mNumberValue = Number.NaN;
            this.mStringValue = stringValue;
            this.mListValue = null;
        }
        
        public DataStruct(ArrayList listValue)
        {
            this.Type = Define.EDataType.List;
            this.mBooleanValue = false;
            this.mNumberValue = Number.NaN;
            this.mStringValue = null;
            this.mListValue = listValue;
        }

        public static DataStruct Undefined
        {
            get { return new DataStruct(); }
        }

        public bool IsUndefined
        {
            get { return Type <= 0; }
        }

        public bool IsBoolean
        {
            get { return Type == Define.EDataType.Boolean; }
        }

        public bool IsNumber
        {
            get { return Type == Define.EDataType.Number; }
        }

        public bool IsString
        {
            get { return Type == Define.EDataType.String; }
        }

        public bool IsList
        {
            get { return Type == Define.EDataType.List; }
        }

        #region override

        public override bool Equals(object obj)
        {
            return (obj is DataStruct) && (this == (DataStruct) obj);
        }


    public override int GetHashCode()
    {
        // Usa números primos para combinar los hash codes, ayuda a la distribución.
        int hashCode = 17; // Valor inicial primo

        // Incluye el tipo en el hash code
        hashCode = hashCode * 31 + Type.GetHashCode();

        // Incluye el hash code del valor activo correspondiente
        // Usa un bloque unchecked para permitir el desbordamiento aritmético,
        // que es normal y aceptable en los cálculos de hash code.
        unchecked
        {
            switch (Type)
            {
                case Define.EDataType.Boolean:
                    hashCode = hashCode * 31 + mBooleanValue.GetHashCode();
                    break;
                case Define.EDataType.Number:
                    // Asume que Number tiene un GetHashCode() adecuado
                    hashCode = hashCode * 31 + mNumberValue.GetHashCode();
                    break;
                case Define.EDataType.String:
                    // Importante: Maneja el caso null para strings
                    hashCode = hashCode * 31 + (mStringValue?.GetHashCode() ?? 0);
                    break;
                case Define.EDataType.List:
                    // Combina los hash codes de los elementos de la lista
                    if (mListValue != null)
                    {
                        foreach (var item in mListValue)
                        {
                            // Maneja elementos null dentro de la lista
                            hashCode = hashCode * 31 + (item?.GetHashCode() ?? 0);
                        }
                    }
                    else
                    {
                        hashCode = hashCode * 31 + 0; // Hash para lista null
                    }
                    break;
                    // Define.EDataType.Undefined y default no tienen valor específico
                    // El Type ya está incluido, así que está bien no añadir más.
            }
        }
        return hashCode;
    }

    public override string ToString()
        {
            switch (this.Type)
            {
                case Define.EDataType.Undefined: return "Undefined";
                case Define.EDataType.Boolean: return mBooleanValue.ToString();
                case Define.EDataType.Number: return mNumberValue.ToString();
                case Define.EDataType.String: return mStringValue;
                case Define.EDataType.List:
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var e in mListValue)
                        sb.Append(e);
                    return sb.ToString();
                }
                default: return "Undefined";
            }
        }
        
        public static bool operator ==(DataStruct a, DataStruct b)
        {
            if (a.Type != b.Type)
                return false;
            switch (a.Type)
            {
                case Define.EDataType.Undefined: return true;
                case Define.EDataType.Boolean: return a.BooleanValue == b.BooleanValue;
                case Define.EDataType.Number: return a.NumberValue == b.NumberValue;
                case Define.EDataType.String: return a.StringValue == b.StringValue;
                case Define.EDataType.List:
                {
                    if (a.ListValue.Count != b.ListValue.Count)
                        return false;
                    for (int i = 0; i < a.ListValue.Count; i++)
                    {
                        if (a.ListValue[i] != b.ListValue[i])
                            return false;
                    }
                    return true;
                }
                default: return false;
            }
        }

        public static bool operator !=(DataStruct a, DataStruct b)
        {
            return !(a == b);
        }


        #endregion
       
    }

/// <summary>
/// hold the actual data for variables
/// </summary>
public class Datas
{
    private Dictionary<string, DataStruct> mDB = null;

    public Datas()
    {
        mDB = new Dictionary<string, DataStruct>();
    }

    public void Reset()
    {
        mDB.Clear();
    }

    /// <summary>
    /// get variable data 
    /// </summary>
    public DataStruct GetData(string varName)
    {
        DataStruct data;
        return mDB.TryGetValue(varName, out data) ? data : DataStruct.Undefined;
    }

    /// <summary>
    /// set variable data
    /// </summary>
    public void SetData(string varName, DataStruct data)
    {
        mDB[varName] = data;
    }
}
