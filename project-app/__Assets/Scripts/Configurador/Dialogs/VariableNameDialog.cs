

using UnityEngine;
using UnityEngine.UI;


    public class VariableNameDialog : BaseDialog
    {
        [SerializeField] private Text m_InputLabel;
        [SerializeField] private InputField m_Input;

        private bool mIsRename = false;
        
        private string mOldVarName;
        public void Rename(string varName)
        {
            mOldVarName = varName;
            mIsRename = true;
            m_InputLabel.text = I18n.Get(MsgDefine.RENAME_VARIABLE);
        }

        protected override void OnInit()
        {
            m_InputLabel.text = I18n.Get(MsgDefine.NEW_VARIABLE);

        WorkSpaceModel activeWorkspace = WorkSpaceView.Active.Workspace; 
        AddCloseEvent(() =>
            {
                if (mIsRename)
                    activeWorkspace.RenameVariable(mOldVarName, m_Input.text);
                else
                    activeWorkspace.CreateVariable(m_Input.text);
            });
        }
    }

