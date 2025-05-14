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

    private float mMarginLeft
    {
        get { return BlockViewSettings.Get().ContentMargin.left + m_ReservedStartX; }
    }

    private float mMarginRight
    {
        get
        {
            if (ChildViews  == null || ChildViews .Count == 0)
                return 0;

            bool applyMargin = true;

            InputView inputView = ChildViews [ChildViews .Count - 1] as InputView;
            if (inputView != null)
            {
                ConnectionInputView conView = inputView.GetConnectionView();
                if (conView != null && !conView.IsSlot)
                    applyMargin = false;
            }
            return applyMargin ? BlockViewSettings.Get().ContentMargin.right : 0;
        }
    }

    private float mMarginTop
    {
        get
        {
            if (ChildViews  == null || ChildViews .Count == 0)
                return 0;

            for (int i = 0; i < ChildViews .Count; i++)
            {
                InputView inputView = ChildViews [i] as InputView;
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

    private float mMarginBottom
    {
        get
        {
            if (ChildViews  == null || ChildViews .Count == 0)
                return 0;

            for (int i = 0; i < ChildViews .Count; i++)
            {
                InputView inputView = ChildViews [i] as InputView;
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

    public override Vector2 ChildStartXY
    {
        get { return new Vector2(mMarginLeft, -mMarginTop); }
    }

    protected override Vector2 CalculateSize()
    {
        Vector2 size = Vector2.zero;
        for (int i = 0; i < ChildViews .Count; i++)
        {
            if (i == ChildViews .Count - 1)
                size.x = ChildViews [i].XY.x + ChildViews [i].Width;

            size.y = Mathf.Max(size.y, ChildViews [i].Height);
        }

        size.x += mMarginRight;
        size.y += mMarginTop + mMarginBottom;
        return size;
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
                            startX -= (BlockViewSettings.Instance?.ContentSpace.x ?? 2f); 
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
                    startX -= BlockViewSettings.Get().ContentSpace.x;
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
    }

    public  RectTransform GetRectTransform()
    {
        if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
        return m_RectTransform;
    }
}//fin clase LineGroupView