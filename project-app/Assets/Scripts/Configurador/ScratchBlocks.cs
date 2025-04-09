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
using Newtonsoft.Json.Linq;

public class ScratchBlocks
{
 
    public static void Init()
    {
        BlockResMgr.Get().LoadI18n();
       // BlockResMgr.Get().LoadJsonDefinitions();
    }

    
    public static void Dispose()
    {
        BlockFactory.Instance.Clear();
        I18n.Dispose();
    }

}
