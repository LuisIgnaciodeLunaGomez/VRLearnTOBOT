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
 * Descripción:  Método de Extensión para ConnectionModel que calcula y establece la posición lógica (Location) de una conexión  relativa a la posición lógica de su bloque propietario.
 */

using System.Linq;
using UnityEngine;

public static class ConnectionModelExtensions
{
   
    public static void SetLocation(this ConnectionModel cm, Vector2Int blockLocation)
    {
        if (cm == null) return; 

        Vector2Int offset = Vector2Int.zero;

        
        switch (cm.Type)
        {
            case EConnection.OutputValue:
                offset = new Vector2Int(-BlockLogicalMetrics.OutputConnectionWidth, Mathf.RoundToInt(-BlockLogicalMetrics.BlockHeight / 2f)); // Izquierda, Centro Vertical
                break;
            case EConnection.PrevStatement:
                offset = new Vector2Int(Mathf.RoundToInt(BlockLogicalMetrics.NotchOffsetHorizontal), 0); // Arriba Centro (Notch X)
                break;
            case EConnection.NextStatement:
                offset = new Vector2Int(Mathf.RoundToInt(BlockLogicalMetrics.NotchOffsetHorizontal), -BlockLogicalMetrics.BlockHeight); // Abajo Centro (Notch X), usa alto del bloque
                break;
            case EConnection.InputValue:
            case EConnection.DummyInput: 
                InputModel parentInput = cm.SourceBlock?.InputList.Find(i => i.Connection == cm || i.Name == cm.NameInInput()); 
                if (parentInput != null)
                {
     
                    offset = new Vector2Int(BlockLogicalMetrics.DefaultInputAttachWidth, Mathf.RoundToInt(-BlockLogicalMetrics.BlockHeight / 2f)  );
                }
                else
                {
                    offset = new Vector2Int(10, -10); 
                    Debug.LogWarning($"Could not determine offset for {cm.Type} on block {cm.SourceBlock?.ID}");
                }
                break;
        }

        // Asignar la posición final: Posición del bloque + Offset calculado
        cm.Location = blockLocation + offset;

     
    }

    // Placeholder: obtener nombre asociado
    private static string NameInInput(this ConnectionModel cm)
    {
        return cm.SourceBlock?.InputList.FirstOrDefault(i => i.Connection == cm)?.Name;
    }
} //Fin clase ConnectionModelExtensions
