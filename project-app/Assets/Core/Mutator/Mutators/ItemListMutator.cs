/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 02/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using System.Xml;



    [MutatorClass(MutatorId = "text_join_mutator;lists_create_with_item_mutator")]
    public class ItemListMutator : Mutator
    {
        private const string EMPTY_NAME = "EMPTY";
        private const string ADD_INPUT_PREFIX = "ADD";
        
        private int mItemCount = 2;
        public int ItemCount { get { return mItemCount; } }

        private string mLabelText;

        public override bool NeedEditor
        {
            get { return true; }
        }
        
        public void Mutate(int itemCount)
        {
            if (mItemCount == itemCount)
                return;
            
            mItemCount = itemCount;
            if (mBlock != null)
                UpdateInternal(mBlock);
        }
        
        public override XmlElement ToXml()
        {
            XmlElement xmlElement = XmlUtil.CreateDom("mutation");
            xmlElement.SetAttribute("items", mItemCount.ToString());
            return xmlElement;
        }

        public override void FromXml(XmlElement xmlElement)
        {
             BlockModel sourceBlock = mBlock;
             mItemCount = int.Parse(xmlElement.GetAttribute("items"));
            UpdateInternal(mBlock);
        }

        protected override void OnAttached()
        {
            InputModel defaultInput = mBlock.InputList[0];
            defaultInput.SetName(EMPTY_NAME);
            FieldLabelModel field = defaultInput.FieldRow[0] as FieldLabelModel;
            mLabelText = field.GetText();
            UpdateInternal(mBlock);
        }

        private void UpdateInternal(BlockModel sourceBlock)
        {
            // currently reserve the dummy input, it will only show the Label Field on UI
            InputModel emptyInput = mBlock.GetInput(EMPTY_NAME);
            if (mItemCount > 0 && emptyInput != null)
            {
                mBlock.RemoveInput(emptyInput);
            }
            else if (mItemCount == 0 && emptyInput == null)
            {
                emptyInput = InputFactory.Create(EConnection.DummyInput, EMPTY_NAME, EAlign.Right, null, sourceBlock);
                emptyInput.AppendField(new FieldLabelModel(null, mLabelText));
                mBlock.AppendInput(emptyInput);
            }

            //add new inputs
            int i = 0;
            for (i = 0; i < mItemCount; i++)
            {
                InputModel addInput = mBlock.GetInput("ADD" + i);
                if (addInput == null)
                {
                    addInput = InputFactory.Create(EConnection.InputValue, ADD_INPUT_PREFIX + i, EAlign.Right, null, sourceBlock);
                    mBlock.AppendInput(addInput);
                }
                if (i == 0)
                {
                    if (mBlock.GetField("Title") == null)
                        addInput.AppendField(new FieldLabelModel("Title", mLabelText));
                }
            }

            // remove deleted inputs
            while (true)
            {
                InputModel addInput = mBlock.GetInput("ADD" + i);
                if (addInput == null)
                    break;

                mBlock.RemoveInput(addInput);
                i++;
            }
        }
    }

