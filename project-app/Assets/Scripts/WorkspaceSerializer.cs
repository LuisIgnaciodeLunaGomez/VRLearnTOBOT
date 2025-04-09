/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase para guardar el area de trabajo
 */

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using UnityEngine; 
using System;

public static class WorkspaceSerializer
{
    public static string SaveToXml(WorkSpaceModel workspace)
    {
        if (workspace == null) return "<xml error=\"Workspace model is null\"/>";

        try
        {
            XElement xmlRoot = new XElement("xml");

            if (workspace.GetAllVariables().Any()) 
                xmlRoot.Add(new XComment(" TODO: Implement Variable Serialization "));

            List<BlockModel> topBlocks = workspace.TopBlocks.OrderBy(b => b.XY.y).ThenBy(b => b.XY.x).ToList();
            foreach (BlockModel block in topBlocks)
            {
                XElement blockElement = BlockModelToXmlElement(block, optNoId: false); 
                if (blockElement != null)
                {
                    blockElement.SetAttributeValue("x", Mathf.RoundToInt(block.XY.x).ToString());
                    blockElement.SetAttributeValue("y", Mathf.RoundToInt(block.XY.y).ToString());
                    xmlRoot.Add(blockElement);
                }
            }

            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xmlRoot);
            return doc.Declaration + Environment.NewLine + doc.ToString(SaveOptions.None); 
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceSerializer.SaveToXml Error: {ex.Message}\n{ex.StackTrace}");
            return $"<xml error=\"Save failed: {System.Security.SecurityElement.Escape(ex.Message)}\"/>";
        }
    }

    // Convierte un BlockModel (y su subárbol) a XElement (<block> o <shadow>)
    private static XElement BlockModelToXmlElement(BlockModel block, bool optNoId)
    {
        if (block == null) return null;

        XElement element = new XElement(block.IsShadow ? "shadow" : "block");
        element.SetAttributeValue("type", block.Type);
        if (!optNoId)
            element.SetAttributeValue("id", block.ID);

        if (block.Collapsed) element.SetAttributeValue("collapsed", "true");
        if (block.Disabled) element.SetAttributeValue("disabled", "true");
        if (!block.Deletable && !block.IsShadow) element.SetAttributeValue("deletable", "false");
        if (!block.Movable && !block.IsShadow) element.SetAttributeValue("movable", "false");
        if (!block.Editable) element.SetAttributeValue("editable", "false");
        // if (block.GetInputsInline()) element.SetAttributeValue("inline", "true"); 


        foreach (var input in block.InputList)
        {
            foreach (var field in input.FieldRow)
            {
                if (!string.IsNullOrEmpty(field.Name))
                {
                    XElement fieldElement = new XElement("field", field.GetValue());
                    fieldElement.SetAttributeValue("name", field.Name);
                    if (field is FieldVariableModel varField)
                    {

                        // fieldElement.SetAttributeValue("variableType", varField.VariableType);
                    }
                    element.Add(fieldElement);
                }
            }
        }

       
        foreach (var input in block.InputList)
        {
            if (input.Connection != null)
            {
                BlockModel childBlock = input.Connection.TargetBlock; 
                if (childBlock != null)
                {
                    XElement container = new XElement((input.Type == EConnection.InputValue) ? "value" : "statement");
                    container.SetAttributeValue("name", input.Name);
                    XElement childElement = BlockModelToXmlElement(childBlock, optNoId);
                    if (childElement != null)
                        container.Add(childElement);
                    element.Add(container);
                }
            }
        }

        BlockModel nextBlock = block.NextBlock;
        if (nextBlock != null)
        {
            XElement container = new XElement("next");
            XElement nextElement = BlockModelToXmlElement(nextBlock, optNoId);
            if (nextElement != null)
                container.Add(nextElement);
            element.Add(container);
        }

        // TODO: Guardar Mutators si los implementas
        // XElement mutatorElement = block.Mutator?.ToXml(); if (mutatorElement != null) element.Add(mutatorElement);
        // TODO: Guardar Comments, etc.

        return element;
    }

    //CARGAR (XML String -> Modifica Modelo) 

    public static List<string> LoadFromXml(string xmlData, WorkSpaceModel workspace)
    {
        if (workspace == null || string.IsNullOrEmpty(xmlData)) return new List<string>();

        try
        {
            XDocument doc = XDocument.Parse(xmlData);
            XElement xmlRoot = doc.Root;
            if (xmlRoot == null || xmlRoot.Name.LocalName.ToLower() != "xml")
            {
                throw new Exception("Invalid XML format: Root element must be <xml>");
            }

            List<string> newBlockIds = new List<string>();
            bool variablesLoaded = false;

            foreach (XElement node in xmlRoot.Elements())
            {
                string nodeName = node.Name.LocalName.ToLower();
                if (nodeName == "variables")
                {
                    if (!variablesLoaded)
                    {
                        ParseAndLoadVariables(node, workspace);
                        variablesLoaded = true;
                    }
                    else throw new Exception("<variables> tag can only appear once at the beginning.");
                }
                else if (nodeName == "block" || nodeName == "shadow")
                {
                    if (!variablesLoaded && xmlRoot.Elements("variables").Any())
                        throw new Exception("<variables> must come before <block> or <shadow> tags.");


                    BlockModel block = BlockFactory.CreateFromXml(workspace, node);

                    if (block != null)
                    {
                        newBlockIds.Add(block.ID);
                        workspace.AddBlock(block);


                        // WorkspaceView.Instance?.GetBlockView(block)?.QueueForceLayoutUpdate();
                    }
                }
            }


            // workspace.UpdateVariableMap();
            // workspace.UpdateProcedureMap();

            return newBlockIds;
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceSerializer.LoadFromXml Error: {ex.Message}\n{ex.StackTrace}");
            workspace.Clear();
            throw;
        }
    }

    // Parsea <variables> y añade a WorkspaceModel
    private static void ParseAndLoadVariables(XElement variablesNode, WorkSpaceModel workspace)
    {
        if (variablesNode == null) return;
        /
        Debug.LogWarning("Variable loading not implemented.");
        /*
        foreach (XElement varNode in variablesNode.Elements("variable")) {
             string name = varNode.Value;
             string type = varNode.Attribute("type")?.Value ?? "";
             string id = varNode.Attribute("id")?.Value;
             workspace.VariableMap.CreateVariable(name, type, id); // O método en WorkspaceController
        }
        */
    }

   
} //Fin clase workSpaceSerializer