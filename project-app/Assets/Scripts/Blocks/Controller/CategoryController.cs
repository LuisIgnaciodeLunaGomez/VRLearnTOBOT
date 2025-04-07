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
    private Dictionary<string, Color> m_CategoryColors;
    private bool m_IsInitialized = false;

    public void InitializeController(BlockListView blockListView)
    {
        m_BlockListView = blockListView;
        if (m_BlockListView == null)
        {
            Debug.LogError("CategoryController: BlockListView reference is missing!", this);
            return;
        }

        m_ToolboxConfig = ToolboxConfig.Load();
        if (m_ToolboxConfig == null || m_ToolboxConfig.BlockCategoryList == null || m_ToolboxConfig.BlockCategoryList.Count == 0)
        {
            Debug.LogError("CategoryController: Failed to load ToolboxConfig from UBlockly BlockResMgr!", this);
            m_CategoryColors = new Dictionary<string, Color>();
        }
        else
        {
            m_CategoryColors = m_ToolboxConfig.BlockCategoryList.ToDictionary(
                cat => cat.CategoryName,
                cat => cat.Color 
            );
            Debug.Log($"CategoryController: Loaded {m_CategoryColors.Count} categories from UBlockly ToolboxConfig.");
        }

        m_IsInitialized = true;
        Debug.Log("CategoryController: Initialized.");
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => m_IsInitialized); 

        if (m_BlockListView == null || m_ToolboxConfig == null) 
        {
            Debug.LogError("CategoryController.Start: Initialization incomplete (BlockListView or ToolboxConfig missing)!", this);
            yield break;
        }

        Debug.Log("CategoryController.Start: Displaying categories...");
        StartDisplayingCategories(); 
    }

    /**
     * Descripción: Selecciona la categoría pulsada en el botón
     * @param categoryName: Nombre de la categoría seleccionada
     */
    public void SelectCategory(string categoryName, BaseToolbox sourceToolBox)
    {
        if (string.IsNullOrEmpty(categoryName) || categoryName == m_ActiveCategoryName)
        {
            return;
        }
        if (m_ToolboxConfig == null || !m_CategoryColors.ContainsKey(categoryName))
        {
            Debug.LogWarning($"CategoryController: Attempted to select unknown category '{categoryName}'", this);
            return;
        }

        Debug.Log($"CategoryController: Selecting category '{categoryName}'");
        m_ActiveCategoryName = categoryName;

        ToolboxBlockCategory categoryConfig = m_ToolboxConfig.GetBlockCategory(categoryName);
        if (categoryConfig == null)
        { 
            Debug.LogError($"Could not get category config for '{categoryName}' from ToolboxConfig.", this);
            m_BlockListView?.ShowEmptyMessage($"Error loading blocks for {categoryName}."); 
            return;
        }

        Color categoryColor = categoryConfig.Color;
        List<string> blockTypesInCategory = categoryConfig.BlockList; 

       
        if (m_BlockListView != null)
        {
            m_BlockListView.ShowBlocksForCategory(categoryName, sourceToolBox,categoryColor, blockTypesInCategory);
        }
    }

    /**
     * Descripción: Inicia la visualización de las categorías
     */
    public void StartDisplayingCategories()
    {
        if (m_BlockListView == null || !m_IsInitialized || m_CategoryColors == null)
        {
            Debug.LogError("CategoryController cannot display categories, initialization incomplete!", this);
            return;
        }
        Debug.Log("CategoryController: StartDisplayingCategories called.");

        List<string> categoryNames = m_CategoryColors.Keys.ToList(); 

        if (categoryNames.Count > 0)
        {
           ;
            if (m_BlockListView != null)
            {
                SelectCategory(categoryNames[0], m_BlockListView);
            }
            else
            {
                Debug.LogError("Cannot select initial category because BlockListView reference is null.", this);
            }
        }
        else
        {
            if (m_BlockListView != null) m_BlockListView.ShowEmptyMessage("No categories found.");
        }
    }

}//Fin clase CategoryController
