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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine; 


public class FieldDropdownModel : FieldModel
{

    public struct FieldDropdownMenu 
    {
        public string Text; 
        public string Value;      

        public FieldDropdownMenu(string text, string val)
        {
            Text = text;
            Value = val;
        }
    }

    private List<FieldDropdownMenu> m_Options;
    private int m_SelectedIndex = -1; 
    private Func<List<FieldDropdownMenu>> m_OptionsProvider = null;

    [FieldCreator(FieldType = "field_dropdown")]
    private static FieldDropdownModel CreateFromJson(JObject json)
    {
        string fieldName = (json["name"] != null && json["name"].Type == JTokenType.String)
                             ? json["name"].ToString()
                             : "FIELDNAME_DROPDOWN";

        FieldDropdownModel dropdown = new FieldDropdownModel(fieldName);
        List<FieldDropdownMenu> options = new List<FieldDropdownMenu>();

        if (json["options"] != null && json["options"].Type == JTokenType.Array)
        {
            foreach (var item in json["options"].Children()) 
            {
                if (item is JArray pair && pair.Count == 2 && pair[0].Type == JTokenType.String && pair[1].Type == JTokenType.String)
                {
                    options.Add(new FieldDropdownMenu(pair[0].ToString(), pair[1].ToString()));
                }
                else
                {
                    Debug.LogWarning($"Invalid option format in JSON for dropdown '{fieldName}'. Expected [String, String]. Found: {item.ToString()}");
                }
            }
        }
        dropdown.SetOptionsInternal(options); 
        
        string initialValue = json["value"]?.ToString();
        if (!string.IsNullOrEmpty(initialValue))
        {
            dropdown.SetValue(initialValue); 
        }

        return dropdown;
    }

    public FieldDropdownModel(string fieldName) : base(fieldName)
    {
        m_Options = new List<FieldDropdownMenu>();
        mText = "...";
    }

    
    public List<FieldDropdownMenu> GetOptions()
    {
        return GenerateOptions();
    }

    
    protected virtual List<FieldDropdownMenu> GenerateOptions()
    {
            return new List<FieldDropdownMenu>(m_Options);
    }

   
    public void SetOptions(List<FieldDropdownMenu> newOptions)
    {
        if (newOptions == null)
        {
            m_Options = new List<FieldDropdownMenu>();
        }
        else
        {
            m_Options = newOptions;
        }

        string currentValue = (m_SelectedIndex >= 0 && m_SelectedIndex < m_Options.Count) ? m_Options[m_SelectedIndex].Value : null;
        m_SelectedIndex = -1; 

        if (currentValue != null && SetValueInternal(currentValue)) 
        {
        }
        else if (m_Options.Count > 0)
        {
            SetValueInternal(m_Options[0].Value); 
        }
        else
        {
            mText = "..."; 
            FireUpdate(null);
        }
      
        OptionsChanged?.Invoke(); 
    }
  
    protected void SetOptionsInternal(List<FieldDropdownMenu> newOptions)
    {
        m_Options = newOptions ?? new List<FieldDropdownMenu>();

        string currentValue = GetValue(); 
        m_SelectedIndex = -1; 

        if (currentValue != null && FindAndSetIndexByValue(currentValue))
        {
        }
        else if (m_Options.Count > 0)
        {
            FindAndSetIndexByValue(m_Options[0].Value);
        }
        else
        {
            mText = "...";
        }

       
        FireOptionsChanged();
    }

  
    protected void FireOptionsChanged()
    {
        OptionsChanged?.Invoke();
    }

    protected bool FindAndSetIndexByValue(string valueToFind)
    {
        m_Options = GetOptions(); 

        for (int i = 0; i < m_Options.Count; i++)
        {
            if (string.Equals(m_Options[i].Value, valueToFind))
            {
                if (m_SelectedIndex != i) 
                {
                    mText = m_Options[i].Text; 
                }
                m_SelectedIndex = i;
                return true;
            }
        }
        return false;
    }


    public void SetOptionsProvider(Func<List<FieldDropdownMenu>> provider)
    {
        m_OptionsProvider = provider;
        UpdateOptionsFromProvider();
    }

   
    public void UpdateOptionsFromProvider()
    {
        if (m_OptionsProvider == null) return; 

        string previousValue = GetValue(); 
        List<FieldDropdownMenu> newOptions = m_OptionsProvider();

       
        m_Options = newOptions ?? new List<FieldDropdownMenu>();
        m_SelectedIndex = -1;

        if (previousValue != null && SetValueInternal(previousValue))
        {
          
        }
        else if (m_Options.Count > 0)
        {
            SetValueInternal(m_Options[0].Value); 
        }
        else
        {
            mText = "..."; 
        }
        OptionsChanged?.Invoke();
      
    }
    public event Action OptionsChanged; 

  
  
    public override string GetValue()
    {
        if (IsValidIndex(m_SelectedIndex))
        {
            return m_Options[m_SelectedIndex].Value;
        }
        return null; 
    }

    public override string GetText()
    {
        if (IsValidIndex(m_SelectedIndex))
        {
            return m_Options[m_SelectedIndex].Text;
        }
        return mText ?? "..."; 
    }

   
    protected bool IsValidIndex(int index)
    {
        return index >= 0 && index < m_Options.Count;
    }


    public override void SetValue(string newValue)
    {
        string oldValue = GetValue();

      
        // if (string.IsNullOrEmpty(newValue)) newValue = (m_Options.Count > 0) ? m_Options[0].Value : null; 

        if (string.Equals(oldValue, newValue))
            return; 

        if (FindAndSetIndexByValue(newValue)) 
        {
            
            string newText = GetText(); 
          
            // base.SetText(newText);

            FireUpdate(newValue); 
            SourceBlock?.OnModelChange(this); 
        }
        else
        {
            
            Debug.LogWarning($"Value '{newValue}' not found in options for field '{Name}'. Field state unchanged.");
          
        }
    }

    
    private bool SetValueInternal(string newValue)
    {
        for (int i = 0; i < m_Options.Count; i++)
        {
            if (string.Equals(m_Options[i].Value, newValue))
            {
                m_SelectedIndex = i;
                mText = m_Options[i].Text; 
                return true; 
            }
        }

        m_SelectedIndex = -1;
        mText = newValue ?? "..."; 
        // Debug.LogWarning($"Value '{newValue}' not found in options for field '{Name}'.");
        return false; 
    }

   
    public virtual void OnItemSelected(int selectedIndex) 
    {
         var options = GetOptions(); 
        if (selectedIndex >= 0 && selectedIndex < options.Count)
        {
                SetValue(options[selectedIndex].Value);
        } else {
             Debug.LogWarning($"FieldDropdown '{Name}': Invalid item index selected: {selectedIndex}");
        }
    }


    public override void Dispose()
    {
        OptionsChanged = null; 
        base.Dispose();
    }
}//Fin de la clase FieldDropdownModel