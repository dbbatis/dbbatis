
using System.Data.Common;
using System.Collections.Specialized;

namespace DBBatis.Action
{
    public class ActionCommand
    {
        public ActionCommand()
        {
            ParameterDescrtions = new StringDictionary();
        }
        /// <summary>
        /// 命令
        /// </summary>
        public DbCommand Command { get; set; }
        /// <summary>
        /// 参数映射
        /// </summary>
        public Mapping ParameterMapping { get; set; }
        /// <summary>
        /// 对应参数描述
        /// </summary>
        public StringDictionary ParameterDescrtions { get; set; }
    }
}
