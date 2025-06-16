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
    [SerializeField] private List<BaseView> m_ChildViews = new List<BaseView>(); // Hijos lógicos directos de esta vista
    public BlockView ParentBlockView { get; protected set; }


    [HideInInspector] public BaseView ParentView { get; set; } // El padre lo establecerá al configurar los hijos

    [Header("Identificador de Definición")]
    [Tooltip("El 'name' del <Arg> en el XML que este componente visual representa.")]
    public string DefinitionName;

    [Header("Posición de Diseño (Leída en Awake)")]
    [SerializeField] private Vector2 m_PrefabAnchoredPosition;

    // --- NUEVAS VARIABLES GLOBALES PARA DEBUG ---
    public static Dictionary<GameObject, Vector2> PrefabPositions = new Dictionary<GameObject, Vector2>();

    public bool IsDirty { get; protected set; } = true;

    /// <summary>
    /// Identifica el tipo de vista (Block, Field, Input, etc.). Implementado por clases hijas.
    /// </summary>
    public abstract ViewType Type { get; }
    public static bool IsInManualLayoutUpdate { get; protected set; } = false;

    /// <summary>
    /// El RectTransform de esta vista. Obtenido automáticamente.
    /// </summary>
    public RectTransform ViewTransform => m_ViewTransform;

    //Propiedades de la jerarquia
    //public BaseView ParentView => m_ParentView;
    public BaseView PreviousView => m_PreviousView;
    public BaseView NextView => m_NextView;
    public List<BaseView> ChildViews => m_ChildViews; //Acceso público a los hijos lógicos
    public bool HasChildren => m_ChildViews != null && m_ChildViews.Count > 0;
    public BaseView FirstChild => HasChildren ? m_ChildViews[0] : null;
    public BaseView LastChild => HasChildren ? m_ChildViews[m_ChildViews.Count - 1] : null;
    public int SiblingIndex => m_ParentView != null ? m_ParentView.m_ChildViews.IndexOf(this) : -1;
 
    private BlockView m_CachedAncestorBlockView = null;
    private bool m_SearchingForBlockView = false;
    public RectTransform M_ViewTransform_Para_Debug => m_ViewTransform;


    protected virtual void InitializeView()
    {

        m_ViewTransform = GetComponent<RectTransform>();

        // ¡CRÍTICO! Nos aseguramos de que todos los RectTransform usen el mismo sistema de anclaje/pivote.
        // Esto previene desplazamientos inesperados.
        if (m_ViewTransform != null && (m_ViewTransform.anchorMin != new Vector2(0, 1) || m_ViewTransform.anchorMax != new Vector2(0, 1) || m_ViewTransform.pivot != new Vector2(0, 1)))
        {
            m_ViewTransform.anchorMin = new Vector2(0, 1); // Anclar a la esquina superior izquierda
            m_ViewTransform.anchorMax = new Vector2(0, 1);
            m_ViewTransform.pivot = new Vector2(0, 1);     // El punto de pivote es la esquina superior izquierda
            m_ViewTransform.anchoredPosition = Vector2.zero; // Resetear la posición relativa al anclaje
        }

        // Limpiamos la jerarquía lógica anterior.
        m_ChildViews.Clear();

        // CONSTRUIMOS la jerarquía lógica a partir de la jerarquía VISUAL del prefab.
        // Esto es simple y robusto: lo que ves en el Editor es lo que el código procesará.
        if (transform == null) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform childTransform = transform.GetChild(i);
            // Solo consideramos hijos ACTIVOS para el layout inicial.
            if (childTransform.gameObject.activeSelf)
            {
                BaseView childView = childTransform.GetComponent<BaseView>();
                if (childView != null)
                {
                    // Establecemos las relaciones padre-hijo y hermano-hermano.
                    if (m_ChildViews.Count > 0)
                    {
                        BaseView previousChild = m_ChildViews[m_ChildViews.Count - 1];
                        previousChild.m_NextView = childView;
                        childView.m_PreviousView = previousChild;
                    }
                    m_ChildViews.Add(childView);
                    childView.m_ParentView = this;
                }
            }
        }
        /* m_ViewTransform = GetComponent<RectTransform>();

         // Configuración para el pivot top-left que necesitamos
         if (m_ViewTransform.anchorMin != new Vector2(0, 1) || m_ViewTransform.anchorMax != new Vector2(0, 1) || m_ViewTransform.pivot != new Vector2(0, 1))
         {
             m_ViewTransform.anchorMin = new Vector2(0, 1);
             m_ViewTransform.anchorMax = new Vector2(0, 1);
             m_ViewTransform.pivot = new Vector2(0, 1);
             m_ViewTransform.anchoredPosition = Vector2.zero; // Resetear posición
         }

         m_ParentView = null;
         m_PreviousView = null;
         m_NextView = null;
         m_ChildViews.Clear();

         // NUEVA LÓGICA: Construimos la jerarquía lógica a partir de la jerarquía de Transforms
         for (int i = 0; i < transform.childCount; i++)
         {
             BaseView childView = transform.GetChild(i).GetComponent<BaseView>();
             if (childView != null)
             {
                 // Lógica para añadir y enlazar hermanos
                 if (m_ChildViews.Count > 0)
                 {
                     BaseView previousChild = m_ChildViews[m_ChildViews.Count - 1];
                     previousChild.m_NextView = childView;
                     childView.m_PreviousView = previousChild;
                 }
                 m_ChildViews.Add(childView);
                 childView.m_ParentView = this;
             }
         }*/
    }
    
    /// <summary>
    /// Método interno para añadir un hijo a la lista m_ChildViews
    /// y establecer SU m_ParentView referencia sin manipular Transforms
    /// o disparar MarkDirty/OnXYUpdated de la manera completa de AddChild.
    /// Se usa para poblar la jerarquia LÓGICA al inicializar desde prefab.
    /// </summary>
    private void InternalAddLogicalChildReference(BaseView childView)
    {
        if (childView == null || childView == this || m_ChildViews.Contains(childView))
        {
            // Debug.LogWarning("Skipping adding null, self, or already present child.", this.gameObject);
            return; 
        }

        int index = m_ChildViews.Count;
        m_ChildViews.Insert(index, childView);

      
        childView.m_ParentView = this;

       
    }

    protected virtual void Awake()
    {
        InitComponents();
        /* Logger.Log($"BaseView ({gameObject.name}): Awake, calling InitializeView().", this.gameObject);
         InitializeView();

         //  GUARDAMOS LA POSICIÓN DEL PREFAB 

         m_ViewTransform = GetComponent<RectTransform>();

         // --- GUARDAMOS LA POSICIÓN INICIAL ---
        // PrefabPositions[this.gameObject] = m_ViewTransform.anchoredPosition;

         // Asignar el padre a los hijos definidos en el inspector
         foreach (var child in m_ChildViews)
         {
             if (child != null)
             {
                 child.ParentView = this;
             }
         }*/
        // Leemos la anchoredPosition que tiene en el editor JUSTO al despertar.
        /* if (m_ViewTransform != null)
         {
             m_PrefabAnchoredPosition = m_ViewTransform.anchoredPosition;
         }

         if (m_ViewTransform == null)
         {
             Debug.LogError($"BaseView ({gameObject.name}): ViewTransform IS NULL AFTER InitializeView()!", this.gameObject);
         }
         else
         {
             //Debug.Log($"BaseView ({gameObject.name}): Awake END. ViewTransform IS assigned.", this.gameObject); // Si está asignado
         }*/


    }

    protected virtual void Start()
    {
        // En Start(), las posiciones del prefab están garantizadas.
        m_PrefabAnchoredPosition = m_ViewTransform.anchoredPosition;
        string logColor = this is BlockView ? "yellow" : (this is LineGroupView ? "green" : (this is InputView ? "orange" : "cyan"));
        Debug.Log($"<color={logColor}><b>[PREFAB READ]</b></color> en '{gameObject.name}': Posición leída en Start -> <b>{m_PrefabAnchoredPosition.ToString("F2")}</b>", gameObject);
    }


    public virtual void InitComponents()
    {
        m_ViewTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Método para acceder a la posición del prefab desde otras clases o scripts.
    /// </summary>
    /// <returns></returns>

    public Vector2 GetPrefabPosition() => m_PrefabAnchoredPosition;

    /// <summary>
    /// Posición local de la esquina superior izquierda (anchor/pivot 0,1).
    /// Establecer este valor llama a OnXYUpdated.
    /// </summary>    
    public Vector2 XY
    {
        get => m_ViewTransform.anchoredPosition;
        set
        {
           // Debug.Log($"BaseView::XY Setter Called for {gameObject.name}. New Value: ({value.x:F2}, {value.y:F2}). Current Value: ({m_ViewTransform?.anchoredPosition.x:F2}, {m_ViewTransform?.anchoredPosition.y:F2})", this.gameObject);

            if (m_ViewTransform != null && m_ViewTransform.anchoredPosition != value)
            {
              //  Debug.Log($"BaseView::XY Setter for {gameObject.name}: Position is DIFFERENT, calling OnXYUpdated().", this.gameObject); 

                m_ViewTransform.anchoredPosition = value;
                OnXYUpdated(); // Notifico que la posición cambió
            }
            else if (m_ViewTransform != null && m_ViewTransform.anchoredPosition == value)
            {
               // Debug.Log($"BaseView::XY Setter for {gameObject.name}: Position is SAME, NOT calling OnXYUpdated().", this.gameObject); 
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

    /// <summary>
    /// Sube por la jerarquía de vistas hasta encontrar el BlockView raíz
    /// y lo devuelve. Devuelve null si no está dentro de un BlockView.
    /// </summary>
    public BlockView SourceBlockView
    {
        get
        {
            BaseView currentView = this;
            while (currentView != null)
            {
                // Si la vista actual es un BlockView, la hemos encontrado.
                if (currentView is BlockView blockView)
                {
                    return blockView;
                }
                // Si no, subimos al padre y seguimos buscando.
                currentView = currentView.m_ParentView;
            }
            // Si llegamos aquí, no estamos dentro de un bloque.
            return null;
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
            if (this is BlockView && BlockViewSettings.Instance != null) // Comprobamos que no sea null
            {
                // Ahora se accede a través del Singleton Instance
                return new Vector2(BlockViewSettings.Instance.BlockInternalPadding.left,
                                 -BlockViewSettings.Instance.BlockInternalPadding.top);
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
       // Debug.Log($"BaseView::OnXYUpdated START for {gameObject.name}. XY: ({XY.x:F2}, {XY.y:F2}). Parent: {ParentView?.gameObject.name ?? "None"}", this.gameObject);
      //  Debug.Log($"BaseView::OnXYUpdated for {gameObject.name}: ChildViews Count: {ChildViews.Count}. HasChildren: {HasChildren}", this.gameObject);
        if (HasChildren)
        {
            //Se inicializa la propagación de lsde la clase base al resto de clases hijas
            foreach (var child in ChildViews.Where(c => c != null))
            {
                bool childIsActive = child.gameObject?.activeInHierarchy ?? false;
                //Debug.Log($"BaseView::OnXYUpdated propagating to child {child.gameObject.name} (Type: {child.Type}). Child Active In Hierarchy: {childIsActive}", this.gameObject);
                if (childIsActive) // <<< Asegurarse de que el hijo esté activo en la jerarquia para propagar movimiento
                {
                    child.OnXYUpdated(); // Llama recursiva solo si activo
                }
                else
                {
                    Debug.LogWarning($"BaseView::OnXYUpdated NOT propagating to {child.gameObject.name} - NOT active in hierarchy!", this.gameObject);
                }
            }
        }

        else
        {
           // Debug.Log("No hay hijos en " + gameObject.name);
        }
       // Debug.Log($"BaseView::OnXYUpdated END for {gameObject.name}.", this.gameObject);
    }

    /// <summary>
    /// Callback llamado cuando el Size de esta vista ha cambiado.
    /// Marca como sucio para recalcular el layout del ancestro.
    /// Los subtipos pueden añadir lógica aquí.
    /// </summary>
    protected internal virtual void OnSizeUpdated()
    {
        if (IsInManualLayoutUpdate)
        {
            return;
        }
        // Si el tamaño cambia, es probable que el layout padre necesite recalcular.
        MarkDirty();
    }

    /// <summary>
    /// Actualiza la posición y tamaño de esta vista Y DE SUS DESCENDIENTES, propagando el layout manualmente.
    /// 'startXY' es la posición donde ESTA vista debe comenzar su layout (esquina sup-izq).
    /// Este es el NÚCLEO del sistema de layout manual estilo UBlockly.
    /// </summary>
    /// 
   // public abstract void UpdateLayout(Vector2 startPos);
    public virtual void UpdateLayout(Vector2 startXY)
    {
        // <<< DEBUG >>>
        Logger.Log($"Frame {Time.frameCount}: <b>[{GetType().Name}.UpdateLayout]</b> en '{gameObject.name}' con startXY = {startXY.ToString("F2")}", gameObject);

        // 1. Me posiciono donde mi padre me indica.
        this.XY = startXY;

        // 2.Los hijos deben implementar su propia lógica de layout.

        // 3. FINALMENTE, cuando todos los hijos ya han sido posicionados y han calculado su propio tamaño,
        // el padre calcula su tamaño final basado en el de sus hijos.
        this.Size = CalculateSize();

        // En el caso del BlockView, después de tener su tamaño, dibuja el fondo.
        if (this is BlockView blockView)
        {
            blockView.ApplyVisualAppearance();
        }
    }

    public void AddChild(BaseView childView, int index = -1)
    {
        //  1. VALIDACIONES Y DEPURACIÓN INICIAL 
        /*  if (this.GetComponent<RectTransform>() == null)
          {
              Debug.LogError($"[BaseView.AddChild] ¡El PADRE '{this.gameObject.name}' no tiene RectTransform!", this.gameObject);
              return;
          }
          if (childView.GetComponent<RectTransform>() == null)
          {
              Debug.LogError($"[BaseView.AddChild] ¡El HIJO '{childView.gameObject.name}' no tiene RectTransform!", childView.gameObject);
              return;
          }

          Debug.Log($"---> BaseView.AddChild on {this.gameObject.name} ({this.GetType()}). Received child: {childView?.name}. Target Visual Parent will be: {this.ViewTransform.name}");
          if (childView == null) return;
          if (childView == this) { Debug.LogError($"BaseView ({gameObject.name}): Cannot add self as child!"); return; }
          if (m_ChildViews.Contains(childView))
          {
              Debug.LogWarning($"BaseView ({gameObject.name}): Already contains child {childView.gameObject.name}.", this.gameObject);
              return;
          }

          // Debug.Log($"BaseView ({gameObject.name}): Attempting to add child {childView.gameObject.name} (Type: {childView.Type}) at index {index}.", this.gameObject);
          //  2. GESTIÓN DE LA JERARQUÍA LÓGICA Y VISUAL 

          // Desvincular del padre lógico anterior si lo tuviera

          childView.m_ParentView?.RemoveChild(childView); // Desvincular del padre lógico anterior

          // Manipular la jerarquía visual de Unity 


          // Establecer el nuevo padre lógico de la vista hija
          childView.m_ParentView = this;
          childView.ParentBlockView = (this is BlockView blockView) ? blockView : this.ParentBlockView;

          // Manipular la jerarquía lógica de vistas 
          index = Mathf.Clamp(index, 0, m_ChildViews.Count);
          //BaseView prevSibling = (index > 0) ? m_ChildViews[index - 1] : null;
          //BaseView nextSibling = (index < m_ChildViews.Count) ? m_ChildViews[index] : null;

          m_ChildViews.Insert(index, childView);

          childView.m_ParentView = this;
          //  childView.m_PreviousView = prevSibling;
          //  childView.m_NextView = nextSibling;

          //   if (prevSibling != null) prevSibling.m_NextView = childView;
          //   if (nextSibling != null) nextSibling.m_PreviousView = childView;

          if (this.ViewTransform != null)
          {
              childView.ViewTransform.SetParent(this.ViewTransform, false); // false es mejor para UI, evita cálculos de posición raros
              childView.ViewTransform.SetSiblingIndex(index); // Mantenemos la jerarquía visual y lógica sincronizadas
          }
          else
          {
              Debug.LogError($"BaseView ({gameObject.name}): No se pudo establecer el padre visual para {childView.gameObject.name} porque ViewTransform del padre es nulo.", this.gameObject);
          }

          // Notificar para re-layout 
          MarkDirty();

        //  Debug.Log($"BaseView ({gameObject.name}): Successfully added child {childView.gameObject.name}. ChildViews Count: {m_ChildViews.Count}", this.gameObject);*/

        if (childView == null || m_ChildViews.Contains(childView)) return;

        // Establecer la relación lógica
        childView.m_ParentView = this;
        m_ChildViews.Add(childView);

        // ¡La propagación clave que arregla el error del SourceBlockView!
        childView.ParentBlockView = (this is BlockView blockView) ? blockView : this.ParentBlockView;

        // El padre visual ya se establece en el builder
    }

    public void RemoveChild(BaseView childView)
    {
        if (childView != null && m_ChildViews.Contains(childView))
        {
            childView.m_ParentView = null;
            m_ChildViews.Remove(childView);
        }
        /*  if (childView == null) return;
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

          MarkDirty(); */
    }

    // Marca la vista y propaga hacia arriba para que el BlockView se recalcule en LateUpdate.
    public virtual void MarkDirty()
    {

        IsDirty = true;
        // La propagación al BlockView se hace más explícita
        if (ParentView != null)
        {
            ParentView.MarkDirty();
        }
        if (this is BlockView blockView)
        {
            blockView.NotifyLayoutDirty();
        }

        // Buscamos el BlockView padre más cercano
        /* BlockView ancestorBlock = FindAncestorBlockView();
         if (ancestorBlock != null)
         {
             // <<< DEBUG >>>
             if (!ancestorBlock.LayoutISDirty) // Solo loguear la primera vez que se ensucia
             {
                 Logger.Log($"<color=#FFA500><b>[DIRTY]</b></color> Frame {Time.frameCount}: Objeto '{gameObject.name}' ha ensuciado el layout del Bloque Ancestro '{ancestorBlock.name}'.", ancestorBlock.gameObject);
             }
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
         }*/
    }

    protected virtual void OnDestroy()
    {
        // Debug.Log($"BaseView ({gameObject.name}) OnDestroy.", this);

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


