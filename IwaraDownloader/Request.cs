using System.Text.Json;

namespace Dawnlc.Module
{
    public enum RequestCode
    {
        Add,
        State
    }
    public struct Request
    {
        public int[] Ver
        {
            get
            {
                return ver ?? new int[] { 0, 0, 0 };
            }
            set
            {
                ver = value;
            }
        }
        private int[]? ver;

        public string? Token
        {
            get
            {
                return token;
            }
            set
            {
                token = value;
            }
        }
        private string? token;

        public RequestCode Code
        {
            get
            {
                return code ?? throw new ArgumentNullException("Request Code is null");
            }
            set
            {
                code = value;
            }
        }
        private RequestCode? code;
        
        public JsonDocument Data
        {
            get
            {
                return data ?? throw new ArgumentNullException("Request Data is null");
            }
            set
            {
                data = value;
            }
        }
        private JsonDocument? data;

    }

}

