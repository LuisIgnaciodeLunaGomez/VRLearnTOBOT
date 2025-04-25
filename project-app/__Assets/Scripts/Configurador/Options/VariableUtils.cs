/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 */

using System;
using System.Collections.Generic;
using System.Linq;            

public static class VariableUtils 
{
    
    public static List<string> GetAllUsedVariableNames(WorkSpaceModel workspace)
    {
        if (workspace == null)
            return new List<string>();

        var allBlocks = workspace.GetAllBlocks(); 
        var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase); 

        foreach (var block in allBlocks)
        {
            List<string> blockVars = block.GetVars(); 
            if (blockVars != null)
            {
                foreach (string varName in blockVars)
                {
                    if (!string.IsNullOrEmpty(varName))
                    {
                        usedVariables.Add(varName); 
                    }
                }
            }
        }

        return usedVariables.ToList(); 
    }
}//Fin clase VariableUtils
