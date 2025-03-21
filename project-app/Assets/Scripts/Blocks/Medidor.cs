/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase para medir sprites y detectar zonas de conexión en bloques de Unity.
 * 
 */
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteConnectionDetector : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    private SpriteRenderer spriteRenderer;
    private RectTransform canvasRectTransform;
    private RectTransform imageRectTransform;
    private Image image;

    public Vector3 TopConnection { get; private set; }
    public Vector3 BottomConnection { get; private set; }
    public Vector3 TopConnectionOffset { get; private set; } // Nueva posición desplazada
    public Vector3 BottomConnectionOffset { get; private set; } // Nueva posición desplazada
    public Vector3 TopConnectionOffset2 { get; private set; } // Nueva posición desplazada
    public Vector3 BottomConnectionOffset2 { get; private set; } // Nueva posición desplazada

    private Vector3 worldTopConnection;
    private Vector3 worldBottomConnection;
    private Vector3 worldTopConnectionOffset;
    private Vector3 worldBottomConnectionOffset;
    private Vector3 worldTopConnectionOffset2;
    private Vector3 worldBottomConnectionOffset2;
    void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        if (this.sprite == null && this.spriteRenderer != null)
            this.sprite = this.spriteRenderer.sprite;

        if (this.sprite != null)
            CreateCanvasAndImage();
        else
            Debug.LogError("No sprite assigned or found in SpriteRenderer!");
    }

    void OnValidate()
    {
        // Se ejecuta en el editor para que los Gizmos se muestren sin entrar en Play
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        if (this.sprite == null && this.spriteRenderer != null)
            this.sprite = this.spriteRenderer.sprite;

        if (this.sprite != null && this.canvasRectTransform == null)
            this.CreateCanvasAndImage();

        this.CalculateConnections();
    }

    private void CreateCanvasAndImage()
    {
        if (canvasRectTransform != null) return;

        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(this.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        this.canvasRectTransform = canvasObj.GetComponent<RectTransform>();
        this.canvasRectTransform.localScale = Vector3.one * 0.01f; // Ajuste para coincidir con unidades del mundo
        this.canvasRectTransform.sizeDelta = new Vector2(this.sprite.texture.width, this.sprite.texture.height);
        this.canvasRectTransform.pivot = new Vector2(0.5f, 0.5f); // Centro para coincidir con SpriteRenderer
        this.canvasRectTransform.localPosition = Vector3.zero;

        GameObject imageObj = new GameObject("SpriteBlock");
        imageObj.transform.SetParent(canvasObj.transform, false);
        this.image = imageObj.AddComponent<Image>();
        this.image.sprite = sprite;

        this.imageRectTransform = imageObj.GetComponent<RectTransform>();
        this.imageRectTransform.sizeDelta = new Vector2(this.sprite.texture.width,this.sprite.texture.height);
        this.imageRectTransform.pivot = new Vector2(0.5f, 0.5f); // Centro
        this.imageRectTransform.anchoredPosition = Vector2.zero;

        
    }

    private void CalculateConnections()
    {
        if (imageRectTransform == null || sprite == null)
        {
           
            return;
        }

        Vector2 size = this.imageRectTransform.rect.size;

        float offsetX =  0.0f; // Centro horizontal
        float offsetY =  20f; // 15% de la altura desde los bordes
        float pixelOffset = 45f; // 15 píxeles
        float unitOffset = pixelOffset / 1f; // Convertir a unidades (con dynamicPixelsPerUnit = 100)

        // Ajustado para coincidir con el pivot central
        this.TopConnection = new Vector3(-size.x/2+offsetX, size.y / 2 - offsetY, 0);
        this.BottomConnection = new Vector3(-size.x/2+ offsetX, -size.y / 2 + offsetY, 0);

        // Conexiones desplazadas 
        this.TopConnectionOffset = new Vector3(-size.x / 2 + unitOffset, size.y / 2 - offsetY, 0);
        this.BottomConnectionOffset = new Vector3(-size.x / 2 + unitOffset, -size.y / 2 + offsetY, 0);

        // Conexiones desplazadas 
        this.TopConnectionOffset2 = new Vector3(-size.x / 2 + unitOffset*3.2f, size.y / 2 - offsetY, 0);
        this.BottomConnectionOffset2 = new Vector3(-size.x / 2 + unitOffset*3.2f,-size.y / 2 + offsetY, 0);

        this.worldTopConnection = this.imageRectTransform.TransformPoint(this.TopConnection);
        this.worldBottomConnection = this.imageRectTransform.TransformPoint(this.BottomConnection);
        this.worldTopConnectionOffset = this.imageRectTransform.TransformPoint(this.TopConnectionOffset);
        this.worldBottomConnectionOffset = this.imageRectTransform.TransformPoint(this.BottomConnectionOffset);
        this.worldTopConnectionOffset2 = this.imageRectTransform.TransformPoint(this.TopConnectionOffset);
        this.worldBottomConnectionOffset2 = this.imageRectTransform.TransformPoint(this.BottomConnectionOffset);

        Debug.Log($"[Relativo] TopConnection: {this.TopConnection}");
        Debug.Log($"[Relativo] BottomConnection: {this.BottomConnection}");
        Debug.Log($"[Mundo] worldTopConnectionOffset: {this.worldTopConnectionOffset}");
        Debug.Log($"[Mundo] worldBottomConnectionOffset: {this.worldBottomConnectionOffset}");
        Debug.Log($"[Mundo] worldTopConnectionOffset: {this.worldTopConnectionOffset2}");
        Debug.Log($"[Mundo] worldBottomConnectionOffset: {this.worldBottomConnectionOffset2}");

    }
    
    void OnDrawGizmos()
    {
        if (this.sprite == null || this.imageRectTransform == null)
        {
            if (this.spriteRenderer == null) this.spriteRenderer = GetComponent<SpriteRenderer>();
            if (this.sprite == null && this.spriteRenderer != null) this.sprite = this.spriteRenderer.sprite;
            if (this.sprite != null && this.canvasRectTransform == null) CreateCanvasAndImage();
            this.CalculateConnections();
        }

        if (this.imageRectTransform != null)
        {
            this.worldTopConnection = imageRectTransform.TransformPoint(this.TopConnection);
            this.worldBottomConnection = imageRectTransform.TransformPoint(this.BottomConnection);
            this.worldTopConnectionOffset = imageRectTransform.TransformPoint(this.TopConnectionOffset);
            this.worldBottomConnectionOffset = imageRectTransform.TransformPoint(this.BottomConnectionOffset);
            this.worldTopConnectionOffset2 = imageRectTransform.TransformPoint(this.TopConnectionOffset2);
            this.worldBottomConnectionOffset2 = imageRectTransform.TransformPoint(this.BottomConnectionOffset2);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(this.worldTopConnection, 0.05f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(this.worldBottomConnection, 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(this.worldTopConnectionOffset, 0.05f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(this.worldBottomConnectionOffset, 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(this.worldTopConnectionOffset2, 0.05f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(this.worldBottomConnectionOffset2, 0.05f);
        }
    }
}