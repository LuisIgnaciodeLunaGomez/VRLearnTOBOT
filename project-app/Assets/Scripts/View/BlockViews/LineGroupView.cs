/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System.Linq;
using UnityEngine;
public class LineGroupView : BaseView
{
    [SerializeField] private float m_ReservedStartX;
    private RectTransform m_RectTransform;
    public override ViewType Type
    {
        get { return ViewType.LineGroup; }
    }

    public float ReservedStartX
    {
        get { return m_ReservedStartX; }
        set { m_ReservedStartX = value; }
    }

    private float m_MarginLeft
    {
        get { return BlockViewSettings.Instance.LineGroupPadding.left + m_ReservedStartX; }
    }

    private float m_MarginRight
    {
        get
        {
            if (!HasChildren) return 0;

            bool applyMargin = true;
            if (ChildViews.LastOrDefault() is InputView inputView)
            {
                var conView = inputView.GetConnectionView();
                if (conView != null && !conView.IsSlot) applyMargin = false;
            }

            // Usamos el padding derecho
            return applyMargin ? BlockViewSettings.Instance.LineGroupPadding.right : 0;
        }
    }

    private float m_MarginTop
    {  // Usamos el padding superior
        get { return BlockViewSettings.Instance.LineGroupPadding.top; }

    }

    private float m_MarginBottom
    {
        // Usamos el padding inferior
        get { return BlockViewSettings.Instance.LineGroupPadding.bottom; }
    }

    /* public override Vector2 ChildStartXY
     {
         get { return new Vector2(mMarginLeft, -mMarginTop); }
     }*/

    public override Vector2 ChildStartXY
    {
        // El primer hijo empieza después del padding
        get { return new Vector2(m_MarginLeft, -m_MarginTop); }
    }
    protected override Vector2 CalculateSize()
    {
        if (BlockViewSettings.Instance == null) return Vector2.zero; // Comprobación de seguridad
        if (!HasChildren) return Vector2.zero;

        float totalWidth = 0;
        float maxHeight = 0;

        var activeChildren = ChildViews.Where(c => c != null && c.gameObject.activeSelf).ToList();

        // Suma el ancho de todos los InputViews hijos + el espaciado entre ellos
        for (int i = 0; i < activeChildren.Count; i++)
        {
            totalWidth += activeChildren[i].Width;
            if (i < activeChildren.Count - 1)
            {
                totalWidth += BlockViewSettings.Instance.HorizontalElementSpacing;
            }
            maxHeight = Mathf.Max(maxHeight, activeChildren[i].Height);
        }

        // Finalmente, añade el padding propio del LineGroup.
        totalWidth += BlockViewSettings.Instance.LineGroupPadding.horizontal;
        maxHeight += BlockViewSettings.Instance.LineGroupPadding.vertical;

        return new Vector2(totalWidth, maxHeight);
    }

    public void UpdateAlignRight(float width)
    {
        if (Mathf.Approximately(this.Width, width))
            return;
        this.Width = width;

        BaseView lastBaseChild = this.LastChild; 
        if (!(lastBaseChild is InputView lastInputView)) return; 

        ConnectionView conView = lastInputView.GetConnectionView();

        if (conView != null) 
        {
           
            if (conView.ConnectionType == EConnection.NextStatement) 
            {
                
                float connectionStartX = lastInputView.XY.x + conView.XY.x; 
                conView.Width = Mathf.Max(0, width - connectionStartX); 
                Debug.Log($"Updating Statement Connection width in AlignRight to: {conView.Width}");

            }
            else 
            {
                     float startX = this.XY.x + width; 
                for (int i = ChildViews.Count - 1; i >= 0; i--)
                {
                    if (ChildViews[i] is InputView inputView) 
                    {
                        if (i < ChildViews.Count - 1)
                        {
                            startX -= BlockViewSettings.Instance.HorizontalElementSpacing;
                        }
                        startX -= inputView.Width; 
                        inputView.XY = new Vector2(startX, inputView.XY.y); 
                    }
                }
            }
        }
        else
        {
            float startX = this.XY.x + width;
            for (int i = ChildViews .Count - 1; i >= 0; i--)
            {
                InputView inputView = ChildViews [i] as InputView;
                if (i < ChildViews .Count - 1)
                    startX -= BlockViewSettings.Instance.HorizontalElementSpacing;
                startX -= inputView.Width;
                inputView.XY = new Vector2(startX, inputView.XY.y);
            }
        }
    }
    protected internal override void OnXYUpdated()
    {
       // Debug.Log($"LineGroupView::OnXYUpdated calling base.OnXYUpdated().", this.gameObject);
    }

    public Vector2 GetDrawSize()
    {
        // Esta función es crucial. Oculta el fondo detrás de bloques de valor conectados.
        Vector2 size = this.Size;
        var conView = (LastChild as InputView)?.GetConnectionView();
        if (conView != null && !conView.IsSlot)
            size.x -= conView.Width;
        return size;
    }

    /// <summary>
    /// Posiciona sus hijos (InputViews) horizontalmente y calcula su propio tamaño.
    /// </summary>
    public override void UpdateLayout(Vector2 startPos)
    {
        // 1. Me posiciono donde me indica mi padre (el BlockView).
        this.XY = startPos;
        Debug.Log($"<color=green><b>[LineGroupView.UpdateLayout]</b></color> en '{gameObject.name}': Posicionado en {startPos:F2}", gameObject);

        // 2. Organizo a mis hijos (los Inputs) horizontalmente.
        if (HasChildren)
        {
            // CORRECCIÓN CLAVE: El hijo empieza en la esquina local + el padding
            Vector2 currentChildPos = new Vector2(BlockViewSettings.Instance.LineGroupPadding.left, -BlockViewSettings.Instance.LineGroupPadding.top);

            foreach (BaseView childInput in ChildViews.Where(c => c != null && c.gameObject.activeSelf))
            {
                childInput.UpdateLayout(currentChildPos);
                // CORREGIDO: Usamos HorizontalElementSpacing para el espacio entre hijos.
                currentChildPos.x += childInput.Width + BlockViewSettings.Instance.HorizontalElementSpacing;
            }
        }

        // 3. Calculo mi tamaño final.
        this.Size = CalculateSize();
        Debug.Log($"<color=green><b>[LineGroupView.UpdateLayout]</b></color> en '{gameObject.name}': Layout completado. Tamaño final: {this.Size:F2}", gameObject);
    }



    /*
    public Vector2 GetDrawSize()
    {

        Vector2 size = Size; // Obtener el tamaño calculado previamente

        // Hacer el cast de forma segura
        InputView lastInputView = LastChild as InputView;

        // Comprobar si el último hijo era realmente un InputView
        if (lastInputView != null)
        {
            // Si lo era, intentar obtener su vista de conexión
            ConnectionInputView conView = lastInputView.GetConnectionView();

            // Si tiene una conexión y no es un slot, ajustar el tamaño
            if (conView != null && !conView.IsSlot)
            {
                size.x -= conView.Width;
            }
        }
        return size;
    }*/

    public RectTransform GetRectTransform()
    {
        if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
        return m_RectTransform;
    }
}//fin clase LineGroupView