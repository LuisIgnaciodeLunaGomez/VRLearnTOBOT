/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/01/2025
 * 
 * Versión: 1.0.0
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



    public class enumerator
{
    public enum UpdateState // enumerator for update state
    {
        Inputs = 0,
        //Fields = 1,
        Connections = 2,
        IsDisabled = 3,
        IsCollapsed = 4,
        IsEditable = 5,
        IsDeletable = 6,
        IsMovable = 7,
        IsInputInline = 8,
        IsShadow = 9,

        //---- max 31 (mask int) ------
    }
}

