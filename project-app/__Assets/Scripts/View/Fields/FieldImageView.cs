/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using UnityEngine;
using UnityEngine.UI; 

[RequireComponent(typeof(Image))] 
[RequireComponent(typeof(LayoutElement))]
public class FieldImageView : FieldView
{
    private Image m_Image;
    private LayoutElement m_LayoutElement;
    private Vector2 m_ImageSize = new Vector2(30, 30); 

    protected override void InitializeView()
    {
        base.InitializeView();
        m_Image = GetComponent<Image>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_LayoutElement.ignoreLayout = false;

        if (m_Image == null) Debug.LogError("FieldImageView requires an Image component.");

        m_Image.preserveAspect = true; 
    }

    protected override Vector2 CalculateSize()
    {
        Vector2 size = m_ImageSize + new Vector2(BlockViewSettings.Instance.FieldHorizontalPadding * 2, BlockViewSettings.Instance.FieldVerticalPadding * 2);
        m_LayoutElement.preferredWidth = size.x;
        m_LayoutElement.preferredHeight = size.y;
        return size;
    }

    protected override void OnValueChanged(string newIconName)
    {
        if (m_Image != null)
        {
            if (string.IsNullOrEmpty(newIconName))
            {
                m_Image.enabled = false; 
                m_Image.sprite = null;
            }
            else
            {
                Sprite sprite = Resources.Load<Sprite>($"Icons/{newIconName}");
                if (sprite != null)
                {
                    m_Image.enabled = true;
                    m_Image.sprite = sprite;
                    
                    // Vector2 newViewSize = CalculateSize();
                    // Size = newViewSize;
                    // ParentView?.UpdateLayout();
                }
                else
                {
                    Debug.LogWarning($"Icon sprite not found: Icons/{newIconName}");
                    m_Image.enabled = false;
                    m_Image.sprite = null;
                }
            }
        }
    }

   
    protected override void RegisterInputListeners()
    {
        // No hacer nada
    }
}//Fin clase FieldImageView