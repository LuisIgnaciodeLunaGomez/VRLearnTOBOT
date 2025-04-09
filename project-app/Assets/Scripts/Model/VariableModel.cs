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
 * Descripción:
 */

using System;
using UnityEngine;


public class VariableModel
{

    public WorkSpaceModel Workspace;

 
    public string Name { get; set; }

    public string Type { get; set; }

    
    public string ID { get; set; }

 
    public VariableModel(WorkSpaceModel workspace, string name, string optType = null, string optId = null)
    {
        this.Workspace = workspace;
        this.Name = name;
        this.Type = string.IsNullOrEmpty(optType) ? "" : optType;
        this.ID = string.IsNullOrEmpty(optId) ? Utilidades.GenUid() : optId;
    }

  
    public static int CompareByName(VariableModel var1, VariableModel var2)
    {
        return String.CompareOrdinal(var1.Name.ToLower(), var2.Name.ToLower());
    }

    public void RequestRename()
    {
        Debug.Log($"UI request to rename variable '{this.Name}' (ID: {this.ID})");
        this.Workspace?.ShowRenameVariablePrompt(this);
    }

    public void RequestDelete()
    {
        Debug.Log($"UI request to delete variable '{this.Name}' (ID: {this.ID})");
        this.Workspace?.ShowDeleteVariablePrompt(this);

    }
}//Fin Clase VariableModel
