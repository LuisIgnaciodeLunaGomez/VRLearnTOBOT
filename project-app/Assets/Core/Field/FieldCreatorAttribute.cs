/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System;
using System.ComponentModel;


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class FieldCreatorAttribute : Attribute
{
    [Description("mark factory method for block fields")]
    public FieldCreatorAttribute() { }

    /// <summary>
    /// type of field, which is the same with that defined in json definition
    /// </summary>
    public string FieldType { get; set; }
}
