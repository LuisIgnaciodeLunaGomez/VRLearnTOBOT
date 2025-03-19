/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 13/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using System.Collections.Generic;
using UnityEngine;

public class ConnectionDB : List<BlockConnection>
{
    public void AddConnection(BlockConnection connection)
    {
        if (connection == null ) return; //|| connection.m_sourceBlock == null
        if (Contains(connection))
        {
            Debug.LogWarning("Connection already in database.");
            return;
        }

        int position = FindPositionForConnection(connection);
        Insert(position, connection);
        Debug.Log($"Connection {connection.type} added at position {position} with y={connection.position.y}");
    }

    public void RemoveConnection(BlockConnection connection)
    {
        if (connection == null) return;
        int index = FindConnection(connection);
        if (index >= 0)
        {
            RemoveAt(index);
            Debug.Log($"Connection {connection.type} removed from database.");
        }
    }

    public BlockConnection FindClosest(BlockConnection connection, float maxRadius, Vector2 dxy)
    {
        if (connection == null || Count == 0) return null;

        Vector2 originalPosition = connection.position;
        connection.position += dxy;

        int minIndex = 0;
        int maxIndex = Count - 1;
        int midIndex = maxIndex;

        while (minIndex < midIndex)
        {
            if (this[midIndex].position.y < connection.position.y)
                minIndex = midIndex;
            else
                maxIndex = midIndex;
            midIndex = (minIndex + maxIndex) / 2;
        }

        BlockConnection closest = null;
        float closestDistance = maxRadius;

        Debug.Log($"FindCloset: BlockBehaviourDB: Buscando conexión más cercana. Total conexiones: {Count}, Posición: {connection.position}");

        for (int i = midIndex; i >= 0 && Mathf.Abs(this[i].position.y - connection.position.y) <= maxRadius; i--)
        {
            float distance = Vector2.Distance(this[i].position, connection.position);

            Debug.Log($"FindCloset: BlockBehaviourDB: Revisando conexión {i}: {this[i].type} at {this[i].position}, distance: {distance}");
            if (distance < closestDistance && connection.CanConnect(this[i]))
            {
                closest = this[i];
                closestDistance = distance;
            }
        }

        for (int i = midIndex + 1; i < Count && Mathf.Abs(this[i].position.y - connection.position.y) <= maxRadius; i++)
        {
            float distance = Vector2.Distance(this[i].position, connection.position);
            Debug.Log($"FindCloset: BlockBehaviourDB: Revisando conexión {i}: {this[i].type} at {this[i].position}, distance: {distance}");
            if (distance < closestDistance && connection.CanConnect(this[i]))
            {
                closest = this[i];
                closestDistance = distance;
            }
        }

        connection.position = originalPosition;
        Debug.Log($"FindCloset: BlockBehaviourDB: Closest connection found: {closest?.type} at {closest?.position}, distance: {closestDistance}");
        return closest;
    }

    private int FindPositionForConnection(BlockConnection connection)
    {
        if (Count == 0) return 0;

        int min = 0;
        int max = Count;
        while (min < max)
        {
            int mid = (min + max) / 2;
            if (this[mid].position.y < connection.position.y)
                min = mid + 1;
            else
                max = mid;
        }
        return min;
    }

    private int FindConnection(BlockConnection connection)
    {
        if (Count == 0) return -1;

        int position = FindPositionForConnection(connection);
        if (position >= Count) return -1;

        float yPos = connection.position.y;
        int min = position;
        int max = position;

        while (min >= 0 && Mathf.Abs(this[min].position.y - yPos) < 0.01f)
        {
            if (this[min] == connection) return min;
            min--;
        }

        while (max < Count && Mathf.Abs(this[max].position.y - yPos) < 0.01f)
        {
            if (this[max] == connection) return max;
            max++;
        }

        return -1;
    }
}

