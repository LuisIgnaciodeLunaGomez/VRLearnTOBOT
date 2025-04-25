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
 * Descripción:
 */

using System.Collections.Generic;

using System;
using System.Reflection;
public static class MutatorFactory
{
    private static Dictionary<string, Type> mMutatorDict = null;
   
    public static Mutator Create(string mutatorId)
    {
        if (mMutatorDict == null)
        {
            mMutatorDict = new Dictionary<string, Type>();
            Assembly assem = Assembly.GetAssembly(typeof(FieldModel));
            foreach (Type type in assem.GetTypes())
            {
                if (type.IsSubclassOf(typeof(Mutator)))
                {
                    var attrs = type.GetCustomAttributes(typeof(MutatorClassAttribute), false);
                    if (attrs.Length > 0)
                    {
                        string mutatorIdStr = ((MutatorClassAttribute)attrs[0]).MutatorId;
                        string[] strs = mutatorIdStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < strs.Length; i++)
                        {
                            mMutatorDict[strs[i]] = type;
                        }
                    }
                }
            }
        }

        Type mutatorType;
        if (!mMutatorDict.TryGetValue(mutatorId, out mutatorType))
            throw new Exception(string.Format(
                "There is no class implementation defined for mutator id: \"{0}\", or you might forget to add a \"MutatorClassAttribute\" to the class.",
                mutatorId));
        Mutator mutator = Activator.CreateInstance(mutatorType) as Mutator;
        mutator.MutatorId = mutatorId;
        return mutator;
    }


}//Fin clase MutatorFactory