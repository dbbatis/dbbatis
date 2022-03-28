using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using DBBatis.Action;

namespace DBBatis.SQLServer
{
    class SQLColumnProperties : ColumnProperties
    {
        public SQLColumnProperties(bool isSQLServer) : base(isSQLServer)
        {
        }

        public override ColumnProperties GetColumns(DbConfig db, string tableName)
        {
            ColumnProperties properties = new SQLColumnProperties(true);
            SqlCommand cmmd = new SqlCommand();
            cmmd.CommandText = "SELECT A.[Name] AS ColName, A.Colstat,D.[Name] AS ColType, A.Length AS ColLength , C.[Value] AS ColDescription,a.IsNullable" +
                " FROM SysColumns A WITH(NOLOCK)" +
                " INNER JOIN SysObjects B WITH(NOLOCK) ON B.[ID] = A.[ID]" +
                " LEFT JOIN sys.extended_properties C WITH(NOLOCK) ON C.[Name] = 'MS_Description' AND C.[MAJOR_ID] = A.[ID] AND C.minor_id = A.ColID" +
                " LEFT JOIN sys.extended_properties L WITH(NOLOCK) ON L.[Name] = 'MS_Lable' AND L.[class] = A.[ID] AND L.minor_id = A.ColID" +
                " INNER JOIN SysTypes D WITH(NOLOCK) ON D.XType = A.XType and D.xtype=D.xusertype" +
                " WHERE (B.XType = 'U' OR B.XType = 'V') and B.[Name] =@TableName" +
                " ORDER BY A.[colid] ";
            cmmd.Parameters.AddWithValue("@TableName", tableName);
            DataTable dt = db.ExecuteDataTable(cmmd);
            foreach(DataRow row in dt.Rows)
            {
                ColumnProperty p = new ColumnProperty();
                p.Name = row["ColName"].ToString();
                p.SqlDbType = ConvertSqlDbType(row["ColType"].ToString());
                p.Colstat = Int16.Parse(row["Colstat"].ToString());
                p.Length = Int16.Parse(row["ColLength"].ToString());
                p.Description = row["ColDescription"].ToString();
                p.IsNullable = row["IsNullable"].ToString() == "1" ? true : false;
                properties.Add(p);
            }
            for (int i = 0; i < properties.Count; i++)
            {
                properties[i].DbType = GetDbType(properties[i].SqlDbType);
            }
            return properties;
        }

        private DbType GetDbType(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.BigInt:
                    return DbType.Int64;
                case SqlDbType.Binary:
                    return DbType.Binary;
                case SqlDbType.Bit:
                    return DbType.Boolean;
                case SqlDbType.Char:
                    return DbType.AnsiStringFixedLength;
                case SqlDbType.DateTime:
                    return DbType.DateTime;
                case SqlDbType.Decimal:
                    return DbType.Decimal;
                case SqlDbType.Float:
                    return DbType.Double;
                case SqlDbType.Image:
                    return DbType.Binary;
                case SqlDbType.Int:
                    return DbType.Int32;
                case SqlDbType.Money:
                    return DbType.Currency;
                case SqlDbType.NChar:
                    return DbType.StringFixedLength;
                case SqlDbType.NText:
                    return DbType.String;
                case SqlDbType.NVarChar:
                    return DbType.String;
                case SqlDbType.Real:
                    return DbType.Single;
                case SqlDbType.UniqueIdentifier:
                    return DbType.Guid;
                case SqlDbType.SmallDateTime:
                    return DbType.DateTime;
                case SqlDbType.SmallInt:
                    return DbType.Int16;
                case SqlDbType.SmallMoney:
                    return DbType.Currency;
                case SqlDbType.Text:
                    return DbType.AnsiString;
                case SqlDbType.Timestamp:
                    return DbType.Binary;
                case SqlDbType.TinyInt:
                    return DbType.Byte;
                case SqlDbType.VarBinary:
                    return DbType.Binary;
                case SqlDbType.VarChar:
                    return DbType.AnsiString;
                case SqlDbType.Variant:
                    return DbType.Object;
                case SqlDbType.Xml:
                    return DbType.Xml;
                case SqlDbType.Udt:
                    return DbType.Object;
                case SqlDbType.Structured:
                    return DbType.Object;
                case SqlDbType.Date:
                    return DbType.Date;
                case SqlDbType.Time:
                    return DbType.Time;
                case SqlDbType.DateTime2:
                    return DbType.DateTime2;
                case SqlDbType.DateTimeOffset:
                    return DbType.DateTimeOffset;
            }
            return DbType.String;
        }


    }
}
