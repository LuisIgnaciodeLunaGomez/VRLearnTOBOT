/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkSpace : MonoBehaviour
{
    public string Id;
    private static Dictionary<string, WorkSpace> m_WorkspaceDB = new Dictionary<string, WorkSpace>();
    public List<Block> TopBlocks { get; private set; } //Lista de bloques principales en el espacio de trabajo
    public Dictionary<string, Block> BlockDB { get; private set; }//Diccionario de bloques en el espacio de trabajo
    private GameObject m_MiddlePanel;
    private GameObject m_RightPanel;
    public WorkSpace(string optId = null)
    {
        Id = optId ?? Utilidades.GenUid();

        if (m_WorkspaceDB.ContainsKey(Id))
        {
            m_WorkspaceDB[Id] = this;
            Debug.LogWarning("Ya existe un espacio de trabajo con el ID" + Id);
        }

        else
        {
            m_WorkspaceDB.Add(Id, this);
        }
        TopBlocks = new List<Block>();
        BlockDB = new Dictionary<string, Block>();
    }


    // Obtiene todos los bloques principales
    public List<Block> GetTopBlocks()
    {
        return new List<Block>(TopBlocks);
    }

    // Limpia el espacio de trabajo
    public void Clear()
    {

        while (TopBlocks.Count > 0)
        {
            
            Block block = TopBlocks[TopBlocks.Count - 1];
            TopBlocks.RemoveAt(TopBlocks.Count - 1);
            block = null; // Libera memoria
        }

        BlockDB.Clear();
    }

   //Obtener un blooque por su ID
   public Block GetBlockByID(string ID)
    {
        Block block = null; //Crea un bloque
        BlockDB.TryGetValue(ID, out block); //Conecta con la base de datos para obtener el valor
        return block; //Devuelve el bloque
    }


    //Obtiene todos los bloques en el espacio de trabajo
    public List<Block> GetAllBlocks()
    {
        return new List<Block>(BlockDB.Values);

    }

    //Agrega un bloque principal al espacio de trabajo
    public void AddTopBlocks(Block block)
    {
        if(!TopBlocks.Contains(block))
        {
            TopBlocks.Add(block);
        }
        if(BlockDB.ContainsKey(block.ID))
        {
            BlockDB[block.ID] = block;
        }
        else
        {
            BlockDB.Add(block.ID, block);
        }
    }

    //Elimna un bloque principal del espacio de trabajo
    public void RemoveTopBlock(Block block)
    {
        if (TopBlocks.Contains(block))
        {
            TopBlocks.Remove(block);
        }
        if (BlockDB.ContainsKey(block.ID))
        {
            BlockDB.Remove(block.ID);
        }
    }

    //Recupera un espacio de trabajo por su ID
    public static WorkSpace GetWorkSpace(string ID)
    {
        return m_WorkspaceDB.TryGetValue(ID, out var workspace) ? workspace : null;
    }

    //Elimina un espacio de trabajo
    public void Dispose()
    {
        m_WorkspaceDB.Remove(Id);
        Clear();
    }

    public void Initizalized(GameObject middle, GameObject right)
    {
        
        this.m_MiddlePanel = middle;
        this.m_RightPanel = right;
        Debug.Log("WorksPace inizializado con MiddlePanel y RightPanel");
    }

   
}
