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
           // Debug.Log($"BlockFactory: Creating workspace block: {type} (ID: {finalUid}) for Workspace {workspace.Id}");
            block = new BlockModel(workspace, type, finalUid); // constructor original que registra en el workspace
        }

        BlockDefinition definition;
        if (!mDefinitions.TryGetValue(type, out definition))
        {
            Debug.LogWarning($"BlockFactory: No definition for block type '{type}'. Creating basic block structure.");
        }
        else
        {
            List<InputModel> inputs = definition.CreateInputList(block);
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
            ConnectionModel output = definition.CreateOutputConnection(block);

           // Debug.Log($"BlockFactory: Created OutputConnectionModel for block '{type}' (ID: {block.ID}). Has output connection: {(output != null)}.");
            ConnectionModel prev = definition.CreatePreviousStatementConnection(block);
          //  Debug.Log($"[Factory:{type}] ConnectionModel 'prev' created by definition. Is null? {prev == null}. ConnID: {ConnectionModel.GetConnectionModelID(prev)}"); 
            // Debug.Log($"BlockFactory: Created PreviousStatementConnectionModel for block '{type}' (ID: {block.ID}). Has previous connection: {(prev != null)}.");   
            ConnectionModel next = definition.CreateNextStatementConnection(block);

          //  Debug.Log($"BlockFactory: Created NextStatementConnectionModel for block '{type}' (ID: {block.ID}). Has next connection: {(next != null)}.");
           // Mutator mutator = definition.CreateMutator();
            bool inputsInline = definition.GetInputsInlineDefault();
            // Debug.Log($"BlockFactory: Created Mutator for block '{type}' (ID: {block.ID}). Has mutator: {(mutator != null)}. Inputs inline default: {inputsInline}.");

           // Debug.Log($"BlockFactory: Created BlockModel '{type}' (ID: {block.ID}). Created Connections: Output={output != null}, Prev={prev != null}, Next={next != null}. Inputs Count={inputs?.Count ?? 0}.");

            block.Reshape(inputs, output, prev, next);
            //Debug.Log($"[Factory:{block.ID}] AFTER Reshape - block.PreviousConnection. Is null? {block.PreviousConnection == null}. ConnID: {ConnectionModel.GetConnectionModelID(block.PreviousConnection)}");
            // --- Asignar SourceBlock a conexiones y campos DEL BLOQUE FINAL ---
           // Debug.Log($"[BlockFactory ID:{block.ID}] Assigning SourceBlock AFTER Reshape...");

            // Conexiones directas
            if (block.OutputConnection != null) block.OutputConnection.SourceBlock = block;
            if (block.PreviousConnection != null) block.PreviousConnection.SourceBlock = block; // Si se asignó bien en Reshape
            if (block.NextConnection != null) block.NextConnection.SourceBlock = block;

          
            ConnectionModel stepsConnectionForDebug = null;
            foreach (var inp in block.InputList)
            {
                if (inp != null && inp.Name == "STEPS")
                {
                    stepsConnectionForDebug = inp.Connection;
                    break; // Asumimos que solo hay un STEPS
                }
            }

            foreach (InputModel input in block.InputList)
            {
                // Debug.Log($"  InputModel '{input.Name}' (Type:{input.Type}) assigned to Block. Has ConnectionModel: {(input.Connection != null)}", null);

                if (input == null) continue;

                input.SourceBlock = block; // Asignar al propio Input
                                           //   Debug.Log($"  - Set SourceBlock for Input '{input.Name}'");

                if (input.Connection != null)
                {
                    if (input.Name == "STEPS") // Solo para el input problemático
                    {
                        bool isSameInstance = System.Object.ReferenceEquals(input.Connection, stepsConnectionForDebug);
                       // Debug.Log($"[Factory Loop Assign Check Instance] Is same as outside loop? {isSameInstance}");

                      //  Debug.Log($"[Factory Loop Assign] Assigning block '{block.ID}' to Conn Hash: {input.Connection.GetHashCode()} ...");

                      //  Debug.Log($"[BlockFactory PRE-ASSIGN] Target Connection Hash: {input.Connection?.GetHashCode() ?? -1}, " +
                      //  $"Current SourceBlock: {input.Connection?.SourceBlock?.ID ?? "NULL"}, " +
                      //  $"Block to Assign: {block?.ID ?? "NULL"} (Hash: {block?.GetHashCode() ?? -1})"); // Agrega el objeto Connection como contexto

                        input.Connection.SourceBlock = block;
                      //  Debug.Log($"[Factory Loop Assign] AFTER assign. Conn Hash: {input.Connection.GetHashCode()}, New SourceBlock: {input.Connection.SourceBlock?.ID ?? "NULL"}, REF EQ After: {System.Object.ReferenceEquals(input.Connection, stepsConnectionForDebug)}");

                        var sourceBlockAfter = input.Connection?.SourceBlock;
                     //   Debug.Log($"[BlockFactory POST-ASSIGN] Target Connection Hash: {input.Connection?.GetHashCode() ?? -1}, " +
                     //             $"NEW SourceBlock IS NOW: {sourceBlockAfter?.ID ?? "NULL"} " +
                     //             $"(Was it assigned?: {(sourceBlockAfter == block ? "YES" : "NO!!!")}, " + // Compara referencias
                     //             $"Is Block var the same?: {(block?.ID) ?? "NULL"})"); // Confirma que 'block' no cambió
                    }
                    input.Connection.SourceBlock = block; // Asignar a la Conexión del Input
                   // Debug.Log($"    - Set SourceBlock for Connection of Input '{input.Name}' (ConnID: {ConnectionModel.GetConnectionModelID(input.Connection)})");

                    // VERIFICACIÓN OPCIONAL (para estar seguros después de la corrección)


                    if (input.Name == "STEPS")
                    {
                   //     Debug.Log($"[Factory Loop Assign] AFTER assign. Conn Hash: {input.Connection.GetHashCode()}, New SourceBlock: {input.Connection.SourceBlock?.ID ?? "NULL"}");
                    }
                }

                if (input.FieldRow != null)
                {
                    foreach (FieldModel field in input.FieldRow)
                    {
                        if (field != null) field.SourceBlock = block; // Asignar a los Fields
                    }
                    //    Debug.Log($"    - Set SourceBlock for Fields in Input '{input.Name}'");
                }
            }
           // Debug.Log($"[BlockFactory ID:{block.ID}] Finished Assigning SourceBlock References.");

            //  if (mutator != null) block.SetMutator(mutator);
            //if (inputsInline) block.SetInputsInline(true);
            if (inputsInline != block.GetInputsInline())
            {
                block.SetInputsInline(inputsInline);
            }

            
            if (block.Workspace != null)
            {
                Debug.Log($"<color=teal>creo la BlockView [BlockFactory:{block.ID}] Assigning DB REFERENCES early for workspace block...</color>");
                List<ConnectionModel> allConnections = block.GetConnections();

                foreach (ConnectionModel conn in allConnections)
                {
                    if (conn != null && conn.SourceBlock == block)
                    {
                        // Obtener las DBs del Workspace del bloque
                        BlockConnectionDB dbRef = block.Workspace.GetConnectionDB(conn.Type);
                        BlockConnectionDB dbOppositeRef = block.Workspace.GetConnectionDB(conn.OppositeType);

                        // Asignar las referencias directamente 
                        conn.DB = dbRef;
                        conn.DBOpposite = dbOppositeRef;
                        conn.Hidden = (dbRef == null); 

                        Debug.Log($"<color=teal> en  BlockFactory BlockConnectionDB - Assigned Refs for Conn: {ConnectionModel.GetConnectionModelID(conn)}. DB? {(conn.DB != null).ToString()}. DBOpposite? {(conn.DBOpposite != null).ToString()}. Hidden? {conn.Hidden.ToString()}</color>");

                    }
                    else if (conn != null) 
                    {
                        Debug.LogWarning($"<color=teal>[BlockFactory:{block.ID}] Skipped assigning refs for {ConnectionModel.GetConnectionModelID(conn)} because its SourceBlock ({conn.SourceBlock?.ID}) doesn't match block being created ({block.ID}).</color>");
                    }
                }
                Debug.Log($"<color=teal>[BlockFactory:{block.ID}] FINISHED assigning DB REFERENCES early.</color>");
            }
           
        }

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
