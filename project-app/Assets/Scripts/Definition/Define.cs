/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System;
using System.Collections.Generic;


public class Define
{
    
    public static EConnection OppositeConnection(EConnection connectionType)
    {
        switch (connectionType)
        {
            case EConnection.InputValue: return EConnection.OutputValue;
            case EConnection.OutputValue: return EConnection.InputValue;
            case EConnection.NextStatement: return EConnection.PrevStatement;
            case EConnection.PrevStatement: return EConnection.NextStatement;
        }
        return EConnection.None;
    }

    public const string VARIABLE_CATEGORY_NAME = "VARIABLE";
    public const string PROCEDURE_CATEGORY_NAME = "PROCEDURE";
    public const string VARIABLE_GET_BLOCK_TYPE = "variables_get";
    public const string VARIABLE_SET_BLOCK_TYPE = "variables_set";
    public const string DEFINE_NO_RETURN_BLOCK_TYPE = "procedures_defnoreturn";
    public const string DEFINE_WITH_RETURN_BLOCK_TYPE = "procedures_defreturn";
    public const string CALL_NO_RETURN_BLOCK_TYPE = "procedures_callnoreturn";
    public const string CALL_WITH_RETURN_BLOCK_TYPE = "procedures_callreturn";

    public static string[] FIELD_TYPES = new string[]
    {
            "field_label", "field_input", "field_angle", "field_checkbox", "field_colour",
            "field_variable", "field_dropdown", "field_image", "field_number", "field_date"
    };


    public static string[] INPUT_TYPES = new string[]
    {
            "input_value", "input_statement", "input_dummy"
    };

    public enum EDataType
    {
        Undefined = 0,
        Boolean = 1,
        Number = 2,       
        String = 3,
        List = 4
    }

    public static Dictionary<EDataType, string[]> DataTypeDB = new Dictionary<EDataType, string[]>()
        {
            {EDataType.Boolean, new[] {"bool", "boolean"}},
            {EDataType.Number, new[] {"float", "int", "double"}},
            {EDataType.String, new[] {"string"}},
            {EDataType.List, new[] {"ArrayList", "list"}}
        };

  
    public const bool FIELD_VARIABLE_ADD_MANIPULATION_OPTIONS = true;


    public static bool FIELD_ANGLE_CLOCKWISE = true;
    public static int FIELD_ANGLE_OFFSET = 90;
    public static int FIELD_ANGLE_WRAP = 360;
    public static int FIELD_IMAGE_WIDTH_DEFAULT = 30;
    public static int FIELD_IMAGE_HEIGHT_DEFAULT = 30;
    public const string CREATE_VARIABLE_TITLE = "MAKE_VARIABLE";
    public const string CREATE_PROCEDURE_TITLE = "MAKE_PROCEDURE";
    public const string RENAME_VARIABLE_OPTION_VALUE = "RENAME_VARIABLE_ID"; 
    public const string DELETE_VARIABLE_OPTION_VALUE = "DELETE_VARIABLE_ID"; 
    public const string NEW_VARIABLE_OPTION_VALUE = "NEW_VARIABLE_ID";
   
    public const string FIELD_IMAGE_SRC_DEFAULT = "Textures/Icons/fieldimage_default"; 
}//Fin clase Define
