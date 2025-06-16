/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 16/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  El controlador dedicado para un único par Modelo-Vista de bloque.  Orquesta la creación de la vista, gestiona la interacción del usuario y actualiza la vista en respuesta a los cambios del modelo.
 */

using UnityEngine;
using UnityEngine.EventSystems;

public class BlockController
{
    // --- Referencias Principales ---
    public BlockModel Model { get; private set; }
    public BlockView View { get; private set; }

    // --- Controlador de Servicio ---
    private readonly LayoutController m_LayoutController;
    private readonly BlockDragController m_DragController;

    /// <summary>
    /// El constructor es el punto de entrada. Se llama desde WorkspaceController cuando se crea un nuevo bloque.
    /// </summary>
    public BlockController(BlockModel model, WorkSpaceView workspaceView, BlockDragController dragController)
    {
        // 1. Validar y almacenar dependencias
        this.Model = model ?? throw new System.ArgumentNullException(nameof(model));
        if (workspaceView == null) throw new System.ArgumentNullException(nameof(workspaceView));
        this.m_DragController = dragController ?? throw new System.ArgumentNullException(nameof(dragController));

        // 2. Usar el Builder para crear la vista asociada a este modelo.
        this.View = VRLearnBlockViewBuilder.BuildBlockView(this.Model, workspaceView);
        if (this.View == null)
        {
            Debug.LogError($"¡Falló la creación de la VISTA para el modelo {Model.Type}! No se pudo crear el BlockController.");
            // El modelo debería ser limpiado por quien llamó al constructor.
            return;
        }

        // 3. Establecer la conexión bidireccional: la vista conoce a su controlador.
        this.View.SetController(this);

        // 4. Cada BlockController gestiona su propio LayoutController.
        m_LayoutController = new LayoutController(this.View);

        // 5. Suscribirse a eventos para reaccionar a cambios.
        SubscribeToEvents();

        // 6. Solicitar el primer cálculo de layout.
        RequestLayoutUpdate();
    }

    /// <summary>
    /// Realiza una limpieza completa del bloque, su vista y sus eventos.
    /// </summary>
    public void Dispose()
    {
        UnsubscribeFromEvents();

        if (View != null && View.gameObject != null)
        {
            // La destrucción del GameObject de la vista provocará la destrucción de sus hijos.
            GameObject.Destroy(View.gameObject);
        }

        // El modelo se dispone al final, para asegurar que la vista puede acceder a él durante su OnDestroy si es necesario.
        Model?.Dispose();
    }

    /// <summary>
    /// Suscribe los métodos de este controlador a los eventos del Modelo y la Vista.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (View != null)
        {
            // Eventos que vienen de la VISTA (interacción del usuario)
            View.OnBeginDragRequested += HandleBeginDrag;
            View.OnDragRequested += HandleDrag;
            View.OnEndDragRequested += HandleEndDrag;
        }

        if (Model != null)
        {
            // Eventos que vienen del MODELO (cambios en los datos)
            Model.AddObserver(new MemorySafeBlockModelObserver(this));
        }
    }

    /// <summary>
    /// Elimina las suscripciones para prevenir memory leaks.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (View != null)
        {
            View.OnBeginDragRequested -= HandleBeginDrag;
            View.OnDragRequested -= HandleDrag;
            View.OnEndDragRequested -= HandleEndDrag;
        }

        if (Model != null)
        {
            // La lógica para des-registrar el observer se complica un poco
            // ya que son anónimos, pero una buena implementación de Observable<T> debería manejarlo.
            // Por simplicidad, asumimos que al disponerse el Model, limpia sus observers.
        }
    }

    // --- MANEJADORES DE EVENTOS DE LA VISTA ---

    private void HandleBeginDrag(PointerEventData eventData)
    {
        m_DragController.StartDrag(this, eventData);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        m_DragController.UpdateDrag(eventData);
    }

    private void HandleEndDrag(PointerEventData eventData)
    {
        m_DragController.EndDrag(eventData);
    }

    // --- MANEJADORES DE EVENTOS DEL MODELO ---

    /// <summary>
    /// Reacciona a un cambio en el modelo lógico.
    /// </summary>
    public void OnModelUpdated(int updateMask)
    {
        if ((updateMask & (1 << (int)UpdateStates.Inputs)) != 0)
        {
            // La estructura interna ha cambiado (ej, por un Mutator).
            // Reconstruimos las vistas internas (inputs, fields) antes de recalcular el layout.
            VRLearnBlockViewBuilder.BuildInternalViews(Model, View);
            RequestLayoutUpdate();
        }

        // Puedes añadir más casos para cambios de color, texto, etc., si no afectan al tamaño.
    }

    /// <summary>
    /// Pide un recálculo del layout marcando la vista como "sucia".
    /// </summary>
    public void RequestLayoutUpdate()
    {
        // En lugar de llamar directamente a m_LayoutController.ExecuteLayout(),
        // notificamos a la vista que su layout está desactualizado.
        // La vista lo ejecutará en su LateUpdate para agrupar todos los cambios del frame.
        if (View != null)
        {
            View.MarkLayoutDirty();
        }
    }

    // Clase interna para observar el modelo de forma segura
    private class MemorySafeBlockModelObserver : IObserver<int>
    {
        private readonly BlockController m_ControllerRef;

        public MemorySafeBlockModelObserver(BlockController controller)
        {
            m_ControllerRef = controller;
        }

        public void OnUpdated(object subject, int args)
        {
            if (m_ControllerRef == null || m_ControllerRef.Model != subject)
            {
                (subject as Observable<int>)?.RemoveObserver(this);
                return;
            }
            m_ControllerRef.OnModelUpdated(args);
        }
    }
}