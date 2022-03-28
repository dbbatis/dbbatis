using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace DBBatis.Action
{
    
    /// <summary>
    /// 处理基类
    /// </summary>
    public class ActionHandlerBase
    {
        public string ActionName { get; set; }
        public short PageID { get; set; }
        public ActionData ActionData { get; internal set; }
        public ActionHandlerBase()
        {
        }


        public event Action<IDbCommand>  OnAddParameter;
        public event Func<ActionResult> OnBefore;
        public event Func<DbConfig, ActionResult> OnBeforeDoCommand;
        public event Func<DbConfig, ActionResult,ActionResult> OnAfterDoCommand;
        public event Func<ActionResult, ActionResult> OnAfter;

        internal bool IsHaveBefore
        {
            get
            {
                return OnBefore != null;
            }
        }
        internal bool IsHaveBeforeDoCommand
        {
            get
            {
                return OnBeforeDoCommand != null;
            }
        }
        internal bool IsHaveAddParameter
        {
            get
            {
                return OnAddParameter != null;
            }
        }
        internal bool IsHaveAfterDoCommand
        {
            get
            {
                return OnAfterDoCommand != null;
            }
        }
        internal bool IsHaveAfter
        {
            get
            {
                return OnAfter != null;
            }
        }
        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="command"></param>
        internal void AddParameter(IDbCommand command)
        {
            OnAddParameter?.Invoke(command);
        }
        /// <summary>
        /// 执行命令前的操作,可做一些准备前的工作
        /// </summary>
        /// <returns></returns>
        internal ActionResult Before()
        {
            return OnBefore?.Invoke();
        }
        
        /// <summary>
        /// 执行SQL命令前的操作
        /// </summary>
        /// <remarks>注意这里已经与数据库建立通讯或事务</remarks>
        /// <returns></returns>
        internal ActionResult Before(DbConfig db)
        {
            return OnBeforeDoCommand?.Invoke(db);
        }
        internal ActionResult After(DbConfig db,
            ActionResult mainresult)
        {
           return OnAfterDoCommand?.Invoke(db, mainresult);
        }
        public virtual ActionResult After(ActionResult mainresult)
        {

            return OnAfter?.Invoke( mainresult);
        }

    }
    public class ActionAttribute : Attribute
    {
        public string ActionName { get; set; }
        public short PageID { get; set; }

    }
}
