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

    public virtual Vector2 CalculatedSize => CalculateSize();
    protected abstract Vector2 CalculateSize();

    public RectTransform ViewTransform => m_ViewTransform;

    public BaseView Parent => m_Parent;

    public BaseView Previous => m_Previous;

    public BaseView Next => m_Next;

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

    public BaseView FirstChild { get { return HasChild() ? m_Childs[0] : null; } } //Obtiene el primer hijo

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
    /**
        * Descripción: Permite actualizar el layout de la vista y sus hijos, reposicionando y redimensionando las vistas según sea necesario.
        * @param: startPos: Vector2 que indica la posición inicial desde donde se debe comenzar a actualizar el layout.
        */
    public void UpdateLayout(Vector2 startPos)
    {
        XY = startPos; //Se asigna la nueva posición (XY) al RectTransform.anchoredPosition del elemento visual.
        Size = CalculateSize(); //Se calcula el nuevo tamaño (Size) de la vista y se asigna.

        switch (Type) //comportamiento de posicionamiento depende del tipo de vista
        {
            //Caso A: (Field, Input, ConnectionInput, LineGroup)
            case ViewType.Field: //Si es un campo
            case ViewType.Input://Si es un input
            case ViewType.ConnectionInput://Si es una ConnectionInput
            case ViewType.LineGroup:
                {
                    if (m_Next == null /*|| (!changePos && !changeSize)*/)
                    {
                        //reach the last child, or no change in current hierarchy, update it's parent view
                        m_Parent.UpdateLayout(m_Parent.SiblingIndex == 0 ? m_Parent.HeaderXY : m_Parent.XY); //Si no hay más elementos en la línea, o no se requieren ajustes visuales adicionales, actualiza directamente el layout del padre.
                                                                                                             //Esto provoca una actualización recursiva hacia arriba(padres), que continúa ajustando la disposición del bloque completo.
                    }
                    else
                    {
                        //update next
                        if (Type != ViewType.LineGroup)
                        {
                            // same line
                            startPos.x += Size.x + BlockViewSettings.Get().ContentSpace.x; //startPos.x += ... posiciona el siguiente elemento a la derecha, separados por un espacio definido en ContentSpace.x (margen horizontal).
                        }
                        else
                        {
                            // start a new line
                            startPos.y -= Size.y + BlockViewSettings.Get().ContentSpace.y; //startPos.y -= ... posiciona un nuevo grupo visual debajo del actual, separándolos verticalmente por ContentSpace.y.
                        }

                        BaseView topmostChild = m_Next.GetTopmostChild(); //Busca el elemento hijo más profundo del siguiente (m_Next). Esto permite empezar a actualizar desde la vista visual más interna.

                        if (topmostChild != m_Next)
                        {
                            //need to update from its topmost child
                            m_Next.XY = startPos; //Asigna la nueva posición al siguiente (XY) y continúa actualizando su layout de manera recursiva.
                                                  //Es importante para vistas compuestas (por ejemplo, bloques con varios inputs).
                            topmostChild.UpdateLayout(topmostChild.HeaderXY);
                        }
                        else
                        {
                            m_Next.UpdateLayout(startPos);
                        }
                    }
                    break;
                }

            //Caso B: (Connection, Block)
            case ViewType.Connection:
            case ViewType.Block:
                {
                    //Las posiciones relativas de conexiones y bloques están mayormente definidas por la jerarquía visual del propio Unity (RectTransform y sistemas automáticos de Unity UI como layouts).
                    if (m_Parent != null)
                    {
                        m_Parent.UpdateLayout(m_Parent.SiblingIndex == 0 ? m_Parent.HeaderXY : m_Parent.XY); //Se propaga la actualización directamente hacia el padre, que suele ser un bloque contenedor
                    }
                    break;
                }
        }
    }


    void Awake()
    {
       Initcomponent();
    }

    public virtual void Initcomponent()
    {
        m_ViewTransform = GetComponent<RectTransform>();
        if (m_ViewTransform == null)
        {
            Debug.LogError("No RectTransform found in BaseView.");
        }
    }

    /**
       * Descripción: Metodo crítico para montar dinámicamente la estructura visual
       * @param: childView: Vista del bloque hijo
       * @param: index: Indice de la vista del bloque hijo
       * index-1 lo añade al final
       */

    public void AddChild(BaseView childView, int index = -1)
    {
        if (m_Childs.Contains(childView)) //Evita añadir el hijo dos veces si ya esta en la vista
            return;

        index = index >= 0 ? index : m_Childs.Count; //Si el indice es menor a 0 se añade al final - > no se pasa indice

        //1. update previous
        BaseView preView = index > 0 ? m_Childs[index - 1] : null; //Si hay una vista previa index >0 crea una lista doblemente enlazada 

        if (preView != null)
        {
            preView.m_Next = childView; //la siguiente al preview es childView
            childView.m_Previous = preView; // anterior al chieldView es preview
        }

        //2. add iteratively
        BaseView itor = childView; //Crea un iterador con la vista del hijo

        do
        {
            m_Childs.Insert(index, itor);  //insertando el elemento itor (de tipo BaseView) en la lista m_Childs en la posición index, inserta los distintos hijos en orden uno detrás de otro
            itor.m_Parent = this;  //Se le asigna el padre 

            if (itor.ViewTransform.parent != this.ViewTransform)  //Verifica si el RectTransform del itor (el BaseView que se está añadiendo) ya está correctamente asignado como hijo en la jerarquía de Unity.
                itor.ViewTransform.SetParent(this.ViewTransform); //Se asegura que el transform visual esté en la jerarquía correcta
            itor.ViewTransform.SetSiblingIndex(index); //Se fija su índice de orden entre los hermanos

            itor = itor.m_Next; //siguiente hijo
            index++; //incrementa el índice
        } while (itor != null);

        //3. update the final next
        BaseView nextView = m_Childs.Count > index ? m_Childs[index] : null;   //Si el índice index está dentro del rango de la lista m_Childs, entonces asigna a nextView el elemento en la posición index; si no, asigna null.
        if (nextView != null)
        {
            nextView.m_Previous = m_Childs[index - 1]; //Si hay un m_Childs[index] tras el último que añadimos  Conecta el último nuevo hijo (index - 1) con ese siguiente existente.
            m_Childs[index - 1].m_Next = nextView;
        }
    }


    /**
      * Descripción: Es el inverso de AddChild y sirve para elminiar una BaseView y sus next de la jerarquía visual de un bloque.
      * @param childView: Vista del bloque hijo
      */
    public void RemoveChild(BaseView childView)
    {
        if (!m_Childs.Contains(childView)) //Si la vista no esta dentro de la lista de m_Childs sale
            return;

        //1. update previous
        BaseView preView = childView.m_Previous; //Obtenemos si tiene un previo

        if (preView != null)  //Si es correcto entocnes 
        {
            preView.m_Next = null; //Desengancha
            childView.m_Previous = null; //Desengancha
        }

        //2. remove iteratively
        BaseView itor = childView; // se crea un iterador con la vista del hijo

        do
        {
            m_Childs.Remove(itor); //Elimina el hijo de la lista
            itor.m_Parent = null; //Desvincula el padre
            itor = itor.m_Next; //Siguiente hijo
        } while (itor != null);
    }

    /**
             * Descripción: Permite que una vista tenga una vista siguiente
             * @param: BaseView nextView
             */
    public void SetNext(BaseView nextView)
    {
        if (m_Next == nextView) return; //Si ya esta conectado como siguiente sale

        if (nextView != null) //Si existe
        {
            if (m_Parent != null) //Si el padre existe
            {
                m_Parent.AddChild(nextView, SiblingIndex + 1); // Añadimos el hijo al padre en la posición siguiente al actual
            }
            else //Si no existe el padre es un topBlock
            {
                if (m_Next != null) //Si existe m_Next
                {
                    m_Next.m_Previous = nextView; //El anterior de m_Next es nextView
                    nextView.m_Next = m_Next; //El siguiente de nextView es m_Next
                }
                m_Next = nextView;
                nextView.m_Previous = this;
                //Matiene la estructura de lista enlazada
            }
        }
        else
        {
            m_Next.SetPrevious(null); //Si es null eliminamos la conexión
        }
    }

    /**
     * Descripción: Similar a SetNext pero para previous
     * @param: BaseView preView
     */

    public void SetPrevious(BaseView preView)
    {
        if (m_Previous == preView) return; // Si ya esta conectada como anterior sale

        if (preView != null) //Si existe preview
        {
            preView.SetNext(this); //La conecto como siguiente de la vista actual
        }
        else
        {
            //set null
            if (m_Parent != null) //Si ya tiene un padre 
            {
                //remove from parent
                m_Parent.RemoveChild(this); //REmueve el hijo del padre para la vista actual
            }
            else
            {
                if (m_Previous != null) //Si existe el previo
                    m_Previous.m_Next = null; //Desconecta el siguiente del previo
                m_Previous = null; //Desconecta el previo
            }
        }
    }

    /**
      * Descripcíón: Devuelve la vista inicial o encabezado es decir la cadena de vistas conectadas como puede sdr FieldView A - FieldView B -FielDview C, si llamamos a GetHeader desde FieldView C nos debe de informar de que 
      * es FieldView A.
      * Este método retorna una instancia de la misma clase o derivada de BaseView, específicamente, la primera vista (cabeza o header) en la cadena horizontal (hermanos conectados por Previous y Next).
      * @return BaseView header 
      */

    public BaseView GetHeader()
    {
        BaseView header = this; // apunta a la instancia actual (this) desde donde se invoca al método.
        while (header.m_Previous != null) //Iniciando un bucle que sigue iterando hacia atrás en la cadena de vistas mientras exista una vista anterior (m_previous)
        {
            header = header.m_Previous; //Apunta al hermano anterior inmediato
        }
        return header;
    }

    /**
     * Descripción: Encontrar y devolver la última vista (BaseView) enlazada en una cadena horizontal (Next).
     * [View A] → [View B] → [View C] → null
     * @return tail
     */
    public BaseView GetTail()
    {
        BaseView tail = this; //apunta a la instancia actual (this) desde donde se invoca el método apunta inicialmente a la vista actual (this). Tail es la vista desde la que hemos llamado al método.
        while (tail.m_Next != null)  //comprueba continuamente si tail.m_Next tiene otra vista enlazada.
                                     //ciclo que continuará ejecutándose mientras haya un siguiente (m_Next) en la cadena enlazada de vistas
        {
            tail = tail.m_Next; //En cada iteración, mueve la referencia de tail a la vista siguiente(m_Next).
        }
        return tail;
    }

    // OnDestroy es un método de Unity que se llama cuando el script se elimina o el GameObject se destruye.
    void OnDestroy()
    {
        if (m_Parent != null)
            m_Parent.RemoveChild(this);
    }

    /**
       * Descripción: propiedad que proporciona acceso al punto central del rectángulo (RectTransform) de esta vista en el espacio local de la interfaz.
       * ¿Cómo calcula el centro?
       * Comienza desde XY, que es la posición superior izquierda.
       * Luego, añade la mitad del ancho (0.5f * Width) para moverse horizontalmente hasta el centro.
       * Y luego resta la mitad de la altura (0.5f * -Height) para moverse verticalmente hacia abajo, dado que en UI el eje Y va hacia abajo al aumentar valores negativos.
       * 
       * 
       */
    public Vector2 CenterXY
    {
        get { return XY + 0.5f * new Vector2(Width, -Height); } //Al leer (get), devuelve la posición del centro del elemento visual.
                                                                //XY: es la posición de la esquina superior izquierda del rectángulo (pivote en la esquina superior izquierda).
                                                                //Width y Height: son el ancho y alto del rectángulo respectivamente.
        set { XY = value - 0.5f * new Vector2(Width, -Height); } //Al asignar (set), reposiciona el elemento para que su centro coincida con la posición dada
                                                                 //Cuando quieres establecer el centro del objeto (value), necesitas convertir ese centro deseado a la posición real XY, que es siempre la esquina superior izquierda.
                                                                 //Parte del valor asignado (value), que es el nuevo centro deseado.
                                                                 //Resta la mitad del ancho (0.5f * Width) para desplazarse hacia la izquierda, hasta la posición de la esquina superior izquierda.
                                                                 // Suma la mitad de la altura (0.5f * -Height), desplazándose hacia arriba, obteniendo así nuevamente la posición original XY.
    }

}


