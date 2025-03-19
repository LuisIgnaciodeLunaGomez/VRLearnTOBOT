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
    public string type { get; private set; }

    public BlockDataLoader.BlockData blockData { get; private set; }

    public void Initialize(BlockDataLoader.BlockData blockData)
    {
        this.blockData = blockData;

        if (blockData.args != null)
        {
            foreach (var arg in blockData.args)
            {
                if (arg.type == "input")
                {
                    Input input = new Input(arg.name, EConnection.InputValue, arg.defaultValue);
                    input.sourceBlock = this;
                    AppendInput(input);
                }
            }
        }
    }

    public Vector2 XY { get; set; }
    //private bool m_Disabled = false;

    //Espacio de trabajo al que pertenece el bloque
    public WorkSpace workSpace { get; set; }

    public BlockBehaviour behaviour { get; private set; }

    //Conexiones de los bloques
    public BlockConnection outputConnection { get; set; }
    public BlockConnection previousConnection { get; set; }
    public BlockConnection nextConnection { get; set; }

    private Block m_SourceBlock; //Bloque al que pertenece la conexión

    //Lista que contiene las entradas de los bloques donde podemos conectar otros bloques
    public List<Input> inputList { get; protected set; }

    //Jearquía de bloques

    public Block parentBlock { get; protected set; }
    public List<Block> childBlocks = new List<Block>();

    //Faltaría crear el Mutator para los bloques que lo necesiten

    public Block(string type, Vector2 position, WorkSpace workSpace)
    {
        this.ID = Utilidades.GenUid();
        this.type = type;
        this.XY = position;

        //Falta añadir el bloque a la base de datos de bloques
        //workSpace.BlockDB.Add(ID, this);

        this.workSpace = workSpace;

        //Crear  conexiones entre bloques
        // Inicializar conexiones con tipos específicos
        this.previousConnection = new BlockConnection(null, EConnection.PrevStatement);
        this.nextConnection = new BlockConnection(null, EConnection.NextStatement);
        this.outputConnection = new BlockConnection(null, EConnection.OutputValue);

        this.inputList = new List<Input>();
        this.childBlocks = new List<Block>();

        workSpace.AddTopBlocks(this); //Añade el bloque a la lista de bloques principales del espacio de trabajo 24/02/2025

    }


    public void SetBlockBehaviour(BlockBehaviour behaviour)
    {
        this.behaviour = behaviour;
        if (this.previousConnection != null) this.previousConnection.sourceBlock = behaviour;
        if (this.nextConnection != null) this.nextConnection.sourceBlock = behaviour;
        if (this.outputConnection != null) this.outputConnection.sourceBlock = behaviour;
       
        foreach (var input in inputList)
        {
            if (input.Connection != null)
            {
                input.Connection.sourceBlock = behaviour;
            }
        }
    }

    /**
     * Descripcion: Método que verifica si tiene un input con el nombre especificado
     * @param: string name
     * 
     */
    public bool HasInput(string name) =>this.inputList.Any(t => t.Name.Equals(name));

    public Block Clone() => new Block(this.type, this.XY, this.workSpace);

    // Obtiene el siguiente bloque en la secuencia de bloques
    public Block NextBlock => nextConnection?.TargetBlock?.blockModel;

    public void Dispose()
    {
        workSpace.BlockDB.Remove(ID); //Elimina el bloque del diccionario de bloques del espacio de trabajo
    }


    public void UnPlug(bool optHealStack = false)
    {
        if (this.outputConnection != null)
        {
            if (this.outputConnection.isConnected)
                this.outputConnection.Disconnect();
        }
        else if (this.previousConnection != null)
        {
            BlockConnection previousTarget = null;
            if (this.previousConnection.isConnected)
            {
                previousTarget = this.previousConnection.targetConnection;
                this.previousConnection.Disconnect();
            }
            Block nextBlock = this.NextBlock;
            if (optHealStack && nextBlock != null)
            {
                var nextTarget = this.nextConnection.targetConnection;
                nextTarget?.Disconnect();
                if (previousTarget != null && previousTarget.CheckType(nextTarget))
                {
                    previousTarget.Connect(nextTarget);
                }
            }
        }

        if (parentBlock != null)
        {
            parentBlock.childBlocks.Remove(this);
            parentBlock = null;
        }
        childBlocks.Clear();
    }

    // Obtiene todas las conexiones de un bloque
    public List<BlockConnection> GetConnection()
    {

        List<BlockConnection> connections = new List<BlockConnection>();
        if (this.outputConnection != null)
        {
            connections.Add(this.outputConnection);
        }
        if (this.previousConnection != null)
        {
            connections.Add(this.previousConnection);
        }
        if (this.nextConnection != null)
        {
            connections.Add(this.nextConnection);
        }
        return connections;

    }

# region Métodos para manejar entradas de bloques o Inputs

    //añade entradas a un bloque
    public void AppendInput(Input input, int index = -1)
    {
        if (!this.inputList.Contains(input))
        {
            input.sourceBlock = this;
            if (index > 0) this.inputList.Insert(index, input);
            else this.inputList.Add(input);


            //TOOD: Revisar si es necesario notificar una actualización del bloque
        }
    }

    public void RemoveInput(Input input)
    {
        if (this.inputList.Contains(input))
        {
            this.inputList.Remove(input);

            //TOOD: Revisar si es necesario notificar una actualización del bloque
        }
    }

    #region Métodos para manejar campos de bloques

    public Input GetInput(string name) => this.inputList.FirstOrDefault(i => i.Name.Equals(name));

    public Input GetInputWithBlock(Block block) => this.inputList.FirstOrDefault(i => i.Connection?.TargetBlock?.blockModel == block);

    public Block GetInputTargetBlock(string name) => GetInput(name)?.Connection?.TargetBlock?.blockModel;

    #endregion

    public List<string> GetVar() => new List<string>();

    public void RenameVar(string oldName, string newName) { }
    
    public string GetFieldValue(string name) => null; 
    public void SetFieldValue(string name, string value) { }    

    #endregion

    #region Métodos para manejar jerarquía de bloques
    public Block GetSurroundParent() => this.parentBlock; // Implementación básica

    public void SetParent(Block newParent)
    {

        if (this.parentBlock == newParent) return;

        if (this.parentBlock != null)
        {
            this.parentBlock.childBlocks.Remove(this);
        }
        else
        {
            this.workSpace.RemoveTopBlock(this);
        }

        this.parentBlock = newParent;
        if (this.parentBlock != null)
        {
            this.parentBlock.childBlocks.Add(this);
        }
        else
        {
            workSpace.AddTopBlocks(this);
        }

    }

    public void UpdateConnectionPositions()
    {
        /* if (previousConnection != null) previousConnection.position = XY;
         if (nextConnection != null) nextConnection.position = XY + new Vector2(0, behaviour != null ? behaviour.GetComponent<RectTransform>().rect.height : 30f);
         foreach (var input in inputList)
         {
             if (input.Connection != null)
             {
                 input.Connection.position = XY;
             }
         }*/

        if (previousConnection != null)
        {
            RectTransform rect = behaviour?.GetComponent<RectTransform>();
            if (rect != null)
            {
                previousConnection.position = rect.anchoredPosition + new Vector2(0, rect.rect.height); // Parte superior

                Debug.Log($"UpdateConnectionPosition: Block: PreviousConnection position updated to {previousConnection.position} for block {type}");
            }
            else
            {
                previousConnection.position = XY;
                Debug.LogWarning($"UpdateConnectionPosition: Block:No RectTransform found for block {type}, using XY: {XY}");
            }
        }
        if (nextConnection != null)
        {
            RectTransform rect = behaviour?.GetComponent<RectTransform>();
            if (rect != null)
            {
                nextConnection.position = rect.anchoredPosition; // Parte inferior
                Debug.Log($"UpdateConnectionPosition: Block: NextConnection position updated to {nextConnection.position} for block {type}");
            }
            else
            {
                nextConnection.position = XY + new Vector2(0, behaviour != null ? behaviour.GetComponent<RectTransform>().rect.height : 30f);

                Debug.LogWarning($"UpdateConnectionPosition: Block: No RectTransform found for block {type}, using XY: {XY}");

            }
        }
        foreach (var input in inputList)

        {
            if (input.Connection != null)
            {
                RectTransform rect = behaviour?.GetComponent<RectTransform>();
                if (rect != null)
                {
                    input.Connection.position = rect.anchoredPosition;
                    Debug.Log($"UpdateConnectionPosition: Block: InputConnection position updated to {input.Connection.position} for block {type}");
                }
                else
                {
                    input.Connection.position = XY;
                    Debug.LogWarning($"UpdateConnectionPosition: Block: No RectTransform found for block {type}, using XY: {XY}");
                }
            }
        }

    }


    public List<Block> GetDescendants() => new List<Block> { this }.Concat(this.childBlocks.SelectMany(c => c.GetDescendants())).ToList();

    #endregion
}

