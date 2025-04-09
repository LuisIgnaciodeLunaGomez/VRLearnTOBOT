
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción:
 * 
 */

using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UBlockly
{
    public sealed class FieldNumberModel : FieldTextInputModel
    {
        [FieldCreator(FieldType = "field_number")]
        private static FieldNumberModel CreateFromJson(JObject json)
        {
         
            string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                                   ? json["name"].ToString()
                                   : "FIELDNAME_DEFAULT"; 

            string valueStr = (json["value"] != null) ? json["value"].ToString() : "0"; 
            string minStr = (json["min"] != null) ? json["min"].ToString() : null;
            string maxStr = (json["max"] != null) ? json["max"].ToString() : null;
            bool intOnly = (json["int"] != null && json["int"].Type == JTokenType.Boolean) ? (bool)json["int"] : false; 

            return new FieldNumberModel(fieldName, valueStr, minStr, maxStr, intOnly);
        }

        private Number mNumber;

        private Number mMin;
        public Number Min { get { return mMin; } }

        private Number mMax;
        public Number Max { get { return mMax; } }

        private bool mIntOnly;
        public bool IntOnly { get { return mIntOnly; } }

        public FieldNumberModel(string fieldName) : this(fieldName, "0") { }

       
        public FieldNumberModel(string fieldName, string optValue, string optMin = null, string optMax = null, bool optIntOnly = false) : base(fieldName)
        {
            string valueToParse = !string.IsNullOrEmpty(optValue) ? optValue : "0";
            if (Number.TryParse(valueToParse, out mNumber))
            {
                if (mNumber.IsNaN)
                {
                    mNumber = new Number(0);
                }
            }
            else
            {
                mNumber = new Number(0);
                Debug.LogWarning($"FieldNumberModel: Could not parse value '{valueToParse}', defaulting to 0.");
            }

          
            this.SetValueDirect(mNumber.ToString()); 

            if (!string.IsNullOrEmpty(optMin))
            {
                if (Number.TryParse(optMin, out mMin))
                {
                    if (mMin.IsNaN)
                    {
                        mMin = Number.MinValue;
                        Debug.LogWarning($"FieldNumberModel: Parsed min value '{optMin}' resulted in NaN, using Number.MinValue.");
                    }
                }
                else
                {
                    mMin = Number.MinValue;
                    Debug.LogWarning($"FieldNumberModel: Could not parse min value '{optMin}', using Number.MinValue.");
                }
            }
            else
            {
                mMin = Number.MinValue;
            }

        
            if (!string.IsNullOrEmpty(optMax))
            {
                if (Number.TryParse(optMax, out mMax))
                {
                    if (mMax.IsNaN)
                    {
                        mMax = Number.MaxValue;
                        Debug.LogWarning($"FieldNumberModel: Parsed max value '{optMax}' resulted in NaN, using Number.MaxValue.");
                    }
                }
                else
                {
                    mMax = Number.MaxValue;
                    Debug.LogWarning($"FieldNumberModel: Could not parse max value '{optMax}', using Number.MaxValue.");
                }
            }
            else
            {
                mMax = Number.MaxValue;
            }

            mIntOnly = optIntOnly;

            
            SetValue(CallValidator(GetValue()));
        }

      
        public FieldNumberModel(string fieldName, Number optValue, Number optMin, Number optMax, bool optIntOnly = false) : base(fieldName)
        {
            mNumber = optValue.IsNaN ? new Number(0) : optValue;
            this.SetValue(mNumber.ToString());

            mMin = optMin.IsNaN ? Number.MinValue : optMin;
            mMax = optMax.IsNaN ? Number.MaxValue : optMax;
            mIntOnly = optIntOnly;
            SetValue(CallValidator(GetValue()));
        }
      
        protected override string ClassValidator(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
       
                return null;
            }
   
            string preparedText = text.Replace(",", ""); 

            Number testNumber; 
            if (!Number.TryParse(preparedText, out testNumber))
            {
               
                return null;
            }

         
            if (testNumber.IsNaN)
            {
                return null;
            }

            if (mIntOnly)
            {
                try
                {
                    
                    int intValue = (int)testNumber;
                    testNumber = new Number(intValue); 
                }
                catch (OverflowException)
                {
                  
                    Debug.LogWarning($"ClassValidator: Input '{text}' is numerically valid but outside the range of Int32 for an integer-only field.");
                    return null;
                }
                
            }

           
            if (mMin.IsNaN || mMax.IsNaN)
            {
                Debug.LogError("ClassValidator: Cannot validate number, Min or Max limit is NaN.");
               
                return null; 
            }
            if (mMin > mMax)
            {
                Debug.LogError("ClassValidator: Min limit cannot be greater than Max limit.");
                return null; 
            }


            testNumber = Number.Clamp(testNumber, mMin, mMax);

            if (testNumber.IsNaN)
            {
                Debug.LogError("ClassValidator: Clamping resulted in NaN. This might indicate an issue with Clamp implementation or limits.");
                return null;
            }

            mNumber = testNumber;
            return mNumber.ToString(); 
        }

        public override void SetValue(string newValue)
        {
            string validatedValue = CallValidator(newValue);
            
            if (validatedValue != null)
            {
                base.SetValue(validatedValue);
            }
            
        }

  
        private void SetValueDirect(string value)
        {
            base.SetValue(value); 
        }

    }
}//Fin clase FieldNumberModel


