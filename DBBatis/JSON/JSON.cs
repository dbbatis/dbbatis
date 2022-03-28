using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Linq;

namespace DBBatis.JSON
{
    public static class JSON
    {
        public static string DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        public static string Encode(object o)
        {
            var jsonSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            IsoDateTimeConverter dt = new IsoDateTimeConverter();
            dt.DateTimeFormat = DateTimeFormat;

            jsonSetting.NullValueHandling = NullValueHandling.Ignore;

            jsonSetting.Converters.Add(dt);

            return Encode(o, jsonSetting);
            
        }
        public static string EncodeIncludeNull(object o)
        {
            var jsonSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include };
            IsoDateTimeConverter dt = new IsoDateTimeConverter
            {
                DateTimeFormat = DateTimeFormat
            };

            jsonSetting.NullValueHandling = NullValueHandling.Include;

            jsonSetting.Converters.Add(dt);

            return Encode(o, jsonSetting);

        }
        public static string Encode(object o,JsonSerializerSettings jsonSetting)
        {
            if (o == null || o.ToString() == "null") return null;

            if (o != null && (o.GetType() == typeof(String) || o.GetType() == typeof(string)))
            {
                return o.ToString();
            }
            

            string v = JsonConvert.SerializeObject(o, Formatting.Indented, jsonSetting);
            return v.Replace(": null", ": \"\"");

        }
        public static object Decode(string json)
        {
            if (String.IsNullOrEmpty(json)
                || json=="null") return "";
            object o = JsonConvert.DeserializeObject(json);
            if (o.GetType() == typeof(String) || o.GetType() == typeof(string))
            {
                o = JsonConvert.DeserializeObject(o.ToString());
            }
            object v = ToObject(o);
            return v;
        }
        public static object Decode(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }
        private static object ToObject(object o)
        {
            if (o == null) return null;

            if (o.GetType() == typeof(string))
            {
                //判断是否符合2010-09-02T10:00:00的格式
                string s = o.ToString();
                if (s.Length == 19 && s[10] == 'T' && s[4] == '-' && s[13] == ':')
                {
                    o = System.Convert.ToDateTime(o);
                }
            }
            else if (o is JObject)
            {
                JObject jo = o as JObject;

                Hashtable h = new Hashtable();

                foreach (KeyValuePair<string, JToken> entry in jo)
                {
                    h[entry.Key] = ToObject(entry.Value);
                }

                o = h;
            }
            else if (o is IList)
            {

                ArrayList list = new ArrayList();
                list.AddRange((o as IList));
                int i = 0, l = list.Count;
                for (; i < l; i++)
                {
                    list[i] = ToObject(list[i]);
                }
                o = list;

            }
            else if (typeof(JValue) == o.GetType())
            {
                JValue v = (JValue)o;
                o = ToObject(v.Value);
            }
            else
            {
            }
            return o;
        }

        public static ArrayList DataTable2ArrayList(System.Data.DataTable data)
        {
            ArrayList array = new ArrayList();
            for (int i = 0; i < data.Rows.Count; i++)
            {
                System.Data.DataRow row = data.Rows[i];

                Hashtable record = new Hashtable();
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    object cellValue = row[j];
                    if (cellValue.GetType() == typeof(DBNull))
                    {
                        cellValue = null;
                    }
                    record[data.Columns[j].ColumnName] = cellValue;
                }
                array.Add(record);
            }
            return array;
        }
        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string ToJson(this object o)
        {
            return JSON.Encode(o);
        }
        public static T ToObject<T>(this string s)
        {
            return (T)JSON.Decode(s);
        }
    }
    
}