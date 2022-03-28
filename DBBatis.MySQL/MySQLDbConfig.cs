using DBBatis.Action;
using System;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using System.Text;

namespace DBBatis.MySQL
{
    /// <summary>
    /// MySQL DB
    /// </summary>
    public class MySQLDbConfig : DbConfig
    {
        public override string DbType { get => "MySql"; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库链接字符串</param>
        public MySQLDbConfig(string connectionString)
        {
            
            base.ConnectionString = connectionString;
        }

        public override DbCommand CloneCommand(DbCommand cmmd)
        {
            return (DbCommand)((MySqlCommand)cmmd).Clone();
        }

        /// <summary>
        /// 创建链接
        /// </summary>
        /// <returns></returns>
        public override DbConnection CreateConnection()
        {
            return new MySqlConnection(base.ConnectionString);
        }
        /// <summary>
        /// 创建 DbDataAdapter
        /// </summary>
        /// <returns></returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        /// <summary>
        /// 获取SQL命令文本
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns></returns>
        public override string GetCommandString(IDbCommand cmmd)
        {
            MySqlCommand sqlcmmd = (MySqlCommand)cmmd;
            //输出命令
            StringBuilder sb = new StringBuilder();
            foreach (MySqlParameter p in sqlcmmd.Parameters)
            {
                try
                {
                    string[] values = GetSQLDeclareAndValue(p);

                    sb.AppendLine(values[0]);
                    sb.AppendLine(values[1]);
                }
                catch (Exception err)
                {

                }

            }
            if (sqlcmmd.Parameters.Count > 0)
            {
                sb.AppendLine("---------------以上为系统根据参数信息自动生成---------------");
            }
            if (cmmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendFormat("EXEC {0}", cmmd.CommandText);
                foreach (System.Data.IDataParameter p in cmmd.Parameters)
                {
                    sb.AppendFormat(" {0},", p.ParameterName);
                }
            }
            else
            {
                sb.Append(cmmd.CommandText);
            }

            return sb.ToString().TrimEnd(',');
        }
        internal static string[] GetSQLDeclareAndValue(MySqlParameter p)
        {
            string parameterName = p.ParameterName;
            if (parameterName.StartsWith("@") == false)
            {
                parameterName = string.Format("@{0}", parameterName);
            }
            string defaultValue;
            bool shuziflag = false;
            if (p.Value == DBNull.Value)
            {
                defaultValue = "NULL";
                shuziflag = true;
            }
            else
            {
                defaultValue = p.Value.ToString();
            }
            string declarestr=string.Empty;

            string setvaluesql;
            if (p.MySqlDbType == MySqlDbType.VarChar
                        || p.MySqlDbType == MySqlDbType.String)
            {
                int size = p.Size;
                if (size == 0) size = 50;
            }
            else
            {
                if (p.MySqlDbType == MySqlDbType.Bit)
                {
                    if (p.Value != DBNull.Value)
                    {
                        defaultValue = defaultValue.ToLower();
                        if (defaultValue == "1" ||
                            defaultValue == "true")
                        {
                            defaultValue = "1";
                        }
                        else
                        {
                            defaultValue = "0";
                        }
                    }


                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Int64
                    || p.MySqlDbType == MySqlDbType.Int32
                    || p.MySqlDbType == MySqlDbType.Int16
                     || p.MySqlDbType == MySqlDbType.Byte)
                {
                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Decimal
                   || p.MySqlDbType == MySqlDbType.Float)
                {
                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Date)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("yyyy-MM-dd");
                        }
                    }
                }
                else if (p.MySqlDbType == MySqlDbType.Time)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("HH:mm:ss");
                        }
                    }

                }
                else if (p.MySqlDbType == MySqlDbType.Date
                   || p.MySqlDbType == MySqlDbType.DateTime)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
            }
            if (shuziflag)
            {
                setvaluesql = string.Format("SET	{0} = {1};", parameterName, defaultValue);
            }
            else
            {
                setvaluesql = string.Format("SET	{0} = '{1}';", parameterName, defaultValue.Replace("'", "''"));
            }
            return new string[] { declarestr, setvaluesql };
        }
        public override string[] GetSQLDeclareAndValue(DbParameter parameter, object value)
        {
            MySqlParameter p = (MySqlParameter)parameter;
            string defaultValue = value == null ? null : value.ToString();
            string declarestr = string.Empty;
            bool shuziflag = false;
            string setvaluesql = string.Empty;
            if (p.MySqlDbType == MySqlDbType.VarChar
                        || p.MySqlDbType == MySqlDbType.String)
            {
                if (defaultValue == null)
                    defaultValue = p.ParameterName.TrimStart('@');

            }
            else
            {
                if (p.MySqlDbType == MySqlDbType.Bit)
                {
                    if (p.Value != DBNull.Value)
                    {
                        defaultValue = defaultValue.ToLower();
                        if (defaultValue == "1" ||
                            defaultValue == "true")
                        {
                            defaultValue = "1";
                        }
                        else
                        {
                            defaultValue = "0";
                        }
                    }


                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Int64
                    || p.MySqlDbType == MySqlDbType.Int32
                    || p.MySqlDbType == MySqlDbType.Int16
                     || p.MySqlDbType == MySqlDbType.Byte)
                {
                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Decimal
                   || p.MySqlDbType == MySqlDbType.Float)
                {
                    shuziflag = true;
                }
                else if (p.MySqlDbType == MySqlDbType.Date)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("yyyy-MM-dd");
                        }
                    }
                }
                else if (p.MySqlDbType == MySqlDbType.Time)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("HH:mm:ss");
                        }
                    }

                }
                else if (p.MySqlDbType == MySqlDbType.Date
                   || p.MySqlDbType == MySqlDbType.DateTime)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
                else
                {
                    if (defaultValue == null)
                        defaultValue = "";
                }
            }
            if (shuziflag)
            {
                setvaluesql = string.Format("SET	{0} = {1};", p.ParameterName, defaultValue);
            }
            else
            {
                setvaluesql = string.Format("SET	{0} = '{1}';", p.ParameterName, defaultValue);
            }
            return new string[] { declarestr, setvaluesql };
        }

        public override string GetTestSQLCommand(DbAction dbaction)
        {
            DbCommand cmmd = dbaction.ActionCommand.Command;


            StringBuilder sb = new StringBuilder();
            ActionCommand actionCommand = dbaction.ActionCommand;

            sb.Append(GetTestSQLCommand(actionCommand));
            //检查是否有statecommand
            IDbCommand statecommand = dbaction.GetStateCommand();
            if (statecommand != null)
            {
                sb.AppendLine(string.Format("{0}以下为状态检查命令{0}BEGIN", "-".PadLeft(20, '-')));
                sb.AppendLine(statecommand.CommandText);
                sb.AppendLine(string.Format("{0}以上为状态检查命令{0}END", "-".PadLeft(20, '-')));
            }

            if (actionCommand.Command.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendFormat("EXECUTE {0}", actionCommand.Command.CommandText);
                for (int i = 0; i < actionCommand.Command.Parameters.Count - 1; i++)
                {
                    sb.AppendFormat(" {0},", actionCommand.Command.Parameters[i].ParameterName);
                }
                sb.AppendFormat(" {0}", actionCommand.Command.Parameters[actionCommand.Command.Parameters.Count - 1].ParameterName);
            }
            else
            {
                sb.Append(actionCommand.Command.CommandText);
            }
            return sb.ToString();
        }

        public override string GetTestSQLCommand(ActionCommand actionCommand)
        {
            DbCommand cmmd = actionCommand.Command;
            StringBuilder sb = new StringBuilder();
            string desc = string.Empty;
            string[] temps = null;
            foreach (DbParameter p in cmmd.Parameters)
            {
                temps = GetSQLDeclareAndValue(p, null);

                if (actionCommand.ParameterDescrtions.ContainsKey(p.ParameterName))
                {
                    desc = string.Format("--{0}", actionCommand.ParameterDescrtions[p.ParameterName]);
                }
                sb.AppendFormat("{0}{1}", temps[0].PadRight(32), desc);
                sb.AppendLine();
                sb.Append(temps[1]);
                sb.AppendLine();
            }
            //自动添加USERID
            MySqlParameter useridp = new MySqlParameter("@USERID", MySqlDbType.Int32);
            useridp.Value = 1;
            temps = GetSQLDeclareAndValue(useridp, null);
            sb.AppendFormat("{0}{1}", temps[0].PadRight(32), "--当前用户,系统会自动为每个命令添加此参数");
            sb.AppendLine();
            sb.Append(temps[1]);
            sb.AppendLine();

            if (sb.Length > 0)
            {
                sb.AppendFormat("{0}以上为自动生成参数{0}", "-".PadLeft(20, '-'));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public override void SetParamertType(DbParameter p, DataTable tableSchema)
        {
            if (tableSchema != null && tableSchema.Rows.Count > 0)
            {
                DataRow[] rows = tableSchema.Select(string.Format("name='{0}'", p.ParameterName.TrimStart('@')));
                if (rows.Length == 1)
                {
                    int max_length = 0;
                    if (int.TryParse(rows[0]["max_length"].ToString(), out max_length))
                    {
                        p.Size = max_length;
                    }
                    ((MySqlParameter)p).MySqlDbType = (MySqlDbType)Enum.Parse(typeof(MySqlDbType), rows[0]["TypeName"].ToString(), true);

                    //try
                    //{
                    //    p.DbType = (System.Data.DbType)Enum.Parse(typeof(System.Data.DbType), rows[0]["TypeName"].ToString(), true);
                    //}
                    //catch (Exception err)
                    //{
                    //    Log.Write(string.Format("SetParamertType转换数据类型[{0}]出错.", rows[0]["TypeName"]), err);
                    //}
                }
            }

        }

        /// <summary>
        /// 获取检查某字段值是否唯一SQL
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="pkField">主键</param>
        /// <param name="field">唯一字段</param>
        /// <param name="isAdd">是否为添加</param>
        /// <returns>某字段值是否唯一SQL</returns>
        public override string GetCheckUniqueKeySQLHandler(Page page, string field, bool isAdd)
        {
            string tableName = page.Name;
            string pkField = page.PKField;

            string insertstr = string.Format("AND {0}!=@{0}", pkField);
            return string.Format(@"
IF EXISTS(SELECT 1 FROM {0} WHERE {1}=@{1} {2} {3})
BEGIN
    SET @UniqueShowText_{1}='{1}'
    SET @UniqueCheckTip_{1}=@UniqueShowText_{1}+':【'+@{1}+'】已经存在,不能重复使用.'
    RAISERROR(@UniqueCheckTip_{1},16,1);RETURN;
END
", tableName, field, isAdd ? string.Empty : insertstr, page.CheckUniqueKeyOtherSQL);
        }

        public override void SetTrySQL(DbCommand cmmd)
        {
            if (string.IsNullOrEmpty(cmmd.CommandText) == false
                        && cmmd.CommandText.Trim().Length > 0
                        && cmmd.CommandType == CommandType.Text)
            {
                cmmd.CommandText =
          string.Format(
          @"{0}", cmmd.CommandText);
            }
        }

        public override DbCommand GetStateOperationCommand(StateOperation StateOperation, string tableName, string PKField)
        {
            throw new NotImplementedException();
        }

        

        public override DataTable GetTableSchema(string tableName)
        {
            MySqlCommand cmmd = new MySqlCommand();
            cmmd.CommandText = @"
SELECT   COLUMN_NAME,DATA_TYPE,COLUMN_COMMENT 
    FROM  information_schema.columns 
    WHERE  TABLE_NAME=@TableName
ORDER BY COLUMN_NAME
";
            cmmd.Parameters.AddWithValue("@TableName", tableName);
            return this.ExecuteDataTable(cmmd);
        }
    }
}
