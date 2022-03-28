using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBBatis.Action
{
    /// <summary>
    /// 值处理类
    /// </summary>
    public class ActionData
    {
        protected ActionData()
        {
        }
        /// <summary>
        /// 上层
        /// </summary>
        public ActionData Parent { get; set; }
        public Web.ContextHandlerBase ContextHandler { get; set; }
        Dictionary<string, string> _Dictionary = null;
        Dictionary<string, string> _OtherVaues = new Dictionary<string, string>();

        Hashtable _Hashtable = null;
        /// <summary>
        /// 当前语言
        /// </summary>
        public string Language { get; set; }
        public ActionData(Dictionary<string, string> keyvalues)
        {
            _Dictionary = keyvalues;
        }
        public ActionData(Hashtable keyvalues)
            : this(keyvalues, null)
        {

        }
        public ActionData(Hashtable keyvalues, ActionData parent)
        {
            this.Parent = parent;
            Init(keyvalues);
        }
        /// <summary>
        /// JSON字符串
        /// </summary>
        /// <param name="jsonString"></param>
        public ActionData(string json)
        {
            Init(json);
        }
        /// <summary>
        /// 接受数据流
        /// </summary>
        /// <param name="stream"></param>
        public ActionData(Stream stream)
        {
            Init(stream);
        }
        protected void Init(Stream stream)
        {
            if (stream != null && stream.Length > 0)
            {
                byte[] bytes = new byte[stream.Length];
                if (stream.Position != 0)
                {
                    stream.Position = 0;
                }
                stream.Read(bytes, 0, bytes.Length);
                string v = Encoding.UTF8.GetString(bytes);
                this.Init(v);
            }

        }
        protected void Init(Hashtable hs)
        {
            _Hashtable = hs;
        }
        protected void Init(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                object o = JSON.JSON.Decode(json);
                if (o.GetType() != typeof(Hashtable))
                {
                    throw new Exception(String.Format("传入json字符串格式必须为对象.{0}", json));
                }
                Init((Hashtable)o);
            }

        }
        /// <summary>
        /// 是否为Hashtable类型的值
        /// </summary>
        public bool IsHashTable
        {
            get { return _Hashtable != null; }
        }
        /// <summary>
        /// OtherValue优先级最高
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddOtherValues(string key, string value)
        {
            _OtherVaues.Add(key, value);
        }
        /// <summary>
        /// 获取HashObjectValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Hashtable GetHashTableValue(string key)
        {
            if (_Hashtable != null)
            {
                object v = _Hashtable[key];
                if (v != null) return (Hashtable)v;
                return null;
            }
            return null;
        }
        /// <summary>
        /// 获取HashObjectValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ArrayList GetArrayListValue(string key)
        {
            if (_Hashtable != null)
            {
                object v = _Hashtable[key];
                if (v != null) return (ArrayList)v;
                return null;
            }
            return null;
        }
        /// <summary>
        /// 获取传入的Hashtable
        /// </summary>
        /// <returns></returns>
        public Hashtable GetHashTable()
        {
            return _Hashtable;
        }
        protected virtual string GetValue(string key)
        {
            return string.Empty;
        }
        public virtual string this[string key]
        {
            get
            {
                if (_OtherVaues.ContainsKey(key))
                {
                    return _OtherVaues[key];
                }
                if (_Dictionary != null)
                {
                    if (_Dictionary.ContainsKey(key))
                    {
                        return _Dictionary[key];
                    }
                    else
                        return string.Empty;
                }
                else if (_Hashtable != null)
                {
                    object v = _Hashtable[key];
                    if (v != null)
                    {
                        Type t = v.GetType();
                        if (typeof(ArrayList).IsAssignableFrom(t)
                            || typeof(Hashtable).IsAssignableFrom(t))
                        {
                            throw new Exception(string.Format("当前Key:{0}值为{1}类型,请使用GetHashTableValue方法来取值.", key, t.Name));
                        }

                        return v.ToString();
                    }
                    else
                    {
                        if (this.Parent != null)
                        {
                            return this.Parent[key];
                        }
                    }
                    return null;
                }
                else
                {
                    return GetValue(key);
                    //throw new ApplicationException("请重写此方法以实现取值.");
                }
            }
        }

        public virtual ICollection GetKeys()
        {
            if (_Dictionary != null)
            {
                return _Dictionary.Keys;
            }
            else if (_Hashtable != null)
            {
                return _Hashtable.Keys;
            }
            else
            {
                throw new ApplicationException("请重写此方法以实现取值.");
            }
        }

    }
}
