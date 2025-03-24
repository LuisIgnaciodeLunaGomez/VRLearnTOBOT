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
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */


using UnityEngine;
using UnityEngine.UI;

    public class InLineGroup : BaseView 
    {

    [SerializeField] private float m_ReservedStartX;
    [SerializeField] private RectTransform m_ViewTransform; //Transform de la vista

    public override ViewType Type => ViewType.LineGroup;
    
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

}

