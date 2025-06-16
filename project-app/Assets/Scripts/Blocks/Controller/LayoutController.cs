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
 * Descripción: Gestiona el cálculo de tamaño y posicionamiento de una jerarquía de vistas de bloques. Centraliza la lógica de layout manual que en UBlockly estaba distribuida en cada BaseView.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LayoutController
{
    private readonly BlockView m_RootBlockView;

    /// <summary>
    /// Un diccionario temporal para almacenar el tamaño y posición calculados de cada
    /// vista antes de aplicarlos a los RectTransforms reales.
    /// Esto evita problemas de lectura/escritura en el mismo frame.
    /// </summary>
    private readonly Dictionary<BaseView, Rect> m_LayoutInfo;

    public LayoutController(BlockView rootBlockView)
    {
        m_RootBlockView = rootBlockView ?? throw new System.ArgumentNullException(nameof(rootBlockView));
        m_LayoutInfo = new Dictionary<BaseView, Rect>();
    }

    /// <summary>
    /// El método principal que ejecuta todo el proceso de layout en dos fases.
    /// </summary>
    public void ExecuteLayout()
    {
        if (m_RootBlockView == null) return;

        // Empezamos siempre desde cero
        m_LayoutInfo.Clear();

        // --- FASE 1: PASADA DE CÁLCULO ---
        // Se recorre toda la jerarquía de vistas (de hojas hacia raíz) para calcular
        // el tamaño de cada elemento y se almacena en m_LayoutInfo.
        // No se modifica ningún RectTransform todavía.
        RecursiveCalculatePass(m_RootBlockView);

        // --- FASE 2: PASADA DE APLICACIÓN ---
        // Se recorre la jerarquía de nuevo (de raíz hacia hojas) para aplicar las posiciones
        // y tamaños que hemos calculado y guardado en la fase anterior.
        RecursiveApplyPass(m_RootBlockView, m_RootBlockView.XY);

        // Al final, la vista raíz del bloque aplica su apariencia visual final (el fondo 9-slice)
        m_RootBlockView.ApplyVisualAppearance();
    }

    // --- LÓGICA DE LA FASE 1: CÁLCULO ---

    /// <summary>
    /// Recorre la jerarquía recursivamente para calcular el tamaño de cada vista.
    /// El orden post-order (hijos primero, luego el padre) es clave.
    /// </summary>
    private void RecursiveCalculatePass(BaseView view)
    {
        if (view == null || m_LayoutInfo.ContainsKey(view)) return; // Si ya está calculado, salir.

        // 1. Primero calculamos el de todos los hijos.
        foreach (var child in view.ChildViews.Where(c => c != null))
        {
            RecursiveCalculatePass(child);
        }

        // 2. Una vez que los tamaños de los hijos son conocidos, calculamos nuestro propio tamaño.
        Vector2 calculatedSize = CalculateViewSize(view);

        // 3. Guardamos solo el tamaño en el diccionario por ahora. La posición vendrá después.
        m_LayoutInfo[view] = new Rect(Vector2.zero, calculatedSize);
    }

    /// <summary>
    /// El cerebro del sistema. Determina el tamaño de una vista basándose en su tipo
    /// y en los tamaños ya calculados de sus hijos (obtenidos de m_LayoutInfo).
    /// </summary>
    private Vector2 CalculateViewSize(BaseView view)
    {
        // ----------------- CASOS BASE (HOJAS) -----------------
        if (view is FieldView fieldView)
        {
            // La vista del campo es la única que sabe realmente medir su propio contenido (texto, imagen, etc.)
            return fieldView.CalculateFieldSize();
        }

        if (view is ConnectionView && !(view is ConnectionInputView))
        {
            // Las conexiones principales (Prev/Next/Output) tienen tamaño fijo desde los settings.
            return BlockViewSettings.Instance.GetConnectionSize(view.GetComponent<ConnectionView>().ConnectionType);
        }

        // ----------------- CASOS DE CONTENEDORES -----------------

        var settings = BlockViewSettings.Instance;
        float totalWidth = 0;
        float maxHeight = 0;

        if (view is ConnectionInputView connectionInputView)
        {
            // Si el hueco está ocupado por un bloque
            if (connectionInputView.ConnectionModel != null && connectionInputView.ConnectionModel.IsConnected)
            {
                BlockView childBlock = connectionInputView.TargetBlockView;
                if (childBlock != null && m_LayoutInfo.ContainsKey(childBlock))
                    return m_LayoutInfo[childBlock].size;
            }
            // Si está vacío, tiene un tamaño de "slot" por defecto
            return settings.GetConnectionSlotSize(connectionInputView.ConnectionType);
        }

        if (view is InputView inputView)
        {
            totalWidth += inputView.ChildViews.Sum(child => m_LayoutInfo.ContainsKey(child) ? m_LayoutInfo[child].width : 0);
            totalWidth += Mathf.Max(0, inputView.ChildViews.Count - 1) * settings.HorizontalElementSpacing;
            maxHeight = inputView.ChildViews.Count > 0 ? inputView.ChildViews.Max(child => m_LayoutInfo.ContainsKey(child) ? m_LayoutInfo[child].height : 0) : settings.MinUnitHeight;
            return new Vector2(totalWidth, maxHeight);
        }

        if (view is LineGroupView lineGroupView)
        {
            totalWidth = lineGroupView.ChildViews.Sum(child => m_LayoutInfo[child].width) + Mathf.Max(0, lineGroupView.ChildViews.Count - 1) * settings.HorizontalElementSpacing;
            totalWidth += settings.LineGroupPadding.horizontal;
            maxHeight = lineGroupView.ChildViews.Count > 0 ? lineGroupView.ChildViews.Max(child => m_LayoutInfo[child].height) : 0;
            maxHeight += settings.LineGroupPadding.vertical;
            return new Vector2(totalWidth, maxHeight);
        }

        if (view is BlockView blockView)
        {
            maxHeight = blockView.ChildViews.OfType<LineGroupView>().Sum(lg => m_LayoutInfo[lg].height);
            maxHeight += Mathf.Max(0, blockView.ChildViews.OfType<LineGroupView>().Count() - 1) * settings.VerticalLineSpacing;
            maxHeight += settings.BlockInternalPadding.vertical;

            totalWidth = blockView.ChildViews.OfType<LineGroupView>().Count() > 0 ? blockView.ChildViews.OfType<LineGroupView>().Max(lg => m_LayoutInfo[lg].width) : 0;
            totalWidth += settings.BlockInternalPadding.horizontal;

            // Asegurar tamaño mínimo para el bloque
            totalWidth = Mathf.Max(totalWidth, settings.MinBlockSize.x);
            maxHeight = Mathf.Max(maxHeight, settings.MinBlockSize.y);
            return new Vector2(totalWidth, maxHeight);
        }

        return view.Size; // Fallback, no debería llegar aquí
    }

    // --- LÓGICA DE LA FASE 2: APLICACIÓN ---

    /// <summary>
    /// Recorre la jerarquía de arriba hacia abajo (pre-order), aplicando la posición y el
    /// tamaño que se calcularon previamente.
    /// </summary>
    private void RecursiveApplyPass(BaseView view, Vector2 position)
    {
        if (view == null || !m_LayoutInfo.ContainsKey(view)) return;

        // 1. Aplicar la posición y el tamaño a esta vista
        Rect calculatedRect = m_LayoutInfo[view];
        view.XY = position;
        view.Size = calculatedRect.size;

        // 2. Posicionar a los hijos
        Vector2 childStartPos = position + view.ChildStartXY;

        foreach (var child in view.ChildViews)
        {
            // Llamada recursiva para el hijo, pasándole su nueva posición inicial
            RecursiveApplyPass(child, childStartPos);

            // Calcular dónde empezará el siguiente hermano
            if (view is InputView || view is LineGroupView)
                childStartPos.x += m_LayoutInfo[child].width + BlockViewSettings.Instance.HorizontalElementSpacing;
            else if (view is BlockView)
                childStartPos.y -= m_LayoutInfo[child].height + BlockViewSettings.Instance.VerticalLineSpacing;
        }
    }
}