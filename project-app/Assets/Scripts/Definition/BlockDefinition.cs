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
 * Descripción: interpreta los bloques desde JSON, permitiendo definir nuevos bloques sin modificar código
 */

using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using UnityEngine; 


public class BlockDefinition
{
    // Identificación y Categoría
    public string type;             // Nombre/tipo único del bloque (leído de atributo 'type')
    public string category;         // Nombre de la categoría (leído de atributo 'category' en <Blocks> o filename)
    public Color color;             // Color final (leído de <Colour> o color de categoría)

    // Configuración Visual
    public string spriteName;       // Nombre final del sprite a usar
    public bool inputsInline;       // Si las entradas van en línea (leído de <InputsInline>)
                                    // Cambiado de 'InputsInlineFromXml' para claridad.

    // Conexiones Superiores/Inferiores
    public bool hasOutput;          // Determinado por existencia de <Output>
    public bool hasPreviousStatement; // Determinado por existencia de <PreviousStatement>
    public bool hasNextStatement;   // Determinado por existencia de <NextStatement>

    // Checks de Tipo para Conexiones
    public List<string> outputChecks;       // Leído de atributo 'check' en <Output>
    public List<string> previousChecks;     // Leído de atributo 'check' en <PreviousStatement>
    public List<string> nextChecks;         // Leído de atributo 'check' en <NextStatement>

    // Argumentos/Entradas
    public List<ArgumentDefinition> args; // Lista de argumentos parseados de <Args>/<Arg>

    [Tooltip("Indica si este bloque tiene asociado un Mutator.")]
    public bool hasMutator; //  Determinado al parsear <Mutator>

    [Tooltip("Nombre/identificador del Mutator asociado (ej. controls_if_mutator).")]
    public string mutatorName; //  Leído del atributo del tag <Mutator>

   
    // Crea la lista de modelos de Input basándose en las definiciones de argumentos.
    
    
    public List<InputModel> CreateInputList()
    {
        List<InputModel> inputList = new List<InputModel>();
        if (this.args == null || this.args.Count == 0)
        {
            return inputList;
        }

        InputModel currentInput = null;

        foreach (ArgumentDefinition argDef in this.args)
        {
            if (argDef.IsInputDefinition)
            {
                
                string inputName = string.IsNullOrEmpty(argDef.name) ? $"INPUT_{inputList.Count}" : argDef.name;
                EConnection inputType = argDef.IsStatement ? EConnection.NextStatement : EConnection.InputValue;
                currentInput = new InputModel(inputType, inputName);
                currentInput.SetAlign(argDef.align);
                currentInput.SetCheck(argDef.checks); 

                inputList.Add(currentInput);
            }
            else // Es un argumento de campo (Field)
            {
                if (currentInput == null || currentInput.Type != EConnection.None) // Si no hay Input dummy o el anterior fue de conexión
                {
                    // Necesita un Input dummy para contener los campos
                    currentInput = new InputModel(EConnection.None, $"DUMMY_INPUT_{inputList.Count}"); 
                    currentInput.SetAlign(argDef.align);
                    inputList.Add(currentInput);
                }

               
                string fieldType = argDef.FieldType; // 
                JObject fieldJson = argDef.DefinitionJson; // 

                if (fieldJson != null && fieldJson["name"] == null && !string.IsNullOrEmpty(argDef.FieldName))
                {
                    fieldJson["name"] = argDef.FieldName; // Añadir el nombre al JSON si no está
                }

                FieldModel fieldModel = FieldFactory.CreateFromJson(fieldType, fieldJson);

                if (fieldModel != null)
                {
                    currentInput.AppendField(fieldModel);
                }
                else
                {
            
                     Debug.LogWarning($"BlockDefinition {this.type}: Could not create field of type {argDef.FieldType}");
                }
            }
        }

        return inputList;
    }

    
    // Crea el ConnectionModel para la salida (Output) si está definido.
  
    public ConnectionModel CreateOutputConnection()
    {
        if (this.hasOutput)
        {
            ConnectionModel outputConnection = new ConnectionModel(EConnection.OutputValue);
            // Añade los checks de tipo leídos del XML/JSON
            if (this.outputChecks != null)
            {
                outputConnection.SetCheck(this.outputChecks);
            }
            return outputConnection;
        }
        return null;
    }

    
    // Crea el ConnectionModel para la conexión superior (Previous Statement) si está definida.
    
    public ConnectionModel CreatePreviousStatementConnection()
    {
        if (this.hasPreviousStatement)
        {
            ConnectionModel prevConnection = new ConnectionModel(EConnection.PrevStatement);
            if (this.previousChecks != null)
            {
                prevConnection.SetCheck(this.previousChecks);
            }
            return prevConnection;
        }
        return null;
    }

   
    // Crea el ConnectionModel para la conexión inferior (Next Statement) si está definida.
    
    public ConnectionModel CreateNextStatementConnection()
    {
        if (this.hasNextStatement)
        {
            ConnectionModel nextConnection = new ConnectionModel(EConnection.NextStatement);
            if (this.nextChecks != null) 
            {
                nextConnection.SetCheck(this.nextChecks);
            }
            return nextConnection;
        }
        return null;
    }


    //Crea una instancia del Mutator si está definido.
    public Mutator CreateMutator()
    {
        if (this.hasMutator && !string.IsNullOrEmpty(this.mutatorName))
        {
            return MutatorFactory.Create(this.mutatorName);
        }
        return null;
    }

   
    // Obtiene el valor por defecto de InputsInline según la definición.
    public bool GetInputsInlineDefault()
    {
        // Devuelve el valor que se leyó del XML/JSON
        return this.inputsInline;
    }
}//Fin clase BlockDefinition




