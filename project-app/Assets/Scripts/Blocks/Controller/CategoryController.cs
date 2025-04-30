/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:Controlador que carga las categorías de bloques y se relaciona con la vista correspondiente.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CategoryController : MonoBehaviour
{
    private BlockListView m_BlockListView;
    private string m_ActiveCategoryName = null;
    private ToolboxConfig m_ToolboxConfig; 
    private bool m_IsInitialized = false;

    /// <summary>
    /// Inicializa el controlador con las dependencias necesarias.
    /// Llamado por AppController o el sistema de inicialización.
    /// </summary>
    public void InitializeController(BlockListView blockListView, ToolboxConfig toolboxConfig)
    {
        if (m_IsInitialized) return; // Evitar re-inicialización

      //  Debug.Log("<color=purple>CategoryController: Initializing...</color>");

        // Recibir y validar dependencias
        m_BlockListView = blockListView ?? throw new System.ArgumentNullException(nameof(blockListView), "BlockListView cannot be null for CategoryController");
        m_ToolboxConfig = toolboxConfig ?? throw new System.ArgumentNullException(nameof(toolboxConfig), "ToolboxConfig cannot be null for CategoryController");

        // Verificar que ToolboxConfig parece válido
        if (m_ToolboxConfig.BlockCategoryList == null)
        {
            Debug.LogError("CategoryController: Received ToolboxConfig but its BlockCategoryList is null!", this);
            // No se puede  operar sin la lista de categorías. Marcar como no inicializado o manejar error.
            m_IsInitialized = false;
            this.enabled = false; // Deshabilitar el controlador
            return;
        }
        m_IsInitialized = true;
       // Debug.Log("<color=purple>CategoryController: Initialized successfully.</color>");

        //Selecciona la primera categoría inmediatamente después de inicializar
        StartCoroutine(SelectInitialCategoryCoroutine());
    }

    private IEnumerator SelectInitialCategoryCoroutine()
    {
        yield return new WaitUntil(() => m_IsInitialized); // Esperamps a InitializeController()
        yield return null; // Esperamos un frame por si acaso (layout)

        if (m_BlockListView == null || m_ToolboxConfig == null || m_ToolboxConfig.BlockCategoryList.Count == 0)
        {
            Debug.LogError("CategoryController: Cannot select initial category, initialization failed or no categories available!", this);
            if (m_BlockListView != null) m_BlockListView.ShowEmptyMessage("No categories available.");
            yield break;
        }

        Debug.Log("<color=purple>CategoryController: Selecting initial category...</color>");

        // Encontramos la primera categoría real 
        ToolboxBlockCategory firstCategory = m_ToolboxConfig.BlockCategoryList.FirstOrDefault(c => c?.Custom != "SEPARATOR");

        if (firstCategory != null && !string.IsNullOrEmpty(firstCategory.CategoryName))
        {
            // Seleccionamos la primera categoría disponible
            SelectCategory(firstCategory.CategoryName);
        }
        else
        {
            Debug.LogWarning("CategoryController: No valid categories found to select initially.");
            m_BlockListView.ShowEmptyMessage("No categories found.");
        }
    }
    /// <summary>
    /// Método público llamado por la VISTA (ej. los botones de categoría
    /// creados por CategoryLoader o BlockListView.BuildMenu) cuando se selecciona una categoría.
    /// </summary>
    /// <param name="categoryName">Nombre de la categoría seleccionada.</param>
    public void SelectCategory(string categoryName) 
    {
        if (!m_IsInitialized) { Debug.LogError("CategoryController not initialized, cannot select category.", this); return; }
        if (string.IsNullOrEmpty(categoryName)) { Debug.LogWarning("SelectCategory called with empty name.", this); return; }
        if (categoryName == m_ActiveCategoryName) { return; } // Ya está seleccionada

        // Busqueda de la configuración de la categoría seleccionada
        ToolboxBlockCategory categoryConfig = m_ToolboxConfig.GetBlockCategory(categoryName);

        if (categoryConfig == null)
        {
            Debug.LogError($"CategoryController: Configuration not found for category '{categoryName}'. Check ToolboxConfig.", this);
            m_BlockListView?.ShowEmptyMessage($"Error: Category '{categoryName}' not found.");
            return;
        }

        // Ignoramos separadores si se intentan seleccionar
        if (categoryConfig.Custom == "SEPARATOR")
        {
            Debug.Log($"CategoryController: Ignoring selection of separator.");
            return;
        }

        // Actualizamos estado interno
        m_ActiveCategoryName = categoryName;
        Color categoryColor = categoryConfig.Color; // Obtenemos el color ya inicializado
        List<string> blockTypesInCategory = categoryConfig.BlockList ?? new List<string>(); // Obtenemos tipos de bloque

        // Le indicamos a la Vista  que muestre los bloques
        if (m_BlockListView != null)
        {
            m_BlockListView.ShowBlockCategory(categoryName, categoryColor); 
        }
        else
        {
            Debug.LogError("CategoryController: BlockListView reference is null. Cannot update view.", this);
        }
    }

    //  Limpieza 
    void OnDestroy()
    {
        m_BlockListView = null;
        m_ToolboxConfig = null;
    }
}//Fin clase CategoryController
