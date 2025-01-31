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


