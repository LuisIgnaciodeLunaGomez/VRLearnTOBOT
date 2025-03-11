/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 24/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using LiteDB;
using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class BlockConnection
{

    public BlockConnection TargetConnection; //Conexión a la que está conectado el bloque
    private Block m_SourceBlock; //Bloque al que pertenece la conexión

    public List<string> Check { get; protected set; } //Lista de tipos de bloques que se pueden conectar

    public BlockConnection() { }
    public ConnectionType Type { get; private set; }
    private UpdateState State { get; set; }

    public bool IsConnected
    {
        get { return TargetConnection != null; }
    }

    public Block TargetBlock
    {
        get
        {
            if (this.IsConnected)
            {
                return this.TargetConnection.m_SourceBlock;
            }
            return null;
        }
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        var otherConnection = TargetConnection;
        if (otherConnection.TargetConnection != this)
        {
            Debug.LogWarning("Target connection not connected to source connection.");
            return;
        }

        TargetConnection = null;
        otherConnection.TargetConnection = null;
    }

    public bool CheckType(BlockConnection otherConnection)
    {
        if (this.Check == null || otherConnection.Check == null)
            return true;

        foreach (var i in this.Check)
        {
            if (otherConnection.Check.Contains(i))
                return true;
        }
        return false;
    }

    public void Connect(BlockConnection otherConnection)
    {
        if (this.TargetConnection == otherConnection)
        {
            return;
        }

        this.CheckConnection(otherConnection);
        if (this.IsSuperior())
            this.ConnectInternal(otherConnection);
        else
            otherConnection.ConnectInternal(this);
    }

    public void CheckConnection(BlockConnection otherConnection)
    {
        if (otherConnection == null)
            throw new Exception("Target connection is null.");

        if (this.m_SourceBlock == otherConnection.m_SourceBlock)
            throw new Exception("Attempted to connect a block to itself.");

        if (!this.CheckType(otherConnection))
            throw new Exception("Block types are incompatible.");
    }

    private void ConnectInternal(BlockConnection childConnection)
    {
        var parentConnection = this;
        var childBlock = childConnection.m_SourceBlock;

        // Asegurar que el bloque hijo se coloque correctamente
        childBlock.XY = parentConnection.m_SourceBlock.XY + new Vector2(0, -30);

        ConnectReciprocally(parentConnection, childConnection);
    }

    public bool IsSuperior()

    {
        return false;
    }

    public void Dispose()
    {

        if (IsConnected)
        {
            Disconnect();
        }
    }

    public void FireUpdate(UpdateState State)
    {
        Debug.Log($"BlockConnection updated: {State}");
    }


    public void ConnectReciprocally(BlockConnection first, BlockConnection second)
    {
        if (first == null || second == null)
            throw new Exception("Cannot connect null connections.");
        first.TargetConnection = second;
        second.TargetConnection = first;
    }
}

