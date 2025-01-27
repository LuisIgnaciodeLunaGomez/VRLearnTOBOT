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
 * Versión: 1.0.0
 */



using UnityEngine;
using UnityEngine.UIElements;

public class BlockUIFactory
{
    public static VisualElement CreateBlockElement(Block block)
    {
        var blockElement = new VisualElement();
        blockElement.AddToClassList("block");

        // Añade las conexiones visuales
        if (block.OutputConnection != null)
        {
            var output = new VisualElement();
            output.AddToClassList("output-connection");
            blockElement.Add(output);
        }

        if (block.NextConnection != null)
        {
            var next = new VisualElement();
            next.AddToClassList("next-connection");
            blockElement.Add(next);
        }

        if (block.PreviousConnection != null)
        {
            var prev = new VisualElement();
            prev.AddToClassList("previous-connection");
            blockElement.Add(prev);
        }

        return blockElement;
    }
}
