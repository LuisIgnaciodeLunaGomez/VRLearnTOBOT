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
 * 
 * Descripción:
 * 
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockScrollList : MonoBehaviour
{

    [SerializeField] private GameObject m_blockPrefab; // Prefab del bloque
    [SerializeField] private Transform m_blockContainer; // Contenedor de bloques

    private Dictionary<string, GameObject> blockLists = new Dictionary<string, GameObject>(); // Diccionario de bloques
    private string activeCategory;

    private TextMeshProUGUI m_categoryText; //Texto para mostrar el nombre de la categoría antes de los bloques

    public void Initialized(GameObject prefab, Transform container)
    {
        if (prefab == null || container == null)
        {
            Debug.LogWarning("Prefab o contenedor de bloques no inicializado");
            return;
        }

        this.m_blockPrefab = prefab;
        this.m_blockContainer = container;

        if (this.m_blockContainer == null)
        {
            Debug.LogWarning("No se ha asignado un contenedor de bloques");
            return;
        }

        Debug.Log("Prefab correctamente asignado en BrockScrollList");

        this.CreateCategoryTitle();
    }

    public void ShowBlockCategory(string categoryName, Color categoryColor)
    {

        if (activeCategory == categoryName)
        {
            return;
        }

        //Ocultar la categoria anterior si existía

        if (!string.IsNullOrEmpty(activeCategory) && blockLists.ContainsKey(activeCategory))
        {
            blockLists[activeCategory].SetActive(false);
        }

        activeCategory = categoryName;

        //Si la categoría ya ha sido cargada, la activamos con esto se evita tener que volver a cargarla
        if (blockLists.ContainsKey(activeCategory))
        {
            blockLists[activeCategory].SetActive(true);
            return;
        }

        if (m_categoryText != null)
        {
            m_categoryText.text = categoryName;
            Debug.Log($"Asignado el nombre de la categoria {categoryName}");
        }

        //Verifico la existencia BlockContainer
        if (m_blockContainer == null)
        {
            Debug.LogWarning("No se ha asignado un contenedor de bloques");
            return;
        }

        //Creo un nuevo contenedor para la categoría 
        GameObject categoryContainer = CreateCategoryContainer(categoryName);

        blockLists[categoryName] = categoryContainer;

        //Cargo los datos de los bloques de la categoria desde BlockDataLoader

        string xmlFilePath = $"XML/Blocks/{categoryName}";//Ubicación del archivo XML

        BlockDataLoader.BlockCategoryData categoryData = BlockDataLoader.LoadCategoryData(xmlFilePath);

        if (categoryData == null || categoryData.blocks == null || categoryData.blocks.Count == 0)
        {
            Debug.LogWarning($"No hay bloques para mostrar para la categoría: ´{categoryName}");
            return;
        }

        //Instancio los bloques en el panel adecuado 

        foreach (var block in categoryData.blocks)
        {

            // Cargar el prefab basado en el nombre del sprite en el XML
            GameObject blockPrefab = Resources.Load<GameObject>($"Prefabs/BlocksPrefab/{block.spriteName}");

            //Revisar si BlockPrefab esta inicializado

            if (blockPrefab == null)
            {
                Debug.LogWarning($"No se encontró el prefab '{block.spriteName}' en Resources/Prefabs/BlocksPrefab/");
                continue;
            }

            GameObject blockGO = Instantiate(blockPrefab, categoryContainer.transform);

            //Obtener el tamaño del prefab desde el XML de tamaños
            Vector2 blockSize = BlockDataLoader.GetBlockSize(block.spriteName);
            //blockGO.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            Vector2 scaledBlockSize = new Vector2(blockSize.x * 0.16f, blockSize.y * 0.16f); // Tamaño reducido (51x28)
           
            RectTransform blockRect = blockGO.GetComponent<RectTransform>();

            if (blockRect != null)
            {
                blockRect.sizeDelta = blockSize; //Tamaño original del bloque
                blockRect.anchorMin = new Vector2(0, 1); // Ancla arriba a la izquierda
                blockRect.anchorMax = new Vector2(0, 1);
                blockRect.pivot = new Vector2(0, 1); // Pivote arriba a la izquierda
                blockRect.anchoredPosition = Vector2.zero; // Posición inicial (ajustada por VerticalLayoutGroup)
            }
            else
            {
                Debug.LogWarning($"El prefab {block.spriteName} no tiene un RectTransform");
            }


            BlockBehaviour blockBehaviour = blockGO.GetComponent<BlockBehaviour>();

            if (blockBehaviour != null)
            {
                blockBehaviour.Initialize(block);
            }
            else
            {
                Debug.LogWarning($"El prefab {block.spriteName} no tiene el componente BlockBehaviour, se lo añado");
                blockGO.AddComponent<BlockBehaviour>();
            }
            
            LayoutElement layoutElement = blockGO.AddComponent<LayoutElement>();

            if (layoutElement == null)
            {
                layoutElement = blockGO.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = blockSize.y;
            layoutElement.preferredWidth = blockSize.x;
            
           
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            blockGO.transform.localScale = new Vector3(0.16f, 0.16f, 1f); // Escala reducida visualmente
            //blockGO.transform.localScale = Vector3.one;
            
            // Aplicar localScale para reducir visualmente el bloque
            
            //Asignar el color de la categoria

            Image blockImage = blockGO.GetComponent<Image>();
            if (blockImage != null)
            {
                blockImage.color = categoryColor;
                blockImage.type = Image.Type.Sliced; // Asegúrate de que sea Sliced para mantener los recortes
                RectTransform imageRect = blockImage.GetComponent<RectTransform>();

                if (imageRect != null)
                {
                    imageRect.sizeDelta = blockSize; // Tamaño reducido visualmente (51x28)
                }
            }
            else
            {
                Debug.LogWarning($"El prefab {block.spriteName} no tiene un componente Image");
            }

            Debug.Log($"Categoría de bloques {categoryName} cargada correctamente con {categoryData.blocks.Count} bloques");
        }
    }

    public void HideBlockCategory(string categoryName)
    {
        if (!string.IsNullOrEmpty(categoryName) || blockLists.ContainsKey(categoryName))
        {
            blockLists[categoryName].SetActive(false);
        }
        categoryName = null;
    }

    private void CreateCategoryTitle()
    {
        if (m_categoryText == null) // Evitar duplicados
        {
            // Crear objeto de texto
            GameObject categoryTextGO = new GameObject("CategoryText");
            categoryTextGO.transform.SetParent(m_blockContainer, false);
            m_categoryText = categoryTextGO.AddComponent<TextMeshProUGUI>();

            // Configurar RectTransform del texto
            RectTransform categoryTextRect = categoryTextGO.GetComponent<RectTransform>();
            categoryTextRect.anchorMin = new Vector2(0.5f, 1);
            categoryTextRect.anchorMax = new Vector2(0.5f, 1);
            categoryTextRect.pivot = new Vector2(0.5f, 1);
            categoryTextRect.anchoredPosition = new Vector2(0, -20); // Espacio debajo del borde superior

            // Configurar texto
            m_categoryText.text = "Categoría"; // Placeholder inicial
            m_categoryText.alignment = TextAlignmentOptions.Center;
            m_categoryText.fontSize = 36;
            m_categoryText.color = Color.black;
        }
    }

   
    public GameObject CreateCategoryContainer(string categoryName)
    {
        // Crear contenedor para la categoría dentro de BlockContainer que contendrá sus bloquees
        GameObject categoryContainer = new GameObject(categoryName);
        categoryContainer.transform.SetParent(m_blockContainer, false);

        // Configurar RectTransform
        RectTransform categoryContainerRect = categoryContainer.AddComponent<RectTransform>();
        categoryContainerRect.anchorMin = new Vector2(0, 1);
        categoryContainerRect.anchorMax = new Vector2(1, 1);
        categoryContainerRect.pivot = new Vector2(0, 1);
        categoryContainerRect.anchoredPosition = Vector2.zero; // Espaciado debajo del nombre de la categoría
        //categoryContainerRect.sizeDelta = new Vector2(0, 0.95f);
        categoryContainerRect.sizeDelta = Vector2.zero;

        // Agregao un Layout para manejar los bloques y que se configuren correctamente uno debajo de otros
        VerticalLayoutGroup layoutGroup = categoryContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // Alinear los bloques a la izquierda
        //layoutGroup.spacing = 0; // Espacio entre bloques
        layoutGroup.spacing = -140f;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.padding = new RectOffset(0, 0, 10, 0); // Sin padding adicional

       //  ContentSizeFitter fitter = categoryContainer.AddComponent<ContentSizeFitter>();
       // fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
       // fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        return categoryContainer;
    }

    private float CalculateSpacing()
    {
        // Calcula un espaciado proporcional al tamaño reducido de los bloques (51x28 con localScale 0.16f)
        Vector2 originalBlockSize = new Vector2(316, 175); // Tamaño original del prefab
        float scaleFactor = 0.16f; // Factor de escala usado en localScale
        float scaledHeight = originalBlockSize.y * scaleFactor; // Altura reducida (28 píxeles)
        // Usa un espaciado proporcional, por ejemplo, 1.6 píxeles (10 * 0.16f) o ajusta según necesites
        return 1.6f; // Espaciado compacto, ajusta si es necesario para Scratch
    }

    public void AssignBlockContainer(Transform container)
    {
        if (container == null)
        {
            Debug.LogWarning("Intento de asignar un contenedor de bloques nulo.");
            return;
        }

        m_blockContainer = container;
        Debug.Log("Contenedor de bloques asignado correctamente.");
    }

}