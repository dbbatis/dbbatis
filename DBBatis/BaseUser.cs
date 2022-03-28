using System;
using System.Data;


namespace DBBatis
{
    /// <summary>
    /// 获取用户委托
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    public delegate BaseUser GetBaseUserHandler(object session);
    /// <summary>
    /// 设置当前用户信息
    /// </summary>
    /// <param name="session">当前session</param>
    /// <param name="user">用户</param>
    public delegate void SetBaseUserHandler(object session, BaseUser user);

    /// <summary>
    /// 用户信息类
    /// </summary>
    public class BaseUser
    {
        /// <summary>
        /// 获取用户信息委托
        /// </summary>
        public static GetBaseUserHandler GetBaseUserHandler;
        /// <summary>
        /// 设置用户信息委托
        /// </summary>
        public static SetBaseUserHandler SetBaseUserHandler;
        /// <summary>
        /// 获取可见基础数据
        /// </summary>
        public static System.Func<int,string, string> GetViewBaseID;
        /// <summary>
        /// 用户ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID
        {
            get;
            set;
        }
        /// <summary>
        /// 所属部门ID
        /// </summary>
        public int DepartmentID { get; set; }
        /// <summary>
        /// 用户编码
        /// </summary>
        public string Code
        {
            get;
            set;
        }
        /// <summary>
        /// 用户名称
        /// </summary>
        public string Name
        {
            get;
            set;
        }
    }
}
