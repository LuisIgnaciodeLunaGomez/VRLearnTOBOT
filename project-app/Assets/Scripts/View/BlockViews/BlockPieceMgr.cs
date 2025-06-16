/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 15/06/2025
 * 
 * Versión: 1.0.2
 * 
 * Descripción: 
 */


using UnityEngine;

public class BlockPieceMgr
{
    private static BlockPieceMgr mInstance;
    public static BlockPieceMgr Get()
    {
        if (mInstance == null)
            mInstance = new BlockPieceMgr();
        return mInstance;
    }

    public GameObject LabelPrefab { get; private set; }
    public GameObject NumberInputPrefab { get; private set; }
    public GameObject DropdownPrefab { get; private set; }
    public GameObject TextInputPrefab { get; private set; }
    public GameObject VariablePrefab { get; private set; }
    public GameObject CheckboxPrefab { get; private set; } 
    public GameObject ImagePrefab { get; private set; }
    public GameObject InputValueSlotPrefab { get; private set; }

    public GameObject InputStatementSlotPrefab { get; private set; }
    private BlockPieceMgr() 
    {
        // Cargamos todos los prefabs de piezas al inicio
        LabelPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_Label_Prefab");
        NumberInputPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_NumberInput_Prefab");
        DropdownPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_Dropdown_Prefab");
        TextInputPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_TextInput_Prefab");
       // VariablePrefab = Resources.Load<GameObject>("Prefabs/BlockElements/Field_Variable_Prefab");
        CheckboxPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_Checkbox_Prefab");
        ImagePrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Field_Image_Prefab");
        InputStatementSlotPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Input_StatementSlot_Prefab");
        InputValueSlotPrefab = Resources.Load<GameObject>("Prefabs/BlocksElements/Input_ValueSlot_Prefab");
        // Comprobamos si se cargaron bien
        if (LabelPrefab == null || NumberInputPrefab == null)
            Debug.LogError("BlockPieceMgr: ¡No se pudieron cargar algunos prefabs de piezas desde la carpeta 'Resources/BlockPieces'!");
    }
}