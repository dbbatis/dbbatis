using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DBBatis.Common
{
    public static class Convert
    {
        /// <summary>
        /// 将数据行转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="ignoreType">是否忽略类型</param>
        /// <remarks>忽略类型将影响性能</remarks>
        /// <returns></returns>
        public static T ToObject<T>(this DataRow r,bool ignoreType)
        {
            
            T t = Activator.CreateInstance<T>();
            PropertyInfo[] ps = t.GetType().GetProperties();
            
            foreach (var item in ps)
            {
                if (r.Table.Columns.Contains(item.Name))
                {
                    object v = r[item.Name];
                    if (v.GetType() == typeof(System.DBNull))
                        v = null;
                    else
                    {
                        if (ignoreType)
                        {
                            if (item.PropertyType != v.GetType())
                            {
                                Type ptype = item.PropertyType;
                                if (ptype == typeof(string))
                                {
                                    if (v.GetType() == typeof(DateTime))
                                    {
                                        v = ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    else
                                    {
                                        v = v.ToString();
                                    }
                                }
                                else if (ptype == typeof(int))
                                {
                                    int tempv = 0;
                                    if (int.TryParse(v.ToString(), out tempv))
                                    {
                                        v = tempv;
                                    }
                                }
                                else if (ptype == typeof(long))
                                {
                                    long tempv = 0;
                                    if (long.TryParse(v.ToString(), out tempv))
                                    {
                                        v = tempv;
                                    }
                                }
                                else if (ptype == typeof(short))
                                {
                                    short tempv = 0;
                                    if (short.TryParse(v.ToString(), out tempv))
                                    {
                                        v = tempv;
                                    }
                                }

                            }
                        }
                    }

                    item.SetValue(t, v, null);
                }
            }
            return t;
        }
        /// <summary>
        /// 将数据表转换为对象集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="ignoreType"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable dt, bool ignoreType)
        {
            
            List<T> list = new List<T>();
            foreach(DataRow r in dt.Rows)
            {
                list.Add(r.ToObject<T>(ignoreType));
            }
            return list;
        }
    }
}
