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
using static enumerator;

public class BlockConnection
{
    public Block SourceBlock { get; private set; }
    public Block TargetBlock { get; private set; }
    public ConnectionType Type { get; private set; }
    public int X { get; set; }
    public int Y { get; set; }

    public BlockConnection(Block sourceBlock, ConnectionType type)
    {
        SourceBlock = sourceBlock;
        Type = type;
    }

    public void Connect(BlockConnection targetConnection)
    {
        if (targetConnection != null)
        {
            TargetBlock = targetConnection.SourceBlock;
        }
    }

    public void Disconnect()
    {
        TargetBlock = null;
    }
}



