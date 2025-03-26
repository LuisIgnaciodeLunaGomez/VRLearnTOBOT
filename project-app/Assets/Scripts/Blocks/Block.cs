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
 * Versión: 1.0.1
 * 
 * Descripción: Clásica que aplica el módelo lógico de un bloque
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Block: Observable<int>
{

    public string ID { get; private set; } //Identificador único del bloque
    public string type { get; private set; } //Tipo de bloque se define en el XML

    public BlockDataLoader.BlockData blockData { get; private set; } //Sistema de carga de datos del xml
  
    public void Initialize(BlockDataLoader.BlockData blockData)
    {
        this.blockData = blockData;

        if (blockData == null) return;

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
    private bool m_Disabled = false;    //Flag para indicar si el bloque está deshabilitado
    private bool m_Movable = true;      //Flag para indicar si el bloque es movible
    private bool m_Deletable = true;    //Flag para indicar si el bloque es eliminable
    private bool m_IsShadow = false;    //Flag para indicar que es un bloque sombra es decir permite conexiones de otros bloques
    private bool m_Collapsed = false;   //Flag para indicar si el bloque es colapsable
    private bool m_Editable = true;     //Flag para indicar si el bloque es editable
    private int mInputsInlineState = -1; // -1: not defined; 0: defined false; 1: defined true


    public Vector2 XY { get; set; }
    //private bool m_Disabled = false;

    //Espacio de trabajo al que pertenece el bloque
    public WorkSpace workSpace { get; set; }

    public BlockBehaviour behaviour { get; private set; }

    //Conexiones de los bloques
    public BlockConnection outputConnection { get; set; }
    public BlockConnection previousConnection { get; set; }
    public BlockConnection nextConnection { get; set; }
    public List<Input> InputList { get; protected set; } //Lista de entradas del bloque  puede contener texto, número, variable y otros bloques mediante una ConnectionIput donde conectar un bloque hijo


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

    /**
 * Descripción: Verifica si todas las entradas requeridas del bloque están conectadas
 * @param includeShadows: Determina si los bloques sombra cuentan como entradas válidas
 * @return: true si todas las entradas están rellenas, false en caso contrario
 */
    public bool AllInputsFilled(bool includeShadows = true)
    {
        // Verifica cada entrada en la lista
        foreach (var input in inputList)
        {
            if (input.Connection != null)
            {
                var targetBlock = input.Connection.TargetBlock;
                if (targetBlock == null || (!includeShadows && targetBlock.IsShadow))
                {
                    return false;
                }

                // Verificación recursiva para bloques anidados
                if (!targetBlock.blockModel.AllInputsFilled(includeShadows))
                {
                    return false;
                }
            }
        }

        // Verifica el siguiente bloque en la cadena
        var next = NextBlock;
        if (next != null)
        {
            return next.AllInputsFilled(includeShadows);
        }

        return true;
    }


    /**
     * Descripción: Encuentra la última conexión disponible en una cadena de bloques
     * @return: La última conexión disponible o null si no hay ninguna
     */
    public BlockConnection LastConnectionInStack()
    {
        var current = this;
        var nextConnection = current.nextConnection;

        while (nextConnection != null)
        {
            var nextBlock = nextConnection.TargetBlock?.blockModel;
            if (nextBlock == null)
            {
                // Encontró una conexión siguiente sin nada conectado
                return nextConnection;
            }
            current = nextBlock;
            nextConnection = current.nextConnection;
        }

        // No hay más conexiones disponibles
        return null;
    }


    /**
     * Descripción: Obtiene todos los bloques descendientes (hijos, nietos, etc.)
     * @return: Lista con todos los bloques descendientes incluyendo this
     */
    public List<Block> GetAllDescendants()
    {
        var result = new List<Block> { this };

        // Agrega los descendientes de los bloques hijo
        foreach (var child in childBlocks)
        {
            result.AddRange(child.GetAllDescendants());
        }

        // Agrega los descendientes de los bloques de entrada
        foreach (var input in inputList)
        {
            var targetBlock = input.Connection?.TargetBlock?.blockModel;
            if (targetBlock != null)
            {
                result.AddRange(targetBlock.GetAllDescendants());
            }
        }

        // Agrega los bloques en la cadena
        var nextBlock = NextBlock;
        if (nextBlock != null)
        {
            result.Add(nextBlock);
            result.AddRange(nextBlock.GetAllDescendants());
        }

        return result;
    }

    /**
 * Descripción: Verifica si el bloque es raíz en el workspace
 * @return: true si es raíz, false en caso contrario
 */
    public bool IsRootBlock()
    {
        return parentBlock == null &&
               (previousConnection == null || !previousConnection.isConnected) &&
               (outputConnection == null || !outputConnection.isConnected);
    }

    /**
 * Descripción: Encuentra conexiones compatibles entre este bloque y otro
 * @param otherBlock: Bloque con el que verificar compatibilidad
 * @return: Lista de pares de conexiones compatibles
 */
    public List<(BlockConnection, BlockConnection)> FindCompatibleConnections(Block otherBlock)
    {
        var result = new List<(BlockConnection, BlockConnection)>();
        var myConnections = GetConnection();
        var otherConnections = otherBlock.GetConnection();

        foreach (var myConn in myConnections)
        {
            foreach (var otherConn in otherConnections)
            {
                if (myConn.CheckType(otherConn))
                {
                    result.Add((myConn, otherConn));
                }
            }
        }

        return result;
    }

    /**
     * Descripción: Obtiene el bloque raíz de la jerarquía actual
     * @return: El bloque raíz
     */
    public Block GetRootBlock()
    {
        Block rootBlock = this;

        while (rootBlock.parentBlock != null)
        {
            rootBlock = rootBlock.parentBlock;
        }

        return rootBlock;
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


    /**
     * Descripción: Actualiza propiedades de estado del bloque y notifica a los observadores
     * @param disabled: Estado de deshabilitado
     * @param movable: Estado de movible
     * @param deletable: Estado de eliminable
     */
    public void UpdateState(bool? disabled = null, bool? movable = null, bool? deletable = null)
    {
        int updateMask = 0;

        if (disabled.HasValue)
        {
            m_Disabled = disabled.Value;
            updateMask |= 1 << 3; // UpdateState.IsDisabled = 3
        }

        if (movable.HasValue)
        {
            m_Movable = movable.Value;
            updateMask |= 1 << 7; // UpdateState.IsMovable = 7
        }

        if (deletable.HasValue)
        {
            m_Deletable = deletable.Value;
            updateMask |= 1 << 6; // UpdateState.IsDeletable = 6
        }

        if (updateMask > 0)
        {
            FireUpdate(updateMask);
        }
    }

    #region Métodos para manejar entradas de bloques o Inputs

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

   

    /**
       * Descripción: Devuelve un campo de tipo Field detnro de un bloque que contenga un nombre específico para dicho campo (etiquetas visibles, valores de texto editables, menús desplegables (dropdowns), variables, etc.
       * @return: Field field 
       */
    public Field GetField(string name)
    {
        //TODO
        return null;
    }

    /**
     * Descripción: Obtiene las variables de un bloque
     * @return: List<string>: Lista de variables
     */
    public List<string> GetVars()
    {
        //TODO
        return null; //Devuelvo la lista de variables
    }
    /**
     * Descripción: Renombra una variable de un bloque
     * @param: Antigua variable
     * @param: Nueva variable
     */
    public void RenameVar(string oldName, string newName)
    {
        //TODO
        
    }

    /**
        * Descripción: Obtiene el valor de un campo de un bloque
        * @param: name: Nombre del campo
        */
    public string GetFieldValue(string name)
    {
        //TODO
        return null;
    }
    /**
       * Descripción: Cambia el valor de un campo de un bloque
       * @param: name: Nombre del campo
       * @param: newValue: Nuevo valor del campo
       */
    public void SetFieldValue(string name, string newValue)
    {
       //TODO
    }

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

        if (previousConnection != null)
        {
            RectTransform rect = behaviour?.GetComponent<RectTransform>();
            if (rect != null)
            {
                previousConnection.position = rect.anchoredPosition + new Vector2(0, rect.rect.height); // Parte superior

               // Debug.Log($"UpdateConnectionPosition: Block: PreviousConnection position updated to {previousConnection.position} for block {type}");
            }
            else
            {
                previousConnection.position = XY;
               // Debug.LogWarning($"UpdateConnectionPosition: Block:No RectTransform found for block {type}, using XY: {XY}");
            }
        }
        if (nextConnection != null)
        {
            RectTransform rect = behaviour?.GetComponent<RectTransform>();
            if (rect != null)
            {
                nextConnection.position = rect.anchoredPosition; // Parte inferior
              //  Debug.Log($"UpdateConnectionPosition: Block: NextConnection position updated to {nextConnection.position} for block {type}");
            }
            else
            {
                nextConnection.position = XY + new Vector2(0, behaviour != null ? behaviour.GetComponent<RectTransform>().rect.height : 30f);

               // Debug.LogWarning($"UpdateConnectionPosition: Block: No RectTransform found for block {type}, using XY: {XY}");

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
                //    Debug.Log($"UpdateConnectionPosition: Block: InputConnection position updated to {input.Connection.position} for block {type}");
                }
                else
                {
                    input.Connection.position = XY;
                //    Debug.LogWarning($"UpdateConnectionPosition: Block: No RectTransform found for block {type}, using XY: {XY}");
                }
            }
        }

    }

    /**
     * Descripción: Devuelve todos los bloques que son descendientes directos de este bloque.
     * @return List<Block>: Lista de bloques descendientes.
     */
    public List<Block> GetDescendants()
    {
        var blocks = new List<Block> { this }; //Añade el bloque actual a la lista de bloques this hace referencia al Block actual

        for (int i = 0; i < childBlocks.Count; i++)
        {
            blocks.AddRange(childBlocks[i].GetDescendants()); //Añade los bloques descendientes de este bloque si existen
        }

        return blocks; //Devuelve los bloques
    }
    #endregion

    #region Propiedades de estado
    public bool Disabled
    {
        get { return m_Disabled; } //getter para obtener el valor de mDisabled
        set
        {
            if (Disabled != value) //Solo actua si hay cambio
            {
                m_Disabled = value;
                FireUpdate(1 << (int)UpdateStates.IsDisabled); //Si el valor de Disabled es distinto al valor pasado por parámetro se llama a FireUpdate para notificar a las vistas que ha habido un cambio
            }
        }
    }

    public bool Deletable
    {
        get { return m_Deletable && !m_IsShadow && !(workSpace != null && workSpace.Options.ReadOnly); } //getter para obtener el valor de mDeletable y comprobar si el bloque es una sombra o si el espacio de trabajo es de solo lectura
        set
        {
            if (m_Deletable != value)//Solo actua si hay cambio
            {
                m_Deletable = value;
                FireUpdate(1 << (int)UpdateStates.IsDeletable); //Si el valor de Deletable es distinto al valor pasado por parámetro se llama a FireUpdate para notificar a las vistas que ha habido un cambio
            }
        }
    }

    public bool Movable
    {
        get { return m_Movable && !m_IsShadow && !(workSpace != null && workSpace.Options.ReadOnly); } //getter para obtener el valor de mMovable y comprobar si el bloque es una sombra o si el espacio de trabajo es de solo lectura
        set
        {
            if (m_Movable != value) //Solo actua si hay cambio
            {
                m_Movable = value;
                FireUpdate(1 << (int)UpdateStates.IsMovable); //Si el valor de Movable es distinto al valor pasado por parámetro se llama a FireUpdate para notificar a las vistas que ha habido un cambio
            }
        }
    }

    public bool IsShadow
    {
        get { return m_IsShadow; } //getter para obtener el valor de mIsShadow
        set
        {
            if (m_IsShadow != value)//Solo actua si hay cambio
            {
                m_IsShadow = value;
                FireUpdate(1 << (int)UpdateStates.IsShadow); //Si el valor de IsShadow es distinto al valor pasado por parámetro se llama a FireUpdate para notificar a las vistas que ha habido un cambio
            }
        }
    }

    public bool Editable
    {
        get { return m_Editable && !(workSpace != null && workSpace.Options.ReadOnly); }
        set
        {
            if (m_Editable != value)//Solo actua si hay cambio
            {
                m_Editable = value;
                FireUpdate(1 << (int)UpdateStates.IsEditable);
            }
        }
    }

    public bool Collapsed
    {
        get { return m_Collapsed; } //getter para obtener el valor de mCollapsed
        set
        {
            if (m_Collapsed != value) //Solo actua si hay cambio
            {
                m_Collapsed = value;
                FireUpdate(1 << (int)UpdateStates.IsCollapsed); //Si el valor de Collapsed es distinto al valor pasado por parámetro se llama a FireUpdate para notificar a las vistas que ha habido un cambio
            }
        }
    }

    #endregion


    /**
     * Descripción: Permite establecer la orientación e las entrddas en una lista
     * @param: bool value True or False 
     */
    public void SetInputsInline(bool value)
    {
        if (value && mInputsInlineState != 1) //Valor existe y mInputsInlineState es distinto de 1
        {
            mInputsInlineState = 1; //mInputsInlineState igual a 1 - en línea activado Horizontal
            FireUpdate(1 << (int)UpdateStates.IsInputInline); //Llama a FireUpdate para notificar a las vistas que ha habido un cambio
        }
        else if (!value && mInputsInlineState != 0) //si es distinto de 0
        {
            mInputsInlineState = 0; //mInputsInlineState = 0 - en línea desactivado Vertical
            FireUpdate(1 << (int)UpdateStates.IsInputInline); //mInputsInlineState
        }
    }

    /**
     * Descripción: Determina si las entradas del bloque deben mostrarse en línea (horizontalmente) o en columna (verticalmente)
     * Esencial para el diseño de bloques de tipo aritmético, texto, lógico, etc.
     * @return bool: True or False
     */
    public bool GetInputsInline()
    {
        //todo
        return false;
    }

    /**
     * Descripción: Devuelve TRUE si alguno de los bloques que envuelven al actual está deshabilitado
     */
    public bool GetInheritedDisabled()
    {
        var ancestor = this.GetSurroundParent(); //Obtiene el bloque padre
        while (ancestor != null)
        {
            if (ancestor.Disabled) //si el ancestro es disable devuelve True
            {
                return true;
            }
            ancestor = ancestor.GetSurroundParent(); //Si no es disable se obtiene el bloque padre del ancestro
        }
       
        return false; //No es disable
    }

    /**
      * Descripción: Devuelve 
      * @param return string msg
      */
    public string ToDevString()
    {
        //TODO

       return null;
    }
}

