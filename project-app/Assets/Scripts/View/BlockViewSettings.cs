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
 * Versión: 2.0.0 (Revisión y Refactorización)
 * 
 * Descripción: Configuraciones visuales centralizadas para todos los bloques y sus elementos.
 */

using System;
using Unity.AppUI.UI;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[CreateAssetMenu(fileName = "BlockViewSettings", menuName = "VRLearn/Block View Settings")]
public class BlockViewSettings : ScriptableObject
{
    private static BlockViewSettings m_instance;
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

                  //  return null;
                }
            }
            return m_instance;
        }
    }
    // =========================================================================================
    // GRUPO 1: PADDING Y MÁRGENES (El "aire" dentro y alrededor de los elementos)
    // =========================================================================================
    [Header("1. Paddings & Margins")]

    [Tooltip("Padding INTERNO del bloque. Espacio entre el borde del bloque y su contenido (LineGroups).")]
    public RectOffset BlockInternalPadding;

    [Tooltip("Padding INTERNO de cada línea (LineGroup). Espacio entre el borde de la línea y su contenido (Inputs).")]
    public RectOffset LineGroupPadding;

    [Tooltip("Padding INTERNO de cada campo de texto (FieldInput). Espacio entre el borde del campo y el texto.")]
    public RectOffset FieldInputTextPadding;

    // =========================================================================================
    // GRUPO 2: ESPACIADO (La distancia ENTRE elementos)
    // =========================================================================================
    [Header("2. Spacing Between Elements")]

    [Tooltip("Espaciado HORIZONTAL entre elementos en la misma línea (ej: 'mover' [aquí] '10' [aquí] 'pasos').")]
    public float HorizontalElementSpacing = 8f;

    [Tooltip("Espaciado VERTICAL entre líneas (LineGroups) dentro de un mismo bloque.")]
    public float VerticalLineSpacing = 5f;

    [Tooltip("Indentación HORIZONTAL para los bloques anidados dentro de un Input de tipo Statement (como un 'repetir').")]
    public float StatementIndent = 20f;

    // =========================================================================================
    // GRUPO 3: TAMAÑOS MÍNIMOS Y POR DEFECTO
    // =========================================================================================
    [Header("3. Minimum & Default Sizes")]

    [Tooltip("Tamaño MÍNIMO que puede tener un bloque, sin importar su contenido.")]
    public Vector2 MinBlockSize = new Vector2(40f, 40f);

    [Tooltip("Ancho MÍNIMO de cualquier elemento individual (Labels, Inputs).")]
    public float MinUnitWidth = 24f;

    [Tooltip("Altura MÍNIMA de cualquier elemento individual (Labels, Inputs).")]
    public float MinUnitHeight = 24f;

    [Tooltip("Tamaño por defecto de los SLOTS de conexión vacíos para Input de VALOR.")]
    public Vector2 InputValueSlotSize = new Vector2(30f, 22f);

    [Tooltip("Tamaño por defecto de los SLOTS de conexión vacíos para Input de STATEMENT.")]
    public Vector2 InputStatementSlotSize = new Vector2(40f, 24f);

    [Tooltip("Ancho por defecto para los campos de texto editables (InputField).")]
    public float DefaultInputFieldWidth = 50f;

    [Tooltip("Altura por defecto para los campos de texto editables (InputField).")]
    public float DefaultInputFieldHeight = 30f;

    [Tooltip("Ancho del icono de la flecha en los campos de tipo Dropdown o Variable.")]
    public float DropdownArrowWidth = 20f;

    [Tooltip("Tamaño por defecto (ancho, alto) para los campos de selección de color.")]
    public Vector2 FieldColorSize = new Vector2(36f, 24f);
    // =========================================================================================
    // GRUPO 4: FORMAS DE CONEXIÓN
    // =========================================================================================
    [Header("4. Connection Shapes & Snapping")]

    [Tooltip("El ANCHO de la muesca (Notch/Tab) para las conexiones de tipo VALOR (triangulares).")]
    public float ValueNotchWidth = 10f;

    [Tooltip("La ALTURA de la muesca (Notch/Tab) para las conexiones de tipo VALOR (triangulares).")]
    public float ValueNotchHeight = 18f;

    [Tooltip("La ALTURA de la pestaña/muesca para las conexiones de tipo STATEMENT (el puzzle).")]
    public float StatementTabHeight = 16f;

    [Tooltip("El ANCHO del 'diente' de la conexión tipo STATEMENT.")]
    public float StatementTabWidth = 30f;

    [Tooltip("A qué distancia del borde izquierdo del bloque se posiciona el 'diente' de la conexión STATEMENT.")]
    public float StatementTabOffsetX = 15f;

    [Tooltip("Radio de las esquinas redondeadas al renderizar el fondo del bloque.")]
    public float BlockCornerRadius = 8f;

    // =========================================================================================
    // GRUPO 5: INTERACCIÓN
    // =========================================================================================
    [Header("5. Interaction")]

    [Tooltip("Rango de búsqueda (en unidades de UI) para encontrar conexiones candidatas al arrastrar.")]
    public float ConnectionSearchRange = 50f;

    [Tooltip("Distancia máxima (en unidades de UI) para que una conexión se 'enganche' (snap) una vez es compatible.")]
    public float ConnectionSnapDistance = 40f;

    [Tooltip("Distancia que 'salta' un bloque al desconectarse.")]
    public Vector2 BumpAwayOffset = new Vector2(10f, 10f);

    // =========================================================================================
    // GRUPO 6: COLORES DE LA INTERFAZ
    // =========================================================================================
    [Header("6. Colors")]

    [Tooltip("Color de fondo para los slots de conexión de input vacíos.")]
    public Color InputSlotColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Un gris claro por defecto

    [Tooltip("Color del texto para campos editables.")]
    public Color EditableFieldColor = Color.black;

    [Tooltip("Color de fondo para los campos de texto editables.")]
    public Color InputFieldBackground = Color.white;
    [Header("7. Fonts & Text")]
    [Tooltip("Tamaño de fuente por defecto para todas las etiquetas y campos de texto.")]
    public float DefaultFontSize = 28f;


    private void OnEnable()
    {
        // Esto sirve como "valores por defecto" si los RectOffset son null.
        // Si ya tienen valores asignados desde el Inspector, este código no los sobrescribirá.
        if (BlockInternalPadding == null || BlockInternalPadding.left == 0 && BlockInternalPadding.right == 0 && BlockInternalPadding.top == 0 && BlockInternalPadding.bottom == 0)
        {
            BlockInternalPadding = new RectOffset(12, 12, 8, 8);
        }

        if (LineGroupPadding == null || LineGroupPadding.left == 0 && LineGroupPadding.right == 0 && LineGroupPadding.top == 0 && LineGroupPadding.bottom == 0)
        {
            LineGroupPadding = new RectOffset(0, 0, 2, 2);
        }

        if (FieldInputTextPadding == null || FieldInputTextPadding.left == 0 && FieldInputTextPadding.right == 0 && FieldInputTextPadding.top == 0 && FieldInputTextPadding.bottom == 0)
        {
            FieldInputTextPadding = new RectOffset(4, 4, 2, 2);
        }
    }


}