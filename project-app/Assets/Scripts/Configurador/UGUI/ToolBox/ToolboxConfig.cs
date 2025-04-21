/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System;
using System.Collections.Generic;


[Serializable]
public class ToolboxConfig
{
    public string Style;
    public List<ToolboxBlockCategory> BlockCategoryList;

    public ToolboxBlockCategory GetBlockCategory(string categoryName)
    {
        var category = BlockCategoryList.Find(c => c.CategoryName.Equals(categoryName));
        if (category == null)
            throw new Exception(string.Format("Can\'t find category configuration for \"{0}\" in Toolbox json configuration.", categoryName));
        return category;
    }
  
    /** 
     * Descripción: Encuentra la categoría a la que pertenece un tipo de bloque específico.
     * @param blockType El tipo de bloque que se desea buscar.
     * @return ToolboxBlockCategory La categoría que contiene el bloque, o null si no se encuentra.
    */
    public ToolboxBlockCategory GetBlockCategoryByType(string blockType)
    {
        if (BlockCategoryList == null || string.IsNullOrEmpty(blockType)) return null;

        foreach (ToolboxBlockCategory category in BlockCategoryList)
        {
            if (category.BlockList != null && category.BlockList.Contains(blockType))
            {
                return category;
            }
        }
        return null; 
    }


}

