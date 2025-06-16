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
using UnityEngine.EventSystems; 

public class FieldColorView : FieldView, IPointerClickHandler 
{
    [Header("UI References")]
    [SerializeField] private Image m_ColorDisplayImage; 

       protected FieldColour FieldColourModel => FieldModel as FieldColour;


    protected override void Awake() 
    {
        base.Awake();
        if (m_ColorDisplayImage == null)
        {
            m_ColorDisplayImage = GetComponentInChildren<Image>(); 
            if (m_ColorDisplayImage == null)
                Debug.LogError($"FieldColorView ({gameObject.name}): ColorDisplayImage (Image) component not found or assigned!", this);
        }
    }

    protected  void OnBindModel()
    {
        UpdateColorDisplay();

       }

    protected  void OnUnBindModel()
    {
      }

    private void UpdateColorDisplay()
    {
        if (m_ColorDisplayImage == null) return;

        if (FieldColourModel == null)
        {
            
            string hexValueBase = FieldModel?.GetValue();
            if (ColorUtility.TryParseHtmlString(hexValueBase, out Color colorFromBase))
            {
                m_ColorDisplayImage.color = colorFromBase;
                Debug.LogWarning($"FieldColorView bound to non-FieldColour model ({FieldModel?.GetType()}), using GetValue(): {hexValueBase}", this);
            }
            else
            {
                Debug.LogError($"FieldColorView ({gameObject.name}): Model is not a compatible Color Field! (Type: {FieldModel?.GetType()}) Cannot parse value: '{hexValueBase}'", this);
                m_ColorDisplayImage.color = Color.magenta; 
            }
            return;
        }


        string hexValue = FieldColourModel.GetValue(); 
        if (ColorUtility.TryParseHtmlString(hexValue, out Color color))
        {
            m_ColorDisplayImage.color = color; 
        }
        else
        {
            Debug.LogError($"FieldColorView ({gameObject.name}): Could not parse color value '{hexValue}' from model.", this);
            m_ColorDisplayImage.color = Color.grey; 
        }
    }

    /*
    protected virtual void OnModelValueChanged()
    {
        UpdateColorDisplay();
    }
    */

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!FieldModel.IsEditable) return; 

        Debug.Log($"FieldColorView clicked: {FieldModel?.Name}");

        
    }

    private void OnColorSelected(string newHexColor)
    {
        if (FieldColourModel != null && FieldColourModel.GetValue() != newHexColor)
        {
            string validatedHex = FieldColourModel.CallValidator(newHexColor);
            if (validatedHex != null)
            {
                FieldColourModel.SetValue(validatedHex); // Actualiza el MODELO
                UpdateColorDisplay(); // Actualiza la UI inmediatamente
                MarkDirty(); // Notifica al BlockView
            }
        }
    }

    protected override Vector2 CalculateSize()
    {
        // Comprobación de seguridad
        if (BlockViewSettings.Instance == null)
        {
            Debug.LogWarning("BlockViewSettings no encontrado. Usando tamaño por defecto para FieldColorView.");
            return new Vector2(36f, 24f); // Devolvemos un valor por defecto si no hay settings
        }

        // Ahora que sabemos que Instance no es null, podemos acceder a la propiedad de forma segura.
        return BlockViewSettings.Instance.FieldColorSize;
    }

    protected override void OnValueChanged(string newValue)
    {
        throw new System.NotImplementedException();
    }

    protected override void RegisterInputListeners()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateLayout(Vector2 startPos)
    {
        this.XY = startPos;
        this.Size = CalculateSize();
    }
}//Fin FieldColorView