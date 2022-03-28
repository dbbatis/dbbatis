using System;
using System.Data;
using System.Data.Common;

namespace DBBatis.Action
{
    /// <summary>
    /// 命令处理委托
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cmmd"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    //public delegate bool CommandHandler(DbAction action,DbCommand cmmd,ref ActionResult result);
    /// <summary>
    /// 返回类型
    /// </summary>
    public class ActionResultType
    {
        /// <summary>
        /// 是不是简单类型
        /// </summary>
        public bool IsSimple { get; set; }
        /// <summary>
        /// 简单类型
        /// </summary>
        public ActionSimpleType Simple { get; set; }
        /// <summary>
        /// 文件
        /// </summary>
        public string ClassFile { get; set; }
        /// <summary>
        /// 类全称
        /// </summary>
        public string ClassFullName { get; set; }

        public string Description { get; set; }
    }
    /// <summary>
    /// 简单类型
    /// </summary>
    public enum ActionSimpleType
    {
        OneValue,
        OneRow,
        DataTable,
        DataSet,
        NonQuery
    }

    /// <summary>
    /// 返回数据的Result
    /// </summary>
    public class ActionResult
    {
        internal Exception Err { get; set; }
        /// <summary>
        /// 返回的数据
        /// </summary>
        public object Data = null;
        /// <summary>
        /// 是否OK
        /// </summary>
        public bool IsOK
        {
            get
            {
                return !IsErr;
            }
        }

        /// <summary>
        /// 是否登录超时
        /// </summary>
        public bool IsOut = false;
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrMessage = string.Empty;

        internal bool IsHandler = false;
        /// <summary>
        /// 是否有错误
        /// </summary>
        internal bool IsErr
        {
            get
            {
                return !string.IsNullOrEmpty(ErrMessage);
            }
        }
        /// <summary>
        /// 设置错误对象
        /// </summary>
        /// <param name="err"></param>
        public void SetError(Exception err)
        {
            Err = err;
            string errinfo = err.Message;
            ErrMessage = errinfo;

        }

        //public string ToJson()
        //{
        //    throw new NotImplementedException();
        //}
    }
}

