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
 * Versión: 1.0.1
 * 
 * Descripción: interpreta los bloques desde JSON, permitiendo definir nuevos bloques sin modificar código
 */

using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using UBlockly;
using System.Linq;


public class BlockDefinition
{
    // Identificación y Categoría
    public string type;           
    public string category;         
    public Color color;             

    // Configuración Visual
    public string spriteName;       
    public bool inputsInline;

    public List<ArgumentDefinition> Arguments { get; set; } = new List<ArgumentDefinition>();
    public string Tooltip { get; set; }      
    public string HelpUrl { get; set; }

    public EConnection OutputType { get; set; } = EConnection.None; // check type - "Number u otro"
    public EConnection PreviousType { get; set; } = EConnection.None; //check type,- "Statement u otro"
    public EConnection NextType { get; set; } = EConnection.None;

    // Conexiones Superiores/Inferiores
    public bool hasOutput;          // Determinado por existencia de <Output>
    public bool hasPreviousStatement; // Determinado por existencia de <PreviousStatement>
    public bool hasNextStatement;   // Determinado por existencia de <NextStatement>

    // Checks de Tipo para Conexiones
    public List<string> outputChecks = new List<string>();         // atributo check en <Output>
    public List<string> previousChecks = new List<string>();       // atributo check en <PreviousStatement>
    public List<string> nextChecks = new List<string>();           // atributo check en <NextStatement>

    // Argumentos/Entradas
   // public List<ArgumentDefinition> args; // Lista de argumentos parseados 

    [Tooltip("Indica si este bloque tiene asociado un Mutator.")]
    public bool hasMutator; //  Determinado al parsear <Mutator>

    [Tooltip("Nombre/identificador del Mutator asociado (ej. controls_if_mutator).")]
    public string mutatorName; //  Leído del atributo del tag <Mutator>

    public BlockDefinition()
    {
        Arguments = new List<ArgumentDefinition>();
    }

    // Crea la lista de modelos de Input basándose en las definiciones de argumentos
    public List<InputModel> CreateInputList()
    {
       // Debug.Log($"-->> CreateInputList START for Block: {type}. Initial Arg Count: {Arguments?.Count ?? 0}");
        List<InputModel> inputList = new List<InputModel>();
        if (Arguments == null || this.Arguments.Count == 0)
        {
            Debug.Log("-->> CreateInputList: No args defined for this block. Returning empty list.");
            return inputList;
        }

        int argIndex = 0; //<---para seguinmiento de los logs
        InputModel currentInput = null; // Referencia  último InputModel creado

        foreach (ArgumentDefinition argDef in Arguments)
        {
            //Debug.Log($"-->> Processing ArgDef #{argIndex}: Name='{argDef.name}', Type='{argDef.type}', IsInputDef={argDef.IsInputDefinition}, IsValue={argDef.IsValue}, IsField={argDef.IsField}");

            //  Manejo de INPUTS (Value, Statement, Dummy implícito) 
            if (argDef.IsInputDefinition) 
            {
              //  Debug.Log($"  --> IsInputDefinition is TRUE. Creating InputModel...");
                //  Creación del InputModel (Value/Statement/Dummy) 
                string inputName = string.IsNullOrEmpty(argDef.name) ? $"INPUT_{inputList.Count}" : argDef.name;
                EConnection inputType;
                if (argDef.IsStatement) inputType = EConnection.NextStatement;
                else if (argDef.IsValue) inputType = EConnection.InputValue;
                else inputType = EConnection.None; // Dummy

                currentInput = new InputModel(inputType, inputName); // Crea el Input
                currentInput.SetAlign(argDef.align);

             //   Debug.Log($"    Created InputModel: Name='{currentInput.Name}', Type={currentInput.Type}");

                // Establecer Checks para inputs de conexión (Value/Statement)
                if (argDef.IsValue || argDef.IsStatement)
                {
                    if (argDef.checks != null && argDef.checks.Count > 0)
                    {
                        currentInput.SetCheck(argDef.checks); // Establece los tipos permitidos

                    }
                  
                    //  Crear campos sombra (Shadow Field) para inputs que continene valores

                    if (argDef.IsValue && !string.IsNullOrEmpty(argDef.shadowFieldType))
                    {
                       // Debug.Log($"    Input is Value AND has shadowFieldType: '{argDef.shadowFieldType}'. Attempting to create shadow field...");
                        // Creamos el FieldModel correspondiente con la información del Shado Field
                        FieldModel shadowField = CreateFieldModelFromArg(argDef.shadowFieldType, argDef.shadowFieldName, argDef.defaultValue, argDef);
                        if (shadowField != null)
                        {
                            currentInput.AppendField(shadowField); // Añadimos el campo sombra al input para contener un valor
                           // Debug.Log($"    -> Appended Shadow Field {shadowField.GetType().Name} ('{shadowField.Name}') to InputModel '{currentInput.Name}'. FieldRow Count: {currentInput.FieldRow.Count}");
                        }
                        else
                        {
                            Debug.LogWarning($"BlockDefinition {this.type}: FAILED to create SHADOW field of type {argDef.shadowFieldType} for input '{argDef.name}'");
                        }
                    }
                }
                inputList.Add(currentInput);

                //Debug.Log($"  ++ ADDED InputModel '{currentInput.Name}' (Type: {currentInput.Type}) to inputList. New List Count: {inputList.Count}");
            }
            // Manejo de FIELDS (Label, Input Text, Dropdown, etc.) 
            else if (argDef.IsField) // Es un campo (field_label, field_input, etc.)
            {
                //Debug.Log($"  --> IsField is TRUE. Checking if Dummy Input is needed...");
                // Nos aseguramos que tenemos un Input (Dummy) para contener este Field
                if (currentInput == null || currentInput.Type != EConnection.None)
                {
                   // Debug.Log($"    Dummy Input needed or previous input wasn't dummy. Creating Dummy Input...");

                    currentInput = new InputModel(EConnection.None, $"DUMMY_INPUT_{inputList.Count}");
                    currentInput.SetAlign(argDef.align); 
                    inputList.Add(currentInput);

                   // Debug.Log($"  ++ ADDED *Dummy* InputModel '{currentInput.Name}' (Type: {currentInput.Type}) to inputList. New List Count: {inputList.Count}");
                }

               // Debug.Log($"    Creating FieldModel for field type '{argDef.FieldType}' with name '{argDef.FieldName}'...");
                //  Creamos el FieldModel basado en la definición del argumento 
                FieldModel fieldModel = CreateFieldModelFromArg(argDef.FieldType, argDef.FieldName, argDef.value, argDef);

                //  Añadimos el Field al Input actual 
                if (fieldModel != null)
                {
                   // Debug.Log($"      Appending FieldModel '{fieldModel.Name}' ({fieldModel.GetType().Name}) to InputModel '{currentInput.Name}'. Current FieldRow Count: {currentInput.FieldRow.Count}");
                    currentInput.AppendField(fieldModel); // Añade el campo al Input actual
                   // Debug.Log($"      -> Field '{fieldModel.Name}' Appended. New FieldRow Count: {currentInput.FieldRow.Count}");
                }
                else
                {
                    Debug.LogWarning($"BlockDefinition {type}: Could not create field of type '{argDef.FieldType}' with name '{argDef.FieldName}'");
                }
            }
            else // tipo de <Arg> desconocido
            {
                Debug.LogError($"BlockDefinition {type}: Unknown argument type '{argDef.type}' in CreateInputList.");
            }
        } // Fin del foreach

       // Debug.Log($"-->> CreateInputList FINISHED for Block: {type}. Returning list with Count: {inputList.Count}");
        return inputList;
    }

    // Crea un FieldModel basado en los datos extraídos del ArgumentDefinition
    private FieldModel CreateFieldModelFromArg(string fieldType, string fieldName, string fieldValueOrDefault, ArgumentDefinition fullArgDef)
    {
        // FieldName -> atributo name del Arg/Field en el XML.
        // FieldValueOrDefault -> value/defaultValue del Arg, o texto interno si es un campo label.

        if (string.IsNullOrEmpty(fieldType))
        {
            Debug.LogWarning($"CreateFieldModelFromArg: fieldType is missing for field '{fieldName}'.");
            return null;
        }
        if (fieldName == null) fieldName = ""; // Evitamos nulls si no tiene nombre

        FieldModel newField = null;
        switch (fieldType)
        {
            case FieldTypes.Label: 
                newField = new FieldLabelModel(fieldName, fieldValueOrDefault ?? ""); 
                break;

            case FieldTypes.TextInput: 
                newField = new FieldTextInputModel(fieldName, fullArgDef.defaultValue ?? fieldValueOrDefault ?? "");
                break;

            case FieldTypes.Number: 
                string numVal = fullArgDef.defaultValue ?? fieldValueOrDefault ?? "0";
                newField = new FieldNumberModel(fieldName, numVal);
                break;

            case FieldTypes.Dropdown:
                
                FieldDropdownModel dropdownField = new FieldDropdownModel(fieldName);

                List<FieldDropdownModel.FieldDropdownMenu> menuOptions;

                if (fullArgDef.dropdownOptions != null)
                {
                   
                    menuOptions = fullArgDef.dropdownOptions
                                        .Select(opt => new FieldDropdownModel.FieldDropdownMenu(opt.display, opt.value))
                                        .ToList();
                }
                else 
                {
                    Debug.LogWarning($"BlockDefinition {this.type}: Dropdown field '{fieldName}' has null options list in ArgumentDefinition.");
                    menuOptions = new List<FieldDropdownModel.FieldDropdownMenu>();
                }

                dropdownField.SetOptions(menuOptions);

                string initialValue = fullArgDef.defaultValue ?? fieldValueOrDefault;
                if (initialValue != null)
                {
                    dropdownField.SetValue(initialValue);
                }

                newField = dropdownField; 
                break;

            case FieldTypes.Variable: 
                newField = new FieldVariableModel(fieldName, fullArgDef.defaultValue ?? fieldValueOrDefault ?? "variable");
                     break;

            case FieldTypes.Checkbox:
                string initialStateString = (fullArgDef.defaultValue ?? fieldValueOrDefault ?? "FALSE").ToUpperInvariant();
                                if (initialStateString != "TRUE" && initialStateString != "FALSE")
                {
                    Debug.LogWarning($"BlockDefinition {this.type}: Invalid initial state '{initialStateString}' for checkbox field '{fieldName}'. Defaulting to FALSE.");
                    initialStateString = "FALSE";
                }

                newField = new FieldCheckboxModel(fieldName, initialStateString); 
                break;

            case FieldTypes.Image:
                string imageSrc = fullArgDef.value ?? ""; // value del XML -> src
                Vector2 size = new Vector2(fullArgDef.imageWidth, fullArgDef.imageHeight);
                string altText = fullArgDef.imageAltText; // Puede ser null

                newField = new FieldImageModel(fieldName, imageSrc, size, altText);
                break;

            

            default:
                Debug.LogWarning($"BlockDefinition {this.type}: Unhandled Field Type '{fieldType}' in CreateFieldModelFromArg.");
                break;
        }

        if (newField != null)
        {
          
        }

       // Debug.Log($"   << CreateFieldModelFromArg RESULT for type '{fieldType}', name '{fieldName}': {(newField == null ? "NULL" : newField.GetType().Name)}");
        return newField;
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
       // Debug.Log($"BlockDefinition.CreateNextStatementConnection() called for type '{this.type}'. Internal hasNextStatement flag: {this.hasNextStatement}", null);
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
    /*public Mutator CreateMutator()
    {
        if (this.hasMutator && !string.IsNullOrEmpty(this.mutatorName))
        {
            return MutatorFactory.Create(this.mutatorName);
        }
        return null;
    }*/

   
    // Obtiene el valor por defecto de InputsInline según la definición.
    public bool GetInputsInlineDefault()
    {
        // Devuelve el valor que se leyó del XML/JSON
        return this.inputsInline;
    }

    public static void LoadAllDefinitionsFromXml(string folderPath = "XML/Blocks") 
    {
        //Debug.Log($"<color=lime>BlockDefinitionLoader: Loading block definitions from Resources/{folderPath}...</color>");
        TextAsset[] xmlAssets = Resources.LoadAll<TextAsset>(folderPath);

        if (xmlAssets == null || xmlAssets.Length == 0)
        {
            Debug.LogError($"BlockDefinitionLoader: No XML definition files found in Resources/{folderPath}! BlockFactory will be empty.");
            return;
        }

        int definitionsLoaded = 0;
        foreach (TextAsset xmlAsset in xmlAssets)
        {
           // Debug.Log($"<color=lime> --- Loading from: {xmlAsset.name}.xml ---</color>");
            try
            {
                XDocument xDoc = XDocument.Parse(xmlAsset.text);
                XElement blocksElement = xDoc.Root;

                if (blocksElement == null || blocksElement.Name.LocalName != "Blocks")
                {
                    Debug.LogWarning($"Invalid XML root in {xmlAsset.name}. Expected <Blocks>. Skipping.");
                    continue;
                }

                // Atributos globales de categoría 
                string globalCategory = blocksElement.Attribute("category")?.Value;
               
                foreach (XElement blockElement in blocksElement.Elements("Block")) 
                {
                
                    BlockDefinition definition = ParseBlockDefinition(blockElement, globalCategory);
                   // Debug.Log($"Checking BlockFactory Instance before adding {definition.type}: {BlockFactory.Instance != null}");
                    if (definition != null && !string.IsNullOrEmpty(definition.type))
                    {
                        if (BlockFactory.Instance == null)
                        {
                            Debug.LogError("CRITICAL: BlockFactory.Instance is NULL!");
                            continue;
                        }
                       // Debug.Log($"<color=orange> ---> Storing definition '{definition.type}' with {definition.Arguments?.Count ?? -1} arguments.</color>");
                        BlockFactory.Instance.AddDefinition(definition.type, definition);
                        definitionsLoaded++;
                      //  Debug.Log($"   - Loaded definition for: {definition.type}");
                    }
                    else
                    {
                        Debug.LogWarning($"   - Skipping block definition in {xmlAsset.name} due to parse error or missing type.");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing block definition XML '{xmlAsset.name}': {e.Message}\n{e.StackTrace}");
            }
        }
       // Debug.Log($"<color=lime>BlockDefinitionLoader: Finished loading. Total definitions added: {definitionsLoaded}</color>");
    }

    private static BlockDefinition ParseBlockDefinition(XElement blockElement, string defaultCategory)
    {
        if (blockElement == null) return null;

        BlockDefinition def = new BlockDefinition();
        def.type = blockElement.Attribute("type")?.Value;

        if (string.IsNullOrEmpty(def.type)) return null;

        def.category = blockElement.Attribute("category")?.Value ?? defaultCategory;
        def.Tooltip = blockElement.Element("Tooltip")?.Value;
        def.HelpUrl = blockElement.Element("HelpUrl")?.Value;
        def.inputsInline = bool.TryParse(blockElement.Element("InputsInline")?.Value ?? "false", out bool inline) && inline;

        // Parsear Conexiones
        XElement output = blockElement.Element("Output");
        //if (output != null) def.OutputType = EConnection.OutputValue; 
        if (output != null)
        {
            def.hasOutput = true;
            def.OutputType = EConnection.OutputValue;
            string checkAttr = output.Attribute("check")?.Value;
            if (!string.IsNullOrEmpty(checkAttr))
            {
                def.outputChecks.AddRange(checkAttr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            }
            Debug.Log($"   - Parsed Output: hasOutput=true, OutputType={def.OutputType}, Checks=[{string.Join(",", def.outputChecks)}]");

        }
        else
        {
            def.hasOutput = false; // Asegurar que sea false si no existe el elemento
           // Debug.Log($"   - No Output element found.");
        }
        XElement prev = blockElement.Element("PreviousStatement");
        // if (prev != null) def.PreviousType = EConnection.PrevStatement; 
        if (prev != null)
        {
            def.hasPreviousStatement = true;
            def.PreviousType = EConnection.PrevStatement;
            string checkAttr = prev.Attribute("check")?.Value;
            if (!string.IsNullOrEmpty(checkAttr))
            {
                def.previousChecks.AddRange(checkAttr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            }
          //  Debug.Log($"   - Parsed PreviousStatement: hasPreviousStatement=true, PreviousType={def.PreviousType}, Checks=[{string.Join(",", def.previousChecks)}]");

        }
        else
        {
            def.hasPreviousStatement = false; // Asegurar que sea false si no existe el elemento
            //Debug.Log($"   - No PreviousStatement element found.");
        }

        XElement next = blockElement.Element("NextStatement");
        //if (next != null) def.NextType = EConnection.NextStatement; 
        if (next != null)
        {
            def.hasNextStatement = true;
            def.NextType = EConnection.NextStatement;
            string checkAttr = next.Attribute("check")?.Value;
            if (!string.IsNullOrEmpty(checkAttr))
            {
                def.nextChecks.AddRange(checkAttr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            }
           // Debug.Log($"   - Parsed NextStatement: hasNextStatement=true, NextType={def.NextType}, Checks=[{string.Join(",", def.nextChecks)}]");

        }
        else
        {
            def.hasNextStatement = false; // Asegurar que sea false si no existe el elemento
            Debug.Log($"   - No NextStatement element found.");
        }

        // Parsear <Args>
        XElement argsElement = blockElement.Element("Args");
        if (argsElement != null)
        {
          //  Debug.Log($"--- Processing Args for block {def.type} ---");
            foreach (XElement argElement in argsElement.Elements("Arg"))
            {
                ArgumentDefinition argDef = new ArgumentDefinition();

               // Debug.Log($"  Parsed ArgDefinition: type='{argDef.type}', name='{argDef.name}', value='{argDef.value}', isValue='{argDef.IsValue}', shadowType='{argDef.shadowFieldType}', shadowName='{argDef.shadowFieldName}', defaultValue='{argDef.defaultValue}'");
                //def.Arguments.Add(argDef); //<-----ESTA PROVOCANDO QUE SE INTRODUZCAN ARGUMENTOS VACIOS
                argDef.type = argElement.Attribute("type")?.Value;
                argDef.value = argElement.Attribute("value")?.Value;
                argDef.name = argElement.Attribute("name")?.Value;

             //   Debug.Log($"--- Parsing Arg: Type={argDef.type}, Name={argDef.name}, Value={argDef.value}");

                try
                {
                    XElement checkElement = argElement.Element("Check");
                    List<string> checkValues = argElement.Elements("Check")
                                                  .Select(el => el.Value?.Trim())
                                                  .Where(s => !string.IsNullOrEmpty(s))
                                                  .ToList();
                    if (checkValues.Count > 0)
                    {
                        argDef.checks.AddRange(checkValues);
                    }
                   // Debug.Log($"   - Checking Checks: Element exists? {checkElement != null}, Attribute value: '{checkValues}'");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing checks for Arg '{argDef.name}': {e.Message}");
                }

                if (argDef.IsValue)
                {
                    Debug.Log("   - Argument IsValue. Checking for Shadow Field...");
                    XElement shadowFieldElement = argElement.Element("Field");
                    if (shadowFieldElement != null)
                    {
                        argDef.shadowFieldType = shadowFieldElement.Attribute("type")?.Value;
                        argDef.shadowFieldName = shadowFieldElement.Attribute("name")?.Value;
                        argDef.defaultValue = shadowFieldElement.Attribute("value")?.Value?.Trim();
                       // Debug.Log($"Leyendo defaultValue {argElement.ToString()} para ver que recupera");
                       // argDef.defaultValue = argElement.Element("DefaultValue").Value?.Trim();
                        // argDef.defaultValue = shadowFieldElement.Value ?? shadowFieldElement.Attribute("value")?.Value;
                        //Debug.Log($" CORREGIDO     - Shadow Parsed: Type={argDef.shadowFieldType}, Name={argDef.shadowFieldName}, Default={argDef.defaultValue}");
                    }
                    else
                    {
                        Debug.Log($"      - Shadow Field Element NOT FOUND for Value Input '{argDef.name}'.");
                    }

                }
               
                def.Arguments.Add(argDef);

               // Debug.Log("--- Finished Parsing Arg ---");
            }

            // TODO: Parsear Mutators si los vamos a usar ....
        }
        return def;
    }

}//Fin clase BlockDefinition




