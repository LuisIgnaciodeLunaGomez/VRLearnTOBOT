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
using UnityEngine; //Vector2 

public class Blocks
{
    public string Type { get; protected set; } 
    public string ID { get; protected set; }
    public WorkSpace Workspace { get; set; } 
    public BlocksConnections OutputConnection { get; set; } 
    public BlocksConnections NextConnection { get; set; } 
    public BlocksConnections PreviousConnection { get; set; } 
    public List<Input> InputList { get; protected set; } 
    public Mutators Mutator { get; protected set; } 
    public Blocks ParentBlock { get; protected set; }
    public List<Blocks> ChildBlocks = new List<Blocks>(); 
    public Vector2 XY { get; set; }



    /// <summary>
    /// Constructor
    /// </summary>
    public Blocks() { }


    /// <summary>
    /// Clone a block from this block
    /// </summary>
    /// <return name="Block">The new block</return>
    public Blocks Clone()
    {
    
        return null;
    }

    /// <summary>
    /// Dispose of this block.
    /// </summary>
    public void Dispose() 
    { 
    }

    public void SetMutator() 
    {
    }

    /// <summary>
    /// Change the shape of the block
    /// </summary>
    public void Reshape() { }

    /// <summary>
    /// Unplug this block from its superior block
    /// </summary>
    public void UnPlug() { }

    /// <summary>
    /// Return s all connections orgination from this block.
    /// </summary>

    public List<BlocksConnections> GetConnections()
    {
        return null;
    }
    /// <summary>
    /// Walks down a stack of blocks and finds the last next connection on the stack.
    /// </summary>
    public BlocksConnections LastConnectionInStack()
    {
        return null;
    }



}

