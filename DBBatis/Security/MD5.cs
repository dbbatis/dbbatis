using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace DBBatis.Security
{

    /// <summary>
    /// MD5
    /// </summary>
    public class MD5
    {
        /// <summary>  
        /// 生成MD5码 
        /// </summary>  
        /// <param name="original">数据源</param>  
        /// <returns>MD5码</returns>  
        public static byte[] MakeMD5(byte[] original)
        {
            using (MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider())
            {
                byte[] keyhash = hashmd5.ComputeHash(original);
                return keyhash;
            }
        }
        /// <summary>
        /// 字符串转base64
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToBase64String(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            //转成 Base64 形式的 System.String  
            str = Convert.ToBase64String(b);
            return str;

        }
        /// <summary>
        /// base64还原字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FromBase64(string str)
        {
            byte[] c = Convert.FromBase64String(str);
            str = Encoding.Default.GetString(c);
            return str;
        }
        /// <summary>
        /// //获取流的MD5码
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <returns>MD5码</returns>
        public static byte[] MakeMD5(Stream stream)
        {
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] hash = md5.ComputeHash(stream);
                return hash;
            }
        }
        /// <summary>
        /// 将字节转换为16进制字符
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] bytes)
        {
            byte[] buffer = MakeMD5(bytes);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buffer.Length; i++)
            {
                builder.Append(buffer[i].ToString("x2"));
            }
            return builder.ToString();
        }
        /// <summary>
        /// 获取字符串MD5值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MakeMD5(string value)
        {
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(value);
            byte[] md5bytes = MakeMD5(bytes);
            return ByteArrayToHexString(md5bytes);
        }
        /// <summary>
        /// 使用默认密钥字符串解密string,
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string DecryptByDefaultKey(string original)
        {
            return Decrypt(original, Action.MainConfig.EncryptionKey);
        }
        /// <summary>
        /// 使用默认密钥字符串加密string,
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string EncryptByDefaultKey(string original)
        {
            return Encrypt(original, Action.MainConfig.EncryptionKey);
        }
        #region 使用 给定密钥字符串 加密/解密string
        /// <summary>  /// 使用给定密钥字符串加密string  
        /// </summary>  
        /// <param name="original">原始文字</param>  
        /// <param name="key">密钥</param>  
        /// <param name="encoding">字符编码方案</param>  
        /// <returns>密文</returns>  
        public static string Encrypt(string original, string key)
        {
            byte[] buff = System.Text.Encoding.Default.GetBytes(original);
            byte[] kb  = System.Text.Encoding.Default.GetBytes(key);
            return Convert.ToBase64String(Encrypt(buff, kb));
        }
        /// <summary>  /// 使用给定密钥字符串解密string  
        /// </summary>  
        /// <param name="original">密文</param>  
        /// <param name="key">密钥</param> 
        /// <returns>明文</returns>  
        public static string Decrypt(string original, string key)
        {
            return Decrypt(original, key, Encoding.Default);
        }
        /// <summary>  
        /// 使用给定密钥字符串解密string,返回指定编码方式明文  
        /// </summary>  
        /// <param name="encrypted">密文</param>  
        /// <param name="key">密钥</param>  
        /// <param name="encoding">字符编码方案</param>  
        /// <returns>明文</returns>  
        public static string Decrypt(string encrypted, string key, Encoding encoding)
        {
            byte[] buff = Convert.FromBase64String(encrypted);
            byte[] kb = System.Text.Encoding.Default.GetBytes(key);
            return encoding.GetString(Decrypt(buff, kb));
        }
        #endregion
        /// <summary>  
        /// 使用给定密钥加密  
        /// </summary>  
        /// <param name="original">明文</param>  
        /// <param name="key">密钥</param> 
        /// <returns>密文</returns>  
        public static byte[] Encrypt(byte[] original, byte[] key)
        {
            TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
            des.Key = MakeMD5(key);
            des.Mode = CipherMode.ECB;
            return des.CreateEncryptor().TransformFinalBlock(original, 0, original.Length);
        }
        /// <summary>  
        /// 使用给定密钥解密数据  
        /// </summary>  
        /// <param name="encrypted">密文</param>  
        /// <param name="key">密钥</param>  
        /// <returns>明文</returns>  
        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
            des.Key = MakeMD5(key);
            des.Mode = CipherMode.ECB;
            return des.CreateDecryptor().TransformFinalBlock(encrypted, 0, encrypted.Length);
        }
    }
}
