/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class ToolboxBlockCategory
{
    public string CategoryName;
    //public string Colour;
    public string Custom;
    public string BlockTypePrefix;
    public List<string> BlockList;

    [NonSerialized] private bool mInited = false;

    public Color Color { get; private set; }


    /// <summary>
    /// Inicializa la categoría con el Color procesado externamente.
    /// Llamado desde UICanvasView.InitializeCategoryColors después de leer Categories.xml.
    /// </summary>
    /// <param name="categoryColor">El Color real para esta categoría.</param>
    public void Init(Color categoryColor) 
    {
        if (mInited) return;
        this.Color = categoryColor; 
        if (BlockList == null) BlockList = new List<string>();

        mInited = true;
    }



}