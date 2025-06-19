/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/01/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Clase que se encarga de cargar los datos de los bloques desde un archivo XML
 */
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;
using System;
using Newtonsoft.Json.Linq;

public static class BlockDataLoader
{

    private static Dictionary<string, Vector2> m_blockSizes = new Dictionary<string, Vector2>(); 
    private static List<(string Name, Color Color)> m_cachedCategories = new List<(string, Color)>();
    private static Dictionary<string, Color> m_categoryColorsCache = new Dictionary<string, Color>();
    private static bool m_isLoaded = false; // Flag para saber si ya se cargó todo
    public static void LoadAllDefinitions()
    {

        if (m_isLoaded) return;

        Debug.Log("<color=teal>BlockDataLoader: Loading all block definitions from XML...</color>");
        TextAsset[] allXmlAssets = Resources.LoadAll<TextAsset>("XML/Blocks");

        if (allXmlAssets.Length == 0)
        {
            Debug.LogWarning("BlockDataLoader: No XML definition files found in Resources/XML/Blocks/");
        }

        BlockFactory.Instance.Clear();

        foreach (TextAsset xmlAsset in allXmlAssets)
        {
            LoadAndParseXmlAsset(xmlAsset, xmlAsset.name); 
        }

        int loadedCount = BlockFactory.Instance.GetAllBlockDefinitions().Count;
        int categoryCount = BlockFactory.Instance.GetAllBlockDefinitions().Values.Select(d => d.category).Distinct().Count();

        Debug.Log($"<color=teal>BlockDataLoader: Finished loading. Added {loadedCount} block definitions to BlockFactory across approx {categoryCount} categories.</color>");
        m_isLoaded = true;
    }

    /** 
     * Descripción: Obtiene todas las definiciones para una categoría. Carga todo si no se ha hecho. 
     * @param categoryName El nombre de la categoría a buscar.
     * @return La lista de definiciones de bloques para la categoría o una lista vacía si no se encontró.
     **/
    public static List<BlockDefinition> GetDefinitionsForCategory(string categoryName)
    {
        if (!m_isLoaded) LoadAllDefinitions();

        string normalizedCategory = NormalizeCategoryName(categoryName);

        var allDefinitions = BlockFactory.Instance.GetAllBlockDefinitions().Values;

        List<BlockDefinition> result = allDefinitions
                                        .Where(def => string.Equals(def.category, normalizedCategory, StringComparison.OrdinalIgnoreCase))
                                        .ToList();

        if (result.Count == 0)
        {
            Debug.LogWarning($"BlockDataLoader: No definitions found in BlockFactory for category '{normalizedCategory}' (original: '{categoryName}').");
        }
        return result;
    }

  
    /** 
     * Descripción: Obtiene el color de una categoría (o gris si no se encuentra).
     * @param categoryName El nombre de la categoría a buscar.
     * @return El color de la categoría o gris si no se encontró.
     */
     
    private static Color GetColorForCategory(string categoryName)
    {
      
        switch (categoryName?.ToLower()) 
        {
            case "motion": return new Color(0.3f, 0.5f, 1f, 1f); 
            case "events": return new Color(1f, 0.8f, 0f, 1f);
            case "control": return new Color(1f, 0.7f, 0f, 1f);
            case "looks": return new Color(0.6f, 0.4f, 1f, 1f); 
            case "sensing": return new Color(0.3f, 0.7f, 0.9f, 1f); 
            case "operators": return new Color(0.4f, 0.8f, 0.2f, 1f); 
            case "variables": return new Color(1f, 0.5f, 0.1f, 1f); 
            // TODO: añadir todas las categorías ...
            default: return Color.grey;
        }
    }

    /** 
     * Descripción: Obtiene el color de una categoría (o gris si no se encuentra) para uso público.
     * @param categoryName El nombre de la categoría a buscar.
     * @return El color de la categoría o gris si no se encontró.
     */
    public static Color GetColorForCategoryPublic(string categoryName)
    {
        return GetColorForCategory(categoryName);
    }

    /**
     * Descripción: Parsea un string de color (hex o hue) y devuelve un color por defecto si falla.
     * @param: colorString El string de color a parsear.
     * @param: defaultColor El color a devolver si falla el parseo.
     * @return: El color parseado o el color por defecto si falla.
     */
    private static Color ParseColorString(string colorString, Color defaultColor)
    {
        if (string.IsNullOrWhiteSpace(colorString)) return defaultColor;

        if (colorString.StartsWith("#"))
        {
            if (ColorUtility.TryParseHtmlString(colorString, out Color htmlColor))
                return htmlColor;
        }
        else if (colorString.StartsWith("%{") && colorString.EndsWith("}"))
        {
         
            Debug.LogWarning($"Hue color parsing ('{colorString}') not fully implemented. Using default.");
        }

        return defaultColor;
    }

    /**
     * Descripción: Obtiene el nombre de sprite por defecto para un bloque según sus conexiones.
     * @param hasPrev Si tiene conexión anterior.
     * @param hasNext Si tiene conexión siguiente.
     * @param hasOutput Si tiene conexión de salida.
     * @return El nombre del sprite por defecto.
     */
    private static string GetDefaultSpriteForConnections(bool hasPrev, bool hasNext, bool hasOutput)
    {
        if (hasOutput) return "reporter_block"; 
        if (!hasPrev && hasNext) return "hat_block";
        if (hasPrev && hasNext) return "stack_block";
        if (hasPrev && !hasNext) return "cap_block"; 
        return "default_block"; // Fallback
    }

    // TODO: Implementar ParseDropdownOptions(XmlNode argNode) para dropdowns
    // private static List<(string display, string value)> ParseDropdownOptions(XmlNode argNode) { }

    // TODO: Añadir lógica para leer checks y field sombra de <Arg>
    // private static List<string> ParseCheckNode(XmlNode checkNode) { }


    /**
     * Descripción: Parsea un asset XML y llena las cachés
     * @param xmlAsset El asset XML a parsear.
     * @param categoryNameFallback El nombre de categoría a usar si no se encuentra en el XML.
     */
    private static void LoadAndParseXmlAsset(TextAsset xmlAsset, string categoryNameFallback)
    {
        if (xmlAsset == null) return;
        try
        {
            XDocument xDoc = XDocument.Parse(xmlAsset.text); 
            XElement blocksRoot = xDoc.Root;

            if (blocksRoot == null || blocksRoot.Name.LocalName != "Blocks")
            {
                Debug.LogError($"Invalid XML root element in {xmlAsset.name}. Expected <Blocks>.");
                return;
            }

            string categoryNameFromXml = blocksRoot.Attribute("category")?.Value ?? categoryNameFallback;
            string finalCategoryName = NormalizeCategoryName(categoryNameFromXml); 

            string colorStringFromXml = blocksRoot.Attribute("color")?.Value;
            Color categoryColor = GetColorForCategory(finalCategoryName);
            if (!string.IsNullOrEmpty(colorStringFromXml))
            {
                categoryColor = ParseColorString(colorStringFromXml, categoryColor);
            }

            if (!m_categoryColorsCache.ContainsKey(finalCategoryName))
                m_categoryColorsCache.Add(finalCategoryName, categoryColor);

            IEnumerable<XElement> blockNodes = blocksRoot.Elements("Block"); 

            if (!blockNodes.Any())
            {
                Debug.LogWarning($"No <Block> elements found in {xmlAsset.name}");
                return;
            }

            foreach (XElement blockNode in blockNodes)
            {
                ParseAndAddToFactory(blockNode, finalCategoryName, categoryColor); 
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing block definition XML {xmlAsset.name}: {e.Message}\n{e.StackTrace}");
        }
    }

    /**
     * Description: Argumento de definición de bloque.Parser
     * @param argNode El nodo XML que representa el argumento.
     * @return La definición del argumento o null si no se pudo parsear.
     */
    private static ArgumentDefinition ParseArgumentDefinition(XElement argNode)
    {
        string type = argNode.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(type)) return null;

        ArgumentDefinition arg = new ArgumentDefinition
        {
            type = type,
            value = argNode.Attribute("value")?.Value ?? argNode.Value,
            name = argNode.Attribute("name")?.Value,
            
          //  inputType = argNode.Attribute("inputType")?.Value,
            defaultValue = argNode.Attribute("defaultValue")?.Value
        };

        // Convertimos la estructura XML a un JObject que nuestra FieldFactory entiende
        arg.DefinitionJson = ConvertArgXmlToJson(argNode);

        XElement fieldNode = argNode.Element("Field");
        if (fieldNode != null)
        {
            arg.shadowFieldType = fieldNode.Attribute("type")?.Value;
            arg.shadowFieldName = fieldNode.Attribute("name")?.Value;
            arg.defaultValue = fieldNode.Attribute("value")?.Value; // Para el valor del campo sombra
        }

        XElement checkNode = argNode.Element("Check");
        if (checkNode != null)
        {
            arg.checks = ParseCheckNode(checkNode);
        }
        /* if (type == "input_value" || type == "input_statement")
         {
             XElement checkNode = argNode.Element("Check");
             arg.checks = ParseCheckNode(checkNode);
         }
         if (type == "input_value")
         {
             XElement fieldNode = argNode.Element("Field");
             if (fieldNode != null)
             {
                 arg.shadowFieldType = fieldNode.Attribute("type")?.Value;
                 arg.shadowFieldName = fieldNode.Attribute("name")?.Value;
                 arg.defaultValue = fieldNode.Attribute("defaultValue")?.Value ?? arg.defaultValue;
             }
         }
         if (type == "field_dropdown")
         {
             IEnumerable<XElement> optionNodes = argNode.Element("Options")?.Elements("Option");
             arg.dropdownOptions = ParseDropdownOptions(optionNodes);
         }

        // Si es un InputValue (que tiene conexión), también parseamos su sombra.
        if (type == BlockInputType.Value)
        {
            XElement fieldNode = argNode.Element("Field");
            if (fieldNode != null)
            {
                arg.shadowFieldType = fieldNode.Attribute("type")?.Value;
            }
        }



        arg.DefinitionJson = ConvertFieldXmlToJson(argNode); 
        */

        return arg;
    }

    /** 
     * Descripción: para parsear el atributo 'check'
     * @param connectionNode El nodo XML que representa la conexión.
     * @return Una lista de checks o null si no se encontraron.
     */
    private static List<string> ParseCheckAttribute(XAttribute checkAttribute)
        {
        string checkValue = checkAttribute?.Value;
        if (string.IsNullOrWhiteSpace(checkValue))
            return null; 
        return checkValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    /** 
     * Descripción: para parsear el nodo 'Check' de un argumento.
     * @param checkNode El nodo XML que representa los checks.
     * @return Una lista de checks o null si no se encontraron.
     */
    private static List<string> ParseCheckNode(XmlNode checkNode)
    {
        string val = checkNode?.InnerText;
        if (string.IsNullOrWhiteSpace(val)) return null;
        return val.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    /** 
     * Descripción: para parsear las opciones de un dropdown.
     * @param optionNodes La lista de nodos XML que representan las opciones.
     * @return Una lista de tuplas (display, value) con las opciones.
     */
    private static List<(string display, string value)> ParseDropdownOptions(XmlNodeList optionNodes)
    {
        var options = new List<(string display, string value)>();
        if (optionNodes == null) return options;
        foreach (XmlNode opt in optionNodes)
        {
            string display = opt.Attributes["display"]?.Value ?? opt.InnerText;
            string value = opt.InnerText;
            options.Add((display, value));
        }
        return options;

    }

    /** 
     * Descripción: Método para obtener los nombres de las categorías y sus colores.
     * @return Una lista de tuplas (nombre, color) con las categorías y sus colores.
     */
    public static List<(string Name, Color Color)> LoadCategoryInfo(string filePath = "XML/Categories")
    {
        List<(string, Color)> categoryList = new List<(string, Color)>();
        TextAsset xmlData = Resources.Load<TextAsset>(filePath);
        if (xmlData == null)  return categoryList; 
        try
        {
            XDocument xmlDoc = XDocument.Parse(xmlData.text);
            IEnumerable<XElement> categories = xmlDoc.Element("Categories").Elements("Category");
            foreach (XElement category in categories)
            {
                string name = category.Element("Name")?.Value;
                string colorHex = category.Element("Color")?.Value;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(colorHex))
                {
                    if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                    {
                        categoryList.Add((name, color));
                    }
                    else { }
                }
            }
        }
        catch (Exception ex) { Debug.LogError(ex); }
        return categoryList;
    }

    

    /**
     * Descripción: Define el sprite por defecto para los bloques según sus conexiones.
     * @param hasPrev Si tiene conexión anterior.
     * @param hasNext Si tiene conexión siguiente.
     * @param hasOutput Si tiene conexión de salida.
     * @param outputChecks Los tipos de salida que puede tener.
     * @param hasStatementInput Si tiene entrada de statement.
     * @return El nombre del sprite por defecto.
     */
    private static string GetDefaultSpriteForConnections(bool hasPrev, bool hasNext, bool hasOutput,  List<string> outputChecks,  bool hasStatementInput) 
    {
        if (hasOutput)
        {
            if (outputChecks != null && outputChecks.Contains("Boolean"))
                return "boolean_block_grey";
            else
                return "reporter_block_grey";
        }
        if (!hasPrev && hasNext) return "hat_block_grey";  
        if (hasPrev && !hasNext) return "cap_block_grey";  

        if (hasPrev && hasNext)
        {
            if (hasStatementInput)
                return "c_block_grey";
            else
                return "stack_block_grey";
        }

        Debug.LogWarning("Could not determine block shape, using default stack.");
        return "stack_block_grey";
    }

    private static void ParseAndAddToFactory(XElement blockNode, string categoryName, Color categoryColor)
    {
        string type = blockNode.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(type))
        {
            Debug.LogError("Block definition is missing 'type' attribute. Skipping.");
            return;
        }

        XElement prevNode = blockNode.Element("PreviousStatement");
        XElement nextNode = blockNode.Element("NextStatement");
        XElement outputNode = blockNode.Element("Output");
        

        bool hasPrev = (prevNode != null);
        bool hasNext = (nextNode != null);
        bool hasOutput = (outputNode != null);

        List<string> previousChecks = ParseCheckAttribute(prevNode?.Attribute("Checks"));
        List<string> nextChecks = ParseCheckAttribute(nextNode?.Attribute("Checks"));
        List<string> outputChecks = ParseCheckAttribute(outputNode?.Attribute("Checks"));

        bool hasStatementInput = blockNode.Element("Args")?.Elements("Arg").Any(arg => arg.Attribute("type")?.Value == "input_statement") ?? false;

        string defaultSpriteName = GetDefaultSpriteForConnections(hasPrev, hasNext, hasOutput, outputChecks, hasStatementInput);
        string spriteFromXml = blockNode.Attribute("spriteName")?.Value?.Trim() ?? blockNode.Element("SpriteName")?.Value?.Trim();
        string finalSpriteName = !string.IsNullOrEmpty(spriteFromXml) ? spriteFromXml : defaultSpriteName;

        string colourString = blockNode.Element("Colour")?.Value;
        bool inputsInline = blockNode.Element("InputsInline")?.Value?.ToLower() == "true";
        // string tooltip = blockNode.Element("Tooltip")?.Value; 
        // string helpUrl = blockNode.Element("HelpUrl")?.Value;

        // Mutator
       /* XElement mutatorNode = blockNode.Element("Mutator");
        bool hasMutator = (mutatorNode != null);
        string mutatorName = mutatorNode?.Attribute("name")?.Value;*/

        BlockDefinition definition = new BlockDefinition
        {
            type = type,
            category = categoryName, 
            color = ParseColorString(colourString, categoryColor),
            spriteName = finalSpriteName,
            inputsInline = inputsInline, 

            hasOutput = hasOutput,
            hasPreviousStatement = hasPrev,
            hasNextStatement = hasNext,

            outputChecks = outputChecks,
            previousChecks = previousChecks,
            nextChecks = nextChecks,

           // hasMutator = hasMutator,
          //  mutatorName = mutatorName,

            Arguments = new List<ArgumentDefinition>(),
        };

        XElement argsContainer = blockNode.Element("Args");
        if (argsContainer != null)
        {
            foreach (XElement argNode in argsContainer.Elements("Arg"))
            {
                ArgumentDefinition arg = ParseArgumentDefinition(argNode); 
                {
                    definition.Arguments.Add(arg);
                }
            }
        }

        BlockFactory.Instance.AddDefinition(definition.type, definition);

        if (float.TryParse(blockNode.Attribute("width")?.Value, out float w) &&
            float.TryParse(blockNode.Attribute("height")?.Value, out float h))
        {
            m_blockSizes[definition.type] = new Vector2(w, h);
        }
    }

    private static string NormalizeCategoryName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "unknown"; 

        switch (rawName.ToLowerInvariant()) 
        {
            case "motion": return "Movimiento";
            case "looks": return "Apariencia";
            case "sound": return "Sonido";
            case "events": return "Eventos";
            case "control": return "Control";
            case "sensing": return "Sensores";
            case "operators": return "Operadores";
            case "variables": return "Variables";
            case "my blocks":
            case "procedures": return "Mis Bloques";
            
            default:
                
                return rawName;
        }
    }

    private static List<string> ParseCheckNode(XElement checkNode)
    {
        string val = checkNode?.Value; 
        if (string.IsNullOrWhiteSpace(val)) return null;
        return val.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    private static List<(string display, string value)> ParseDropdownOptions(IEnumerable<XElement> optionNodes)
    {
        var options = new List<(string display, string value)>();
        if (optionNodes == null) return options;
        foreach (XElement opt in optionNodes)
        {
            string display = opt.Attribute("display")?.Value ?? opt.Value;
            string value = opt.Value; 
            options.Add((display, value));
        }
        return options;
    }

    // Convierte la información relevante de un nodo XML <Arg> de tipo Field
    // a un JObject que FieldFactory pueda entender.
    private static JObject ConvertFieldXmlToJson(XElement argNode)
    {
        string fieldType = argNode.Attribute("type")?.Value;
        if (!fieldType.StartsWith("field_")) return null;

        JObject json = new JObject();
        json["type"] = fieldType; 

        string name = argNode.Attribute("name")?.Value;
        if (!string.IsNullOrEmpty(name)) json["name"] = name;

        string value = argNode.Attribute("value")?.Value ?? argNode.Value;
        string defaultValue = argNode.Attribute("defaultValue")?.Value;

        if (!string.IsNullOrEmpty(defaultValue)) json["text"] = defaultValue; 
        else if (!string.IsNullOrEmpty(value)) json["text"] = value;

        if (fieldType == "field_dropdown")
        {
            JArray optionsArray = new JArray();
            var options = ParseDropdownOptions(argNode.Element("Options")?.Elements("Option"));
            foreach (var opt in options)
            {
                optionsArray.Add(new JArray(opt.display, opt.value));
            }
            json["options"] = optionsArray;
        }

        foreach (var attr in argNode.Attributes())
        {
            if (attr.Name.LocalName != "type" && attr.Name.LocalName != "name" &&
                attr.Name.LocalName != "value" && attr.Name.LocalName != "defaultValue")
            {
                json[attr.Name.LocalName] = attr.Value;
            }
        }

        //Debug.Log($"Converted XML Arg to JSON for FieldFactory:\n{json.ToString()}");
        return json;
    }

    /// <summary>
    /// Convierte la información relevante de un nodo <Arg> del XML
    /// a un JObject que FieldFactory pueda entender. Es un traductor.
    /// </summary>
    private static JObject ConvertArgXmlToJson(XElement argNode)
    {
        /* string fieldType = argNode.Attribute("type")?.Value;
         if (string.IsNullOrEmpty(fieldType)) return null;

         JObject json = new JObject();
         json["type"] = fieldType;

         string name = argNode.Attribute("name")?.Value;
         if (!string.IsNullOrEmpty(name)) json["name"] = name;

         // Damos prioridad al atributo 'value', luego a 'defaultValue', y si no, al contenido del tag
         string value = argNode.Attribute("value")?.Value ?? argNode.Attribute("defaultValue")?.Value ?? argNode.Value;
         if (!string.IsNullOrEmpty(value)) json["text"] = value; // UBlockly usa 'text' para el valor del label

         // Lógica específica para dropdowns, que necesitan un array 'options'
         if (fieldType == "field_dropdown")
         {
             JArray optionsArray = new JArray();
             var optionsNodes = argNode.Element("Options")?.Elements("Option");
             if (optionsNodes != null)
             {
                 foreach (var optNode in optionsNodes)
                 {
                     string display = optNode.Attribute("display")?.Value ?? optNode.Value;
                     string optionValue = optNode.Value;
                     optionsArray.Add(new JArray(display, optionValue));
                 }
             }
             json["options"] = optionsArray;
         }

         // Pasar otros atributos directamente (para min, max en field_number)
         foreach (var attr in argNode.Attributes())
         {
             if (attr.Name.LocalName != "type" && attr.Name.LocalName != "name" && attr.Name.LocalName != "value")
             {
                 json[attr.Name.LocalName] = attr.Value;
             }
         }*/

        string fieldType = argNode.Attribute("type")?.Value;
        if (!fieldType.StartsWith("field_") && !fieldType.StartsWith("input_")) return null;

        JObject json = new JObject();
        json["type"] = fieldType;

        string name = argNode.Attribute("name")?.Value;
        if (!string.IsNullOrEmpty(name)) json["name"] = name;

        // El "text" de JSON corresponde a `value` o al contenido del XML.
        json["text"] = argNode.Attribute("value")?.Value ?? argNode.Value;

        // Para los inputs, el "defaultValue" es lo que va en el campo sombra.
        string shadowValue = argNode.Element("Field")?.Attribute("value")?.Value;
        if (!string.IsNullOrEmpty(shadowValue))
            json["value"] = shadowValue;


        return json;
    }
}// Fin de la clase BlockDataLoader