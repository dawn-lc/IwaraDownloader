using System.Text.Json.Serialization;
using static Dawnlc.Module.Utils;

namespace IwaraDownloader
{
    public struct Config
    {
        
        public Config()
        {
        }

        public enum AuthenticationType
        {
            None,
            Token
        }
        public int Port { get; set; } = 6800;
        public int ConcurrentDownloads { get; set; } = 4;
        public long BufferBlockSize { get; set; } = 16000000;
        public int ParallelCount { get; set; } = 8;
        public string WebRootPath { get; set; } = Path.Combine(Env.Path, "root");
        public AuthenticationType AuthType { get; set; } = AuthenticationType.None;
        public bool IsHTTPS { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CertPath { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? KeyPath { get; set; }
    }
}
