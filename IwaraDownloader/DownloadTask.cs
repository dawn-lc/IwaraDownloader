using System.Net;
using static Dawnlc.Module.Utils;

namespace Dawnlc.Module
{

    public class DownloadTask
    {
        public event Action<double>? DownloadProgressChanged;
        public void OnDownloadProgressChanged(double progressPercentage)
        {
            Progress = progressPercentage;
            DownloadProgressChanged?.Invoke(progressPercentage);
        }
        public enum TaskState
        {
            Waiting,
            Downloading,
            Downloaded,
            Error
        }
        public TaskState State { get; set; }
        public VideoTask Video { get; set; }
        public double Progress { get; set; }
        public DownloadOptions Options { get; set; }
        public DownloadTask(VideoTask video)
        {
            Video = video;
            Options = new();
            CookieContainer Cookies = new();
            foreach (var item in Video.DownloadCookies)
            {
                Cookies.Add(item);
            }
            Options.Cookies = Cookies;
            if (Video.DownloadProxy != null)
            {
                Options.Proxy = new (new Uri(Video.DownloadProxy));
            }
            if (Video.Authorization != null)
            {
                Options.Headers ??= Env.Headers;
                Options.Headers.Remove(HttpRequestHeader.Authorization.ToString());
                Options.Headers.Add(HttpRequestHeader.Authorization.ToString(), Video.Authorization);
            }
            State = TaskState.Waiting;
        }
    }
}
