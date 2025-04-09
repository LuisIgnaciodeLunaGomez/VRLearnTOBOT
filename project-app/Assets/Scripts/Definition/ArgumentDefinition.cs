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
 * Descripción: Estructura para almacenar la definición de un argumento/entrada individual cargado desde un nodo <Arg> en el XML.
 */

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public partial class ArgumentDefinition
{
    [Tooltip("Tipo del argumento: input_value, input_statement, input_dummy, o un tipo de field (field_label, field_input, etc.)")]
    public string type;

    [Tooltip("Nombre del argumento (para Inputs) o del Campo (para Fields)")]
    public string name; 

    [Tooltip("Valor textual (para field_label) o valor inicial/por defecto (para algunos fields)")]
    public string value; 

    [Tooltip("Valor por defecto explícito (usado también para campos sombra)")]
    public string defaultValue; 

    [Tooltip("Checks de tipo para conexiones InputValue/InputStatement (lista de strings como 'Number', 'Boolean')")]
    public List<string> checks; 

    [Tooltip("Alineación para el Input que contiene este argumento/campo")]
    public EAlign align = EAlign.Left; 

    [Header("Sombra (si type='input_value')")]
    [Tooltip("Tipo del campo que actúa como sombra si el input está vacío (e.g., 'field_number')")]
    public string shadowFieldType;

    [Tooltip("Nombre del campo sombra (opcional, si el campo sombra necesita nombre)")]
    public string shadowFieldName; 

    [Header("Dropdown (si type='field_dropdown')")]
    [Tooltip("Opciones para el campo dropdown")]
    public List<(string display, string value)> dropdownOptions; 

    [Header("Interno")]
    [Tooltip("Definición JSON completa del campo, generada para FieldFactory")]
    public JObject DefinitionJson { get; set; } 

    [Tooltip("Indica si este argumento define un Input (Value, Statement, o Dummy)")]
    public bool IsInputDefinition => type == BlockInputType.Value || type == BlockInputType.Statement || type == BlockInputType.Dummy;

    [Tooltip("Indica si este argumento define un Input de Statement ('input_statement')")]
    public bool IsStatement => type == BlockInputType.Statement;

    [Tooltip("Indica si este argumento define un Input de Valor ('input_value')")]
    public bool IsValue => type == BlockInputType.Value;

    [Tooltip("Indica si este argumento define un Input Dummy ('input_dummy')")]
    public bool IsDummy => type == BlockInputType.Dummy;

    [Tooltip("Indica si este argumento define un Field (cualquier cosa que empiece con 'field_')")]
    public bool IsField => type != null && type.StartsWith("field_");

    [Tooltip("Obtiene el tipo de campo si IsField es true, sino null")]
    public string FieldType => IsField ? type : null;

    [Tooltip("Obtiene el nombre del campo si IsField es true, sino null (es el mismo que 'name')")]
    public string FieldName => IsField ? name : null;

    
    public ArgumentDefinition()
    {
        checks = new List<string>();
        dropdownOptions = new List<(string display, string value)>();
        align = EAlign.Left; 
    }

} //fin clase ArgumentDefinition
