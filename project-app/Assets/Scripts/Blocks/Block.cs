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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Block
{

    public string ID { get; private set; }
    public string Type { get; private set; }

    public BlockDataLoader.BlockData BlockData { get; private set; }

    public void Initialize(BlockDataLoader.BlockData blockData)
    {
        this.BlockData = blockData;
    }

    public Vector2 XY { get; set; }
    //private bool m_Disabled = false;

    //Espacio de trabajo al que pertenece el bloque
    public WorkSpace workSpace { get; set; }

    //Conexiones de los bloques
    public BlockConnection OutputConnection { get; set; }
    public BlockConnection PreviousConnection { get; set; }
    public BlockConnection NextConnection { get; set; }

    //Lista que contiene las entradas de los bloques donde podemos conectar otros bloques
    public List<Input> InputList { get; protected set; }

    //Jearquía de bloques

    public Block ParentBlock { get; protected set; }
    public List<Block> ChildBlocks = new List<Block>();

    //Faltaría crear el Mutator para los bloques que lo necesiten

    public Block(string type, Vector2 position, WorkSpace workSpace)
    {
        this.ID = Utilidades.GenUid();
        this.Type = type;
        this.XY = position;

        //Falta añadir el bloque a la base de datos de bloques
        //workSpace.BlockDB.Add(ID, this);

        this.workSpace = workSpace;

        //Añadido el 24/02/2025 para crear las conexiones entre bloques
        this.OutputConnection = null;
        this.PreviousConnection = null;
        this.NextConnection = null;
        this.InputList = new List<Input>();

        workSpace.AddTopBlocks(this); //Añade el bloque a la lista de bloques principales del espacio de trabajo 24/02/2025

    }

    public bool HasInput(string name)
    {
        return InputList.Any(t => name.Equals(t.Name));
    }

    public Block Clone()
    {
        return new Block(this.Type, this.XY, this.workSpace);
    }

    public void Dispose()
    {
        workSpace.BlockDB.Remove(ID); //Elimina el bloque del diccionario de bloques del espacio de trabajo
    }

    // Obtiene el siguiente bloque en la secuencia de bloques
    public Block NextBlock
    {
        get { return null != NextConnection ? NextConnection.TargetBlock : null; }
    }

    public void UnPlug(bool optHealStack = false)
    {
        if (this.OutputConnection != null)
        {
            if (this.OutputConnection.IsConnected)
                this.OutputConnection.Disconnect();
        }
        else if (this.PreviousConnection != null)
        {
            BlockConnection previousTarget = null;
            if (this.PreviousConnection.IsConnected)
            {
                previousTarget = PreviousConnection.TargetConnection;
                PreviousConnection.Disconnect();
            }
            Block nextBlock = this.NextBlock;
            if (optHealStack && nextBlock != null)
            {
                var nextTarget = this.NextConnection.TargetConnection;
                nextTarget.Disconnect();
                if (previousTarget != null && previousTarget.CheckType(nextTarget))
                {
                    previousTarget.Connect(nextTarget);
                }
            }
        }
    }

    // Obtiene todas las conexiones de un bloque
    public List<BlockConnection> GetConnection()
    {

        List<BlockConnection> connections = new List<BlockConnection>();
        if (OutputConnection != null)
        {
            connections.Add(OutputConnection);
        }
        if (PreviousConnection != null)
        {
            connections.Add(PreviousConnection);
        }
        if (NextConnection != null)
        {
            connections.Add(NextConnection);
        }
        return connections;

    }

# region Métodos para manejar entradas de bloques o Inputs

    //añade entradas a un bloque
    public void AppendInput(Input input, int index = -1)
    {
        if (!InputList.Contains(input))
        {
            input.sourceBlock = this;
            if (index > 0) InputList.Insert(index, input);
            else InputList.Add(input);


            //TOOD: Revisar si es necesario notificar una actualización del bloque
        }
    }

    public void RemoveInput(Input input)
    {
        if (InputList.Contains(input))
        {
            InputList.Remove(input);

            //TOOD: Revisar si es necesario notificar una actualización del bloque
        }
    }

    public Input GetInput(string name)
    {
        //TODO: Buscar en InputList por nombre y retornar la entrada correspondiente.
        return null;

    }

    public Input GetInputWithBlock(Block block)
    {
        //TODO Recorrer InputList y encontrar si un bloque está conectado a alguna entrada
        return null;
    }

    public Block GetInputTargetBlock(string name)
    {
        //TODO Buscar en InputList si hay una conexión con otro bloque.
        return null;
    }

    #endregion

    public List<string> GetVar()
    {
        //TODO Recorrer los campos de tipo variable y devolver sus nombres
        return null;
    }

    public void RenameVar(string oldName, string newName)
    {
        //TODO Buscar en los campos del bloque la variable con oldName y cambiarla a newName
    }

    #region Métodos para manejar campos de bloques
    public string GetFiedlValue(string name)
    {
        //TODO: Recorrer la lista de campos del bloque y devolver el valor del campo con el nombre `name`

        return null;
    }

    public void SetFieldValue(string name, string value)
    {
        //TODO Buscar el campo por nombre y asignarle el nuevo valor
    }

    #endregion

    #region Métodos para manejar jerarquía de bloques
    public Block GetSurroundParen()
    {
        //TODO
        return null;
    }

    public void SetParent(Block newParent)
    {

        //TODO

    }

    public List<Block> GetDescendants()
    {
        //TODO
        return null;
    }

    #endregion
}

