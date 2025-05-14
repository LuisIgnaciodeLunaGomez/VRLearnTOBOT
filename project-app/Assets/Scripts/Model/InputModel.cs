/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class InputModel
{
    public string Name { get; private set; }
    public readonly EConnection Type;
    public readonly ConnectionModel Connection;
    public readonly List<FieldModel> FieldRow;
   // private Align mAlign = Align.Left;
    private BlockModel mSourceBlock;

    public BlockModel SourceBlock
    {
        get { return mSourceBlock; }
        set
        {
            if (mSourceBlock == value)
            {
                //   Debug.Log($"[InputModel.SourceBlock Setter '{this.Name}'] Skip: Same value ({value?.ID ?? "NULL"})");
                return;
            }
            if (mSourceBlock != null && value != null && mSourceBlock != value)
            {
                Debug.LogError($"[InputModel.SourceBlock Setter '{this.Name}'] Input is already a member of block {mSourceBlock.ID}. Attempting to reassign to {value.ID}.");
               
            }

            // Debug.Log($"[InputModel.SourceBlock Setter '{this.Name}'] Setting SourceBlock from '{mSourceBlock?.ID ?? "NULL"}' to '{value?.ID ?? "NULL"}'");
            mSourceBlock = value;

            if (this.Connection != null)
            {
                // Debug.Log($"  -> Propagating SourceBlock '{mSourceBlock?.ID ?? "NULL"}' to internal Connection...");
                this.Connection.SourceBlock = mSourceBlock;
            }
        }
    }
    public BlockModel ConnectedBlock
    {
        get { return Connection != null ? Connection.TargetBlock : null; }
    }
    public InputModel(EConnection type, string name, BlockModel block, ConnectionModel connection = null)
    {
        if (type != EConnection.DummyInput && string.IsNullOrEmpty(name))
        {
            throw new Exception("Value inputs and statement inputs must have non-empty name.");
        }
        Type = type;
        Name = name;

        mSourceBlock = block;
        Connection = connection;

        FieldRow = new List<FieldModel>();

        Align = EAlign.Left;

        if (type == EConnection.InputValue || type == EConnection.NextStatement)
        {
           
            this.Connection = new ConnectionModel(mSourceBlock, type);

            this.Connection.Input = this; 

           this.SetAlign(Align); 
        }
        else
        {
            this.Connection = null; 
        }

       // Debug.Log($"[InputModel Ctor] Created Input: '{this.Name}', Type: {this.Type}. Connection Created? {this.Connection != null}. Connection.Input set? {this.Connection?.Input != null}");
    }

    private InputModel(EConnection type, string name, ConnectionModel connection = null) : this(type, name, null, connection)
    {
    }

    private InputModel(EConnection type, string name) 
    {
        this.Type = type;
        this.Name = name; 
        this.FieldRow = new List<FieldModel>();

      
        if (type != EConnection.None)
        {
            EConnection connectionType = (type == EConnection.NextStatement) ? EConnection.PrevStatement : EConnection.InputValue;
            this.Connection = new ConnectionModel(mSourceBlock,connectionType);
            this.Connection.Input = this; 
        }
    }

    public void SetName(string newName)
    {
        if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(newName))
            Name = newName;
    }

  
    public EAlign Align { get; private set; }

  
    public InputModel SetAlign(EAlign align)
    {
        if (Align != align)
        {
            Align = align;
        }
        return this;
    }


    public InputModel AppendField(FieldModel field)
    {
        this.InsertFieldAt(FieldRow.Count, field);
        return this;
    }

   
    public InputModel AppendField(string field, string optName = null)
    {
        this.InsertFieldAt(FieldRow.Count, field, optName);
        return this;
    }


    public int InsertFieldAt(int index, string field, string optName = null)
    {
        FieldLabelModel fieldLabel = string.IsNullOrEmpty(field) ? null : new FieldLabelModel(optName, field);
        return InsertFieldAt(index, fieldLabel);
    }

   
    public int InsertFieldAt(int index, FieldModel field)
    {
        if (index < 0 || index > FieldRow.Count)
            throw new Exception("index " + index + " out of bounds.");

        field.SetSourceBlock(this.mSourceBlock);
        if (field.PrefixField != null)
        {
            index = this.InsertFieldAt(index, field.PrefixField);
        }

        this.FieldRow.Insert(index, field);
        ++index;

        if (field.SuffixField != null)
        {
            index = this.InsertFieldAt(index, field.SuffixField);
        }
        return index;
    }

   
    public void RemoveField(string fieldName)
    {
        foreach (FieldModel field in FieldRow)
        {
            if (field.Name.Equals(fieldName))
            {
                field.Dispose();
                FieldRow.Remove(field);
            }
        }
    }


    public void SetCheck(string check)
    {
        if (string.IsNullOrEmpty(check))
            return;
        if (this.Connection == null)
            throw new Exception("This input does not have a connection.");
        this.Connection.SetCheck(new List<string>() { check });
    }

 
    public void SetCheck(List<string> check)
    {
        if (check == null || check.Count == 0)
            return;
        if (Connection == null)
            throw new Exception("This input does not have a connection.");
        Connection.SetCheck(check);
    }

    public void Dispose()
    {
        foreach (var field in FieldRow)
        {
            field.Dispose();
        }

        if (Connection != null)
        {
            Connection.Disconnect();
            Connection.Dispose();
        }

        mSourceBlock = null;
    }

    public void SetSourceBlock(BlockModel block)
    {
        SourceBlock = block;
        if (Connection != null)
        {
            Connection.SourceBlock = block;
        }
        foreach (var field in FieldRow)
        {
            if (field != null) 
            {
                field.SetSourceBlock(block); 
            }
        }
    }

    public void AppendField(FieldModel field, int index = -1)
    {
        if (field == null) return;

        field.SetSourceBlock(this.mSourceBlock);

        if (index >= 0 && index < FieldRow.Count)
        {
            FieldRow.Insert(index, field);
        }
        else
        {
            FieldRow.Add(field);
        }

       
        // mSourceBlock?.FireUpdate(1 << (int)UpdateStates.Fields); 
    }
}//fin clase InputModel

