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
 */

 using UnityEngine;

public class MeasureSprite : MonoBehaviour
{
   
   
        public SpriteRenderer spriteRenderer;

    void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("No se encontró el SpriteRenderer. Asigna un sprite al objeto.");
            return;
        }

        // Obtener dimensiones en píxeles
        Vector2 sizeInPixels = new Vector2(spriteRenderer.sprite.texture.width, spriteRenderer.sprite.texture.height);

        Debug.Log($"📏 Dimensiones del sprite: {sizeInPixels.x}px de ancho, {sizeInPixels.y}px de alto");
    }
}


