/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/01/2025
 * 
 * Versión: 1.0.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class BlocksConnections
{
    public int SourceBlockId { get; set; }
    public int TargetBlockId { get; set; }
    public string ConnectionType { get; set; } // Por ejemplo, "Input", "Output", "Next".



    public BlocksConnections(int source, int target, string connectionType = "Default")
    {
        if (source < 0 || target < 0)
            throw new ArgumentException("Los IDs no pueden ser negativos.");
        SourceBlockId = source;
        TargetBlockId = target;
        ConnectionType = connectionType;
    }

    public override string ToString()
    {
        return $"{ConnectionType} Connection from {SourceBlockId} to {TargetBlockId}";
    }

}