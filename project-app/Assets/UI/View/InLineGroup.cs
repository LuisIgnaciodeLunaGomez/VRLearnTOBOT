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

    public class InLineGroup : BaseView 
    {

    [SerializeField] private float m_ReservedStartX;
    [SerializeField] private RectTransform m_ViewTransform; //Transform de la vista

    public override ViewType Type
    {
        get { return ViewType.LineGroup; }
    }

    protected override Vector2 CalculateSize()
    {
        Vector2 size = Vector2.zero;
        for (int i = 0; i < Childs.Count; i++)
        {
            size.x = Mathf.Max(size.x, Childs[i].Size.x);
            size.y += Childs[i].Size.y;
        }
        return size;
    }
  

    public void UpdateAlignRight(float width)
    {
        if (Mathf.Approximately(this.Width, width))
            return;
        this.Width = width;

        Debug.Log($"UpdateAlignRight -> Ancho: {this.Width}, Último hijo: {LastChild.XY}");


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

