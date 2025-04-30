/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/02/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Clase que se encarga de la creación de los bloques para cada categoría
 */

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine; 

public class BlockFactory
{
    private static BlockFactory mInstance = null;
    public static BlockFactory Instance
    {
        get { return mInstance ?? (mInstance = new BlockFactory()); }
    }

    // Dicionario de todos los bloques cargados (typeName -> BlockDefinition)
    private Dictionary<string, BlockDefinition> mDefinitions = new Dictionary<string, BlockDefinition>();

    //Devuelve todas las definiciones de bloques
    public Dictionary<string, BlockDefinition> GetAllBlockDefinitions()
    {
        return mDefinitions;
    }

    //Agrupa bloques por prefijos 
    private Dictionary<string, List<string>> mPrefixCategories = new Dictionary<string, List<string>>();

    // Devuelve los bloques que comparten un prefijo
    public List<string> GetBlockTypesOfPrefix(string prefix)
    {
        List<string> blockTypes;
        mPrefixCategories.TryGetValue(prefix, out blockTypes);
        return blockTypes;
    }

    // Borra todas las definiciones
    public void Clear()
    {
        mDefinitions.Clear();
        mPrefixCategories.Clear();
    }

    // Crea un bloque basado en su tipo
    public BlockModel CreateBlock(WorkSpaceModel workspace, string type, string uid = null)
    {
        
        BlockModel block; 

        string finalUid = string.IsNullOrEmpty(uid) ? Utilidades.GenUid() : uid; 

        if (workspace == null)
        {
            //Creando una plantilla para el Toolbox (sin ws)
           // Debug.Log($"BlockFactory: Creating TEMPLATE block: {type} (ID: {finalUid})");
            block = BlockModel.CreateTemplate(type, finalUid);                                               
        }
        else
        {
            // Creando un bloque real para el workspace
            if (!string.IsNullOrEmpty(uid) && workspace.GetBlockById(uid) != null)
            {
                Debug.LogWarning($"BlockFactory: Block with provided ID '{uid}' already exists in workspace. Generating new ID.");
                finalUid = Utilidades.GenUid();
            }
            Debug.Log($"BlockFactory: Creating workspace block: {type} (ID: {finalUid}) for Workspace {workspace.Id}");
            block = new BlockModel(workspace, type, finalUid); // constructor original que registra en el workspace
        }

        BlockDefinition definition;
        if (!mDefinitions.TryGetValue(type, out definition))
        {
            Debug.LogWarning($"BlockFactory: No definition for block type '{type}'. Creating basic block structure.");
        }
        else
        {
            List<InputModel> inputs = definition.CreateInputList();
         //   Debug.Log("BlockFactory: Created InputModel list for block: " + string.Join(", ", inputs.Select(i => i.Name).ToArray()));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (inputs != null)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    sb.Append(inputs[i]?.Name ?? "NULL_INPUT");
                    if (i < inputs.Count - 1) sb.Append(", ");
                }
            }
          //  Debug.Log($"BlockFactory: Corrected InputModel List: [{sb.ToString()}] (Count: {inputs?.Count ?? 0})");
            ConnectionModel output = definition.CreateOutputConnection();

           // Debug.Log($"BlockFactory: Created OutputConnectionModel for block '{type}' (ID: {block.ID}). Has output connection: {(output != null)}.");
            ConnectionModel prev = definition.CreatePreviousStatementConnection();

           // Debug.Log($"BlockFactory: Created PreviousStatementConnectionModel for block '{type}' (ID: {block.ID}). Has previous connection: {(prev != null)}.");   
            ConnectionModel next = definition.CreateNextStatementConnection();

          //  Debug.Log($"BlockFactory: Created NextStatementConnectionModel for block '{type}' (ID: {block.ID}). Has next connection: {(next != null)}.");
           // Mutator mutator = definition.CreateMutator();
            bool inputsInline = definition.GetInputsInlineDefault();
            // Debug.Log($"BlockFactory: Created Mutator for block '{type}' (ID: {block.ID}). Has mutator: {(mutator != null)}. Inputs inline default: {inputsInline}.");

           // Debug.Log($"BlockFactory: Created BlockModel '{type}' (ID: {block.ID}). Created Connections: Output={output != null}, Prev={prev != null}, Next={next != null}. Inputs Count={inputs?.Count ?? 0}.");

            block.Reshape(inputs, output, prev, next);

            if (inputs != null)
            {
                foreach (InputModel input in inputs)
                {
                    input.SourceBlock = block; // Set Input's SourceBlock
                    if (input.Connection != null) input.Connection.SourceBlock = block; 
                                                                                        
                    if (input.FieldRow != null) foreach (FieldModel field in input.FieldRow) field.SourceBlock = block; 
                }
            }
            foreach (InputModel input in block.InputList)
            {
               // Debug.Log($"  InputModel '{input.Name}' (Type:{input.Type}) assigned to Block. Has ConnectionModel: {(input.Connection != null)}", null);
            }

          //  if (mutator != null) block.SetMutator(mutator);
            //if (inputsInline) block.SetInputsInline(true);
            if (inputsInline != block.GetInputsInline())
            {
                block.SetInputsInline(inputsInline);
            }

            // Asignamos el SourceBlock a inputs/connections creados
            foreach (var input in inputs) { input.SourceBlock = block; }
            if (output != null) output.SourceBlock = block;
            if (prev != null) prev.SourceBlock = block;
            if (next != null) next.SourceBlock = block;
        }
      //  Debug.Log($"BlockFactory: Finished creating BlockModel '{type}' (ID: {block.ID}).", null);
        //Debug.Log($"BlockFactory: Finished creating BlockModel '{type}' (ID: {block.ID}). Final Block Connections: Output={(block.OutputConnection != null)}, Prev={(block.PreviousConnection != null)}, Next={(block.NextConnection != null)}. Inputs Count={(block.InputList != null ? block.InputList.Count : 0)}.");

        return block;
    }

    //Creamos un Bloque desde el XML
    public static BlockModel CreateFromXml(WorkSpaceModel workspace, XElement xmlBlock)
    {
        if (workspace == null || xmlBlock == null) return null;

        string blockType = xmlBlock.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(blockType))
        {
            Debug.LogError("BlockFactory.CreateFromXml: Block XML is missing the 'type' attribute.");
            return null;
        }

        string id = xmlBlock.Attribute("id")?.Value; 
      
        BlockModel block = Instance.CreateBlock(workspace, blockType, id); 
        if (block == null)
        {
            Debug.LogError($"BlockFactory.CreateFromXml: Failed to create basic block of type '{blockType}'.");
            return null; 
        }

        block.IsShadow = (xmlBlock.Name.LocalName.ToLower() == "shadow");

        block.Collapsed = bool.TryParse(xmlBlock.Attribute("collapsed")?.Value ?? "false", out bool collapsed) && collapsed;
        block.Disabled = bool.TryParse(xmlBlock.Attribute("disabled")?.Value ?? "false", out bool disabled) && disabled;
        if (!block.IsShadow)
        {
            block.Deletable = bool.TryParse(xmlBlock.Attribute("deletable")?.Value ?? "true", out bool deletable) && deletable;
            block.Movable = bool.TryParse(xmlBlock.Attribute("movable")?.Value ?? "true", out bool movable) && movable;
            block.Editable = bool.TryParse(xmlBlock.Attribute("editable")?.Value ?? "true", out bool editable) && editable;
            // block.SetInputsInline(bool.TryParse(xmlBlock.Attribute("inline")?.Value ?? "false", out bool inline) && inline); 
        }

        foreach (XElement fieldNode in xmlBlock.Elements("field"))
        {
            string fieldName = fieldNode.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(fieldName)) continue;

            FieldModel field = block.GetField(fieldName); 
            if (field != null)
            {
                field.SetValue(fieldNode.Value); 
                if (field is FieldVariableModel varField)
                {
                    string variableId = fieldNode.Value;
                    VariableModel variable = workspace.VariableMap?.GetVariableById(variableId); 
                    if (variable != null)
                    {
                        varField.SetValue(variable.Name); 
                    }
                    else
                    {
                   
                        Debug.LogWarning($"CreateFromXml: Variable with ID '{variableId}' not found for field '{fieldName}' in block '{block.ID}'. Field might be empty or have default value.");
                        varField.SetValue("default_var"); 
                    }
                }

            }
            else
            {
                Debug.LogWarning($"CreateFromXml: Field '{fieldName}' not found in block model '{blockType}'. Skipping XML field.");
            }
        }

        foreach (XElement inputContainerNode in xmlBlock.Elements())
        {
            BlockModel childBlock = null; 

            if (inputContainerNode.Name.LocalName == "value" || inputContainerNode.Name.LocalName == "statement")
            {
                string inputName = inputContainerNode.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(inputName)) continue;

                InputModel input = block.GetInput(inputName); 
                if (input?.Connection == null)
                {
                    Debug.LogWarning($"CreateFromXml: Input '{inputName}' not found or has no connection in block model '{blockType}'. Skipping XML input container.");
                    continue;
                }
               
                XElement childNode = inputContainerNode.Elements().FirstOrDefault(); 
                if (childNode != null && (childNode.Name.LocalName == "block" || childNode.Name.LocalName == "shadow"))
                {
                    childBlock = CreateFromXml(workspace, childNode); 

                    if (childBlock != null)
                    {
                        
                        ConnectionModel parentConnection = input.Connection;
                        ConnectionModel childConnection = (input.Type == EConnection.InputValue) ? childBlock.OutputConnection : childBlock.PreviousConnection;

                       if (parentConnection != null && childConnection != null && parentConnection.CanConnectWithReason(childConnection) == ConnectionModel.CAN_CONNECT) 
                {
                    parentConnection.Connect(childConnection);
                }
                else
                {
                    int reasonCode = (parentConnection == null || childConnection == null) ? -1 : parentConnection.CanConnectWithReason(childConnection);
                    Debug.LogError($"CreateFromXml: Failed to connect blocks! Parent: {block.ID}.{inputName} - Child: {childBlock.ID}. Reason Code: {reasonCode}. Incompatible or null connection.");
                    childBlock.Dispose(false);
                }
                    }
                }
            }
        }

        XElement nextContainerNode = xmlBlock.Element("next");
        if (nextContainerNode != null)
        {
            // Buscams un block o shadow dentro de next
            XElement nextBlockNode = nextContainerNode.Elements().FirstOrDefault();
            if (nextBlockNode != null && (nextBlockNode.Name.LocalName == "block" || nextBlockNode.Name.LocalName == "shadow"))
            {
                BlockModel nextBlock = CreateFromXml(workspace, nextBlockNode); 
                if (nextBlock?.PreviousConnection != null && block.NextConnection != null &&
                    block.NextConnection.CanConnectWithReason(nextBlock.PreviousConnection) == ConnectionModel.CAN_CONNECT)
                {
                    block.NextConnection.Connect(nextBlock.PreviousConnection);
                }
                else
                {
                    Debug.LogError($"CreateFromXml: Failed to connect NEXT block! Parent: {block.ID} - Child: {nextBlock?.ID}. Incompatible or null connection.");
                    nextBlock?.Dispose(false);
                }
            }
        }

        // REVISAR Procesar Mutator si en Scratch es necesario
        // XElement mutationNode = xmlBlock.Element("mutation");
        // block.Mutator?.FromXml(mutationNode); // El mutator aplica los cambios al bloque
        // block.Reshape? - Mutator.FromXml debería manejarlo

        return block; 
    }

    /*
     * Descripción: Añade una definición de bloque al diccionario de definiciones.
     */
    public void AddDefinition(string typeName, BlockDefinition definition)
    {
        if (string.IsNullOrEmpty(typeName) || definition == null)
        {
            Debug.LogError("BlockFactory.AddDefinition: Invalid typeName or null definition.");
            return;
        }

        if (mDefinitions.ContainsKey(typeName))
        {
            Debug.LogWarning($"BlockFactory.AddDefinition: Overwriting existing definition for type: {typeName}");
        }
        mDefinitions[typeName] = definition; // Añade o sobrescribe

        int length = typeName.IndexOf("_");
        string prefix = length > 0 ? typeName.Substring(0, length) : typeName;
        if (!mPrefixCategories.ContainsKey(prefix))
            mPrefixCategories[prefix] = new List<string>();
        if (!mPrefixCategories[prefix].Contains(typeName)) 
            mPrefixCategories[prefix].Add(typeName);
    }

}// Fin de la clase BlockFactory
