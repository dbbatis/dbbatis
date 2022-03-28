using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace DBBatis.Action
{
    public delegate Factory CreateFactory();
    public delegate DbConfig CreateDbConfig(ActionDbType dbType, string cnnstring);

    public abstract class Factory
    {
        static CreateFactory _CreateFactoryHandler;
        public static CreateFactory CreateFactoryHandler
        {
            get
            {
                if (_CreateFactoryHandler == null)
                    _CreateFactoryHandler = DefaultFactoryBuilder;

                return _CreateFactoryHandler;
            }
            set { _CreateFactoryHandler = value; }
        }
        static Factory DefaultFactoryBuilder()
        {
            if (DBassembly != null)
            {
                return (Factory)DBassembly.CreateInstance("DBBatis.SQLServer.SQLFactory");
            }
            return null;
        }

        public abstract StateManager CreateStateManager();
        public abstract ColumnProperties CreateColumnProperties();


        static CreateDbConfig _CreateDbConfigHandler;
        static System.Reflection.Assembly _DBassembly;
        static System.Reflection.Assembly DBassembly
        {
            get
            {
                if (_DBassembly != null) return _DBassembly;
                string basepath = AppDomain.CurrentDomain.RelativeSearchPath;
                if (string.IsNullOrEmpty(basepath))
                {
                    basepath = AppDomain.CurrentDomain.BaseDirectory;
                }
                string file = string.Format("{0}\\DBBatis.SQLServer.dll", basepath);
                if (System.IO.File.Exists(file))
                {

                    System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFile(file);
                    _DBassembly = assembly;
                }
                return _DBassembly;

            }
        }
        
        public static Factory CreateFactory()
        {
            if (CreateFactoryHandler == null)
            {
                throw new ApplicationException("请指定委托:Factory.CreateFactoryHandler");
            }
            return CreateFactoryHandler();
        }

    }
}
