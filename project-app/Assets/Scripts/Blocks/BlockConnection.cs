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
 * Versión: 1.0.1
 * 
 * Descripción: Clase que gestiona las conexiones entre bloques
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockConnection
{

    public BlockConnection targetConnection; //Conexión a la que está conectado el bloque
    private BlockBehaviour m_SourceBlock; //Bloque al que pertenece la conexión

    public List<string> Check { get; protected set; } //Lista de tipos de bloques que se pueden conectar
    private UpdateState m_state { get; set; }

    public EConnection type { get; private set; }

    public Vector2 position { get; set; }

    public delegate void ConnectionStateHandler(UpdateState State);

    public event ConnectionStateHandler onStateChanged;

    public bool isConnected => this.targetConnection != null;

    public BlockBehaviour sourceBlock  {get => m_SourceBlock; set => m_SourceBlock = value;}

    public BlockBehaviour TargetBlock
    {
        get
        {
            if (this.isConnected)
            {
                return this.targetConnection.m_SourceBlock;
            }
            return null;
        }
    }

    public bool IsSuperior =>  this.type == EConnection.NextStatement; //this.type == EConnection.OutputValue ||

    public BlockConnection(BlockBehaviour sourceBlock, EConnection type)
    {
        this.m_SourceBlock = sourceBlock;
        this.type = type;
        this.Check = new List<string>();
        this.position = Vector2.zero;
        Debug.Log($"BlockConnection: Constructor: BlockConnection creada para tipo {type}, SourceBlock: {sourceBlock?.gameObject.name}");
    }

    public void Connect(BlockConnection otherConnection)
    {

        if (this.targetConnection == otherConnection) return;

        this.CheckConnection(otherConnection);

        if (this.IsSuperior) this.ConnectInternal(otherConnection);
        else otherConnection.ConnectInternal(this);
    }

    public void Disconnect()
    {
        if(!this.isConnected) return;

        var otherConnection = targetConnection;
        if(otherConnection.targetConnection != this)
        {
            Debug.LogWarning("Error en la desconexión de bloques");
            return;
        }

        this.targetConnection = null;   
        otherConnection.targetConnection = null;
        this.onStateChanged?.Invoke(UpdateState.Disconnected);

        if (m_SourceBlock != null && otherConnection.m_SourceBlock != null)
        {
            if (this.type == EConnection.NextStatement)
            {
                this.m_SourceBlock.blockModel.SetParent(null);
            }
            else if (type == EConnection.PrevStatement)
            {
                otherConnection.m_SourceBlock.blockModel.SetParent(null);
            }
        }
    }

    public bool CheckType(BlockConnection otherConnection)
    {
       if(this.Check==null || otherConnection.Check == null) return true;

        Debug.Log($"CheckType: BlockConnection:  this.Check={string.Join(",", this.Check)}, other.Check={string.Join(",", otherConnection.Check)}");

        foreach (var i in this.Check)
        {
            if(otherConnection.Check.Contains(i)) return true;
        }

        return false;
    }

    public void CheckConnection(BlockConnection otherConnection) 
    {

        if (otherConnection == null) throw new Exception("CheckConnection: BlockConnection: La conexión destino es nula");

        if(this.m_SourceBlock == otherConnection.m_SourceBlock) throw new Exception("CheckConnection: BlockConnection:No se puede conectar un bloque consigo mismo");

        if(!this.CheckType(otherConnection)) throw new Exception("CheckConnection: BlockConnection:No se pueden conectar los bloques ya que son incompatibles");

    }


    public void ConnectInternal(BlockConnection otherConnection) 
    {
        var parentConnection = this;
        var childBlock = otherConnection.m_SourceBlock;

        // Ajustar posición para encajar verticalmente
       float blockHeight = childBlock.GetComponent<RectTransform>().rect.height;
       childBlock.transform.localPosition = parentConnection.m_SourceBlock.transform.localPosition + new Vector3(0, -blockHeight,0);

        this.ConnectReciprocally(parentConnection, otherConnection);
        this.onStateChanged?.Invoke(UpdateState.Connected);

        if (type == EConnection.NextStatement)
        {
            childBlock.blockModel.SetParent(parentConnection.m_SourceBlock.blockModel);
        }
    }

    public void ConnectReciprocally(BlockConnection firstConnection, BlockConnection secondConnection)
    {
        if(firstConnection == null || secondConnection ==null ) 
        {

            Debug.LogWarning("No se pueden llevar a cabo conexiones nulas.");
            return;
        }
        firstConnection.targetConnection = secondConnection;
        secondConnection.targetConnection = firstConnection;
    }

   
    public void Dispose()
    {
        if (this.isConnected) Disconnect();
    }

    public void FireUpdate(UpdateState state)
    {
        
        onStateChanged?.Invoke(state);
    }   

    public float DistanceTo(BlockConnection otherConnection)
    {
        if (otherConnection == null) return float.MaxValue;
        return Vector2.Distance(this.position, otherConnection.position);
    }


    public bool CanConnect(BlockConnection otherConnection)
    {

        if(otherConnection == null) return false;
        Debug.Log($"CanConnect: BlockConnection: this.type={this.type}, other.type={otherConnection.type}, CheckType={CheckType(otherConnection)}");
        if (this.type == EConnection.NextStatement && otherConnection.type == EConnection.PrevStatement) return true;
        if (this.type == EConnection.PrevStatement && otherConnection.type == EConnection.NextStatement) return true;
         if (this.type == EConnection.InputValue && otherConnection.type == EConnection.OutputValue) return true;
         if (this.type == EConnection.OutputValue && otherConnection.type == EConnection.InputValue) return true;

        //return CheckType(otherConnection);

        return false;
    }

    public void Highlight(bool hightlight)
    {
       onStateChanged.Invoke(hightlight ? UpdateState.Highlight : UpdateState.UnHighlight);
    }
}
