/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Configuraciones visuales para los bloques en el editor de Blockly.
 */

using System;
using Unity.AppUI.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockViewSettings", menuName = "Block View Settings", order = 1)]
public class BlockViewSettings : ScriptableObject
{
    private static BlockViewSettings m_instance;

   //TODO: Revisar las configuraciones ya que son provisionales
    public static BlockViewSettings Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = Resources.Load<BlockViewSettings>("BlockViewSettings"); 

                if (m_instance == null)
                {
                    Debug.LogError("BlockViewSettings asset not found in Resources/Settings folder! Please create one via Assets > Create > UBlockly > Block View Settings and place it in a Resources/Settings folder.");
                   
                    return null; 
                }
            }
            return m_instance;
        }
    }

    // CONFIGURACIONES VISUALES (NO ESTÁTICAS) 
 
    [Header("Layout & Spacing")]
    [Tooltip("Spacing between elements within a line (fields, inputs) and between lines (statement inputs). X=Horizontal, Y=Vertical.")]
    public Vector2 ContentSpace = new Vector2(5f, 5f);

    [Tooltip("Espaciado horizontal entre campos/elementos en la misma línea.")]
    public float HorizontalElementSpacing = 5f;

    [Tooltip("Espaciado vertical entre LineGroups (líneas visuales).")]
    public float VerticalLineSpacing = 5f;

    [Tooltip("Minimum size for any block or visual element unit.")]
    public Vector2 MinUnitSize = new Vector2(20f, 20f);

    [Tooltip("Horizontal and vertical padding inside the block borders.")]
    public Vector2 InternalPadding = new Vector2(8f, 4f); // X = padding izq/der, Y = padding arr/abj

    [Header("Connection Shapes")]
    [Tooltip("Width of the connection notch/tab.")]
    public float NotchWidth = 260f;

    [Tooltip("Height of the connection notch/tab.")]
    public float NotchHeight = 16f;

    [Tooltip("Defines the rectangle for the NextStatement connection point relative to its parent block's layout origin (often top-left). Used for positioning.")]
    public Rect StatementConnectPointRect = new Rect(15f, 0f, 20f, 5f); 

    [Header("Connection Interaction")]
    [Tooltip("Visual size for hit detection of connections.")]
    public Vector2 ConnectionSize = new Vector2(12f, 12f);

    [Header("Rendering & Prefabs")]
    [Tooltip("Prefab used to visually highlight a potential connection.")]
    public GameObject PrefabConnectHighlight;

    [Header("Connection Interaction")] 
    [Tooltip("Offset to bump blocks away upon disconnection.")]
    public Vector2 BumpAwayOffset = new Vector2(10f, 10f);

    [Tooltip("Radius for the rounded corners when drawing blocks.")]
    public float BlockCornerRadius = 5f;

    [Tooltip("Max distance in Workspace logical units for a valid snap after a connection is deemed compatible.")]
    [SerializeField] public float ConnectionSnapDistance = 40f;

    [Tooltip("Maximum distance (in workspace units/pixels) to search for a compatible connection when dragging a block.")]
    public float ConnectionSearchRange = 50f; 

    public float NotchConnectorOffsetY = 0f; // Offset vertical para el conector de la muesca
    public float BlockStartX = 46f; // Offset horizontal para el conector de la muesca
    private void OnEnable()
    {
   
    }

    [Tooltip("Defines the position and size rectangle for value Input/Output connection points (Notch/Tab), relative to their attachment point.")]
    public Rect ValueConnectPointRect = new Rect(0f, -9f, 10f, 18f); 
                                                                     
    [Tooltip("Basic block height, without margin.")]
    [SerializeField] public int BlockHeight = 60;
    [SerializeField] public int MinBlockContentSize = 40;
    [SerializeField] public RectOffset ContentMargin;
    [SerializeField] public int ColorFieldWidth;
    [Tooltip("Whether block pattern views in toolbox have masks and are unable to receive input.")]
    [SerializeField] public bool MaskedInToolbox = true;
    [Header("Field Specific Sizes")]
    [Tooltip("Tamaño visual (ancho, alto) para los campos de selección de color.")]
    [SerializeField] public Vector2 FieldColorSize = new Vector2(25f, 25f); 
    [Header("Input Slot Appearance")] 
    [Tooltip("Background color for empty input slots.")]
    [SerializeField] public Color InputSlotColor = new Color(0.85f, 0.85f, 0.85f, 1f); 
    [Tooltip("Minimum visual size (Width, Height) for an empty Value Input slot.")]
    [SerializeField] public Vector2 InputValueSlotSize = new Vector2(30f, 22f); 

    [Tooltip("Minimum visual size (Width, Height) for an empty Statement Input slot.")]
    [SerializeField] public Vector2 InputStatementSlotSize = new Vector2(40f, 24f);

    [Header("Field Padding")] 
    [Tooltip("Horizontal padding applied around individual fields like labels, inputs, images.")]
    [SerializeField] public float FieldHorizontalPadding = 2f; // Padding izq/der para campos

    [Tooltip("Vertical padding applied around individual fields like labels, inputs, images.")]
    [SerializeField] public float FieldVerticalPadding = 1f; // Padding arr/abj para campos

    [Tooltip("Minimum visual width for any UI element within a block (fields, slots).")]
    [SerializeField] public float MinUnitWidth = 20f;

    [Tooltip("Minimum visual height for any UI element within a block (fields, slots).")]
    [SerializeField] public float MinUnitHeight = 20f;

    [Header("Field Defaults")]
    [Tooltip("Default text color for fields and labels.")]
    [SerializeField] public Color DefaultFieldColor = Color.black; 

    [Tooltip("Default font size for text fields and labels.")]
    [SerializeField] public int DefaultFontSize = 36; 

    [Tooltip("Default color for text INSIDE editable input fields.")]
    [SerializeField] public Color EditableFieldColor = new Color(0.1f, 0.1f, 0.1f, 1f); 

    [Tooltip("Background color for text input fields.")]
    [SerializeField] public Color InputFieldBackground = Color.white; 

    [Tooltip("Default preferred width for text input fields. Can be overridden by content.")]
    [SerializeField] public float DefaultInputFieldWidth = 50f; 

    [Tooltip("Default preferred height for text input fields.")]
    [SerializeField] public float DefaultInputFieldHeight = 22f; 

    [Tooltip("Ancho visual asignado para la flecha/botón del desplegable en campos de variable.")]
    [SerializeField] public float DropdownArrowWidth = 18f;

    [Tooltip("Height of the connection tab (when block connected below). Often same as NotchHeight.")]
    public float TabHeight = 0f; //define la altura visual de la pestaña (el saliente) de la conexión NextStatement cuando hay un bloque conectado debajo

    [Tooltip("Horizontal indentation (from left edge) for the previous/next connection point.")]
    public float ConnectorIndentX = 15f; //Define cuánto se desplaza horizontalmente (desde el borde izquierdo del bloque) el inicio de la muesca o la pestaña de las conexiones Previous/NextStatement.

    [Header("Layout & Spacing")] 
    [Tooltip("Minimum width for any block, regardless of content size.")]
    public float MinBlockWidth = 60f;// Define el ancho mínimo absoluto que puede tener un bloque, sin importar cuán pequeño sea su contenido. Esto evita que los bloques se vean demasiado estrechos.
   
    
    public float ContentHeight
    {
        get { return BlockHeight; }
    }

    public static BlockViewSettings Get()
    {
        if (m_instance == null)
            m_instance = Resources.Load<BlockViewSettings>("BlockViewSettings");
        if (m_instance == null)
            throw new Exception("There is no \"BlockViewSettings\" ScriptObject under Resources folder");

        return m_instance;
    }

    public static void Dispose()
    {
        m_instance = null;
    }
}