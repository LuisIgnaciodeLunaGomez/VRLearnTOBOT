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

    public void AddConnection(ConnectionModel connectionToAdd)
    {
        if (connectionToAdd == null)
        {
            Debug.LogError($"BlockConnectionDB: Attempted to add a null connection.");
            return;
        }

        // Compruebo si ya existe en la lista (comparando referencias)
        if (this.Contains(connectionToAdd))
        {
            Debug.LogWarning($"BlockConnectionDB: Connection '{ConnectionModel.GetConnectionModelID(connectionToAdd)}' is already in the DB list. Ensuring InDB flag is set.");
           
            if (!connectionToAdd.InDB)
            {
                connectionToAdd.InDB = true;
            }
        }
        else
        {

            // this.Add(connectionToAdd);

            int index = FindPositionForConnection(connectionToAdd); // Encuentra dónde debería ir
            Insert(index, connectionToAdd); // Inserta en esa posición
            connectionToAdd.InDB = true;
            connectionToAdd.InDB = true; 
            // Debug.Log($"ConnectionDB [{this}]: Added connection '{ConnectionModel.GetConnectionModelID(connectionToAdd)}'. Current count: {this.Count}");
        }
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

  
    public void RemoveConnection(ConnectionModel connectionToRemove)
    {
        if (connectionToRemove == null)
        {
            Debug.LogWarning($"BlockConnectionDB: Attempted to remove a null connection.");
            return;
        }

        // Intento eliminar de la lista usando el método Remove heredado (busca el objeto)
        bool removed = this.Remove(connectionToRemove);

        if (removed)
        {
            connectionToRemove.InDB = false; 
        // Debug.Log($"ConnectionDB [{this}]: Removed connection '{ConnectionModel.GetConnectionModelID(connectionToRemove)}'. Current count: {this.Count}");
        }
        else
        {
          
            if (connectionToRemove.InDB)
            {
                connectionToRemove.InDB = false;
            }
        }
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


    public void UpdateConnectionLocation(ConnectionModel connectionToUpdate)
    {
        if (connectionToUpdate == null)
        {
            Debug.LogError("BlockConnectionDB.UpdateConnectionLocation: Received a null connection.");
            return;
        }

        // Busco la instancia específica en la lista.
        // El enfoque más simple y generalmente seguro es buscar la misma referencia de objeto.
        bool found = false;
        for (int i = 0; i < this.Count; i++)
        {
            // Comparar referencias directamente
            if (this[i] == connectionToUpdate)
            {
               
                // Debug.Log($"ConnectionDB [{this}]: Confirmed connection '{ConnectionModel.GetConnectionModelID(connectionToUpdate)}' exists in list. Location ({this[i].Location.x:F2}, {this[i].Location.y:F2}).");
                found = true;
                break; 
            }
        }

        // Si después de recorrer la lista, no se encontró la referencia...
        if (!found)
        {
            Debug.LogWarning($"BlockConnectionDB: UpdateConnectionLocation - Connection '{ConnectionModel.GetConnectionModelID(connectionToUpdate)}' reported InDB=true but was NOT found in the list. Forcing InDB=false.");
            // Fuerzo el flag a false para corregir el estado
            connectionToUpdate.InDB = false;

         
        }
    }

    //Método de depuración
    public void Debug_LogSortOrder(string dbType) // Pasa el tipo de conexión para identificar la DB
    {
       // Debug.Log($"--- Checking Sort Order for DB: {dbType} (Count: {this.Count}) ---");
        float lastY = float.NegativeInfinity;
        bool sorted = true;
        for (int i = 0; i < this.Count; i++)
        {
            float currentY = this[i].Location.y;
            bool comparisonOk = currentY >= lastY;
           // Debug.Log($"  [{i}] Conn: {ConnectionModel.GetConnectionModelID(this[i])}, Y: {currentY:F2} (>= Last: {comparisonOk})");
            if (!comparisonOk) sorted = false;
            lastY = currentY;
        }
        if (!sorted)
        {
            Debug.LogError($"  !!!! DB {dbType} IS NOT SORTED BY Y !!!!");
        }
        else
        {
          //  Debug.Log($"  DB {dbType} APPEARS SORTED BY Y.");
        }
   //     Debug.Log("--- End Check ---");
    }

}//fin clase BlockConnectionDB

