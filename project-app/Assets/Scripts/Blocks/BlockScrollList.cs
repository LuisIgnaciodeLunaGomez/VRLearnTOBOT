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
 * Descripción: Esta clase se encarga de generar las imagenes de los bloques para su representación correcta
 * 
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlockScrollList : MonoBehaviour
{

    [SerializeField] private GameObject m_blockPrefab; // Prefab del bloque
    [SerializeField] private Transform m_blockContainer; // Contenedor de bloques
    [SerializeField] private WorkSpaceView m_workSpaceView;


    private Dictionary<string, GameObject> blockLists = new Dictionary<string, GameObject>(); // Diccionario de bloques
    private string activeCategory;
    private Transform rightPanelTransform;
    private WorkSpace m_workSpace; // Espacio de trabajo

    private TextMeshProUGUI m_categoryText; //Texto para mostrar el nombre de la categoría antes de los bloques

    public void Initialized(GameObject prefab, Transform container)
    {
       /* if (prefab == null)
        {
            Debug.LogWarning("El prefab de bloques es nulo. Se requiere un prefab válido.");
            return;
        }*/

        if (container == null)
        {
            //Debug.LogWarning("El contenedor de bloques es nulo. Se requiere un Transform válido.");
            return;
        }

        this.m_blockPrefab = prefab;
        this.m_blockContainer = container;

        if (this.m_blockContainer == null)
        {
            Debug.LogWarning("No se ha asignado un contenedor de bloques");
            return;
        }

       // Debug.Log("Prefab correctamente asignado en BrockScrollList");

        this.CreateCategoryTitle();
    }


    public void SetWorkSpace(WorkSpace workSpace)
    {
        if (workSpace == null)
        {
           // Debug.LogError("Intento de asignar un WorkSpace nulo a BlockScrollList.");
            return;
        }
        m_workSpace = workSpace;
      //  Debug.Log("WorkSpace asignado correctamente a BlockScrollList.");
    }

    public void SetWorkSpaceView(WorkSpaceView workSpaceView)
    {
        if (workSpaceView == null)
        {
            //Debug.LogError("Intento de asignar un WorkSpaceView nulo a BlockScrollList.");
            return;
        }
        m_workSpaceView = workSpaceView;
      //  Debug.Log("WorkSpaceView asignado correctamente a BlockScrollList.");
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

        //Verifico la existencia del BlockContainer
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

       this.BuidlView(categoryData, categoryName, categoryContainer, categoryColor);

    }

    public void BuidlView(BlockDataLoader.BlockCategoryData categoryData, string categoryName, GameObject categoryContainer, Color categoryColor)
    {

        foreach (var blockData in categoryData.blocks)
        {
            //GameObject blockGO = NewBlockView2(blockData, categoryColor, categoryContainer, categoryName, categoryData); //Versión anterior

            GameObject blockGO = NewBlockView(blockData, categoryData, categoryName, categoryContainer, categoryColor);

            if (blockGO != null)
            {
                blockGO.transform.SetParent(categoryContainer.transform, false);
                Debug.Log($"Bloque {blockData.type} añadido al contenedor de la categoría {categoryName}");
            }
            else
            {
                Debug.LogWarning($"No se pudo crear el bloque {blockData.type} para la categoría {categoryName}");
            }

            // NewBlockView(blockData, categoryData, categoryName);

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

            // Configurar RectTransform del texto para CategoryText
            RectTransform categoryTextRect = categoryTextGO.GetComponent<RectTransform>();
            categoryTextRect.anchorMin = new Vector2(0f, 1);
            categoryTextRect.anchorMax = new Vector2(0f, 1);
            categoryTextRect.pivot = new Vector2(0f, 1);
            categoryTextRect.anchoredPosition = Vector2.zero; // Espacio debajo del borde superior

            // Configurar texto
            m_categoryText.text = "Categoría"; // Placeholder inicial quitar después
            m_categoryText.alignment = TextAlignmentOptions.Center;
            m_categoryText.fontSize = 24;
            m_categoryText.color = Color.black;
        }
    }


    public GameObject CreateCategoryContainer(string categoryName)
    {
        // Crear contenedor para la categoría dentro de BlockContainer que contendrá sus bloques por ejemplo Movimiento, Eventos, etc.
        GameObject categoryContainer = new GameObject(categoryName);
        categoryContainer.transform.SetParent(m_blockContainer, false);

        // Configurar RectTransform
        RectTransform categoryContainerRect = categoryContainer.AddComponent<RectTransform>();
        categoryContainerRect.anchorMin = new Vector2(0, 1);
        categoryContainerRect.anchorMax = new Vector2(1, 1);
        categoryContainerRect.pivot = new Vector2(0.5f, 0.5f);
        categoryContainerRect.anchoredPosition = Vector2.zero; // Espaciado debajo del nombre de la categoría
        //categoryContainerRect.sizeDelta = new Vector2(0, 0.95f);
        categoryContainerRect.sizeDelta = Vector2.zero;

        // Agregao un Layout para manejar los bloques y que se configuren correctamente uno debajo de otros
        VerticalLayoutGroup layoutGroup = categoryContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // Alinear los bloques a la izquierda
        //layoutGroup.spacing = 0; // Espacio entre bloques
        layoutGroup.spacing = 5f;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childScaleHeight = true;
        layoutGroup.childScaleWidth = true;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10); // Sin padding adicional

        //ContentSizeFitter fitter = categoryContainer.AddComponent<ContentSizeFitter>();
        //fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
       // fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        return categoryContainer;
    }

    public void AssignBlockContainer(Transform container)
    {
        if (container == null)
        {
            Debug.LogWarning("Intento de asignar un contenedor de bloques nulo.");
            return;
        }

        m_blockContainer = container;
       // Debug.Log("Contenedor de bloques asignado correctamente.");
    }

    /**
     * Método para instanciar un bloque en el panel de bloques
     * @param blockData Datos del bloque a instanciar
     * @param categoryColor Color de la categoría de bloques
     * @param categoryContainer Contenedor de la categoría de bloques
     * @param categoryName Nombre de la categoría de bloques
     * @param categoryData Datos de la categoría de bloques
     * @return GameObject Instancia del bloque creado
     */
   
    private GameObject NewBlockView(BlockDataLoader.BlockData blockData, BlockDataLoader.BlockCategoryData categoryData, string categoryName, GameObject categoryContainer, Color categoryColor)
    {

        Debug.Log($"Creando bloque {blockData.type} en la categoría {categoryName}");

        //Creación del bloque lógico
        Block newBlock = new Block(blockData.type, Vector2.zero, m_workSpace); 
        m_workSpace.AddTopBlocks(newBlock); // Añadir el bloque al WorkSpace

        //Creación de la vista del bloque usando BlockViewFactory
        BlockView blockView = BlockViewFactory.CreateView(newBlock, blockData, m_workSpaceView);
        if (blockView == null)
        {
            Debug.LogError($"No se pudo crear BlockView para {blockData.type}");
            return null;
        }

        //Creación de  un GameObject basado en un prefab
       //GameObject blockGO = new GameObject(blockData.type);//1er motion_movesteps

        GameObject blockGO = blockView.gameObject;
        blockGO.transform.SetParent(categoryContainer.transform, false); // Añadir al contenedor de la categoría

        // Obtener el tamaño original del prefab desde BlockDataLoader o el RectTransform
       Vector2 originalSize = BlockDataLoader.GetBlockSize(blockData.spriteName); // Asegúrate de que este método devuelva 316x175

        Debug.Log($"Tamaño original del bloque {blockData.spriteName}: {originalSize}");
        if (originalSize == Vector2.zero)
        {
            originalSize = new Vector2(316, 175); // Valor por defecto si no se encuentra
            Debug.LogWarning($"Tamaño no encontrado para {blockData.spriteName}, usando 316x175 como predeterminado.");
        }

        RectTransform blockRect = blockGO.GetComponent<RectTransform>();
        //Vector2 blockSize = BlockDataLoader.GetBlockSize(blockData.spriteName); // Obtener tamaño desde XML

        //blockRect.sizeDelta = blockSize;
        blockRect.sizeDelta = originalSize; // new Vector2(316, 175);
        blockRect.anchorMin = new Vector2(0, 1); // Anclar arriba a la izquierda
        blockRect.anchorMax = new Vector2(0, 1);
        blockRect.pivot = new Vector2(0, 1);
        blockRect.anchoredPosition = Vector2.zero;
        //blockRect.sizeDelta = new Vector2(100f, 50f);

        //Se añade una Image para el fondo del bloque
        Image blockImage = blockGO.GetComponent<Image>();
        blockImage.color = categoryColor; // Usar el color de la categoría
        blockImage.type = Image.Type.Sliced; // Para que se adapte al tamaño
        RectTransform imageRect = blockImage.GetComponent<RectTransform>();
        imageRect.sizeDelta = blockRect.sizeDelta;
        //imageRect.sizeDelta = Vector2.zero;

        //Se añade el BlockView al GameObject
        blockView.transform.SetParent(blockGO.transform, false);
        blockView.SetWorkSpaceView(m_workSpaceView);
        blockView.BindModel(newBlock, blockData);
        blockView.ChangeBgColor(categoryColor);
        blockView.UpdatePosition(Vector2.zero); // Posición inicial

        // Actualizar el RectTransform con el CalculatedSize del BlockView
        Vector2 calculatedSize = blockView.CalculatedSize;
        if (calculatedSize.x <= 0 )
        {
            Debug.LogWarning($"CalculatedSize inválido para {blockData.type}: {calculatedSize}. Usando tamaño mínimo.");
            calculatedSize.x = 100f;
        }
        float currentHeight = blockRect.sizeDelta.y;
        blockRect.sizeDelta = new Vector2(calculatedSize.x, currentHeight); // Actualizar solo el ancho
        imageRect.sizeDelta = new Vector2(calculatedSize.x, currentHeight); // Sincronizar el tamaño de la imagen
        //Se añade un LayoutElement para que el VerticalLayoutGroup lo maneje
        LayoutElement layoutElement = blockGO.AddComponent<LayoutElement>();
        //Vector2 calculatedSize = blockView.CalculatedSize;
        float scaleFactor = 0.32f; // Factor de escala que usaste originalmente
        layoutElement.preferredWidth = calculatedSize.x;// * scaleFactor;
        layoutElement.preferredHeight = currentHeight;// * scaleFactor;
        //layoutElement.preferredWidth = 200;
       // layoutElement.preferredHeight = 50;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        //Se escala el bloque (similar a Scratch)
        
        blockGO.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1); // Escalar a 0.16

        //Se añade comportamiento
        BlockBehaviour blockBehaviour = blockGO.AddComponent<BlockBehaviour>();
        if (blockBehaviour != null)
        {
            blockBehaviour.Initialize(blockData);
        }

        return blockGO;
    }
    public void SetWorkspaceTransform(Transform workspaceTransform)
    {
        this.rightPanelTransform = workspaceTransform;
        //Debug.Log("RightPanel Transform asignado correctamente en BlockScrollList");
    }

    private void PickBlockView(PointerEventData eventData, GameObject blockGO, Transform workspaceTransform)
    {
        Debug.Log($"Iniciando arrastre del bloque {blockGO.name}");

        // Calcular la posición local en el RightPanel
        Vector3 localPos = workspaceTransform.InverseTransformPoint(blockGO.transform.position);

        // Clonar el bloque en el RightPanel
        GameObject newBlockGO = CloneBlockView(blockGO, new Vector2(localPos.x, localPos.y), workspaceTransform);

        // Activar el evento de arrastre en el bloque clonado
        newBlockGO.GetComponent<BlockBehaviour>().OnBeginDrag(eventData);

        BlockBehaviour blockBehaviour = newBlockGO.GetComponent<BlockBehaviour>();
        if (blockBehaviour != null)
        {
            // 🔹 Llamar a OnBeginDrag()
            blockBehaviour.OnBeginDrag(eventData);

            // 🔹 Llamar a OnPickBlockView() correctamente
            blockBehaviour.OnPickBlockView();
        }
        else
        {
            Debug.LogError("El bloque clonado no tiene el componente BlockBehaviour.");
        }
        // Asegurar que el bloque clonado sea el que se arrastra
        eventData.pointerDrag = newBlockGO;
   
    }

    private GameObject CloneBlockView(GameObject originalBlock, Vector2 position, Transform workspaceTransform)
    {
        GameObject clonedBlock = Instantiate(originalBlock, workspaceTransform); // Lo instanciamos dentro del RightPanel
        clonedBlock.transform.localPosition = position;
        clonedBlock.GetComponent<BlockBehaviour>().SetDraggable(true);
        return clonedBlock;
    }

}