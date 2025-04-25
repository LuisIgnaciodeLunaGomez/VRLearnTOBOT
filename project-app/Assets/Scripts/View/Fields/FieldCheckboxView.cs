/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 03/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;
using UnityEngine.UI; 

public class FieldCheckboxView : FieldView 
{
    [Header("UI Reference")]
    [Tooltip("Assign the Toggle UI component from the prefab in the Inspector.")]
    [SerializeField] protected Toggle m_Toggle; 

    protected FieldCheckboxModel CheckboxModel => FieldModel as FieldCheckboxModel; 

    protected  override void InitializeView() 
    {
        base.InitializeView(); 
        if (m_Toggle == null)
        {
            m_Toggle = GetComponentInChildren<Toggle>(true); 
            if (m_Toggle == null)
                Debug.LogError($"FieldCheckboxView ({gameObject.name}): UI Toggle component not found or not assigned in prefab!", this);
        }
    }

    protected  void OnBindModel()
    {
        if (m_Toggle == null) return; 

        if (CheckboxModel == null)
        {
            Debug.LogError($"FieldCheckboxView ({gameObject.name}): The bound model is not a FieldCheckboxModel! (Actual Type: {FieldModel?.GetType()})", this);
            m_Toggle.isOn = false; 
            m_Toggle.interactable = false; 
            return;
        }

         m_Toggle.onValueChanged.RemoveListener(OnUiToggleChanged);
        m_Toggle.isOn = CheckboxModel.IsChecked;
        m_Toggle.onValueChanged.AddListener(OnUiToggleChanged); 

        m_Toggle.interactable = FieldModel.IsEditable;

        // CheckboxModel.AddObserver(OnModelValueChangedCallback);
    }

    protected  void OnUnBindModel() 
    {
        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.RemoveListener(OnUiToggleChanged);
        }

       
        // CheckboxModel?.RemoveObserver(OnModelValueChangedCallback);
    }

    protected virtual void OnUiToggleChanged(bool uiIsOn)
    {
        if (CheckboxModel != null && CheckboxModel.IsChecked != uiIsOn)
        {
            CheckboxModel.IsChecked = uiIsOn;
            MarkDirty();
        }
    }

   
    /*
    protected virtual void OnModelValueChangedCallback(string modelValue) 
    {
        if (m_Toggle == null || CheckboxModel == null) return;

        bool modelIsChecked = CheckboxModel.IsChecked; // Obtener estado bool actual del modelo

        // Si el estado de la UI es diferente al del modelo...
        if (m_Toggle.isOn != modelIsChecked)
        {
            // actualizar la UI SIN disparar el callback OnUiToggleChanged.
            m_Toggle.onValueChanged.RemoveListener(OnUiToggleChanged);
            m_Toggle.isOn = modelIsChecked;
            m_Toggle.onValueChanged.AddListener(OnUiToggleChanged);
        }
    }
    */

    protected override Vector2 CalculateSize()
    {
        if (m_Toggle != null)
        {
            RectTransform toggleRect = m_Toggle.GetComponent<RectTransform>();
            if (toggleRect != null)
            {
                return new Vector2(
                    Mathf.Max(toggleRect.rect.width, BlockViewSettings.Get().MinUnitSize.x),
                    Mathf.Max(toggleRect.rect.height, BlockViewSettings.Get().MinUnitSize.y)
                );
            }
        }
        return BlockViewSettings.Get().MinUnitSize; 
    }

    protected override void OnValueChanged(string newValue)
    {
        throw new System.NotImplementedException();
    }

    protected override void RegisterInputListeners()
    {
        throw new System.NotImplementedException();
    }
}//Fin clase FiedlCheckboxView