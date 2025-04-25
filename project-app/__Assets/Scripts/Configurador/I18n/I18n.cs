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
using Newtonsoft.Json.Linq;

public class I18n
{
    public const string EN = "en";
    public const string ES = "sp";

    private static Dictionary<string, string> mMsg = null;

    public static string Get(string key)
    {
        if (mMsg == null)
            BlockResMgr.Get().LoadI18n();

        string value;
        if (mMsg.TryGetValue(key, out value))
            return value;

        throw new Exception("I18n file doesn't contain translation for key: " + key);
    }

    public static void Add(string key, string value)
    {
        if (mMsg == null)
            BlockResMgr.Get().LoadI18n();

        mMsg[key] = value;
    }

    public static bool Contains(string key)
    {
        if (mMsg == null)
            BlockResMgr.Get().LoadI18n();

        return mMsg.ContainsKey(key);
    }

    public static void AddI18nFile(string text)
    {
        if (mMsg == null)
            mMsg = new Dictionary<string, string>();

        JObject jobj = JObject.Parse(text);
        foreach (KeyValuePair<string, JToken> pair in jobj)
        {
            mMsg[pair.Key] = pair.Value.ToString();
        }
    }

    public static void Dispose()
    {
        if (mMsg != null)
            mMsg.Clear();
        mMsg = null;
    }
}
