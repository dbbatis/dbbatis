using DBBatis.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace DBBatis.IO.Log
{

    /// <summary>
    /// 文件日志
    /// </summary>
    public class FileLog
    {
        internal static ErrorCommand LastErrorCommand = null;

        /// <summary>
        /// 数据库
        /// </summary>
        public string DbFlag { get; set; }
        private string m_FileName;
        /// <summary>
        /// 文件大小
        /// </summary>
        private short m_Size;
        /// <summary>
        /// 构造体
        /// </summary>
        /// <param name="filePath">路径</param>
        /// <param name="fileName">文件名</param>
        /// <remarks>默认文件大小为4M</remarks>
        public FileLog(string filePath, string fileName) : this(filePath, fileName, 4)
        {
            
        }
        /// <summary>
        /// 构造体
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileName">文件名</param>
        /// <param name="size">文件大小(M),当文件大于size时,系统会自动保存,然后新建一个文件</param>
        public FileLog(string filePath, string fileName, short size)
        {
            string filefullName = string.Format("{0}\\{1}", filePath.TrimEnd('\\'), fileName);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            m_FileName = filefullName;
            m_Size = size;

            //创建文件
            CreateFile();
        }
        /// <summary>
        /// 创建日志文件
        /// </summary>
        private void CreateFile()
        {
            if (!File.Exists(m_FileName))
            {
                TextWriter tw = File.CreateText(m_FileName);

                tw.WriteLine("#################################################################");
                tw.WriteLine("#");
                tw.WriteLine(string.Format("# IniFile Create : {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                tw.WriteLine("#");
                tw.WriteLine("# Author: DBBatis@163.com");
                tw.WriteLine("#");
                tw.WriteLine("################################################################");
                tw.WriteLine("");
                tw.Close();
            }
        }
        /// <summary>
        /// 添加日志信息
        /// </summary>
        /// <param name="value"></param>
        /// <param name="detailInfo">详细信息</param>
        public void AddLogInfo(string value, string detailInfo)
        {
            Redo:
            FileInfo f = new FileInfo(m_FileName);
            lock (f)
            {
                //检测是否大于指定文件大小
                if (f.Length > (m_Size * 1048576))
                {
                    f.CopyTo(string.Format("{0}\\{1}", f.DirectoryName, DateTime.Now.ToString("yyyyMMddHHmmss")));
                    f.Delete();
                    CreateFile();
                    goto Redo;
                }

                using (TextWriter tw = TextWriter.Synchronized(f.AppendText()))
                {
                    lock (tw)
                    {
                        tw.WriteLine(string.Format("[时间]  {0}", DateTime.Now.ToString()));
                        tw.WriteLine(value);
                        if (detailInfo != null)
                        {
                            tw.WriteLine("详细信息:");
                            tw.WriteLine(detailInfo);
                        }
                        tw.Close();
                    }
                }


            }
        }
        /// <summary>
        /// 添加日志信息
        /// </summary>
        /// <param name="value"></param>
        public void AddLogInfo(string value)
        {
            AddLogInfo(value, null);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="TipInfo"></param>
        /// <param name="cmmd"></param>
        /// <param name="err"></param>
        public void Write(string TipInfo, IDbCommand cmmd, Exception err,DbConfig dbConfig)
        {
            System.Diagnostics.Debug.WriteLine(TipInfo);
            if (cmmd != null)
            {
                LastErrorCommand = new ErrorCommand(cmmd, err, dbConfig);
                if (string.IsNullOrEmpty(TipInfo))
                {
                    TipInfo = "错误命令";
                }
                this.AddLogInfo(TipInfo,dbConfig.GetCommandString(cmmd));
                this.AddLogInfo(TipInfo, err.ToString());
                System.Diagnostics.Debug.WriteLine(cmmd.CommandText);
            }
            else
            {
                this.AddLogInfo(TipInfo, err.ToString());
            }
            System.Diagnostics.Debug.WriteLine(err.ToString());
        }

    }// end class
}
