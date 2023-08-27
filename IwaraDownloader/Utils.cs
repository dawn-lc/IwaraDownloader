using IwaraDownloader;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IwaraDownloader.Config;

namespace Dawnlc.Module
{
    public class Utils
    {
        public static class Env
        {
            private static readonly Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new(1,0,0);
            public static readonly string Name = "IwaraDownloader";
            public static readonly string Path = AppDomain.CurrentDomain.BaseDirectory;
            public static readonly string Developer = "dawn-lc";
            public static readonly string HomePage = $"https://github.com/{Developer}/{Name}";
            public static readonly int[] Version = new int[] { version.Major, version.Minor, version.Build };
            public static readonly HttpHeaders Headers = new HttpClient().DefaultRequestHeaders;
            public static Config MainConfig { get; set; } = DeserializeJSONFile<Config>(System.IO.Path.Combine(Path, "config.json"));
        }
        public static JsonSerializerOptions JsonOptions { get; set; } = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public class DownloadOptions
        {
            public CookieContainer? Cookies { get; set; }
            public WebProxy? Proxy { get; set; }
            public HttpHeaders? Headers { get; set; }
        }

        public class AuthenticationException : Exception
        {
            public AuthenticationType AuthenticationType { get; set; }
            public AuthenticationException(AuthenticationType type, string message) : base(message)
            {
                AuthenticationType = type;
            }
        }

        public static void Log(string? value)
        {
            Console.WriteLine($"[{DateTime.Now}] I {value}");
            //AnsiConsole.MarkupLine($"[bold][[{DateTime.Now}]] [lime]I[/][/] {Markup.Escape(value ?? "null")}");
        }
        public static void Warn(string value)
        {
            Console.WriteLine($"[{DateTime.Now}] W {value}");
            //AnsiConsole.MarkupLine($"[bold][[{DateTime.Now}]] [orangered1]W[/][/] {Markup.Escape(value)}");
        }
        public static void Error(string value)
        {
            Console.WriteLine($"[{DateTime.Now}] E {value}");
            //AnsiConsole.MarkupLine($"[bold][[{DateTime.Now}]] [red]E[/][/] {Markup.Escape(value)}");
        }


        public static bool IsValidPath(string path)
        {
            try
            {
                return !(string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || !Directory.Exists(Path.GetPathRoot(path)) || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<T> DeserializeJSONFileAsync<T>(string path) where T : new()
        {
            T? data;
            return File.Exists(path) ? (data = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), JsonOptions)) != null ? data : new T() : new T();
        }

        public static T DeserializeJSONFile<T>(string path) where T : new()
        {
            T? data;
            return File.Exists(path) ? (data = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)) != null ? data : new T() : new T();
        }
    }
}
