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
using System.Reflection;
using System;
using UnityEngine;
public static class FieldFactory
{
    private static Dictionary<string, Func<JObject, FieldModel>> m_FieldRegistry = null;

    static FieldFactory()
    {
        InitializeFactory();
    }

    private static void InitializeFactory()
    {
        if (m_FieldRegistry != null) return; 

        m_FieldRegistry = new Dictionary<string, Func<JObject, FieldModel>>();

        
        // Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try 
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        var attribute = method.GetCustomAttribute<FieldCreatorAttribute>();
                        if (attribute != null)
                        {
                            if (string.IsNullOrEmpty(attribute.FieldType))
                            {
                                Debug.LogWarning($"FieldFactory: Factory method {type.Name}.{method.Name} has null or empty FieldType in Attribute.");
                                continue;
                            }
                            ParameterInfo[] parameters = method.GetParameters();
                            if (method.ReturnType == typeof(FieldModel) || method.ReturnType.IsSubclassOf(typeof(FieldModel)) &&
                                parameters.Length == 1 && parameters[0].ParameterType == typeof(JObject))
                            {
                                // Crear un delegate para invocar el método fábrica
                                Func<JObject, FieldModel> factoryDelegate = (json) => (FieldModel)method.Invoke(null, new object[] { json });

                                // Registrar el delegate en el diccionario
                                if (m_FieldRegistry.ContainsKey(attribute.FieldType))
                                {
                                    Debug.LogWarning($"FieldFactory: Duplicate FieldType '{attribute.FieldType}' found for method {type.Name}.{method.Name}. Overwriting.");
                                }
                                m_FieldRegistry[attribute.FieldType] = factoryDelegate;
                                // Debug.Log($"FieldFactory: Registered FieldType '{attribute.FieldType}' for {type.Name}.{method.Name}");
                            }
                            else
                            {
                                Debug.LogError($"FieldFactory: Method {type.Name}.{method.Name} marked with [FieldCreator] has invalid signature. Expected: static FieldModel MethodName(JObject json)");
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Loguear si hay problemas cargando tipos de un ensamblado
                Debug.LogWarning($"FieldFactory: Could not load types from assembly {assembly.FullName}. Errors: {string.Join(", ", Array.ConvertAll(ex.LoaderExceptions, e => e.Message))}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"FieldFactory: Error scanning assembly {assembly.FullName}. Error: {ex.Message}");
            }
        }
        Debug.Log($"<color=cyan>FieldFactory: Initialization complete. Registered {m_FieldRegistry.Count} field types.</color>");
    }

    public static FieldModel CreateFromJson(string fieldType, JObject json)
    {
        if (m_FieldRegistry == null)
        {
            Debug.LogError("FieldFactory is not initialized! Call InitializeFactory() or ensure static constructor runs.");
            InitializeFactory(); 
        }

        if (string.IsNullOrEmpty(fieldType))
        {
            Debug.LogError("FieldFactory.CreateFromJson: fieldType cannot be null or empty.");
            return null;
        }
        if (json == null)
        {
            Debug.LogError($"FieldFactory.CreateFromJson: json definition cannot be null for fieldType '{fieldType}'.");
            return null;
        }

        if (m_FieldRegistry.TryGetValue(fieldType, out var factoryDelegate))
        {
            try
            {
              
                return factoryDelegate(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FieldFactory: Error creating field of type '{fieldType}' using its factory method. Exception: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        else
        {
            Debug.LogError($"FieldFactory: No factory method registered for FieldType '{fieldType}'.");
            return null;
        }
    }

  
}//Fin clase FieldFactory