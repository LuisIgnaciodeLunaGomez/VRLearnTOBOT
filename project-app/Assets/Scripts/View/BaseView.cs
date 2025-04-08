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
 * Versión: 2.0.0 
 * 
 * Descripción: Clase abstracta que contiene la lógica de las vistas de los bloques de la que heredan todas las vistas de bloques
 */

using System.Collections.Generic;
using UnityEngine;

public abstract class BaseView : MonoBehaviour
{
    [SerializeField] protected RectTransform m_ViewTransform;
    [SerializeField] private BaseView m_ParentView; 
    [SerializeField] private BaseView m_PreviousView; 
    [SerializeField] private BaseView m_NextView; 
    [SerializeField] private List<BaseView> m_ChildViews = new List<BaseView>();

    public abstract ViewType Type { get; } 
    protected abstract Vector2 CalculateSize(); 
    protected internal virtual void OnSizeUpdated() {
        MarkDirty(); 
    }
    protected internal virtual void OnXYUpdated(){ }

    public List<BaseView> ChildViews => m_ChildViews;

    public BaseView LastChild
    {
        get
        {
         
            return (ChildViews != null && ChildViews.Count > 0) ? ChildViews[ChildViews.Count - 1] : null;
        }
    }

    private BlockView m_CachedAncestorBlockView = null;
    private bool m_SearchingForBlockView = false;

    public RectTransform ViewTransform => m_ViewTransform;
    public BaseView ParentView => m_ParentView;
    public BaseView PreviousView => m_PreviousView;
    public BaseView NextView => m_NextView;
    public bool HasChildren => m_ChildViews.Count > 0;
    public BaseView FirstChildView => HasChildren ? m_ChildViews[0] : null;
    public BaseView LastChildView => HasChildren ? m_ChildViews[m_ChildViews.Count - 1] : null;
    public int SiblingIndex => m_ParentView != null ? m_ParentView.m_ChildViews.IndexOf(this) : -1;
    protected virtual void InitializeView()
    {
        m_ViewTransform = GetComponent<RectTransform>();
        if (m_ViewTransform == null)
        {
            Debug.LogError($"BaseView: RectTransform not found on {gameObject.name}. Adding one.");
            m_ViewTransform = gameObject.AddComponent<RectTransform>();
            // Configuro anchors/pivot por defecto
            m_ViewTransform.anchorMin = new Vector2(0, 1);
            m_ViewTransform.anchorMax = new Vector2(0, 1);
            m_ViewTransform.pivot = new Vector2(0, 1);    
        }
        // Reseteo listas/refs 
        m_ParentView = null;
        m_PreviousView = null;
        m_NextView = null;
        m_ChildViews.Clear();
    }
    protected virtual void Awake()
    {
        InitializeView(); 
    }

    // Tamaño y Posición 
    public Vector2 XY
    {
        get => m_ViewTransform.anchoredPosition;
        set
        {
            if (m_ViewTransform != null && m_ViewTransform.anchoredPosition != value)
            {
                m_ViewTransform.anchoredPosition = value;
                OnXYUpdated(); // Notifico que la posición cambió
            }
            else if (m_ViewTransform == null)
            {
                Debug.LogError($"BaseView ({gameObject.name}): ViewTransform is null in XY setter.");
            }
        }
    }
    public Vector2 Size
    {
        get => m_ViewTransform != null ? m_ViewTransform.rect.size : Vector2.zero;
        set
        {
            if (m_ViewTransform == null) { Debug.LogError($"BaseView ({gameObject.name}): ViewTransform is null in Size setter."); return; }

            bool changed = false;
            if (!Mathf.Approximately(m_ViewTransform.rect.width, value.x))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                changed = true;
            }
            if (!Mathf.Approximately(m_ViewTransform.rect.height, value.y))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
                changed = true;
            }
            if (changed)
                OnSizeUpdated(); // Notifico que el tamaño cambió
        }
    }
    public float Width { get => Size.x; set => Size = new Vector2(value, Size.y); }
    public float Height { get => Size.y; set => Size = new Vector2(Size.x, value); }


    // Posiciones Relativas para Layout 
    // Posición inicial donde el primer hijo DEBERÍA empezar DENTRO de esta vista
    public virtual Vector2 ChildStartXY => Vector2.zero; // Default: Top-left sin margen

    // Posición inicial donde ESTA vista DEBERÍA empezar, relativa a su hermano anterior o al inicio del padre
    public Vector2 HeaderXY => m_PreviousView == null ? (m_ParentView?.ChildStartXY ?? Vector2.zero) : Vector2.zero;

    //  Gestión de Jerarquía Visual 
    public void AddChildView(BaseView childView, int index = -1) 
    {
        Debug.LogWarning($"--> AddChildView called on parent '{this.gameObject.name}' to add child '{childView?.gameObject?.name}'. Parent Child Count Before: {m_ChildViews.Count}", this.gameObject);
        if (m_ChildViews.Contains(childView)) return;
        if (childView == this) { Debug.LogError("Cannot add self as child!"); return; } 
        index = (index >= 0 && index <= m_ChildViews.Count) ? index : m_ChildViews.Count;

        //  Gestión Enlazada Hermanos -Previous/Next
        BaseView prevSibling = (index > 0) ? m_ChildViews[index - 1] : null;
        BaseView nextSibling = (index < m_ChildViews.Count) ? m_ChildViews[index] : null;

        // Desconectar el que estaba antes en 'index' de su previo
        if (nextSibling != null) nextSibling.m_PreviousView = null;
        // Desconectar 'childView' de su entorno anterior (si lo tuviera)
        childView.m_ParentView?.RemoveChildView(childView); // Si ya tenía padre, quitarlo
        if (childView.m_PreviousView != null) childView.m_PreviousView.m_NextView = childView.m_NextView; // Puentea el anterior
        if (childView.m_NextView != null) childView.m_NextView.m_PreviousView = childView.m_PreviousView; // Puentea el siguiente

        Debug.Log($"   - AddChildView: Inserting '{childView.gameObject.name}' into m_ChildViews at index {index}. Current Count BEFORE Insert: {m_ChildViews.Count}", this.gameObject);

        //  Añadir a la Lista y Configurar Jerarquía Unity 
        m_ChildViews.Insert(index, childView);

        Debug.Log($"   - AddChildView: Inserted? Current Count AFTER Insert: {m_ChildViews.Count}. Child just added should be at [{index}]: {m_ChildViews[index]?.gameObject.name}", this.gameObject);

        childView.m_ParentView = this;
        /* if (childView.ViewTransform.parent != this.ViewTransform) 
             childView.ViewTransform.SetParent(this.ViewTransform, false); // 'false' para mantener pos local
         childView.ViewTransform.SetSiblingIndex(index);*/

        if (childView.ViewTransform == null) Debug.LogError($"Child '{childView.gameObject.name}' has null ViewTransform after insert!", childView.gameObject);
        // if (this.ViewTransform == null) Debug.LogError($"Parent '{this.gameObject.name}' has null ViewTransform!", this.gameObject); 

        if (childView.ViewTransform != null && this.ViewTransform != null && childView.ViewTransform.parent != this.ViewTransform)
        {
            childView.ViewTransform.SetParent(this.ViewTransform, false);
            Debug.Log($"   - AddChildView: Set parent transform for '{childView.gameObject.name}' to '{this.gameObject.name}'.");
        }
        if (childView.ViewTransform != null) childView.ViewTransform.SetSiblingIndex(index);
        //  Reconectar Hermanos 
        // Conectar con previo
        childView.m_PreviousView = prevSibling;
        if (prevSibling != null) prevSibling.m_NextView = childView;
        // Conectar con siguiente
        childView.m_NextView = nextSibling;
        if (nextSibling != null) nextSibling.m_PreviousView = childView;

        Debug.Log($"--> AddChildView FINISHED on parent '{this.gameObject.name}' for child '{childView.gameObject.name}'. Parent Child Count Final: {m_ChildViews.Count}", this.gameObject);
    }

    public void RemoveChildView(BaseView childView) 
    {
        if (!m_ChildViews.Contains(childView)) return;

        BaseView prevSibling = childView.m_PreviousView;
        BaseView nextSibling = childView.m_NextView;

        // Quitar de la lista y desvincular padre
        m_ChildViews.Remove(childView);
        childView.m_ParentView = null;

        // Reconectar hermanos que quedaron
        if (prevSibling != null) prevSibling.m_NextView = nextSibling;
        if (nextSibling != null) nextSibling.m_PreviousView = prevSibling;

        // Limpiar refs del hijo quitado
        childView.m_PreviousView = null;
        childView.m_NextView = null;
    }

    /**
    * Actualiza la posición y tamaño de esta vista y propaga la actualización.
    * ¡OJO MVC!: Esta lógica es PURA de la Vista. Calcula cómo deben posicionarse
    * las vistas hermanas y padres en la UI basándose en tamaños.
    * NO debe interactuar con el Modelo directamente aquí.
    */
    public virtual void UpdateLayout(Vector2 startXY)
    {
        this.XY = startXY;

        //Calcular y establecer el tamaño de esta vista
        this.Size = CalculateSize();

        //Posicionar a los hijos recursivamente
        if (HasChildren) 
        {
            Vector2 currentChildXY = this.XY + this.ChildStartXY; 

            BlockViewSettings settings = BlockViewSettings.Instance; 
            if (settings == null)
            {
                Debug.LogError("BlockViewSettings is null in UpdateLayout!");
            }

             for (int i = 0; i < ChildViews.Count; i++)
            {
                BaseView child = ChildViews[i];

                child.UpdateLayout(currentChildXY); 

                 currentChildXY.y -= child.Size.y; 

                if (settings != null && i < ChildViews.Count - 1)
                {
                    currentChildXY.y -= settings.ContentSpace.y;
                }
            }
          
        }

        UpdateRectTransform();
    }

    
    protected virtual void UpdateRectTransform()
    {
        if (m_ViewTransform == null)
            m_ViewTransform = GetComponent<RectTransform>();
        if (m_ViewTransform == null) return; 
        m_ViewTransform.pivot = new Vector2(0, 1);
        m_ViewTransform.anchorMin = new Vector2(0, 1);
        m_ViewTransform.anchorMax = new Vector2(0, 1);

           m_ViewTransform.anchoredPosition = new Vector2(this.XY.x, -this.XY.y); 

        m_ViewTransform.sizeDelta = this.Size;
    }

  
    public BaseView GetTopmostChild(bool untilBlock = true) 
    {
        BaseView curView = this;
        while (curView.HasChildren && (!untilBlock || curView.m_ChildViews[0].Type != ViewType.Block))
        {
            curView = curView.m_ChildViews[0];
        }
        return curView;
    }

    public void OnDestroy()
    {
        m_ParentView?.RemoveChildView(this);

        m_ParentView = null;
        m_PreviousView = null;
        m_NextView = null;
        
        if (m_ChildViews != null)
            m_ChildViews.Clear();
    }

    /**
    *Marca esta vista y sus ancestros relevantes como necesitados de una actualización de layout.
    * Propaga la señal hacia arriba hasta encontrar el BlockView responsable.
    */
    public virtual void MarkDirty()
    {
        BlockView ancestorBlock = FindAncestorBlockView();

        if (ancestorBlock != null)
        {
            ancestorBlock.NotifyLayoutDirty();
        }
        else if (!m_SearchingForBlockView) 
        {
            Debug.LogWarning($"BaseView ({gameObject.name}): Could not find an ancestor BlockView to mark dirty.", this);
        }

       
    }

    /**
     * Método para encontrar el BlockView ancestro.
     * Usa una caché simple para evitar búsquedas repetidas.
     */
    protected BlockView FindAncestorBlockView()
    {
        if (m_CachedAncestorBlockView != null)
        {
            return m_CachedAncestorBlockView;
        }

        if (m_SearchingForBlockView)
        {
            return null;
        }

        m_SearchingForBlockView = true; 

        BaseView current = this;
        while (current != null)
        {
           
            if (current is BlockView blockView)
            {
                m_CachedAncestorBlockView = blockView; 
                m_SearchingForBlockView = false; 
                return blockView;
            }
            current = current.m_ParentView; 
        }

        m_SearchingForBlockView = false; 
        return null;
    }
}// Fin de la clase BaseView


