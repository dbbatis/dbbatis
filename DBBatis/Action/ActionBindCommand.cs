using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;

namespace DBBatis.Action
{
    /// <summary>
    /// Action绑定后的命令
    /// </summary>
    internal class ActionBindCommand
    {
        public short PageID { get; set; }
        public string ActionName { get; set; }
        public DbCommand CheckActionCommand { get; set; }
        public DbCommand CheckActionStateCommand { get; set; }
        public DbCommand MainActioncmmd { get; set; }
        public ActionSimpleType ResultType { get; set; }
        public DbCommand StateCommand { get; set; }
        /// <summary>
        /// 主命令是否为插入
        /// </summary>
        public bool IsInsert { get; set; }
        public System.Collections.Specialized.StringCollection OutFields { get; set; }
        public Collection<ActionBindCommand> ActionBindCommands { get; set; }

        public Collection<BatchActionBindCommand> BatchActionBindCommands { get; set; }

        string _ErrMessage = string.Empty;
        public string ErrMessage
        {
            get
            {
                if (string.IsNullOrEmpty(_ErrMessage))
                {
                    StringBuilder sb = new StringBuilder();
                    //检查子命令
                    if (ActionBindCommands != null)
                    {
                        foreach(ActionBindCommand cmd in ActionBindCommands)
                        {
                            if (cmd.IsErr)
                            {
                                sb.AppendFormat("Action{0}:{1}", cmd.ActionName, cmd.ErrMessage);
                            }
                        }
                    }
                    if (BatchActionBindCommands != null)
                    {
                        foreach(BatchActionBindCommand cmd in BatchActionBindCommands)
                        {
                            if (cmd.IsErr)
                            {
                                sb.AppendFormat("Action{0}:{1}", cmd.ActionName, cmd.ErrMessage);
                            }
                        }
                    }
                    _ErrMessage = sb.ToString();
                }
                return _ErrMessage;
            }
            set
            {
                _ErrMessage = value;
            }
        }
        public bool IsErr
        {
            get
            {
                return !string.IsNullOrEmpty(ErrMessage);
            }
        }
        private StringDictionary GetOutValues(object data)
        {
            StringDictionary stringDictionary = new StringDictionary();
            if (ResultType== ActionSimpleType.OneValue)
            {
                stringDictionary.Add(OutFields[0], data.ToString());
            }
            else
            {
                DataTable dt = new DataTable();
                if(ResultType== ActionSimpleType.DataTable)
                {
                    dt=(DataTable)data;
                }else if(ResultType== ActionSimpleType.DataSet)
                {
                    dt=((DataSet)data).Tables[0];
                }
                if (dt.Rows.Count > 0)
                {
                    foreach(string key in OutFields)
                    {
                        if (!dt.Columns.Contains(key))
                        {
                            throw new Exception(String.Format("获取OutFields时出错。Action:{0}返回数据没有{1}字段信息"
                                , this.ActionName, key));
                        }
                        stringDictionary.Add(key, dt.Rows[0][key].ToString());
                    }
                }
                else
                {
                    throw new Exception(String.Format("获取OutFields时出错。Action:{0}未能返回有效数据", this.ActionName));
                }
            }
            return stringDictionary;
        }
        /// <summary>
        /// 邦定子命令对应的传出参数值
        /// </summary>
        /// <param name="outkeyvalues"></param>
        private void BindOutKeyValues(StringDictionary outkeyvalues)
        {
            foreach(string key in outkeyvalues.Keys)
            {
                string value = outkeyvalues[key];
                if (ActionBindCommands != null)
                {
                    foreach(ActionBindCommand cmd in ActionBindCommands)
                    {
                        if(cmd.MainActioncmmd != null)
                        {
                            BindOutKeyValues(cmd.MainActioncmmd.Parameters, key, value);
                        }
                        if (cmd.CheckActionCommand != null)
                        {
                            BindOutKeyValues(cmd.CheckActionCommand.Parameters, key, value);
                        }
                        if (cmd.CheckActionStateCommand != null)
                        {
                            BindOutKeyValues(cmd.CheckActionStateCommand.Parameters, key, value);
                        }
                        if (cmd.StateCommand != null)
                        {
                            BindOutKeyValues(cmd.StateCommand.Parameters, key, value);
                        }
                    }
                }
                if (BatchActionBindCommands != null)
                {
                    foreach (BatchActionBindCommand cmd in BatchActionBindCommands)
                    {
                        if (cmd.CheckActionCommand != null)
                        {
                            BindOutKeyValues(cmd.CheckActionCommand.Parameters, key, value);
                        }
                        if (cmd.CheckCommand != null)
                        {
                            BindOutKeyValues(cmd.CheckCommand.Parameters, key, value);
                        }
                        if (cmd.DoCommands != null)
                        {
                            foreach(DbCommand cmd2 in cmd.DoCommands)
                            {
                                BindOutKeyValues(cmd2.Parameters, key, value);
                            }
                            
                        }
                        if (cmd.ResultCommand != null)
                        {
                            BindOutKeyValues(cmd.ResultCommand.Parameters, key, value);
                        }
                    }
                }
                

            }
            
        }
        public void BindOutKeyValues(object data)
        {
            if (OutFields == null) return;
            StringDictionary outkeyvalues = this.GetOutValues(data);
            BindOutKeyValues(outkeyvalues);
        }
        private void BindOutKeyValues(DbParameterCollection dbParameters,string key,string value)
        {
            string key1=string.Format("@{0}",key);
            for (int i = 0; i < dbParameters.Count; i++)
            {
                DbParameter dbParameter = dbParameters[i];
                if (dbParameter.ParameterName.Equals(key1, StringComparison.OrdinalIgnoreCase)
                    || dbParameter.ParameterName.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    dbParameter.Value = value;
                }
            }
        }
        /// <summary>
        /// 添加参数委托
        /// </summary>
        internal Action<IDbCommand> AddParamter;
        internal void AddOutParamter()
        {
            if (AddParamter != null)
            {
                if (CheckActionCommand != null)
                    AddParamter(CheckActionCommand);
                if (CheckActionStateCommand != null)
                    AddParamter(CheckActionStateCommand);
                if (MainActioncmmd != null)
                    AddParamter(MainActioncmmd);
                if (StateCommand != null)
                    AddParamter(StateCommand);
                if(ActionBindCommands != null)
                {
                    for(int i = 0;i < ActionBindCommands.Count; i++)
                    {
                        ActionBindCommands[i].AddParamter = this.AddParamter;
                        ActionBindCommands[i].AddOutParamter();
                    }
                }
                if (BatchActionBindCommands != null)
                {
                    for(int i = 0; i < BatchActionBindCommands.Count; i++)
                    {
                        BatchActionBindCommands[i].AddParamter = this.AddParamter;
                        BatchActionBindCommands[i].AddOutParamter();
                    }
                }
            }
        }
    }
    /// <summary>
    /// BatchAction绑定后的命令
    /// </summary>
    internal class BatchActionBindCommand
    {
        public string ActionName { get; set; }
        public DbCommand CheckActionCommand { get; set; }
        public DbCommand CheckActionStateCommand { get; set; }
        public DbCommand CheckCommand{ get; set; }
        public DbCommand[] DoCommands { get; set; }
        public DbCommand ResultCommand { get; set; }
        public string ErrMessage
        {
            get; set;
        }
        public bool IsErr
        {
            get
            {
                return !string.IsNullOrEmpty(ErrMessage);
            }
        }
        /// <summary>
        /// 添加参数委托
        /// </summary>
        internal Action<IDbCommand> AddParamter;
        internal void AddOutParamter()
        {
            if (AddParamter != null)
            {
                if (CheckActionCommand != null)
                {
                    AddParamter(CheckActionCommand);
                }
                if (CheckActionStateCommand != null)
                {
                    AddParamter(CheckActionStateCommand);
                }
                if (CheckCommand != null)
                {
                    AddParamter(CheckCommand);
                }
                if (DoCommands != null)
                {
                    for(int i = 0; i < DoCommands.Length; i++)
                    {
                        AddParamter(DoCommands[i]);
                    }
                    
                }
                if (ResultCommand != null)
                {
                    AddParamter(ResultCommand);
                }

            }
        }
    }
}
