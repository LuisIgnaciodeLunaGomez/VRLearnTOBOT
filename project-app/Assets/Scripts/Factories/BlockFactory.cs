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

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine; 


// crea los bloques en tiempo de ejecución, asegurando que todos sigan la estructura definida en BlockDefinition
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

    //Agrupa bloques por prefijos (motion_move pertenece a motion)
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

    // Carga bloques desde un archivo JSON
    public void AddJsonDefinitions(string jsonText)
    {
        JArray jsonArray = JArray.Parse(jsonText);
        for (int i = 0; i < jsonArray.Count; i++)
        {
            JObject element = jsonArray[i] as JObject;
            string typeName = element["type"].ToString();
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogError("Block definition in JSON is missing 'type' attribute. Skipping.");
                continue;
            }

            if (mDefinitions.ContainsKey(typeName))
            {
                Debug.LogWarning($"Block definition in JSON array has duplicated type name: {typeName}. Overwriting or Skipping?");
                
                continue;
                // Para sobrescribir: // mDefinitions.Remove(typeName);
            }

            int length = typeName.IndexOf("_");
            string prefix = length > 0 ? typeName.Substring(0, length) : typeName;
            if (!mPrefixCategories.ContainsKey(prefix))
                mPrefixCategories[prefix] = new List<string>();
            mPrefixCategories[prefix].Add(typeName);
        }
    }


    // Crea un bloque basado en su tipo
    public BlockModel CreateBlock(WorkSpaceModel workspace, string type, string uid = null)
    {
        BlockModel block;
        string finalUid = string.IsNullOrEmpty(uid) ? Utilidades.GenUid() : uid; 

        if (workspace == null)
        {
            //Creando una plantilla para el Toolbox (sin workspace)
            Debug.Log($"BlockFactory: Creating TEMPLATE block: {type} (ID: {finalUid})");
            block = BlockModel.CreateTemplate(type, finalUid);                                               
        }
        else
        {
            // Creando un bloque real para el workspace
            // Validar uid aquí para evitar conflicto si se proporciona
            if (!string.IsNullOrEmpty(uid) && workspace.GetBlockById(uid) != null)
            {
                Debug.LogWarning($"BlockFactory: Block with provided ID '{uid}' already exists in workspace. Generating new ID.");
                finalUid = Utilidades.GenUid();
            }
            Debug.Log($"BlockFactory: Creating workspace block: {type} (ID: {finalUid}) for Workspace {workspace.Id}");
            block = new BlockModel(workspace, type, finalUid); // Usa el constructor original que registra en el workspace
        }

        BlockDefinition definition;
        if (!mDefinitions.TryGetValue(type, out definition))
        {
            Debug.LogWarning($"BlockFactory: No definition for block type '{type}'. Creating basic block structure.");
        }
        else
        {
            List<InputModel> inputs = definition.CreateInputList();
            ConnectionModel output = definition.CreateOutputConnection();
            ConnectionModel prev = definition.CreatePreviousStatementConnection();
            ConnectionModel next = definition.CreateNextStatementConnection();
            Mutator mutator = definition.CreateMutator();
            bool inputsInline = definition.GetInputsInlineDefault();

            block.Reshape(inputs, output, prev, next);

            if (mutator != null) block.SetMutator(mutator);
            //if (inputsInline) block.SetInputsInline(true);
            if (inputsInline != block.GetInputsInline()) // Solo si difiere del cálculo automático
            {
                block.SetInputsInline(inputsInline);
            }

            // Asignar el SourceBlock a inputs/connections creados
            foreach (var input in inputs) { input.SourceBlock = block; }
            if (output != null) output.SourceBlock = block;
            if (prev != null) prev.SourceBlock = block;
            if (next != null) next.SourceBlock = block;
        }
        return block;
    }

    //Crea un Bloque desde el XML
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
                    // Intenta encontrar la variable en el workspace por ID
                    VariableModel variable = workspace.VariableMap?.GetVariableById(variableId); 
                    if (variable != null)
                    {
                        varField.SetValue(variable.Name); // Asigna el nombre al field
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

-
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
                    childBlock = CreateFromXml(workspace, childNode); // ¡RECURSIÓN!

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
                // TODO: Manejar Shadow DOM  (crear shadow si childBlock es null y el input acepta shadows)
            }
        }

        XElement nextContainerNode = xmlBlock.Element("next");
        if (nextContainerNode != null)
        {
            // Buscar un block o shadow dentro de next
            XElement nextBlockNode = nextContainerNode.Elements().FirstOrDefault();
            if (nextBlockNode != null && (nextBlockNode.Name.LocalName == "block" || nextBlockNode.Name.LocalName == "shadow"))
            {
                BlockModel nextBlock = CreateFromXml(workspace, nextBlockNode); // ¡RECURSIÓN!
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

        // REVISAR Procesar Mutator 
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

        // Opcional: Actualizar también mPrefixCategories si es necesario
        int length = typeName.IndexOf("_");
        string prefix = length > 0 ? typeName.Substring(0, length) : typeName;
        if (!mPrefixCategories.ContainsKey(prefix))
            mPrefixCategories[prefix] = new List<string>();
        if (!mPrefixCategories[prefix].Contains(typeName)) // Evitar duplicados en lista
            mPrefixCategories[prefix].Add(typeName);
    }


}// Fin de la clase BlockFactory
