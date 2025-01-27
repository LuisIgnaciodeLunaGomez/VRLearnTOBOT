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



using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using System.Linq;

public class ConnectionTypes
{
    private List<BlockConnection> connections;

    public ConnectionTypes()
    {
        connections = new List<BlockConnection>();
    }

    public void AddConnection(BlockConnection connection)
    {
        connections.Add(connection);
    }

    public void RemoveConnection(BlockConnection connection)
    {
        connections.Remove(connection);
    }

    public List<BlockConnection> GetNeighbours(BlockConnection target, int maxLimit)
    {
        // Implementa lógica para buscar conexiones cercanas
        return connections
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(target.X, target.Y)) <= maxLimit)
            .ToList();
    }
}
