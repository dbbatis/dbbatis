using DBBatis.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DBBatis.Action
{
    public abstract class HandlerAction
    {
        DbAction _Action = null;
        readonly IDbCommand _Command = null;

        public HandlerAction(DbAction action,IDbCommand command)
        {
            _Action = action;
            _Command = command;
        }
        /// <summary>
        /// 获取命令后的结果
        /// </summary>
        public ActionResult Result { get; protected set; }
        public abstract IDbCommand GetBeforeCommand();
        public abstract IDbCommand GetAfterCommand();
    }
}
