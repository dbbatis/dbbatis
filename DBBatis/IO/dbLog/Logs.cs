using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DBBatis.IO.Log
{
    public class Logs
    {
        /// <summary>
        /// 路径
        /// </summary>
        public static string Path { get; set; }
        static Dictionary<string, FileLog> FileLogs = new Dictionary<string, FileLog>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static FileLog GetLog(string fileName)
        {
            lock (FileLogs)
            {
                if (FileLogs.ContainsKey(fileName))
                {
                    return FileLogs[fileName];
                }
                else
                {
                    if (string.IsNullOrEmpty(Path))
                    {
                        Path = string.Format("{0}\\Log\\", AppDomain.CurrentDomain.BaseDirectory);
                    }
                    string tempname = fileName;
                    if(!fileName.EndsWith(".txt", StringComparison.CurrentCultureIgnoreCase))
                    {
                        tempname = string.Format("{0}.txt", fileName);

                    }
                    FileLog Log = new FileLog(Path, tempname);
                    FileLogs.Add(fileName, Log);
                    return Log;
                }
            }
        }

    }
}
