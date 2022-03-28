using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using DBBatis.Action;
using System.Data.Common;

namespace DBBatis.SQLServer
{
    class SQLStateManager: StateManager
    {
        public override DbCommand GetStateOperationCommand(StateOperation StateOperation,string tableName,string PKField)
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
//            //设置Parameter
//            try
//            {
//                DataTable schema = GetTableSchema(MainConfig.DbConfigs[pageConfig.DbFlag], pageConfig.Name);
//                SetParamertType(p, schema);
//            }
//            catch (Exception err)
//            {
//#if (DEBUG)
//                {
//                    throw new ApplicationException("GetStateCommandByPageConfig未能获取TableSchemaCache", err);
//                }
//#endif
//            }
            return cmmd;
        }
        
    }
}
