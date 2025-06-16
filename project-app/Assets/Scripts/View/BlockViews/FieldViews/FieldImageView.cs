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

//[RequireComponent(typeof(Image))] 
//[RequireComponent(typeof(LayoutElement))]
public class FieldImageView : FieldView
{
    private Image m_Image;
    private LayoutElement m_LayoutElement;
    private Vector2 m_ImageSize = new Vector2(30, 30); 

    protected override void InitializeView()
    {
        base.InitializeView();
        m_Image = GetComponent<Image>();
        m_LayoutElement = GetComponent<LayoutElement>(); // Puede ser null

        // Protegemos la asignación
        if (m_LayoutElement != null)
        {
            m_LayoutElement.ignoreLayout = false;
        }

        if (m_Image == null) Debug.LogError("FieldImageView requires an Image component.");
        m_Image.preserveAspect = true;
    }

    protected override Vector2 CalculateSize()
    {
        // Comprobación de seguridad
        if (BlockViewSettings.Instance == null)
        {
            return m_ImageSize; // Devolvemos el tamaño base si no hay settings.
        }

        // Obtenemos los paddings horizontal y vertical del RectOffset correspondiente.
        float horizontalPadding = BlockViewSettings.Instance.FieldInputTextPadding.horizontal; // .horizontal suma .left y .right
        float verticalPadding = BlockViewSettings.Instance.FieldInputTextPadding.vertical;     // .vertical suma .top y .bottom

        Vector2 finalSize = m_ImageSize + new Vector2(horizontalPadding, verticalPadding);

        // Protegemos el acceso al LayoutElement opcional.
        if (m_LayoutElement != null)
        {
            m_LayoutElement.preferredWidth = finalSize.x;
            m_LayoutElement.preferredHeight = finalSize.y;
        }

        return finalSize;
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

    public override void UpdateLayout(Vector2 startPos)
    {
        this.XY = startPos;
        this.Size = CalculateSize();
    }
}//Fin clase FieldImageView