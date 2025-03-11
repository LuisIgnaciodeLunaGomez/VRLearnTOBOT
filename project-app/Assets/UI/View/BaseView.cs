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
using System.Collections.Generic;

using UnityEngine;


public abstract class BaseView : MonoBehaviour
{
    [SerializeField] private RectTransform m_ViewTransform; //Transform de la vista
    [SerializeField] private BaseView m_Parent; //Vista padre
    [SerializeField] private BaseView m_Previous; //Vista anterior
    [SerializeField] private BaseView m_Next; //Vista siguiente
    [SerializeField] private List<BaseView> m_Childs = new List<BaseView>();
   
    public abstract ViewType Type { get; }
    protected internal virtual void OnSizeUpdated() { }

    protected abstract Vector2 CalculateSize();

    public float Width
    {
        get { return m_ViewTransform.rect.width; }
        set
        {
            if (!Mathf.Approximately(m_ViewTransform.rect.width, value))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
                if (Application.isPlaying)
                    OnSizeUpdated();
            }
        }
    }

    public float Height
    {
        get { return m_ViewTransform.rect.height; }
        set
        {
            if (!Mathf.Approximately(m_ViewTransform.rect.height, value))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
                if (Application.isPlaying)
                    OnSizeUpdated();
            }
        }
    }

    public Vector2 HeaderXY
    {
        get { return m_Parent != null ? m_Parent.ChildStartXY : Vector2.zero; }
    }

    public virtual Vector2 ChildStartXY
    {
        get { return Vector2.zero; }
    }

    public List<BaseView> Childs

    {

        get { return m_Childs; }

    }

    public int SiblingIndex
    {
        get { return m_Parent != null ? m_Parent.m_Childs.IndexOf(this) : -1; }
    }

    public bool HasChild()
    {
        return m_Childs.Count > 0;
    }

    public BaseView LastChild
    {
        get { return HasChild() ? m_Childs[m_Childs.Count - 1] : null; }
    }
    public Vector2 XY
    {
        get { return m_ViewTransform.anchoredPosition; }
        set
        {
            if (m_ViewTransform.anchoredPosition != value)
            {
                m_ViewTransform.anchoredPosition = value;
                OnXYUpdated();
            }
        }
    }

    public Vector2 Size
    {
        get { return m_ViewTransform.rect.size; }
        set
        {
            bool changed = false;
            if (!Mathf.Approximately(m_ViewTransform.rect.width, value.x))
            {
                changed = true;
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
            }
            if (!Mathf.Approximately(m_ViewTransform.rect.height, value.y))
            {
                changed = true;
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
            }
            if (changed)
                OnSizeUpdated();
        }
    }

    protected internal virtual void OnXYUpdated()
    {
        Childs.ForEach(child => child.OnXYUpdated());
    }

    public BaseView GetTopmostChild(bool untilConnection = true)
    {
        BaseView curView = this;
        while (curView.HasChild() && curView.m_Childs[0].Type != ViewType.Block)
        {
            curView = curView.m_Childs[0];
        }
        return curView;
    }
    public void UpdateLayout(Vector2 startPos)
    {
        XY = startPos;
        Size = CalculateSize();

        switch (Type)
        {
            case ViewType.Field:
            case ViewType.Input:
            case ViewType.ConnectionInput:
            case ViewType.LineGroup:
                {
                    if (m_Next == null /*|| (!changePos && !changeSize)*/)
                    {
                        //reach the last child, or no change in current hierarchy, update it's parent view
                        m_Parent.UpdateLayout(m_Parent.SiblingIndex == 0 ? m_Parent.HeaderXY : m_Parent.XY);
                    }
                    else
                    {
                        //update next
                        if (Type != ViewType.LineGroup)
                        {
                            // same line
                            startPos.x += Size.x + BlockViewSettings.Get().ContentSpace.x;
                        }
                        else
                        {
                            // start a new line
                            startPos.y -= Size.y + BlockViewSettings.Get().ContentSpace.y;
                        }

                        BaseView topmostChild = m_Next.GetTopmostChild();
                        if (topmostChild != m_Next)
                        {
                            //need to update from its topmost child
                            m_Next.XY = startPos;
                            topmostChild.UpdateLayout(topmostChild.HeaderXY);
                        }
                        else
                        {
                            m_Next.UpdateLayout(startPos);
                        }
                    }
                    break;
                }
            case ViewType.Connection:
            case ViewType.Block:

                {
                    m_Parent.UpdateLayout(m_Parent.SiblingIndex == 0 ? m_Parent.HeaderXY : m_Parent.XY);
                }
                break;
        }
    }
}


