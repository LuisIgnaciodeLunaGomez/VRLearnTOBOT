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
    public Dictionary<string, BlockDefinition> GetAllBlockDefinitions() => mDefinitions;
   /*{
        return mDefinitions;
    }*/

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

        //  1. Obtener la definición ANTES que nada 
        if (!mDefinitions.TryGetValue(type, out BlockDefinition definition))
        {
            Debug.LogError($"BlockFactory: No se pudo encontrar la definición para el tipo '{type}'. Imposible crear el bloque.");
            return null;
        }

        //  2. Crear el BlockModel usando el constructor apropiado 
        BlockModel block;
        string finalUid = string.IsNullOrEmpty(uid) ? Utilidades.GenUid() : uid;

        if (workspace == null)
        {
            // Para plantillas del toolbox, que no tienen workspace.
            
            block = BlockModel.CreateTemplate(type, finalUid);
            block.SetDefinition(definition); // Le asignamos la definición.
        }
        else
        {
            // Para bloques reales en el workspace.
            if (!string.IsNullOrEmpty(finalUid) && workspace.GetBlockById(finalUid) != null)
            {
                finalUid = Utilidades.GenUid();
            }
            //Usamos  constructor que  acepta la definición
            block = new BlockModel(workspace, definition, finalUid);
        }

        //  3. Ahora configuramos el bloque que ya tiene su definición 

        List<InputModel> inputs = definition.CreateInputList(block);
        ConnectionModel output = definition.CreateOutputConnection(block);
        ConnectionModel prev = definition.CreatePreviousStatementConnection(block);
        ConnectionModel next = definition.CreateNextStatementConnection(block);

        block.Reshape(inputs, output, prev, next);

        //  4. Asignamos SourceBlock, esto es importante y ya lo tenías bien 
        if (block.OutputConnection != null) block.OutputConnection.SourceBlock = block;
        if (block.PreviousConnection != null) block.PreviousConnection.SourceBlock = block;
        if (block.NextConnection != null) block.NextConnection.SourceBlock = block;

        foreach (InputModel input in block.InputList)
        {
            if (input == null) continue;
            input.SourceBlock = block;
            if (input.Connection != null) input.Connection.SourceBlock = block;
            if (input.FieldRow != null)
            {
                foreach (FieldModel field in input.FieldRow)
                {
                    if (field != null) field.SourceBlock = block;
                }
            }
        }

        //  5. Configuraciones finales 
        bool inputsInline = definition.GetInputsInlineDefault();
        if (inputsInline != block.GetInputsInline())
        {
            block.SetInputsInline(inputsInline);
        }

        if (block.Workspace != null)
        {
            List<ConnectionModel> allConnections = block.GetConnections();
            foreach (ConnectionModel conn in allConnections)
            {
                if (conn != null && conn.SourceBlock == block)
                {
                    conn.DB = block.Workspace.GetConnectionDB(conn.Type);
                    conn.DBOpposite = block.Workspace.GetConnectionDB(conn.OppositeType);
                    conn.Hidden = (conn.DB == null);
                }
            }
        }

        Debug.Log(block.ToDevString());
        Debug.Log($"Inputs: {block.InputList.Count}");

        return block;
    }

    //Creamos un Bloque desde el XML
    public static BlockModel CreateFromXml(WorkSpaceModel workspace, XElement xmlBlock)
    {
        if (workspace == null || xmlBlock == null) return null;

        string blockType = xmlBlock.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(blockType))
        {
           // Debug.LogError("BlockFactory.CreateFromXml: Block XML is missing the 'type' attribute.");
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
