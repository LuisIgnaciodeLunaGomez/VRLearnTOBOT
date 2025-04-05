
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción:
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;


    [MutatorClass(MutatorId = "procedures_defnoreturn_mutator;procedures_defreturn_mutator")]
    public class ProcedureDefinitionMutator : ProcedureMutator
    {
        public override bool NeedEditor
        {
            get { return true; }
        }
        
        /// Sets the mutator name, including setting the associated name field on the block.
        /// </summary>
        protected override void SetProcedureNameInternal(string name)
        {
            mProcedure = mProcedure.CloneWithName(name);
            FieldLabelModel nameField = GetNameField();
            if (nameField != null)
                nameField.SetValue(name);
        }

        /// <summary>
        /// Called when the mutator is attached to a block. It will make sure the procedure name on the
        /// block's name field is in sync with the mutator's Procedure Info, and register a listener on the name field for future edits.
        /// </summary>
        protected override void OnAttached()
        {
            string procedureName = null;

        // Update the Procedure Info with procedure name from NAME field.
        // In the case of this class, this will not update the mutation serialization, 
        // but initializes the value to synch with caller's Procedure Info.
        FieldLabelModel nameField = GetNameField();
            if (nameField != null)
            {
                string blockProcName = nameField.GetValue();
                string infoProcName = (mProcedure == null) ? null : mProcedure.Name;
                if (!string.IsNullOrEmpty(blockProcName) && !blockProcName.Equals(infoProcName))
                {
                    if (!string.IsNullOrEmpty(infoProcName))
                        throw new Exception("Attached to block that already has a differing procedure name.");
                    procedureName = blockProcName;
                }
                else
                {
                    procedureName = infoProcName;
                }
            }
            mProcedure = mProcedure == null
                        ? new Procedure(procedureName, new List<string>(), ProcedureDB.HAS_STATEMENTS_DEFAULT)
                        : new Procedure(procedureName, mProcedure.Arguments, mProcedure.DefinitionHasStatementBody);
            
            base.OnAttached();

            if (nameField != null)
                nameField.SetValue(procedureName);
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
           FieldLabelModel nameField = this.GetNameField();
            if (mProcedure != null && nameField != null)
            {
                nameField.SetValue(mProcedure.Name);
            }
        }

        protected override List<InputModel> BuildUpdatedInputs()
        {
            List<InputModel> newInputs = new List<InputModel>();
            newInputs.Add(BuildNewHeader());
            if (mProcedure.DefinitionHasStatementBody)
                newInputs.Add(GetStatementsInput());

             // For procedures_defreturn_mutator
            InputModel returnInput = mBlock.GetInput(ProcedureDB.RETURN_INPUT_NAME);
            if (returnInput != null)
                newInputs.Add(returnInput);
            
            return newInputs;
        }
        
        protected override XmlElement SerializeProcedure(Procedure info)
        {
            return Procedure.Serialize(info, true);
        }

        protected override Procedure DeserializeProcedure(XmlElement xmlElement)
        {
            Procedure info = Procedure.Deserialize(xmlElement);
            FieldLabelModel nameField = GetNameField();
            if (string.IsNullOrEmpty(info.Name) && nameField != null)
            {
                info = new Procedure(nameField.GetText(), info.Arguments, info.DefinitionHasStatementBody);
            }
            return info;
        }

        /// <summary>
        /// Constructs the block's header. The new header maintains the same name field instance, 
        /// but updated argument list.
        /// </summary>
        /// <returns>a new header reflecting the latest mutator state.</returns>
        protected InputModel BuildNewHeader()
        {
            InputModel descriptionInput = mBlock.InputList[0];
            List<FieldModel> oldFields = descriptionInput.FieldRow;
            InputModel input = InputFactory.Create(EConnection.DummyInput, null, EAlign.Left, null);
            input.SourceBlock = mBlock;
            input.AppendField(oldFields[0]);
            input.AppendField(oldFields[1]);
            input.AppendField(new FieldLabelModel("PARAMS", GetParametersListDescription()));
            return input;
        }

        /// <summary>
        /// An Input to contain the procedure body statements. 
        /// </summary>
        protected InputModel GetStatementsInput()
        {
            InputModel stackInput = mBlock.GetInput(ProcedureDB.STATEMENT_INPUT_NAME);
            if (stackInput == null)
            {
                stackInput = InputFactory.Create(EConnection.NextStatement, ProcedureDB.STATEMENT_INPUT_NAME,
                                                 EAlign.Left, null);
            }
            return stackInput;
        }

        /// <summary>
        /// A human-readable string describing the procedure's parameters.
        /// </summary>
        protected String GetParametersListDescription()
        {
            if (mProcedure == null)
                return "";

            StringBuilder sb = new StringBuilder();
            List<String> arguments = mProcedure.Arguments;
            if (arguments.Count > 0)
            {
                sb.Append(I18n.Get(MsgDefine.PROCEDURES_BEFORE_PARAMS));
                for (int i = 0; i < arguments.Count; ++i)
                {
                    sb.Append(i == 0 ? " " : ", ");
                    sb.Append(arguments[i]);
                }
            }
            return sb.ToString();
        }

        public FieldLabelModel GetNameField()
        {
            if (mBlock == null) return null;

            FieldLabelModel field = mBlock.GetField(ProcedureDB.PROCEDURE_NAME_FIELD) as FieldLabelModel;
            return field;
        }
    }

