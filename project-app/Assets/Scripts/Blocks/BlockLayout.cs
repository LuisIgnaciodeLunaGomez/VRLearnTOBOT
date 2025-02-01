/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de posicionar los elementos dentro de un bloque y controla el crecimiento dinámico de los bloques cuandos se añaden elementos a su interior
 */

using UnityEngine;
using UnityEngine.UIElements;

public class BlockLayout
{

    public static void AdjustSize(VisualElement blockElement, float minWidth, float maxWidth)
    {
        float currentWidth = blockElement.style.width.value.value;

        if(currentWidth < minWidth)
        {
            blockElement.style.width = minWidth;
        }
        else if (currentWidth > maxWidth)
        {
            blockElement.style.width = maxWidth;
        }
    }

}
