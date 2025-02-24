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

public interface  IBlockView: IBlockElement
{
    
    //Tipo del bloque
    string BlockType { get;  }

    //Indica si el bloque esta en la caja de herramientas
    bool inToolBox { get; set; }

    //Asocia el modelo de lógica con el bloque de vista
    void BindModel(Block block);

    //Desasocia el modelo de lógica con el bloque de vista
    void unBindModel();

    //Elimina el bloque de la vista
    void Dispose();

    //Posiciona el bloque en la interfaz
    void UpdatePosition(Vector2 position);

}


