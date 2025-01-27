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
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using System;

public class WorkSpace
{
    public VisualElement RootElement { get; private set; } // El contenedor principal
    public List<Block> Blocks { get; private set; } // Los bloques añadidos al workspace
    public Dictionary<Define .EConnection, ConnectionTypes> ConnectionDBList { get; private set; } // Base de datos de conexiones

    public WorkSpace(VisualElement rootElement)
    {
        RootElement = rootElement;
        Blocks = new List<Block>();
        ConnectionDBList = new Dictionary<Define.EConnection, ConnectionTypes>();

        // Inicializar las bases de datos de conexiones para cada tipo de conexión
        foreach (Define.EConnection type in Enum.GetValues(typeof(Define.EConnection)))
        {
            ConnectionDBList[type] = new ConnectionTypes();
        }
    }

    public void AddBlock(Block block)
    {
        Blocks.Add(block);
        RootElement.Add(block.VisualElement); // Añadir el elemento visual al espacio de trabajo

        // Agregar conexiones a la base de datos
        ConnectionDBList[Define.EConnection.NextStatement].AddConnection(block.NextConnection);
        ConnectionDBList[Define.EConnection.PrevStatement].AddConnection(block.PreviousConnection);
        if (block.OutputConnection != null)
            ConnectionDBList[Define.EConnection.OutputValue].AddConnection(block.OutputConnection);

    }

    public void RemoveBlock(Block block)
    {
        Blocks.Remove(block);
        RootElement.Remove(block.VisualElement); // Quitar el elemento visual del espacio de trabajo
    }

    public void ConnectBlocks(Block parent, Block child)
    {
        // Comprueba que las conexiones son válidas
        if (parent.NextConnection != null && child.PreviousConnection != null)
        {
            // Conecta las conexiones
            parent.NextConnection.Connect(child.PreviousConnection);

            // Actualiza la posición visual del bloque hijo respecto al padre
            child.VisualElement.style.left = parent.VisualElement.resolvedStyle.left + 50; // Ajusta el offset
            child.VisualElement.style.top = parent.VisualElement.resolvedStyle.top + 20;  // Ajusta el offset
        }
        else
        {
            Debug.LogWarning("Conexiones inválidas. Asegúrate de que los bloques tienen conexiones compatibles.");
        }
    }



}
