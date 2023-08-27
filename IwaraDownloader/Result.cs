using System.Text.Json.Serialization;

namespace Dawnlc.Module
{
    public enum ResultCode
    {
        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized = -1,
        /// <summary>
        /// 请求成功
        /// </summary>
        OK,
        /// <summary>
        /// 客户端请求的语法错误，服务器无法理解(错误请求)
        /// </summary>
        BadRequest,
        /// <summary>
        /// 服务器理解请求客户端的请求，但是拒绝执行此请求(没有权限)
        /// </summary>
        Forbidden,
        /// <summary>
        /// 请求要求用户的身份认证(未登录)
        /// </summary>
        Unauthorized,
        /// <summary>
        /// 未被处理的错误(内部错误)
        /// </summary>
        Unhandled,
        /// <summary>
        /// 已存在
        /// </summary>
        Exists,
        /// <summary>
        /// 不存在
        /// </summary>
        NotFound,
        /// <summary>
        /// 路径不存在
        /// </summary>
        PathNotFound

    }
    public class Result
    {
        private int? ver;
        public int Ver
        {
            get
            {
                return ver ?? 1;
            }
            set
            {
                ver = value;
            }
        }
        private ResultCode? code;
        public ResultCode Code
        {
            get
            {
                return code ?? ResultCode.Uninitialized;
            }
            set
            {
                code = value;
            }
        }
        private string? msg;
        public string Msg
        {
            get
            {
                return msg ?? Code.ToString();
            }
            set
            {
                msg = value;
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }
}
