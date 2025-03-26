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
 * Versión: 1.0.1
 * 
 * Descripción: 
 * 
 */


using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEngine.UIElements;

    public class InLineGroupView : BaseView 
    {

    private float m_ReservedStartX =0f; // Espacio reservado para elementos adicionales 
    private RectTransform m_ViewTransform; //Transform de la vista

    public override ViewType Type => ViewType.LineGroup;

    // Márgenes calculados 
      //Punto de inicio de los hijos dentro del bloque(posición relativa)
    public override Vector2 ChildStartXY => new Vector2(m_MarginLeft, -m_MarginTop);
   
    
    public override Vector2 CalculatedSize
    {

        get
        {
            Vector2 size = Vector2.zero;
            HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
            /*for (int i = 0; i < Childs.Count; i++)
            {
                size.x = Mathf.Max(size.x, Childs[i].Size.x);
                size.y += Childs[i].Size.y;
            }*/

            // Iterar sobre los transform hijos del HorizontalLayoutGroup
            foreach (Transform child in transform)
            {
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    Vector2 childSize = childRect.sizeDelta;
                    if (childSize == Vector2.zero)
                    {
                        // Si sizeDelta es cero, intentar obtener el tamaño preferido del contenido
                        LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                        if (layoutElement != null)
                        {
                            childSize = new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
                        }
                    }
                    size.x += childSize.x;
                    size.y = Mathf.Max(size.y, childSize.y);
                 //   Debug.Log($"Child {child.name} size: {childSize}");
                }
            }
            if (layout != null)
            {
                size.x += layout.padding.left + layout.padding.right;
                size.x += (Childs.Count - 1) * layout.spacing;
                size.y += layout.padding.top + layout.padding.bottom;
            }

            if (size.x == 0) size.x = 100f; // Tamaño mínimo
            if (size.y == 0) size.y = 50f; // Tamaño mínimo
          //  Debug.Log($"InLineGroup CalculateSize: {size}");
            return size;
        }
    }

    protected override Vector2 CalculateSize()
    {
        return CalculatedSize;
    }
    public void UpdateAlignRight(float width)
    {
        if (Mathf.Approximately(this.Width, width))
            return;
        this.Width = width;

     //   Debug.Log($"UpdateAlignRight -> Ancho: {this.Width}, Último hijo: {LastChild.XY}");


        ConnectionInputView conView = ((InputView)LastChild).GetConnectionView();
        if (conView != null && conView.ConnectionIViewType== ConnectionInputViewType.Statement)
        {
            conView.Width = width - (LastChild.XY.x + conView.XY.x);
        }
        else
        {
            float startX = this.XY.x + width;
            for (int i = Childs.Count - 1; i >= 0; i--)
            {
                InputView inputView = Childs[i] as InputView;
                if (i < Childs.Count - 1)
                    startX -= BlockViewSettings.Get().ContentSpace.x;
                startX -= inputView.Width;
                inputView.XY = new Vector2(startX, inputView.XY.y);
            }
        }
    }

    public Vector2 GetDrawSize()
    {
        
        Vector2 size = Size;
        ConnectionInputView conView = ((InputView)LastChild).GetConnectionView();
        if (conView != null && !conView.IsSlot)
            size.x -= conView.Width;
        return size;
    }

    #region Internos

    private bool HasSlotConnection()
    {
        foreach (var child in Childs)
        {
            if (child is InputView inputView)
            {
                var con = inputView.GetConnectionView();
                if (con != null && con.IsSlot) return true;
            }
        }
        return false;
    }

    private bool ApplyRightMargin()
    {
        if (Childs.Count == 0) return false;
        var lastInput = Childs[Childs.Count - 1] as InputView;
        if (lastInput == null) return false;

        var conView = lastInput.GetConnectionView();
        return conView == null || conView.IsSlot; // solo aplicamos margen si es un slot o no tiene conexión
    }

    #endregion

/**
 * Descripción: Permite reservar espacio al principio de una línea de inputs
 * */
public float ReservedStartX
{
    get { return m_ReservedStartX; }
    set { m_ReservedStartX = value; }
}

    /**
     * Descripción: Calcula el margen izquierdo total de la línea, teniendo en cuenta el espacio reservado
     */
    private float m_MarginLeft => BlockViewSettings.Get().ContentMargin.left + m_ReservedStartX;

    /**
     * Descripción: Calcula el margen derecho total de la línea, teniendo en cuenta el espacio reservado
     */
    private float mMarginRight
    {
        get
        {
            if (Childs == null || Childs.Count == 0)
                return 0;

            bool applyMargin = true;

            InputView inputView = Childs[Childs.Count - 1] as InputView;
            if (inputView != null)
            {
                ConnectionInputView conView = inputView.GetConnectionView();
                if (conView != null && !conView.IsSlot)
                    applyMargin = false;
            }
            return applyMargin ? BlockViewSettings.Get().ContentMargin.right : 0;
        }
    }

    private float m_MarginTop
    {
        get
        {
            if (Childs == null || Childs.Count == 0)
                return 0;

            for (int i = 0; i < Childs.Count; i++)
            {
                InputView inputView = Childs[i] as InputView;
                if (inputView != null)
                {
                    ConnectionInputView conView = inputView.GetConnectionView();
                    if (conView != null && conView.IsSlot)
                        return BlockViewSettings.Get().ContentMargin.top;
                }
            }
            return 0;
        }
    }

    private float m_MarginBottom
    {
        get
        {
            if (Childs == null || Childs.Count == 0)
                return 0;

            for (int i = 0; i < Childs.Count; i++)
            {
                InputView inputView = Childs[i] as InputView;
                if (inputView != null)
                {
                    ConnectionInputView conView = inputView.GetConnectionView();
                    if (conView != null && conView.IsSlot)
                        return BlockViewSettings.Get().ContentMargin.bottom;
                }
            }
            return 0;
        }
    }


}

