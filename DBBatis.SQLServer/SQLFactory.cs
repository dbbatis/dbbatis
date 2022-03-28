using DBBatis.Action;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace DBBatis.SQLServer
{
    public class SQLFactory : Factory
    {
      
      
        

        

        public override ColumnProperties CreateColumnProperties()
        {
            return new SQLColumnProperties(true);
        }

        


        public override StateManager CreateStateManager()
        {
            return new SQLStateManager();
        }

        
    }
}
