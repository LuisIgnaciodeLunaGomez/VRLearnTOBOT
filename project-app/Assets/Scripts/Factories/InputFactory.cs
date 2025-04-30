/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 */

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.MemoryProfiler;
using UnityEngine;
public static class InputFactory
{
    public static InputModel CreateFromJson(JObject json)
    {
        string inputType = json["type"].ToString();
        EConnection inputTypeInt = EConnection.InputValue;
        string inputName = json["name"] != null ? json["name"].ToString() : "";
        ConnectionModel connection = null;
        switch (inputType)
        {
            case "input_value":
                inputTypeInt = EConnection.InputValue;
                connection = new ConnectionModel(inputTypeInt);
                break;
            case "input_statement":
                inputTypeInt = EConnection.NextStatement;
                connection = new ConnectionModel(inputTypeInt);
                break;
            case "input_dummy":
                inputTypeInt = EConnection.DummyInput;
                break;
        }

        InputModel input = new InputModel(inputTypeInt, inputName, connection);
        if (json["align"] != null)
        {
            string alignText = json["align"].ToString();
            EAlign align = alignText.Equals("LEFT")
                ? EAlign.Left
                : (alignText.Equals("RIGHT") ? EAlign.Right : EAlign.Center);
            input.SetAlign(align);
        }
        if (json["check"] != null)
        {
            JArray checkArray = json["check"] as JArray;
            if (checkArray != null)
            {
                List<string> checkList = checkArray.Select(token => token.ToString()).ToList();
                input.SetCheck(checkList);
            }
            else
            {
                input.SetCheck(json["check"].ToString());
            }
        }

        //Debug.Log($"InputFactory: Created InputModel '{inputName}' (Type:{inputType}). Has Connection: {(input.Connection != null)}", null); 

        return input;
    }
    
   
    public static InputModel Create(EConnection type, string name, EAlign align, List<string> check)
    {
        ConnectionModel connection = null;
        if (type == EConnection.InputValue || type == EConnection.NextStatement)
            connection = new ConnectionModel(type);

        InputModel input = new InputModel(type, name, connection);
        input.SetAlign(align);
        input.SetCheck(check);
        return input;
    }
} //Fin InputFactory