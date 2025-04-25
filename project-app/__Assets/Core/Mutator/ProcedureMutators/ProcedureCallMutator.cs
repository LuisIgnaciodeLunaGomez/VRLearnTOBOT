/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 04/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 * 
 */

using System;
using System.Collections.Generic;
using System.Xml;


[MutatorClass(MutatorId = "procedures_callnoreturn_mutator;procedures_callreturn_mutator")]
public class ProcedureCallMutator : ProcedureMutator
{
    public override bool NeedEditor
    {
        get { return false; }
    }

    /// <summary>
    /// This retrieves the block's Input that represents the nth argument.
    /// </summary>
    /// <param name="index">The index of the argument asked for.</param>
    public InputModel GetArgumenInput(int index)
    {
        return index <= mBlock.InputList.Count ? mBlock.InputList[index + 1] : null;
    }

    protected override void SetProcedureNameInternal(string name)
    {
        mProcedure = mProcedure.CloneWithName(name);
        mBlock.GetField(ProcedureDB.PROCEDURE_NAME_FIELD).SetText(name);
    }

    protected override void UpdateInternal()
    {
        base.UpdateInternal();
        if (mProcedure != null)
        {
            mBlock.GetField(ProcedureDB.PROCEDURE_NAME_FIELD).SetText(mProcedure.Name);
        }
    }

    /// <summary>
    /// A new set of Inputs reflecting the current Procedure state.
    /// </summary>
    protected override List<InputModel> BuildUpdatedInputs()
    {
        List<string> args = mProcedure.Arguments;
        int argCount = args.Count;
        List<InputModel> inputs = new List<InputModel>();

        // Procedure name
        inputs.Add(mBlock.InputList[0]);

        // Argument inputs
        for (int i = 0; i < argCount; ++i)
        {
            InputModel stackInput = InputFactory.Create(EConnection.InputValue, "ARG" + i, EAlign.Right, null);

            // add "with: " label
            if (i == 0)
            {
                FieldLabelModel withLabel = new FieldLabelModel("WITH", I18n.Get(MsgDefine.PROCEDURES_CALL_BEFORE_PARAMS));
                stackInput.AppendField(withLabel);
            }

            // add argument's label
            FieldLabelModel label = new FieldLabelModel(null, args[i]);
            stackInput.AppendField(label);

            inputs.Add(stackInput);
        }
        return inputs;
    }

    protected override XmlElement SerializeProcedure(Procedure info)
    {
        return Procedure.Serialize(info, false);
    }

    protected override Procedure DeserializeProcedure(XmlElement xmlElement)
    {
        Procedure info = Procedure.Deserialize(xmlElement);
        if (string.IsNullOrEmpty(info.Name))
            throw new Exception("No procedure name specified in mutation for " + mBlock.ToDevString());
        return info;
    }
}
