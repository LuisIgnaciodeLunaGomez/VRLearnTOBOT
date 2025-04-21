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
 * Descripción: Clase abstracta que contiene la lógica de las vistas de los bloques de la que heredan todas las vistas de bloques Gestiona la estructura visual padres/jios/hermanos
 * la posciión y el tamaño básico (RectTransform) de cada vista. Además gestionar la interfaz para el sistema de layout manual. No lleva a cabo interación directa con el modelo de datos.
 * Siguiendo el patrón MVC.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseView : MonoBehaviour
{
    [SerializeField] protected RectTransform m_ViewTransform;  
    [SerializeField] private BaseView m_ParentView;  // Padre lógico en la jerarquía de la vista
    [SerializeField] private BaseView m_PreviousView;  //Hermano anterior lógico
    [SerializeField] private BaseView m_NextView;  //Hermano siguiente lógico
    [SerializeField] private List<BaseView> m_ChildViews = new List<BaseView>(); // Hijos lógicos

    /// <summary>
    /// Identifica el tipo de vista (Block, Field, Input, etc.). Implementado por clases hijas.
    /// </summary>
    public abstract ViewType Type { get; }

    /// <summary>
    /// El RectTransform de esta vista. Obtenido automáticamente.
    /// </summary>
    public RectTransform ViewTransform => m_ViewTransform;

    //Propiedades de la jerarquia
    public BaseView ParentView => m_ParentView;
    public BaseView PreviousView => m_PreviousView;
    public BaseView NextView => m_NextView;
    public List<BaseView> ChildViews => m_ChildViews; //Acceso público a los hijos lógicos
    public bool HasChildren => m_ChildViews != null && m_ChildViews.Count > 0;
    public BaseView FirstChild => HasChildren ? m_ChildViews[0] : null;
    public BaseView LastChild => HasChildren ? m_ChildViews[m_ChildViews.Count - 1] : null;
    public int SiblingIndex => m_ParentView != null ? m_ParentView.m_ChildViews.IndexOf(this) : -1;
 
    private BlockView m_CachedAncestorBlockView = null;
    private bool m_SearchingForBlockView = false;

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

     /// <summary>
    /// Posición local de la esquina superior izquierda (anchor/pivot 0,1).
    /// Establecer este valor llama a OnXYUpdated.
    /// </summary>    
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

    /// <summary>
    /// Tamaño (ancho, alto) de la vista.
    /// Establecer este valor llama a OnSizeUpdated y marca el layout como sucio.
    /// </summary>
    public Vector2 Size
    {
        get => m_ViewTransform != null ? m_ViewTransform.rect.size : Vector2.zero;
        set
        {
            if (m_ViewTransform == null) { Debug.LogError($"BaseView ({gameObject.name}): ViewTransform is null in Size setter."); return; }

            bool changed = false;
            // Comprobar y establecer ancho

            if (!Mathf.Approximately(m_ViewTransform.rect.width, value.x))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                changed = true;
            }
            // Comprobar y establecer ancho

            if (!Mathf.Approximately(m_ViewTransform.rect.height, value.y))
            {
                m_ViewTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
                changed = true;
            }
            // Si cambió, notificar

            if (changed)
                OnSizeUpdated(); // El handler debe marcar como sucio si el cambio afecta al layout
        }
    }

    public float Width { get => Size.x; set => Size = new Vector2(value, Size.y); }
    public float Height { get => Size.y; set => Size = new Vector2(Size.x, value); }


    /// <summary>
    /// Origen local (respecto a this.XY) donde empezaría el primer hijo de esta vista.
    /// Los subtipos pueden override para añadir padding interno.
    /// Usa InternalPadding de BlockViewSettings por defecto si este es un BlockView.
    /// </summary>
    public virtual Vector2 ChildStartXY
    {
        get
        {
            if (this is BlockView && BlockViewSettings.Instance != null)
            {
                // Para BlockView, los hijos empiezan después del padding superior e izquierdo
                return new Vector2(BlockViewSettings.Instance.InternalPadding.x, -BlockViewSettings.Instance.InternalPadding.y);
            }
            return Vector2.zero; // Otros tipos no tienen padding interno por defecto
        }
    }

    /// <summary>
    /// Método abstracto: Las clases hijas deben implementar cómo calcular su tamaño
    /// basándose en su contenido y vistas hijas. Llamado por el sistema de layout manual.
    /// </summary>
    protected abstract Vector2 CalculateSize();

    /// <summary>
    /// Callback llamado cuando el XY de esta vista ha cambiado.
    /// Puede ser usado por clases hijas (como ConnectionView) para actualizar su estado/DB.
    /// Propaga la llamada a los hijos visuales por defecto.
    /// </summary>
    protected internal virtual void OnXYUpdated()
    {
        // REVISAR si los hijos (como ConnectionView) necesitan saber que el padre se movió para actualizar, su posición en el ConnectionDB.
        //foreach(var child in ChildViews.Where(c => c != null)) child.OnXYUpdated(); 
    }

    /// <summary>
    /// Callback llamado cuando el Size de esta vista ha cambiado.
    /// Marca como sucio para recalcular el layout del ancestro.
    /// Los subtipos pueden añadir lógica aquí.
    /// </summary>
    protected internal virtual void OnSizeUpdated()
    {
        // Si el tamaño cambia, es probable que el layout padre necesite recalcular.
        MarkDirty();
    }

    /// <summary>
    /// Actualiza la posición y tamaño de esta vista Y DE SUS DESCENDIENTES, propagando el layout manualmente.
    /// 'startXY' es la posición donde ESTA vista debe comenzar su layout (esquina sup-izq).
    /// Este es el NÚCLEO del sistema de layout manual estilo UBlockly.
    /// </summary>
    public virtual void UpdateLayout(Vector2 startXY)
    {
        // 1.Posicionamiento de la vista
        this.XY = startXY; // Llama a OnXYUpdated internamente

        // 2. Calculamos tamaño vista (inlcuyendo hijos)
        
        this.Size = CalculateSize(); 


        // 3. Posicionamiento recursivo de los hijos)
        if (HasChildren)
        {
            Vector2 currentChildLayoutStart = this.XY + this.ChildStartXY; // Posición inicial para el primer hijo

            var activeChildren = ChildViews.Where(c => c != null && c.gameObject.activeInHierarchy).ToList();
            float accumulatedWidthInLine = 0; // Ancho acumulado para elementos en la misma línea
            float maxItemHeightInLine = 0; // Altura máxima en la línea actual

            for (int i = 0; i < activeChildren.Count; i++)
            {
                BaseView child = activeChildren[i];

                // Llamamos a UpdateLayout del hijo - El hijo hará lo mismo. StartXY es la posición actual acumulada.
                child.UpdateLayout(currentChildLayoutStart); // Lanza el layout del hijo

                // Calculamos la posición de inicio para el *siguiente* hermano
                if (child.Type != ViewType.LineGroup && child.Type != ViewType.Block) 
                {
                    // Elementos en la misma línea horizontal
                    currentChildLayoutStart.x += child.Size.x + (BlockViewSettings.Instance?.HorizontalElementSpacing ?? 0);
                    accumulatedWidthInLine += child.Size.x + (BlockViewSettings.Instance?.HorizontalElementSpacing ?? 0);
                    maxItemHeightInLine = Mathf.Max(maxItemHeightInLine, child.Size.y);
                }
                else 
                {
                    // El siguiente hermano empieza debajo de este hijo (LineGroup), reiniciando X.
                    currentChildLayoutStart.x = this.XY.x + this.ChildStartXY.x; // Reiniciar X
                    currentChildLayoutStart.y -= child.Size.y + (BlockViewSettings.Instance?.VerticalLineSpacing ?? 0); // Bajar Y
                    // Reseteamos acumuladores para la nueva línea
                    accumulatedWidthInLine = 0;
                    maxItemHeightInLine = 0;
                }
            }
        }

    }
   
    public void AddChild(BaseView childView, int index = -1)
    {
        if (childView == null) return;
        if (childView == this) { Debug.LogError($"BaseView ({gameObject.name}): Cannot add self as child!"); return; }
        if (m_ChildViews.Contains(childView)) return;

        childView.m_ParentView?.RemoveChild(childView); // Desvincular del padre lógico anterior

        index = Mathf.Clamp(index, 0, m_ChildViews.Count);
        BaseView prevSibling = (index > 0) ? m_ChildViews[index - 1] : null;
        BaseView nextSibling = (index < m_ChildViews.Count) ? m_ChildViews[index] : null;

        m_ChildViews.Insert(index, childView);

        childView.m_ParentView = this;
        childView.m_PreviousView = prevSibling;
        childView.m_NextView = nextSibling;

        if (prevSibling != null) prevSibling.m_NextView = childView;
        if (nextSibling != null) nextSibling.m_PreviousView = childView;

        MarkDirty(); 
    }

    public void RemoveChild(BaseView childView)
    {
        if (childView == null) return;
        if (!m_ChildViews.Contains(childView)) return;

        BaseView prevSibling = childView.m_PreviousView;
        BaseView nextSibling = childView.m_NextView;

        m_ChildViews.Remove(childView); // Quitar de la lista

        // Reconectar hermanos
        if (prevSibling != null) prevSibling.m_NextView = nextSibling;
        if (nextSibling != null) nextSibling.m_PreviousView = prevSibling;

        // Desvincular hijo
        childView.m_ParentView = null;
        childView.m_PreviousView = null;
        childView.m_NextView = null;

        MarkDirty(); 
    }

    // Marca la vista y propaga hacia arriba para que el BlockView se recalcule en LateUpdate.
    public virtual void MarkDirty()
    {
        // Buscamos el BlockView padre más cercano
        BlockView ancestorBlock = FindAncestorBlockView();
        if (ancestorBlock != null)
        {
            // Notificamos al BlockView que necesita recalcular
            ancestorBlock.NotifyLayoutDirty();
        }

        // Si es un BlockView y se dispara MarkDirty, simplemente activamos el flag
        else if (this is BlockView thisBlockView)
        {
            thisBlockView.NotifyLayoutDirty();
        }
        else
        {
            
            //Debug.LogWarning($"BaseView ({gameObject.name}): Marked dirty but could not find ancestor BlockView.", this);
        }
    }


    protected virtual void OnDestroy()
    {
        // Debug.Log($"BaseView ({gameObject.name}) OnDestroy.", this);
        // Desvincularse del padre LÓGICO al ser destruido físicamente.

        m_ParentView?.RemoveChild(this); // La vista le dice al padre lógico que ya no es su hijo/a

        // Limpiar referencias 
        m_ParentView = null;
        m_PreviousView = null;
        m_NextView = null;
        if (m_ChildViews != null) m_ChildViews.Clear(); // Limpia la lista de referencias
    }

    // Buscamos el BlockView ancestro 
    protected BlockView FindAncestorBlockView()
    {
        if (m_CachedAncestorBlockView != null && m_CachedAncestorBlockView.gameObject != null) return m_CachedAncestorBlockView; // Cache válida

        BaseView current = this; // Empezar desde esta vista 
        while (current != null)
        {
            if (current is BlockView blockView)
            {
                m_CachedAncestorBlockView = blockView;
                return blockView;
            }
            current = current.m_ParentView;
        }
        // Debug.LogWarning($"BaseView ({gameObject.name}): Could not find ancestor BlockView.", this);
        return null;
    }

    // Obtiene todos los hijos de un tipo específico T.
    public List<T> GetChildrenOfType<T>() where T : BaseView
    {
        return m_ChildViews?.OfType<T>().ToList() ?? new List<T>();
    }

    /// <summary>
    /// Ejecuta el layout manual descendente para esta vista y sus hijos.
    /// Calcula el tamaño propio y posiciona a los hijos recursivamente llamando a este mismo método en ellos.
    /// NO propaga el cálculo hacia arriba al padre. El orquestador (BlockView.BuildLayout) inicia esto.
    /// </summary>
    /// <param name="startXY">La posición superior-izquierda donde debe comenzar el layout de ESTA vista.</param>
    protected virtual void ManualLayoutRecursive(Vector2 startXY)
    {
        // 1. Posicionamos esta vista en la ubicación asignada
        this.XY = startXY; // Esto llama a OnXYUpdated si la posición cambia

        // 2. Posicionamos a los hijos recursivamente pasándoles su propia posición de inicio
        if (HasChildren)
        {
            Vector2 currentChildLayoutStart = this.XY + this.ChildStartXY; // Posición inicial para el primer hijo
            var activeChildren = ChildViews.Where(c => c != null && c.gameObject.activeInHierarchy).ToList();

            for (int i = 0; i < activeChildren.Count; i++)
            {
                BaseView child = activeChildren[i];

                // Llamamos recursivamente a el de cada hijo
                child.ManualLayoutRecursive(currentChildLayoutStart);

                // Calculamos dónde empieza el siguiente hijo, basado en cómo terminó el actual
                currentChildLayoutStart = child.LayoutEndXY; 
            }
        }

        // 3. Calculamos y establecemos el tamaño de la vista uan vez que los hijos se hayan posicionado y calculado su propio tamaño.
        this.Size = CalculateSize(); 
                                       
    }

    /// <summary>
    /// Calcula la posición donde TERMINA esta vista en el flujo del layout,
    /// indicando dónde debe empezar el SIGUIENTE hermano.
    /// Implementación por defecto asume flujo horizontal simple. Los subtipos
    /// como LineGroup lo deben override si inician nueva línea.
    /// </summary>
    public virtual Vector2 LayoutEndXY
    {
        get
        {
            Vector2 endPos = this.XY; // Empezamos en la posición de inicio
            endPos.x += this.Width;  // Terminamos en el borde derecho
                                     
            return endPos;
        }
    }
}// Fin de la clase BaseView


