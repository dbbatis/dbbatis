using DBBatis.Web;
using DBBatis.JSON;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Collections;
using DBBatis.IO.Log;

namespace DBBatis.Action
{
    public class ComboBoxManager
    {

        /// <summary>
        /// 获取下拉处理
        /// </summary>
        public static Func<ComboBoxManager> GetComboBoxManager { get; set; }
        string Language;
        ContextHandlerBase Handler;
        
        public ComboBoxManager(ContextHandlerBase handler)
        {

            Language = handler.Data.Language;
            Handler = handler;


        }
        public virtual void Process()
        {
            //访问无效，则退出
            if (!Handler.CheckValid()) return;
            ActionResult result = new ActionResult();
            string name = Handler.Data["Name"];
            string onlydata = Handler.Data["OnlyData"];
            string value=Handler.Data["Value"];
            string pid = Handler.Data["Pid"];
            DbConfig db=Handler.GetDbConfig();
            if (string.IsNullOrEmpty(name) == false)
            {
                string jsonvalue = string.Empty;
                if (value == null)
                {   
                    //说明不是过滤
                    DataSet ds = GetDropDownData(db, name, Handler.UserID, Handler.Data.Language);
                    if (!string.IsNullOrEmpty(onlydata))
                    {
                        if (ds.Tables.Count == 1)
                        {
                            jsonvalue = ds.Tables[0].ToJson();
                        }
                        else
                        {
                            jsonvalue = ds.ToJson();
                        }
                    }
                    else
                    {
                        result.Data = ds;
                        jsonvalue=result.ToJson();
                    }
                }
                else
                {
                    DataTable dt = GetAutocompleteData(db, name, value, pid
                        , Handler.UserID, Handler.Data.Language);
                    if (!string.IsNullOrEmpty(onlydata))
                    {
                        jsonvalue=dt.ToJson();
                    }
                    else
                    {
                        result.Data=dt;
                        jsonvalue = result.ToJson();
                    }
                }
                Handler.Write(jsonvalue);
            }
            else
            {
                result.ErrMessage = "请指定Name参数";
            }
            Handler.Write(result.ToJson());
        }
        /// <summary>
        /// 获取多个下拉数据
        /// </summary>
        /// <param name="context"></param>
        private static DataSet GetDropDownData(DbConfig db
            , string dropdownData, int userid
            , string language)
        {
            string data = dropdownData;
            NameValueCollection ns = new NameValueCollection();

            if (string.IsNullOrEmpty(data))
            {
                return new DataSet();
            }
            string[] items = data.Split(',');
            StringCollection datakeys = new StringCollection();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string item in items)
            {
                if (string.IsNullOrEmpty(item)) continue;
                if (datakeys.Contains(item)) continue;
                datakeys.Add(item);
                sb.Append(GloblaDbAction.GetComboBoxSQL(item, language, userid));
                sb.AppendLine();
            }
            DbCommand cmmd = db.CreateCommand();
            cmmd.CommandText = sb.ToString();
            DataSet ds = null;
            try
            {

                MappingBind.AddUserParameter(cmmd, userid);
                MappingBind.AddParameterHandler?.Invoke(cmmd,userid);
                ds = db.ExecuteDataset(cmmd);
            }
            catch (DbException dberr)
            {
                throw new ApplicationException(string.Format("加载下拉数据:", dberr));
            }
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                ds.Tables[i].TableName = datakeys[i];
            }
            ds.DataSetName = "Data";
            return ds;
        }
        /// <summary>
        /// 获取多个下拉数据
        /// </summary>
        /// <param name="context"></param>
        private static DataTable GetAutocompleteData(DbConfig db
            , string name,string value,string pid, int userid
            , string language)
        {

            DbCommand cmmd = GloblaDbAction.GetAutocompleteSQL(name
                        , language, pid, value, userid);
            
            try
            {
                DataTable dt = db.ExecuteDataTable(cmmd);
                return dt;
            }
            catch (DbException dberr)
            {
                throw new ApplicationException(string.Format("加载下拉数据:", dberr));
            }
            
        }
    }
}
