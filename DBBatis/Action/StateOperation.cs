using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DBBatis.Action
{
    /// <summary>
    /// 状态操作
    /// </summary>
    public class StateOperation
    {
        public string Field { get; set; }
        public StateOperationType Type { get; set; }
        public bool UnDoIsNull { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CommandText { get; set; }
        public string WhereSQL { get; set; }


        public System.Collections.ObjectModel.Collection<CheckState> States { get; set; }

    }
    /// <summary>
    /// 状态操作类型
    /// </summary>
    public enum StateOperationType
    {
        Do = 0,
        UnDo = 1,
        Check = 2
    }
    /// <summary>
    /// 检查状态
    /// </summary>
    public class CheckState
    {
        public string Field { get; set; }
        public string Value { get; set; }
        public string Lable { get; set; }
        public string Match { get; set; }
    }


    public abstract class StateManager
    {
        public abstract DbCommand GetStateOperationCommand(StateOperation StateOperation, string tableName, string PKField);
    }
}
