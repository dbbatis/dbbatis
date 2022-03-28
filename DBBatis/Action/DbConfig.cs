using DBBatis.IO.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using DBBatis.Common;
using System.Reflection;
using System.Diagnostics;

namespace DBBatis.Action
{

    public delegate void LogHandler(string tipInfo, IDbCommand cmmd, object result
        , DateTime beginTime,DbConfig dbConfig);
    public delegate void ErrLogHandler(IDbCommand cmmd, Exception err,DbConfig dbConfig);

    /// <summary>
    /// 数据库配置
    /// </summary>
    public abstract class DbConfig
    {
        static ErrLogHandler _ErrHandler;
        /// <summary>
        /// 当命令执行出错时，处理方法
        /// </summary>
        public static ErrLogHandler ErrHandler
        {
            get
            {
                if (_ErrHandler == null) _ErrHandler = Log.WriteErrorCommand;
                return _ErrHandler;
            }
            set
            {
                _ErrHandler = value;
            }
        }
        
        static LogHandler _LogHandler;
        /// <summary>
        /// 日志处理方法
        /// </summary>
        public static LogHandler LogHandler
        {
            get
            {
                if (_LogHandler == null)
                {
                    _LogHandler = CommandLog.Log;
                }
                return _LogHandler;
            }
            set
            {
                _LogHandler = value;
            }
        }
        
        /// <summary>
        /// 最后出错的命令
        /// </summary>
        public static string LastErrorCommand = string.Empty;
        public static Exception LastError;
        public static DateTime LastErrorTime;
        /// <summary>
        /// 内部指定
        /// </summary>
        internal DbConnection _Connection;
        public DbConnection Connection
        {
            get
            {
                if (_Connection != null) return _Connection;
                return CreateConnection();
            }
        }

        /// <summary>
        /// 是否记录日志
        /// </summary>
        public bool IsLog { get; set; }

        /// <summary>
        /// 调用方法
        /// </summary>
        //public string MethodName { get; set; }

        /// <summary>
        /// 获取DbConfig
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
       internal static DbConfig GetDbConfig(string dbType, string connectionString)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string searchpath = AppDomain.CurrentDomain.RelativeSearchPath;

            string[] files= System.IO.Directory.GetFiles(path, "DBBatis.*.dll", System.IO.SearchOption.AllDirectories);
            System.Collections.ObjectModel.Collection<Assembly> array = new System.Collections.ObjectModel.Collection<Assembly>();
            foreach(string f in files)
            {
                try
                {
                    array.Add(Assembly.LoadFrom(f));
                }
                catch
                {

                }
            }
            for (int j = 0; j < array.Count; j++)
            {
                Assembly assembly = array[j];
                Type[] array2 = assembly.GetTypes();
                for (int k = 0; k < array2.Length; k++)
                {
                    Type T2 = array2[k];
                    if (T2.BaseType == typeof(DbConfig))
                    {
                        Type[] types = new Type[1];
                        types[0] = typeof(string);
                        DbConfig dbConfig = (DbConfig)T2.GetConstructor(types)
                            .Invoke(new string[] { connectionString });
                        if (dbConfig.DbType.Equals(dbType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return dbConfig;
                        }
                    }
                }
            }
            throw new System.IO.FileNotFoundException(string.Format("未找到[{0}]类型数据库处理程序集", dbType));
        }
        /// <summary>
        /// 标记
        /// </summary>
        public string DBFlag { get; set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public abstract string DbType { get; }
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// 创建Connection
        /// </summary>
        /// <returns></returns>
        public abstract DbConnection CreateConnection();
        /// <summary>
        /// 创建DataAdapter
        /// </summary>
        /// <returns></returns>
        public abstract DbDataAdapter CreateDataAdapter();
        /// <summary>
        /// 获取命令文本
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns></returns>
        public abstract string GetCommandString(IDbCommand cmmd);

        public abstract DbCommand GetStateOperationCommand(StateOperation StateOperation, string tableName, string PKField);
        //public abstract ColumnProperties CreateColumnProperties();
        /// <summary>
        /// 获取检查某字段值是否唯一SQL
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="pkField">主键</param>
        /// <param name="field">唯一字段</param>
        /// <param name="isAdd">是否为添加</param>
        /// <returns>某字段值是否唯一SQL</returns>
        public abstract string GetCheckUniqueKeySQLHandler(Action.Page page, string field, bool isAdd);
        /// <summary>
        /// 设置TrySQL 用于捕捉出错信息
        /// </summary>
        /// <param name="cmmd"></param>
        public abstract void SetTrySQL(DbCommand cmmd);
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns>结果集</returns>
        public DataSet ExecuteDataset(DbCommand cmmd)
        {
            DbDataAdapter adapter = CreateDataAdapter();
            DataSet ds = new DataSet();
            try
            {
                DateTime begintime = DateTime.Now;
                cmmd.Connection = Connection;
                adapter.SelectCommand = cmmd;
                adapter.Fill(ds);
                if (IsLog)
                    WriteLog(cmmd, ds, begintime);

                return ds;
            }
            catch (Exception err)
            {
                WriteError(cmmd, err);
                throw err;
            }

        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>结果集</returns>
        public DataSet ExecuteDataset(string sql)
        {
            DbCommand cmmd = Connection.CreateCommand();
            cmmd.CommandText = sql;
            return ExecuteDataset(cmmd);
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的第一个结果集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>第一个结果集</returns>
        public DataTable ExecuteDataTable(string sql)
        {
            DbCommand cmmd = Connection.CreateCommand();
            cmmd.CommandText = sql;
            return ExecuteDataset(cmmd).Tables[0];
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的第一个结果集
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns>第一个结果集</returns>
        public DataTable ExecuteDataTable(DbCommand cmmd)
        {
            return ExecuteDataset(cmmd).Tables[0];
        }
        /// <summary>
        /// 将第一行数据行转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmmd"></param>
        /// <remarks>属性类型与数据库类型不一致，会转换出错</remarks>
        /// <returns></returns>
        public T Execute<T>(DbCommand cmmd)
        {
            return Execute<T>(cmmd, false);
        }
        /// <summary>
        /// 将第一行数据行转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmmd"></param>
        /// <param name="ignoreType">忽略类型</param>
        /// <remarks>属性类型与数据库类型不一致，会默认转换</remarks>
        /// <returns></returns>
        public T Execute<T>(DbCommand cmmd,bool ignoreType)
        {
            DataTable dt= ExecuteDataset(cmmd).Tables[0];
            if (dt.Rows.Count == 0)
            {
                return default(T);
            }
            else
            {
                return dt.Rows[0].ToObject<T>(ignoreType);
            }
        }
        public List<T> ExecuteList<T>(DbCommand cmmd, bool ignoreType)
        {
            DataTable dt = ExecuteDataset(cmmd).Tables[0];
            return dt.ToList<T>(ignoreType);
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>结果集中第一行的第一列。</returns>
        public object ExecuteScalar(string sql)
        {
            DbCommand cmmd = CreateCommand();
            cmmd.CommandText = sql;
            return ExecuteScalar(cmmd);

        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns>结果集中第一行的第一列。</returns>
        public object ExecuteScalar(DbCommand cmmd)
        {
            using (DbConnection cnn = CreateConnection())
            {
                cmmd.Connection = cnn;
                cnn.Open();
                try
                {

                    DateTime begintime = DateTime.Now;

                    object v = cmmd.ExecuteScalar();

                    WriteLog(cmmd, v, begintime);

                    return v;
                }
                catch (Exception err)
                {
                    WriteError(cmmd, err);
                    throw err;
                }
            }
        }
        /// <summary>
        /// 对连接对象执行 SQL 语句。
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>受影响的行数。</returns>
        public int ExecuteNonQuery(string sql)
        {

            DbCommand cmmd = CreateCommand();
            cmmd.CommandText = sql;
            return ExecuteNonQuery(cmmd);
        }
        /// <summary>
        /// 对连接对象执行 SQL 语句。
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns>受影响的行数。</returns>
        public int ExecuteNonQuery(DbCommand cmmd)
        {
            using (DbConnection cnn = CreateConnection())
            {
                cmmd.Connection = cnn;
                cnn.Open();
                try
                {
                    DateTime begintime = DateTime.Now;
                    int v = cmmd.ExecuteNonQuery();
                    WriteLog(cmmd, v, begintime);
                    return v;
                }
                catch (Exception err)
                {
                    WriteError(cmmd, err);
                    throw err;
                }
            }
        }
        /// <summary>
        /// 执行 System.Data.Common.DbCommand.CommandText，并使用
        /// System.Data.CommandBehavior.CloseConnection 值返回 System.Data.Common.DbDataReader。
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommand cmmd)
        {
            DbConnection cnn = CreateConnection();
            cmmd.Connection = cnn;
            cnn.Open();
            try
            {
                DateTime begintime = DateTime.Now;
                IDataReader r = cmmd.ExecuteReader(CommandBehavior.CloseConnection);
                WriteLog(cmmd, "IDataReader", begintime);
                return r;
            }
            catch (Exception err)
            {
                WriteError(cmmd, err);
                throw err;
            }

        }
        /// <summary>
        /// 创建命令
        /// </summary>
        /// <returns></returns>
        public DbCommand CreateCommand()
        {
            return Connection.CreateCommand();
        }
        /// <summary>
        /// 复制命令
        /// </summary>
        /// <param name="cmmd"></param>
        /// <returns></returns>
        public abstract DbCommand CloneCommand(DbCommand cmmd);
        /// <summary>
        /// 获取声明和参数赋值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string[] GetSQLDeclareAndValue(DbParameter p, object value);
        /// <summary>
        /// 获取DbAction对应的SQL命令
        /// </summary>
        /// <param name="dbaction"></param>
        /// <returns></returns>
        public abstract string GetTestSQLCommand(DbAction dbaction);
        /// <summary>
        /// 获取DbAction对应的测试SQL命令
        /// </summary>
        /// <param name="dbaction"></param>
        /// <returns></returns>
        public abstract string GetTestSQLCommand(ActionCommand actionCommand);
        /// <summary>
        /// 获取表结构
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <remarks>
        /// 
        /// </remarks>
        /// <returns></returns>
        public abstract DataTable GetTableSchema(string tableName);
        /// <summary>
        /// 根据数据库表结构设置参数类型
        /// </summary>
        /// <param name="p"></param>
        /// <param name="tableSchema"></param>
        public abstract void SetParamertType(DbParameter p, DataTable tableSchema);
        /// <summary>
        /// 创建参数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public DbParameter CreateParameter(string name, object value)
        {
            DbParameter p = CreateCommand().CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return p;
        }
        /// <summary>
        /// 创建参数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public DbParameter CreateParameter(string name, object value, DbType dbType)
        {
            DbParameter p = CreateParameter(name, value);
            p.DbType = dbType;
            return p;
        }
        /// <summary>
        /// 记录执行出错信息
        /// </summary>
        /// <param name="cmmd"></param>
        /// <param name="err"></param>
        protected void WriteError(DbCommand cmmd, Exception err)
        {
            ErrHandler?.Invoke(cmmd, err,this);
        }
        /// <summary>
        /// 记录命令日志
        /// </summary>
        /// <param name="cmmd"></param>
        /// <param name="result">执行的结果</param>
        /// <param name="beginTime"></param>
        protected void WriteLog(DbCommand cmmd, object result, DateTime beginTime)
        {
            if (IsLog)
            {
                StackTrace ss = new StackTrace(true);
                int indx = 2;
                MethodBase mb = ss.GetFrame(2).GetMethod();
                while (mb.DeclaringType.FullName.Equals("DBBatis.Action.DbConfig"
                    , StringComparison.OrdinalIgnoreCase))
                {
                    indx++;
                    mb = ss.GetFrame(indx).GetMethod();
                }
                LogHandler?.Invoke(mb.Name, cmmd, result, beginTime, this);
            }
                

        }
       
    }
}
