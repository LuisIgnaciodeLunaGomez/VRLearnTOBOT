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


using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine.UIElements;
using static enumerator;
using System;

public class Block
{
    public string Type { get; protected set; } 
    public string ID { get; protected set; }

    public VisualElement VisualElement { get; private set; }
    public WorkSpace Workspace { get; private set; }

    // Conexiones
    public BlockConnection OutputConnection { get; private set; }
    public BlockConnection NextConnection { get; private set; }
    public BlockConnection PreviousConnection { get; private set; }
    public List<BlockConnection> InputConnections { get; private set; }
    public Block(string type, WorkSpace workspace)
    {
        Type = type;
        ID = Guid.NewGuid().ToString();
        Workspace = workspace;

        VisualElement = new VisualElement(); // Define el contenedor visual del bloque
        VisualElement = BlockUIFactory.CreateBlockElement(Type, "Texto de prueba", "Sprites/event_block");

        VisualElement.AddToClassList("block");

        // Inicializar conexiones

        // Inicializar conexiones con posiciones iniciales
        OutputConnection = new BlockConnection(this, ConnectionType.Output) { X = 0, Y = 0 };
        NextConnection = new BlockConnection(this, ConnectionType.Next) { X = 0, Y = 0 };
        PreviousConnection = new BlockConnection(this, ConnectionType.Previous) { X = 0, Y = 0 };

        InputConnections = new List<BlockConnection>();
    }

    public void AddInputConnection(BlockConnection connection)
    {
        InputConnections.Add(connection);
    }
}

