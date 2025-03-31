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

public class CategoryController : MonoBehaviour
{
   
    // Vista que muestra los bloques de la categoría seleccionada
    [SerializeField] private BlockListView m_BlockListView;

    private string m_ActiveCategory = null;
    
    private Dictionary<string, Color> m_CategoryColors; // Referencia al color de la categoría
   // private List<(string Name, Color Color)> m_Categories; // Caché de tuplas
    private bool m_IsInitialized = false; // Flag para controlar Start
    
    
    public void InitializeController(BlockListView blockListView)
    {
        m_BlockListView = blockListView;
        if (m_BlockListView == null)
        {
            Debug.LogError("CategoryController: BlockListView reference is missing!", this.gameObject);
            return; 
        }

        // Cargar información necesaria (colores, nombres de categoría)
        //  LoadCategoryInfo();
       // UICanvasView uiView = Object.FindFirstObjectByType<UICanvasView>();
       // if (uiView != null) m_categoryButtonContainer = uiView.CategoryButtonContainer;

       // if (m_BlockListView == null) Debug.LogError("...", this.gameObject);
        //if (m_categoryButtonContainer == null) Debug.LogError("CategoryController couldn't find CategoryButtonContainer via UICanvasView!", this.gameObject);

       // List<string> categoryNames = m_CategoryColors.Keys.ToList(); 
       // m_BlockListView?.DisplayCategories(categoryNames, m_CategoryColors, SelectCategory);

        Debug.Log("CategoryController: Initial reference set.");
        m_IsInitialized = true; // Marcar como listo para Start
    }

    void Start()
    {
        // Solo ejecutar si InitializeController fue llamado con éxito
        if (!m_IsInitialized || m_BlockListView == null)
        {
            if (!m_IsInitialized) Debug.LogError("CategoryController.Start: InitializeController was not called!", this.gameObject);
            return;
        }

        Debug.Log("CategoryController.Start: Loading info and displaying categories...");

        // Mover la lógica de carga y display aquí
        LoadCategoryInfo();

        List<string> categoryNames = m_CategoryColors?.Keys.ToList() ?? new List<string>();


        if (categoryNames.Count == 0)
        {
            Debug.LogWarning("CategoryController: No categories loaded or found to display.");
        }

        if (categoryNames.Count > 0)
        {
            m_BlockListView.DisplayCategories(categoryNames, m_CategoryColors, this.SelectCategory);
           
            SelectCategory(categoryNames[0]);
        }
        else
        {
            
            Debug.LogWarning("CategoryController: No categories loaded or found to display.");
        }
    }

    private void LoadCategoryInfo()
    {
        // Carga m_CategoryColors
        m_CategoryColors = BlockDataLoader.GetAllCategoryColorsOrDefault(); // Carga la lista de tuplas

        //Validar si el diccionario resultante está vacío/null
        if (m_CategoryColors == null || m_CategoryColors.Count == 0)
        {
            Debug.LogWarning("CategoryController: No category color info loaded from BlockDataLoader.");
            m_CategoryColors = new Dictionary<string, Color>(); // Asegurar que no sea null para evitar errores después
                                                                
        }
        else
        {
            Debug.Log($"CategoryController: Loaded {m_CategoryColors.Count} categories with colors.");
        }
        /* if (m_Categories == null || m_Categories.Count == 0)
         {
             Debug.LogWarning("CategoryController: No category info loaded.");
             m_CategoryColors = new Dictionary<string, Color>(); // Asegurar diccionario vacío
             return;
         }
         // Convierte la lista de tuplas al diccionario de colores 
         m_CategoryColors = m_Categories.ToDictionary(c => c.Name, c => c.Color);*/
    }


    // Acciones llamadas por los botones de categoría en la Vista

    public void SelectCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName) || categoryName == m_ActiveCategory)
        {
            return; 
        }

        Debug.Log($"CategoryController: Selecting category '{categoryName}'");

        m_ActiveCategory = categoryName;

       
        Color categoryColor = m_CategoryColors.TryGetValue(categoryName, out Color color) ? color : Color.grey;

        // BlockListView  muestra los bloques de esta categoría
        if (m_BlockListView != null)
        {
            // obtener las BlockDefinitions desde BlockDataLoader
            m_BlockListView.ShowBlockCategory(categoryName, categoryColor);
        }
    }

    public void StartDisplayingCategories()
    {
        if (m_BlockListView == null || !m_IsInitialized)
        {
            Debug.LogError("CategoryController cannot display categories, refs missing!", this.gameObject);
            return;
        }
        Debug.Log("CategoryController: StartDisplayingCategories...");
        LoadCategoryInfo(); // Carga m_CategoryColors

        List<string> categoryNames = m_CategoryColors?.Keys.ToList() ?? new List<string>();

        if (categoryNames.Count > 0)
        {
           
            m_BlockListView.DisplayCategories(categoryNames, m_CategoryColors, this.SelectCategory);
            
            SelectCategory(categoryNames[0]);
        }
        else
        {
            m_BlockListView.ShowEmptyMessage("No categories found."); 
        }
    }
}
