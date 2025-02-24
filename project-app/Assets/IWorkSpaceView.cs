/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;

using System.Collections.Generic;

public interface IWorkSpaceView
{
   RectTransform CodingArea { get;  }

   void BindModel(WorkSpace workSpace);

    void UnBindModel();

    void AddBlockView(IBlockView blockView);

    void RemoveBlockView(IBlockView blockView);

    void Dispose();
}


