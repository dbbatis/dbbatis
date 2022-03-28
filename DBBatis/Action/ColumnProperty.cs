using System;
using System.Data;

namespace DBBatis.Action
{
    /// <summary>
    /// 列属性
    /// </summary>
    public class ColumnProperty
    {
        /// <summary>
        /// 列名
        /// </summary>
        private string m_Name;
        /// <summary>
        /// 列名
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        /// <summary>
        /// 类型 1--自增 4 计算列
        /// </summary>
        public short Colstat { get; set; }
        /// <summary>
        /// 数据类型
        /// </summary>
        private DbType m_DbType;
        /// <summary>
        /// 数据类型
        /// </summary>
        public DbType DbType
        {
            get { return m_DbType; }
            set { m_DbType = value; }
        }
        /// <summary>
        /// SQL DbType
        /// </summary>
        public SqlDbType SqlDbType { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        private Int16 m_Length;
        /// <summary>
        /// 长度
        /// </summary>
        public Int16 Length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }
        /// <summary>
        /// 描述
        /// </summary>
        private string m_Description;
        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }
        /// <summary>
        /// Lable显示文本
        /// </summary>
        private string m_Lable;
        /// <summary>
        /// Lable显示文本
        /// </summary>
        public string Lable
        {
            get { return m_Lable; }
            set { m_Lable = value; }
        }
        /// <summary>
        /// 是否可以为Null
        /// </summary>
        private bool m_IsNullable;
        /// <summary>
        /// 是否可以为Null
        /// </summary>
        public bool IsNullable
        {
            get { return m_IsNullable; }
            set { m_IsNullable = value; }
        }
        private object m_Tag;
        /// <summary>
        /// 冗余字段，用于用户扩展
        /// </summary>
        public object Tag
        {
            get { return m_Tag; }
            set { m_Tag = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_Name;
        }
    }
    /// <summary>
    /// 列属性集合
    /// </summary>
    public abstract class ColumnProperties : System.Collections.ObjectModel.Collection<ColumnProperty>
    {
        bool _isSQLServer = false;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isSQLServer"></param>
        public ColumnProperties(bool isSQLServer)
        {
            _isSQLServer = isSQLServer;
        }
        /// <summary>
        /// 是否为SQLServer数据库，当为SQL Server时，使用SqlDbType 反之使用DbType
        /// </summary>
        public bool IsSQLServer
        {
            get { return _isSQLServer; }
        }

        /// <summary>
        /// 根据字段名，获取ColumnProperty
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public ColumnProperty this[string fieldName]
        {
            get
            {
                foreach (ColumnProperty p in base.Items)
                {
                    if (p.Name.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                        return p;
                }
                throw new IndexOutOfRangeException(string.Format("字段名[{0}]无效。", fieldName));
            }
        }
        /// <summary>
        /// 获取列信息
        /// </summary>
        /// <param name="db"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract ColumnProperties GetColumns(DbConfig db, string tableName);

        private string[] m_DbTypes;
        /// <summary>
        /// 将类型名称转换为SqlDbType
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns></returns>
        protected DbType ConvertDbType(string typeName)
        {
            if (m_DbTypes == null) m_DbTypes = Enum.GetNames(typeof(DbType));
            foreach (string n in m_DbTypes)
            {
                if (n.ToLower() == typeName.ToLower())
                {
                    return (DbType)Enum.Parse(typeof(DbType), n);

                }
            }
            return default(DbType);
        }
        private string[] m_SqlDbTypes;
        /// <summary>
        /// 将类型名称转换为SqlDbType
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns></returns>
        protected SqlDbType ConvertSqlDbType(string typeName)
        {
            if (m_SqlDbTypes == null) m_SqlDbTypes = Enum.GetNames(typeof(SqlDbType));
            foreach (string n in m_SqlDbTypes)
            {
                if (n.ToLower() == typeName.ToLower())
                {
                    return (SqlDbType)Enum.Parse(typeof(SqlDbType), n);

                }
            }
            return default(SqlDbType);
        }
    }
}
