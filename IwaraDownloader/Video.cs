using System.ComponentModel;
using System.Net;
using static Dawnlc.Module.Utils;

namespace Dawnlc.Module
{
    public class Video : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Video()
        {
        }
        public Video(FileInfo file)
        {
            Path = System.IO.Path.GetFullPath(file.FullName);
            Size = file.Length;
            Hash = file.OpenRead().SHA1Hash();
        }
        /// <summary>
        /// ID
        /// </summary>
        public string ID
        {
            get
            {
                if (id == null)
                {
                    id = Guid.NewGuid().ToString();
                    OnPropertyChanged(nameof(ID));
                }
                return id;
            }
            set
            {
                if (value != id)
                {
                    id = value;
                    OnPropertyChanged(nameof(ID));
                }
            }
        }
        private string? id;

        /// <summary>
        /// Source
        /// </summary>
        public string Source
        {
            get
            {
                if (!string.IsNullOrEmpty(source))
                {
                    return source.ToLower();
                }
                throw new ArgumentNullException(nameof(Source));
            }
            set
            {
                if (source != value)
                {
                    source = value;
                    OnPropertyChanged(nameof(Source));
                }
            }
        }
        private string? source;

        /// <summary>
        /// 名字
        /// </summary>
        public string Name
        {
            get
            {
                return name ?? ID;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private string? name;

        /// <summary>
        /// 作者别名
        /// </summary>
        public string Alias
        {
            get
            {
                return alias ?? "Unknown";
            }
            set
            {
                if (alias != value)
                {
                    alias = value;
                    OnPropertyChanged(nameof(Alias));
                }
            }
        }
        private string? alias;
        
        /// <summary>
        /// 作者
        /// </summary>
        public string Author
        {
            get
            {
                return author ?? "Unknown";
            }
            set
            {
                if (author != value)
                {
                    author = value;
                    OnPropertyChanged(nameof(Author));
                }
            }
        }
        private string? author;

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tag
        {
            get
            {
                return tag ?? new List<string>() { "Uncategorized" };
            }
            set
            {
                if (tag != value)
                {
                    tag = value;
                    OnPropertyChanged(nameof(Tag));
                }
            }
        }
        private List<string>? tag;

        /// <summary>
        /// 附加信息
        /// </summary>
        public string Info
        {
            get
            {
                return info ?? "";
            }
            set
            {
                if (info != value)
                {
                    info = value;
                    OnPropertyChanged(nameof(Info));
                }
            }
        }
        private string? info;

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UploadTime
        {
            get
            {
                return uploadTime ?? new();
            }
            set
            {
                if (value != uploadTime)
                {
                    uploadTime = value;
                    OnPropertyChanged(nameof(UploadTime));
                }
            }
        }
        private DateTime? uploadTime;

        /// <summary>
        /// 下载时间
        /// </summary>
        public DateTime DownloadTime
        {
            get
            {
                return downloadTime ?? new();
            }
            set
            {
                if (value != downloadTime)
                {
                    downloadTime = value;
                    OnPropertyChanged(nameof(DownloadTime));
                }
            }
        }
        private DateTime? downloadTime;

        /// <summary>
        /// 大小
        /// </summary>
        public long Size
        {
            get
            {
                return size ?? 0;
            }
            set
            {
                if (size != value)
                {
                    size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }
        private long? size;

        /// <summary>
        /// 路径
        /// </summary>
        public string Path
        {
            get
            {
                path ??= System.IO.Path.Combine(Env.MainConfig.WebRootPath, Author, $"{ID}[{Source}].mp4");
                string directoryPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? System.IO.Path.Combine(Env.MainConfig.WebRootPath, Author);
                if (System.IO.Path.Exists(directoryPath)) 
                {
                    return path;
                }
                else
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    return path;
                }
            }
            set
            {
                if (value != path)
                {
                    path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }
        private string? path;

        /// <summary>
        /// 是否存在
        /// </summary>
        public bool Exists
        {
            get
            {
                return exists;
            }
            set
            {
                if (value != exists)
                {
                    exists = value;
                    OnPropertyChanged(nameof(Exists));
                }
            }
        }
        private bool exists;

        /// <summary>
        /// Hash
        /// </summary>
        public byte[]? Hash
        {
            get
            {
                return hash;
            }
            set
            {
                if (hash != value)
                {
                    hash = value;
                    OnPropertyChanged(nameof(Hash));
                }
            }
        }
        private byte[]? hash;
    }
    public class VideoTask: Video
    {
        
        /// <summary>
        /// 下载地址
        /// </summary>
        public string DownloadUrl
        {
            get
            {
                return downloadUrl ?? throw new ArgumentNullException(nameof(DownloadUrl));
            }
            set
            {
                if (downloadUrl != value)
                {
                    downloadUrl = value;
                    OnPropertyChanged(nameof(DownloadUrl));
                }
            }
        }
        private string? downloadUrl;
        /// <summary>
        /// 下载地址
        /// </summary>
        public string? DownloadProxy
        {
            get
            {
                return downloadProxy;
            }
            set
            {
                if (downloadProxy != value)
                {
                    downloadProxy = value;
                    OnPropertyChanged(nameof(DownloadProxy));
                }
            }
        }
        private string? downloadProxy;
        /// <summary>
        /// 下载使用的Authorization
        /// </summary>
        public string? Authorization
        {
            get
            {
                return authorization;
            }
            set
            {
                if (authorization != value)
                {
                    authorization = value;
                    OnPropertyChanged(nameof(Authorization));
                }
            }
        }
        private string? authorization;
        /// <summary>
        /// 下载使用的Cookies
        /// </summary>
        public List<Cookie> DownloadCookies
        {
            get
            {
                return downloadCookies ?? new List<Cookie>();
            }
            set
            {
                if (downloadCookies != value)
                {
                    downloadCookies = value;
                    OnPropertyChanged(nameof(DownloadCookies));
                }
            }
        }
        private List<Cookie>? downloadCookies;
    }
}
