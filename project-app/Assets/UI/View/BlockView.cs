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
using UnityEngine.UI;
using System.Collections.Generic;
public class BlockView : MonoBehaviour, IBlockView
{

    [SerializeField] private List<Image> m_BgImages = new List<Image>(); //Lista de imagenes que forman el fondo del bloque 
    private Block m_Block; //Referencia al modelo lógico del bloque
        
    public bool inToolBox { get; set; }
    public Vector2 Position { get; set; }
    public RectTransform ViewRectransform { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    string IBlockView.BlockType => m_Block?.Type ?? "Unknown";
    public Block Block => m_Block;

    public void BindModel(Block block)
    {
        if (m_Block == block) return; //Si el modelo lógico del bloque es el mismo que el modelo lógico del bloque actual, no se hace nada

        if (m_Block == block) return;
        unBindModel(); // Si hay un bloque anterior, desvincularlo

        m_Block = block;
        UpdatePosition(m_Block.XY); // Posiciona el bloque en el área de trabajo

        // Agregar el bloque al espacio de trabajo
       // GeneradorUI.workSpaceView.AddBlockView(this);
    }

    public void Dispose()
    {
        if(m_Block != null) unBindModel(); // Si el bloque no es nulo, desvincularlo
        GameObject.Destroy(ViewRectransform.gameObject); // Destruye el bloque
    }

    public void unBindModel()
    {
        m_Block = null; // Desvincula el bloque
    }

    public void UpdatePosition(Vector2 position)
    {
        if (ViewRectransform != null)
        {
            ViewRectransform.anchoredPosition = position; // Actualiza la posición del bloque en la interfaz
        }
    }

    public void UpdateLayout()
    {
        // Asegurar que los bloques se distribuyan correctamente en el contenedor
        if (ViewRectransform != null)
        {
            ViewRectransform.SetAsLastSibling();
        }
    }

    /**
     * Añade una imagen de fondo al bloque
     * @param image Imagen de fondo a añadir
     */
    public void AddBgImage(Image image)
    {
        if (image != null && !m_BgImages.Contains(image)) //Si la imagen de fondo no es nula y no ha sido añadida antes
            m_BgImages.Add(image); //Añade la imagen de fondo a la lista de imágenes de fondo del bloque
    }

    /**
     * Añade una image de fondo al bloque, si esta no ha sido agregada antes
     * @param image Imagen de fondo a añadir
     */

    public void ChangeBgColor(Color color)
    {
        m_BgImages.RemoveAll(bg => bg == null); //Elimina las imágenes de fondo que sean null
        foreach (Image bg in m_BgImages)
        {
            bg.color = color;   //Cambia el color de las imágenes de fondo
        }
    }

}
