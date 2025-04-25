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


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MutatorClassAttribute : Attribute
{
    [Description("mark class for block mutator")]
    public MutatorClassAttribute() { }

   
    public string MutatorId { get; set; }
}//Fin clase MutatorClassAttribute