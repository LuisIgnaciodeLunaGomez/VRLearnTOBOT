/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de la creación de los bloques para cada categoría
 */

using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Dynamic;
using System;
using System.Web;
using System.Security.Cryptography;


public class BlockManager : MonoBehaviour
{
   // public GameObject blockPrefab;
    public Transform blockContainer;

    /**
     * LoadBlocks
     * 
     * Load blocks from a category
     * 
     * @param string categoryName
     * @param Color categoryColor
     * @return void
     */
    public void LoadBlocks(string categoryName, Color categoryColor)
    {

        string xmlPath = "XML/Blocks/" + categoryName;
        TextAsset xmlData = Resources.Load<TextAsset>(xmlPath);

        if (xmlData == null)
        {
            Debug.LogError("No se pudo cargar el archivo XML" + xmlPath);
            return;
        }

        //Elimino los bloques anteriores antes de cargar nuevos

        foreach (Transform child in blockContainer)
        {
            Destroy(child.gameObject);
        }

        XDocument xmlDoc = XDocument.Parse(xmlData.text);
        IEnumerable<XElement> blocks = xmlDoc.Element("Blocks").Elements("Block");

        foreach (XElement block in blocks)
        {
            string type = block.Element("Type").Value;
            string label = block.Element("Label").Value;
            string spriteName = block.Element("Sprite").Value;

            bool hasTopConnection = block.Element("connections")?.Element("top")?.Value == "true";
            bool hasBottomConeection = block.Element("connections")?.Element("bottom")?.Value == "true";

            // Cargar la textura del bloque
            Sprite blockSprite = Resources.Load<Sprite>("Textures/" + spriteName);

            if (blockSprite == null)
            {
                Debug.LogError("No se pudo cargar la textura en bloques " + spriteName);
                return;
            }

            CreateBlock(type, label, categoryColor, blockSprite, hasTopConnection, hasBottomConeection);
        }
    }
    /**
     * CreateBlock
     * 
     * Create a block
     * 
     * @param string type
     * @param string label
     * @param Color categoryColor
     * @return void
     */
    private void CreateBlock(string type, string label, Color categoryColor, Sprite blockSprite, bool hasTopConnection, bool hasBottomConnection)
    {
        // Crear el objeto del bloque
        GameObject newBlock = new GameObject(type, typeof(RectTransform), typeof(Image));
        newBlock.transform.SetParent(blockContainer, false);
        newBlock.name = type;

        RectTransform rect = newBlock.GetComponent<RectTransform>();


        if (rect == null)
        {
            rect = newBlock.AddComponent<RectTransform>(); 
        }

        // Configuro anclajes y posición para que los bloques aparezcan en la zona correcta
        rect.anchorMin = new Vector2(0, 1);  // Arriba-izquierda
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -40); // * blockContainer.childCount); 
        rect.sizeDelta = new Vector2(100, 60);
        rect.localScale = Vector3.one;

        //Configuro el tamaño y el color del bloque
        Image blockImage = newBlock.GetComponent<Image>();
        blockImage.sprite = blockSprite;
        // Configuro el tamaño de la imagen
        blockImage.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
        blockImage.color = categoryColor;

        GameObject textObject = new GameObject("BlockText");

        textObject.transform.SetParent(newBlock.transform);

        //Configuro el texto del bloque
        TextMeshProUGUI blockText = newBlock.GetComponentInChildren<TextMeshProUGUI>();
        if (blockText != null)
        {
            blockText.text = label;
            blockText.alignment = TextAlignmentOptions.Center;
            blockText.fontSize = 18;
            blockText.color = Color.black;

            // Configuración del RectTransform del texto
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(100, 60);
            textRect.anchoredPosition = Vector2.zero;
        }

        //Configuración de conexiones visuales
        Transform topConnection = newBlock.transform.Find("TopConnection");
        Transform bottomConnection = newBlock.transform.Find("BottomConnection");

        if (topConnection != null)
            topConnection.gameObject.SetActive(hasTopConnection);

        if (bottomConnection != null)
            bottomConnection.gameObject.SetActive(hasBottomConnection);
    }
}

