using DBBatis.Action;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace DBBatis.SQLServer
{
    /// <summary>
    /// SQL DB
    /// </summary>
    public class SQLDbConfig : DbConfig
    {
        /// <summary>
        /// 当前数据库类型
        /// </summary>
        public override string DbType { get => "SqlServer"; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库链接字符串</param>
        public SQLDbConfig(string connectionString)
        {
            base.ConnectionString = connectionString;
        }

        public override DbCommand CloneCommand(DbCommand cmmd)
        {
            return ((SqlCommand)cmmd).Clone();
        }

        /// <summary>
        /// 创建链接
        /// </summary>
        /// <returns></returns>
        public override DbConnection CreateConnection()
        {
            return new SqlConnection(base.ConnectionString);
        }
        /// <summary>
        /// 创建 DbDataAdapter
        /// </summary>
        /// <returns></returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        /// <summary>
        /// 获取SQL命令文本
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns></returns>
        public override string GetCommandString(IDbCommand cmmd)
        {
            SqlCommand sqlcmmd = (SqlCommand)cmmd;
            //输出命令
            StringBuilder sb = new StringBuilder();
            foreach (SqlParameter p in sqlcmmd.Parameters)
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
        internal static string[] GetSQLDeclareAndValue(SqlParameter p)
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
                defaultValue = p.SqlValue.ToString();
            }
            string declarestr;

            string setvaluesql;
            if (p.SqlDbType == SqlDbType.VarChar
                        || p.SqlDbType == SqlDbType.Char
                        || p.SqlDbType == SqlDbType.NVarChar
                        || p.SqlDbType == SqlDbType.NChar)
            {
                int size = p.Size;

                if (size == 0) size = 50;

                declarestr = string.Format("DECLARE {0} {1}({2});", parameterName, p.SqlDbType.ToString().ToUpper(),
                    p.Size == 0 ? size.ToString() : (p.Size == -1 ? "Max" : p.Size.ToString()));

            }
            else
            {
                declarestr = string.Format("DECLARE {0} {1};", parameterName, p.SqlDbType.ToString().ToUpper());
                if (p.SqlDbType == SqlDbType.Bit)
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
                else if (p.SqlDbType == SqlDbType.BigInt
                    || p.SqlDbType == SqlDbType.Int
                    || p.SqlDbType == SqlDbType.SmallInt
                     || p.SqlDbType == SqlDbType.TinyInt)
                {
                    shuziflag = true;
                }
                else if (p.SqlDbType == SqlDbType.Decimal
                   || p.SqlDbType == SqlDbType.Float
                   || p.SqlDbType == SqlDbType.Money)
                {
                    shuziflag = true;
                }
                else if (p.SqlDbType == SqlDbType.Date)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("yyyy-MM-dd");
                        }
                    }
                }
                else if (p.SqlDbType == SqlDbType.Time)
                {
                    if (p.Value != DBNull.Value)
                    {
                        if (defaultValue.Length > 0)
                        {
                            defaultValue = ((DateTime)p.Value).ToString("HH:mm:ss");
                        }
                    }

                }
                else if (p.SqlDbType == SqlDbType.Date
                   || p.SqlDbType == SqlDbType.DateTime
                    || p.SqlDbType == SqlDbType.DateTime2
                    || p.SqlDbType == SqlDbType.DateTimeOffset)
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
                setvaluesql = string.Format("SET     {0} = {1};", parameterName, defaultValue);
            }
            else
            {
                setvaluesql = string.Format("SET     {0} = '{1}';", parameterName, defaultValue.Replace("'", "''"));
            }
            return new string[] { declarestr, setvaluesql };
        }
        public override string[] GetSQLDeclareAndValue(DbParameter parameter, object value)
        {
            SqlParameter p = (SqlParameter)parameter;
            string defaultValue = value == null ? null : value.ToString();
            string declarestr = string.Empty;
            bool shuziflag = false;
            string setvaluesql = string.Empty;
            if (p.SqlDbType == SqlDbType.VarChar
                        || p.SqlDbType == SqlDbType.Char
                        || p.SqlDbType == SqlDbType.NVarChar
                        || p.SqlDbType == SqlDbType.NChar)
            {
                if (defaultValue == null)
                    defaultValue = p.ParameterName.TrimStart('@');

                declarestr = string.Format("DECLARE {0} {1}({2});", p.ParameterName, p.SqlDbType.ToString().ToUpper(),
                    p.Size == 0 ? defaultValue.ToString().Length.ToString() : (p.Size == -1 ? "Max" : p.Size.ToString()));

            }
            else
            {
                declarestr = string.Format("DECLARE {0} {1};", p.ParameterName, p.SqlDbType.ToString().ToUpper());
                if (p.SqlDbType == SqlDbType.Bit)
                {
                    if (defaultValue == null)
                        defaultValue = "1";
                    shuziflag = true;
                }
                else if (p.SqlDbType == SqlDbType.BigInt
                    || p.SqlDbType == SqlDbType.Int
                    || p.SqlDbType == SqlDbType.SmallInt
                     || p.SqlDbType == SqlDbType.TinyInt)
                {
                    if (defaultValue == null)
                        defaultValue = "120";
                    shuziflag = true;
                }
                else if (p.SqlDbType == SqlDbType.Decimal
                   || p.SqlDbType == SqlDbType.Float
                   || p.SqlDbType == SqlDbType.Money)
                {
                    if (defaultValue == null)
                        defaultValue = "132.12";
                    shuziflag = true;
                }
                else if (p.SqlDbType == SqlDbType.Date)
                {
                    if (defaultValue == null)
                        defaultValue = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else if (p.SqlDbType == SqlDbType.Time)
                {
                    if (defaultValue == null)
                        defaultValue = DateTime.Now.ToString("HH:mm:ss");
                }
                else if (p.SqlDbType == SqlDbType.Date
                   || p.SqlDbType == SqlDbType.DateTime
                    || p.SqlDbType == SqlDbType.DateTime2
                    || p.SqlDbType == SqlDbType.DateTimeOffset)
                {
                    if (defaultValue == null)
                        defaultValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    if (defaultValue == null)
                        defaultValue = "";
                }
            }
            if (shuziflag)
            {
                setvaluesql = string.Format("SET     {0} = {1};", p.ParameterName, defaultValue);
            }
            else
            {
                setvaluesql = string.Format("SET     {0} = '{1}';", p.ParameterName, defaultValue);
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
            SqlParameter useridp = new SqlParameter("@USERID", SqlDbType.Int);
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
                    ((SqlParameter)p).SqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), rows[0]["TypeName"].ToString(), true);

                    
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
    DECLARE @UniqueShowText_{1} NVARCHAR(100);
    DECLARE @UniqueCheckTip_{1} NVARCHAR(100);
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
                bool needtran = cmmd.CommandText.IndexOf("BEGIN TRAN", StringComparison.OrdinalIgnoreCase) != -1;
                cmmd.CommandText =
          string.Format(
          @"BEGIN TRY
{0}
END TRY
BEGIN CATCH
	{1}
	DECLARE @Action_TryError NVARCHAR(MAX)
	DECLARE @Action_SEVERITY INT,@Action_STATE INT;
	SELECT @Action_SEVERITY=ERROR_SEVERITY(),@Action_STATE=ERROR_STATE(),@Action_TryError= '行:'+CONVERT(VARCHAR(10), ERROR_LINE())+'异常信息:'+ ERROR_MESSAGE();
	RAISERROR(@Action_TryError,@Action_SEVERITY,@Action_STATE)
END CATCH
", cmmd.CommandText, needtran ? "IF @@TRANCOUNT>0 ROLLBACK TRAN;" : "");
            }
        }

        public override DbCommand GetStateOperationCommand(StateOperation StateOperation, string tableName, string PKField)
        {
            string sql = "";
            StringBuilder sbcheckstates = new StringBuilder();
            string checkfields = string.Empty;
            StringBuilder fieldwheresb = new StringBuilder();

            for (int i = 0; i < StateOperation.States.Count; i++)
            {
                CheckState checkstate = StateOperation.States[i];
                if (i > 0)
                {
                    sbcheckstates.Append("+");
                }
                fieldwheresb.AppendFormat(" AND {0}{1}'{2}'", checkstate.Field, checkstate.Match, checkstate.Value);
                sbcheckstates.AppendFormat(@"
CASE WHEN {0}{1}'{2}' THEN '' ELSE '{3};' END", checkstate.Field, checkstate.Match, checkstate.Value, checkstate.Lable.Replace("'", "'"));
            }

            if (StateOperation.Type == StateOperationType.Do)
            {

                sql = string.Format(@"
DECLARE @ERR    NVARCHAR(1000);
SET @ERR='';
SELECT @ERR={0} FROM {1} WHERE {2}=@{2} {3}
IF @@ROWCOUNT=0
BEGIN
    SELECT '未到有效数据,请重新打开单据操作.';RETURN;
END
IF @ERR=''
BEGIN
    BEGIN TRAN;
    UPDATE {1} SET Is{4}=1,{4}By=@UserID,{4}Date=GETDATE(),UPDATE_BY=@UserID,UPDATE_DATE=GETDATE() WHERE {2}=@{2} {3} {5};
    IF @@ROWCOUNT=1
    BEGIN
        {6}
        COMMIT TRAN;SELECT '';RETURN;
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        SELECT '更新数据失败,数据可能发生变化,请重新打开单据操作.';RETURN;
    END
END
ELSE
BEGIN
    SELECT '仅在【'+SUBSTRING(@ERR,1,LEN(@ERR)-1)+'】时才可操作,请检查单据状态.';
END
", sbcheckstates.ToString(), tableName, PKField, StateOperation.WhereSQL, StateOperation.Field, fieldwheresb, StateOperation.CommandText);
            }
            else if (StateOperation.Type == StateOperationType.UnDo)
            {
                if (StateOperation.UnDoIsNull)
                {
                    sql = string.Format(@"
DECLARE @ERR    NVARCHAR(1000);
SET @ERR='';
SELECT @ERR={0} FROM {1} WHERE {2}=@{2} {3}
IF @@ROWCOUNT=0
BEGIN
    SELECT '未到有效数据,请重新打开单据操作.';RETURN;
END
IF @ERR=''
BEGIN
    BEGIN TRAN;
    UPDATE {1} SET Is{4}=0,{4}By=NULL,{4}Date=NULL,UPDATE_BY=@UserID,UPDATE_DATE=GETDATE() WHERE {2}=@{2} {3} {5};
    IF @@ROWCOUNT=1
    BEGIN
        {6}
        COMMIT TRAN;
        SELECT '';RETURN;
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        SELECT '更新数据失败,数据可能发生变化,请重新打开单据操作.';RETURN;
    END
END
ELSE
BEGIN
    SELECT '仅在【'+SUBSTRING(@ERR,1,LEN(@ERR)-1)+'】时才可操作,请检查单据状态.';
END
", sbcheckstates.ToString(), tableName, PKField, StateOperation.WhereSQL, StateOperation.Field, fieldwheresb, StateOperation.CommandText);
                }
                else
                {
                    sql = string.Format(@"
DECLARE @ERR    NVARCHAR(1000);
SET @ERR='';
SELECT @ERR={0} FROM {1} WHERE {2}=@{2} {3}
IF @@ROWCOUNT=0
BEGIN
    SELECT '未到有效数据,请重新打开单据操作.';RETURN;
END
IF @ERR=''
BEGIN
    BEGIN TRAN;
    UPDATE {1} SET Is{4}=0,{4}By=@UserID,{4}Date=GETDATE(),UPDATE_BY=@UserID,UPDATE_DATE=GETDATE() WHERE {2}=@{2} {3} {5};
    IF @@ROWCOUNT=1
    BEGIN
        {6}
        COMMIT TRAN;
        SELECT '';RETURN;
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        SELECT '更新数据失败,数据可能发生变化,请重新打开单据操作.';RETURN;
    END
END
ELSE
BEGIN
    SELECT '仅在【'+SUBSTRING(@ERR,1,LEN(@ERR)-1)+'】时才可操作,请检查单据状态.';
END
", sbcheckstates.ToString(), tableName, PKField, StateOperation.WhereSQL, StateOperation.Field, fieldwheresb, StateOperation.CommandText);
                }

            }
            else if (StateOperation.Type == StateOperationType.Check)
            {
                sql = string.Format(@"
DECLARE @ERR    NVARCHAR(1000);
SET @ERR='';

SELECT @ERR={0} 
FROM {1} 
WHERE {2}=@{2} {3}

IF @@ROWCOUNT=0
BEGIN
    SELECT '未到有效数据,请重新打开单据操作.';RETURN;
END
IF @ERR>''
BEGIN
    SELECT '仅在【'+SUBSTRING(@ERR,1,LEN(@ERR)-1)+'】时才可操作,请检查单据状态.';
END", sbcheckstates.ToString(), tableName, PKField, StateOperation.WhereSQL, StateOperation.Field, fieldwheresb);
                if (!string.IsNullOrEmpty(StateOperation.CommandText))
                {
                    sql = string.Format(@"{0}
ELSE
BEGIN
{1}
END
", sql, StateOperation.CommandText);
                }
            }
            SqlCommand cmmd = new SqlCommand(sql);
            IDbDataParameter p = cmmd.CreateParameter();
            p.ParameterName = string.Format("@{0}", PKField);
            p.SourceColumn = PKField;
            cmmd.Parameters.Add(p);
            return cmmd;
        }

        //public override ColumnProperties CreateColumnProperties()
        //{
        //    return new SQLColumnProperties(true);
        //}

        public override DataTable GetTableSchema(string tableName)
        {
            SqlCommand cmmd = new SqlCommand();
            cmmd.CommandText = @"
SELECT  sys.columns.name ,
        sys.types.name AS TypeName ,
        sys.columns.max_length ,
        sys.columns.is_nullable ,
        ( SELECT    COUNT(*)
          FROM      sys.identity_columns
          WHERE     sys.identity_columns.object_id = sys.columns.object_id
                    AND sys.columns.column_id = sys.identity_columns.column_id
        ) AS is_identity ,
        ( SELECT    CASE WHEN CHARINDEX(' ', CONVERT(NVARCHAR(100), value)) > 0
                         THEN SUBSTRING(CONVERT(NVARCHAR(100), value), 0,
                                        CHARINDEX(' ',
                                                  CONVERT(NVARCHAR(100), value)))
                         ELSE value
                    END
          FROM      sys.extended_properties
          WHERE     sys.extended_properties.major_id = sys.columns.object_id
                    AND sys.extended_properties.minor_id = sys.columns.column_id
        ) AS description
FROM    sys.columns ,
        sys.tables ,
        sys.types
WHERE   sys.columns.object_id = sys.tables.object_id
        AND sys.columns.user_type_id = sys.types.user_type_id
        AND sys.tables.name = @TableName
ORDER BY sys.columns.column_id";
            cmmd.Parameters.AddWithValue("@TableName", tableName);
            return this.ExecuteDataTable(cmmd);
        }
    }
}
