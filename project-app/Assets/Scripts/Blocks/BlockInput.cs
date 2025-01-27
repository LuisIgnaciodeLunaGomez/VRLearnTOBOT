/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/01/2025
 * 
 * Versión: 1.0.0
 */


using UnityEditor.MemoryProfiler;
using UnityEngine;

public class BlockInput
{
    public string Name { get; private set; }
    public BlockConnection Connection { get; private set; }

    public BlockInput(string name)
    {
        Name = name;
        Connection = null;
    }

    public void SetConnection(BlockConnection connection)
    {
        Connection = connection;
    }
}
