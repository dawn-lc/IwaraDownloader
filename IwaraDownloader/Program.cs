using Dawnlc.Module;
using Microsoft.AspNetCore.StaticFiles;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Dawnlc.Module.Utils;

namespace IwaraDownloader
{
    public partial class Program
    {
        public static Database DB { get; set; } = new();
        private static ConcurrentDictionary<string, DownloadTask> DownloadQueue = new();
        public static WebApplicationBuilder Initialize()
        {
            AnsiConsole.Write(Figgle.FiggleFonts.Standard.Render(Env.Name));
            DirectoryInfo WebRootPath = new(Env.MainConfig.WebRootPath);
            if (!WebRootPath.Exists)
            {
                Directory.CreateDirectory(WebRootPath.FullName);
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                WebRootPath = WebRootPath.FullName
            });
            builder.Services.AddMemoryCache();
            builder.Logging.ClearProviders();

            builder.Environment.ApplicationName = Env.Name;
            builder.Environment.ContentRootPath = Env.Path;

            builder.WebHost.UseKestrel();
            builder.WebHost.UseQuic();


            if (Env.MainConfig.IsHTTPS)
            {
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(httpsOptions =>
                    {

                        FileInfo cert = new(Env.MainConfig.CertPath ?? Path.Combine(Env.Path, "cert.pem"));
                        FileInfo key = new(Env.MainConfig.KeyPath ?? Path.Combine(Env.Path, "key.pem"));
                        if (!cert.Exists || !key.Exists)
                        {
                            throw new FileNotFoundException($"{(cert.Exists ? $"Not found {cert.FullName}{Environment.NewLine}" : "")}{(key.Exists ? $"Not found {key.FullName}" : "")}");
                        }
                        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(cert.FullName, key.FullName);

                    });
                });
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", policy =>
                {
                    policy.WithOrigins("*");
                });
            });
            builder.Services.AddControllers();

            if (!File.Exists(Path.Combine(Env.Path, $"config.json")))
            {
                File.WriteAllText(Path.Combine(Env.Path, $"config.json"), JsonSerializer.Serialize(Env.MainConfig, JsonOptions));
            }

            return builder;
        }

        public static async Task Main()
        {
            try
            {
                WebApplication app = Initialize().Build();
                app.UseStaticFiles();
                app.Urls.Add($"http://0.0.0.0:{Env.MainConfig.Port}");
                app.Urls.Add($"http://[::]:{Env.MainConfig.Port}");
                if (Env.MainConfig.IsHTTPS)
                {
                    app.UseHttpsRedirection();
                    app.Urls.Add($"https://0.0.0.0:{Env.MainConfig.Port}");
                    app.Urls.Add($"https://[::]:{Env.MainConfig.Port}");
                }
                app.UseCors("AllowAllOrigins");

                app.MapPost("/jsonrpc", RPC);
                app.MapGet("/jsonrpc", RPC);
                app.MapGet("/playlist.xspf",(HttpRequest Request, HttpResponse Response) => PlayList(Request, Response));
                app.MapGet("/{ID:guid}.mp4", (HttpRequest Request, HttpResponse Response, string ID) => FileServer(Request, Response,ID));

                
                List<Video> ErrorFileList = new();
                foreach (var item in DB.Videos)
                {
                    item.Exists = File.Exists(item.Path);
                    if (!item.Exists)
                    {
                        ErrorFileList.Add(item);
                        Warn($"{item.Name} �ļ���ʧ!");
                        continue;
                    }
                    using (FileStream file = File.OpenRead(item.Path))
                    {
                        if (item.Size != file.Length)
                        {
                            ErrorFileList.Add(item);
                            Warn($"{item.Name} �ļ�У��ʧ��!");
                            continue;
                        }
                        if (!(await file.SHA256HashAsync()).SequenceCompare(item.Hash))
                        {
                            ErrorFileList.Add(item);
                            Warn($"{item.Name} �ļ�У��ʧ��!");
                            continue;
                        }
                    }
                }
                foreach (var item in ErrorFileList)
                {
                    DB.Videos.Remove(item);
                    Log($"{item.Name} ���Ƴ�");
                }
                Log($"�������");
                app.Run();
            }
            catch (Exception e)
            {
                Error($"{e.Message}");
                Console.WriteLine("������˳�...");
                Console.ReadKey();
            }
            
        }
        public static IResult FileServer(HttpRequest Request, HttpResponse Response, string ID)
        {
            Log($"{Request.Method} {Request.Path} {Request.ContentLength}");

            if (!DB.Videos.Any(i => i.ID == ID))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.BadRequest();
            }
            Video video = DB.Videos.First(i => i.ID == ID);
            FileInfo file = new(video.Path);
            if (!file.Exists)
            {
                video.Exists = false;
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.BadRequest();
            }
            new FileExtensionContentTypeProvider().TryGetContentType(file.FullName, out string? contentType);

            return Results.Stream(File.OpenRead(file.FullName), contentType, enableRangeProcessing: true);

        }
        public static async Task PlayList(HttpRequest Request, HttpResponse Response)
        {
            Log($"{Request.Method} {Request.Path} {Request.ContentLength}");
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/xml";
            string orderby = Request.Query.ContainsKey("orderby") ? Request.Query["orderby"]! : "uploadTime";
            IEnumerable<Video> OrderList = orderby.ToLower() switch
            {
                "name" => DB.Videos.Where(i => !i.Exists || !Request.Query.ContainsKey("key") || i.Name.Contains(Request.Query["key"]!, StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(p => p.Name),
                "author" => DB.Videos.Where(i => !i.Exists || !Request.Query.ContainsKey("key") || (i.Author.Contains(Request.Query["key"]!, StringComparison.CurrentCultureIgnoreCase) || i.Alias.Contains(Request.Query["key"]!, StringComparison.CurrentCultureIgnoreCase))).OrderByDescending(p => p.Author),
                "tag" => DB.Videos.Where(i => !i.Exists || !Request.Query.ContainsKey("key") || i.Tag.Any(t=>t.Contains(Request.Query["key"]!, StringComparison.CurrentCultureIgnoreCase))),
                "size" => DB.Videos.Where(i => i.Exists).OrderByDescending(p => p.Size),
                _ => DB.Videos.Where(i => i.Exists).OrderByDescending(p => p.UploadTime),
            };
            string list = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><playlist xmlns=\"http://xspf.org/ns/0/\" xmlns:vlc=\"http://www.videolan.org/vlc/playlist/ns/0/\" version=\"1\"><title>{Env.Name} OrderBy {orderby}</title><trackList>";
            foreach (var item in OrderList)
            {
                list += $"<track><location>/{item.ID}.mp4</location></track>";
            }
            list += "</trackList></playlist>";
            await Response.WriteAsync(list);
        }
        public static async Task<Task> RPC(HttpContext context)
        {
            Result result = context.Preprocessing(out HttpRequest Request, out HttpResponse Response);
            Log($"{Request.Method} {Request.Path} {Request.ContentLength}");
            try
            {
                Request quest = await RequestCheck(Request);
                Response.StatusCode = StatusCodes.Status200OK;
                switch (quest.Code)
                {
                    case RequestCode.Add:
                        VideoTask Task = JsonSerializer.Deserialize<VideoTask>(quest.Data, JsonOptions) ?? throw new ArgumentNullException(nameof(VideoTask), "Deserialization failed");
                        if (DB.Videos.Any(i => i.Source == Task.Source))
                        {
                            result = new() { Code = ResultCode.Exists, Msg = "�Ѵ���" };
                            break;
                        }
                        PustDownloadTask(Task);
                        result = new() { Code = ResultCode.OK, Msg = "�����" };
                        break;
                    case RequestCode.State:
                        Log($"Ver:{string.Join('.', quest.Ver)}");
                        result = new() { Code = ResultCode.OK, Data = DownloadQueue };
                        break;
                    default:
                        throw new ArgumentException($"δ֪���� {quest}");
                }

            }
            catch (ArgumentException ex)
            {
                result = new() { Code = ResultCode.BadRequest, Msg = $"�����ʽ���������쳣[{ex.Message}]" };
            }
            catch (AuthenticationException)
            {
                result = new() { Code = ResultCode.Unauthorized, Msg = "δ��Ȩ" };
            }
            catch (Exception ex)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                result = new() { Code = ResultCode.Unhandled, Msg = ex.Message };
            }
            return Response.WriteAsJsonAsync(result);
        }
        public static async Task<Request> RequestCheck(HttpRequest Request)
        {
            if (!Request.HasJsonContentType())
            {
                throw new ArgumentException("�����ʽ����ȷ");
            }
            Request quest = await Request.ReadFromJsonAsync<Request>(JsonOptions);
            return Authentication(quest) ? quest : throw new AuthenticationException(Env.MainConfig.AuthType, "��֤ʧ��");
        }
        public static bool Authentication(Request Request)
        {
            switch (Env.MainConfig.AuthType)
            {
                case Config.AuthenticationType.Token:
                    if (!TokenCheck().Match(Request.Token ?? "").Success)
                    {
                        throw new ArgumentException($"Token format error");
                    }
                    return Env.MainConfig.Token == Request.Token;
                case Config.AuthenticationType.None:
                default:
                    return true;
            }
        }

        public static void PustDownloadTask(VideoTask task)
        {
            Log($"{task.Name}�ѽ������ض���!");
            DownloadQueue.AddOrUpdate(task.ID, new DownloadTask(task), (k, v) => v);
            if (DownloadQueue.Count(i => i.Value.State == DownloadTask.TaskState.Downloading) < Env.MainConfig.ConcurrentDownloads)
            {
                DownloadFile();
            }
        }
        public static async void DownloadFile()
        {
            if (DownloadQueue.Any(i => i.Value.State == DownloadTask.TaskState.Waiting))
            {
                DownloadTask task = DownloadQueue.First(i => i.Value.State == DownloadTask.TaskState.Waiting).Value;
                task.State = DownloadTask.TaskState.Downloading;
                Log($"��ʼ���� {task.Video.Name}");
                try
                {
                    Action<double> action = (e) =>
                    {
                        Log($"{task.Video.Name} {e:N2}%");
                    };
                    task.DownloadProgressChanged += action.LimitInvocationRate(1, TimeSpan.FromSeconds(2));
                    await HTTP.DownloadAsync(new(task.Video.DownloadUrl), task.Video.Path, task);
                    task.State = DownloadTask.TaskState.Downloaded;
                    Log($"{task.Video.Name} ����У���ļ�...");
                    using (FileStream file = File.OpenRead(task.Video.Path))
                    {
                        task.Video.Exists = true;
                        task.Video.Size = file.Length;
                        task.Video.Hash = await file.SHA256HashAsync();
                    }
                    task.Video.DownloadTime = DateTime.Now;
                    DB.Videos.Add(task.Video);
                    Log($"{task.Video.Name} �������");
                }
                catch (Exception ex)
                {
                    Warn($"{task.Video.Name} ����ʧ�� {ex}");
                    task.State = DownloadTask.TaskState.Error;
                    return;
                }
                finally
                {
                    DownloadFile();
                }
            }
        }

        [GeneratedRegex("^(?![a-zA-Z]+$)(?!\\d+$)(?![^\\da-zA-Z\\s]+$).{6,}$")]
        private static partial Regex TokenCheck();
    }
}