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
 * Descripción: Clase que se encarga de aplicar el color a la categoria correctamente sobre la textura del bloque y se asegura que la imagen de fondo no distorsione los bordes del bloque
 */

using UnityEngine;

public class BlockRenderere
{
    public static Texture2D ApplyColorToTexture(Texture2D original, Color color)
    {
        Texture2D coloredTexture = new Texture2D(original.width, original.height);
        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                Color pixelColor = original.GetPixel(x, y);
                if (pixelColor.a > 0) // Mantener transparencia
                {
                    pixelColor *= color;
                    coloredTexture.SetPixel(x, y, pixelColor);
                }
            }
        }
        coloredTexture.Apply();
        return coloredTexture;
    }
}