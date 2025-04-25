/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockConnectionDB : List<ConnectionModel>
{

    public BlockConnectionDB()
    {
    }

   
    public void AddConnection(ConnectionModel connection)
    {
        if (connection.InDB)
        {
            throw new Exception("Connection already in database.");
        }

        var position = this.FindPositionForConnection(connection);
        this.Insert(position, connection);
        connection.InDB = true;
    }

  
    public int FindConnection(ConnectionModel connection)
    {
        if (this.Count == 0)
            return -1;

        var bestGuess = this.FindPositionForConnection(connection);
        if (bestGuess >= this.Count)
        {
            return -1;
        }

        var yPos = connection.Location.y;
        var pointerMin = bestGuess;
        var pointerMax = bestGuess;
        while (pointerMin >= 0 && this[pointerMin].Location.y == yPos)
        {
            if (this[pointerMin] == connection)
                return pointerMin;
            pointerMin--;
        }

        while (pointerMax < this.Count && this[pointerMax].Location.y == yPos)
        {
            if (this[pointerMax] == connection)
                return pointerMax;
            pointerMax++;
        }

        return -1;
    }

    public int FindPositionForConnection(ConnectionModel connection)
    {
        if (this.Count == 0)
        {
            return 0;
        }

        var pointerMin = 0;
        var pointerMax = this.Count;
        while (pointerMin < pointerMax)
        {
            int pointerMid = (pointerMin + pointerMax) / 2;
            if (this[pointerMid].Location.y < connection.Location.y)
            {
                pointerMin = pointerMid + 1;
            }
            else if (this[pointerMid].Location.y > connection.Location.y)
            {
                pointerMax = pointerMid;
            }
            else
            {
                pointerMin = pointerMid;
                break;
            }
        }
        return pointerMin;
    }

 
    public void RemoveConnection(ConnectionModel connection)
    {
        if (!connection.InDB) return;

        var removeIndex = FindConnection(connection);
        if (removeIndex == -1)
            throw new Exception("Unable to find connection in connectionDB, but the connection's property \"InDB\" is true");

        connection.InDB = false;
        this.RemoveAt(removeIndex);
    }

   
    public List<ConnectionModel> GetNeighbours(ConnectionModel connection, int maxRadius)
    {
        var currentX = connection.Location.x;
        var currentY = connection.Location.y;

        int pointerMin = 0;
        int pointerMax = this.Count - 2;
        int pointerMid = pointerMax;
        while (pointerMin < pointerMid)
        {
            if (this[pointerMid].Location.y < currentY)
                pointerMin = pointerMid;
            else
                pointerMax = pointerMid;
            pointerMid = (pointerMin + pointerMax) / 2;
        }

        List<ConnectionModel> neighbours = new List<ConnectionModel>();

        Func<int, bool> checkConnection = (yIndex) =>
        {
            var c = this[yIndex];
            var dx = currentX - c.Location.x;
            var dy = currentY - c.Location.y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            if (r <= maxRadius)
                neighbours.Add(c);

            return dy < maxRadius;
        };

        pointerMin = pointerMid;
        pointerMax = pointerMid + 1;
        if (this.Count > 0)
        {
            while (pointerMin >= 0 && checkConnection(pointerMin))
                pointerMin--;

            while (pointerMax < this.Count && checkConnection(pointerMax))
                pointerMax++;
        }

        return neighbours;
    }


    public void SearchForClosest(ConnectionModel connection, float maxRadius, Vector2 dxy,
                                 out ConnectionModel closestConnection, out float closestRadius)
    {
        closestConnection = null;
        closestRadius = maxRadius; 

        if (this.Count == 0) return;

        var originalLocation = connection.Location; 
                                                    
        connection.Location = originalLocation + dxy;

        var closestIndex = FindPositionForConnection(connection);

        ConnectionModel temp;

        
        Func<int, float, float, bool> isInYRange = (idx, refY, radius) => Mathf.Abs(this[idx].Location.y - refY) <= radius;

        var pointerMin = closestIndex - 1;
        while (pointerMin >= 0 && isInYRange(pointerMin, connection.Location.y, maxRadius)) 
        {
            temp = this[pointerMin];
            float distance = connection.DistanceFrom(temp); 
            if (connection.IsConnectionAllowed(temp, closestRadius) && distance < closestRadius) 
            {
                closestConnection = temp;
                closestRadius = distance; 
            }
            pointerMin--;
        }

        var pointerMax = closestIndex;
        while (pointerMax < this.Count && isInYRange(pointerMax, connection.Location.y, maxRadius))
        {
            temp = this[pointerMax];
            float distance = connection.DistanceFrom(temp);
            if (connection.IsConnectionAllowed(temp, closestRadius) && distance < closestRadius)
            {
                closestConnection = temp;
                closestRadius = distance;
            }
            pointerMax++;
        }

        connection.Location = originalLocation;
    }


    public static Dictionary<EConnection, BlockConnectionDB> Build()
    {
        var dbList = new Dictionary<EConnection, BlockConnectionDB>();
        dbList.Add(EConnection.InputValue, new BlockConnectionDB());
        dbList.Add(EConnection.OutputValue, new BlockConnectionDB());
        dbList.Add(EConnection.NextStatement, new BlockConnectionDB());
        dbList.Add(EConnection.PrevStatement, new BlockConnectionDB());
        return dbList;
    }
}//fin clase BlockConnectionDB

