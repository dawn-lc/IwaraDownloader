using System.Net;
using static Dawnlc.Module.Utils;

namespace Dawnlc.Module
{
    public static class HTTP
    {
        private static ClientPool Handler { get; set; } = new(10, new(0, 1, 0));
        private class ClientHandler : HttpClientHandler
        {
            private readonly HttpMessageInvoker Handler = new(new SocketsHttpHandler()
            {
                SslOptions = new()
                {
                    //Domain Fronting
                    TargetHost = "download.windowsupdate.com"
                }
            });
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Handler.Dispose();
            }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return await Handler.SendAsync(request, cancellationToken);
            }
        }
        private class ClientPool : IDisposable
        {
            private class Client : IDisposable
            {
                public DateTime LastUseTime { get; set; }
                public string Host { get; set; }
                private HttpClient ClientHandle { get; set; }
                public Client(Uri uri, TimeSpan timeout)
                {
                    Host = uri.Host;
                    ClientHandle = new(new ClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip,
                        AllowAutoRedirect = true,
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                    })
                    {
                        Timeout = timeout
                    };
                }
                public HttpResponseMessage Send(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
                {
                    return Send(httpRequestMessage, DefaultCompletionOption, cancellationToken);
                }
                public HttpResponseMessage Send(HttpRequestMessage httpRequestMessage, HttpCompletionOption completionOption, CancellationToken cancellationToken)
                {
                    LastUseTime = DateTime.Now;
                    return ClientHandle.Send(httpRequestMessage, completionOption, cancellationToken);
                }
                public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
                {
                    return SendAsync(httpRequestMessage, DefaultCompletionOption, cancellationToken);
                }
                public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, HttpCompletionOption completionOption, CancellationToken cancellationToken)
                {
                    LastUseTime = DateTime.Now;
                    return ClientHandle.SendAsync(httpRequestMessage, completionOption, cancellationToken);
                }
                public void ClearDefaultRequestHeaders()
                {
                    ClientHandle.DefaultRequestHeaders.Clear();
                }
                public void Dispose()
                {
                    ClientHandle.Dispose();
                }
            }
            private volatile bool _disposed;
            private List<Client> Clients { get; set; }
            private TimeSpan Timeout { get; set; }
            private int MaxClient { get; set; }
            public static HttpCompletionOption DefaultCompletionOption { get; set; } = HttpCompletionOption.ResponseContentRead;
            public ClientPool(int maxClient, TimeSpan timeout)
            {
                Timeout = timeout;
                Clients = new();
                MaxClient = maxClient;
            }
            public ClientPool StartClient(Uri url)
            {
                CheckDisposed();
                if (!Clients.Any(i => i.Host == url.Host))
                {
                    Clients.Add(new(url, Timeout));
                }
                while (Clients.Count > MaxClient)
                {
                    Client client = Clients.OrderBy(i => i.LastUseTime).Last();
                    client.Dispose();
                    Clients.Remove(client);
                }
                return this;
            }
            private static HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri uri, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null)
            {
                HttpRequestMessage request = new(method, uri);
                request.Headers.Add("user-agent", new List<string> { $"{Env.Name} {string.Join(".", Env.Version)}" });
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
                return request;
            }

            public Task<HttpResponseMessage> HeadAsync(Uri requestUri, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => HeadAsync(requestUri, DefaultCompletionOption, headers);
            public Task<HttpResponseMessage> HeadAsync(Uri requestUri, HttpCompletionOption completionOption, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => HeadAsync(requestUri, completionOption, CancellationToken.None, headers);
            public Task<HttpResponseMessage> HeadAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => SendAsync(CreateRequestMessage(HttpMethod.Head, requestUri, headers), completionOption, cancellationToken);

            public Task<HttpResponseMessage> GetAsync(Uri requestUri, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => GetAsync(requestUri, DefaultCompletionOption, headers);
            public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => GetAsync(requestUri, completionOption, CancellationToken.None, headers);
            public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => SendAsync(CreateRequestMessage(HttpMethod.Get, requestUri, headers), completionOption, cancellationToken);
            public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent? content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => PostAsync(requestUri, content, CancellationToken.None, headers);
            public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null)
            {
                HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, requestUri, headers);
                request.Content = content;
                return SendAsync(request, cancellationToken);
            }
            public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent? content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => PutAsync(requestUri, content, CancellationToken.None, headers);
            public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null)
            {
                HttpRequestMessage request = CreateRequestMessage(HttpMethod.Put, requestUri, headers);
                request.Content = content;
                return SendAsync(request, cancellationToken);
            }
            public Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent? content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => PatchAsync(requestUri, content, CancellationToken.None, headers);
            public Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null)
            {
                HttpRequestMessage request = CreateRequestMessage(HttpMethod.Patch, requestUri, headers);
                request.Content = content;
                return SendAsync(request, cancellationToken);
            }
            public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => DeleteAsync(requestUri, CancellationToken.None, headers);
            public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers = null) => SendAsync(CreateRequestMessage(HttpMethod.Delete, requestUri, headers), cancellationToken);
            public HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Send(request, DefaultCompletionOption, cancellationToken);
            }
            public HttpResponseMessage Send(HttpRequestMessage httpRequestMessage, HttpCompletionOption completionOption, CancellationToken cancellationToken)
            {
                CheckDisposed();
                Client? client = Clients.Find(i => i.Host == httpRequestMessage.RequestUri?.Host) ?? throw new("未找到可用的HTTP客户端。");
                client.ClearDefaultRequestHeaders();
                return client.Send(httpRequestMessage, completionOption, cancellationToken);
            }
            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, DefaultCompletionOption, cancellationToken);
            }
            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, HttpCompletionOption completionOption, CancellationToken cancellationToken)
            {
                CheckDisposed();
                Client? client = Clients.Find(i => i.Host == httpRequestMessage.RequestUri?.Host) ?? throw new("未找到可用的HTTP客户端。");
                client.ClearDefaultRequestHeaders();
                return client.SendAsync(httpRequestMessage, completionOption, cancellationToken);
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected virtual void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;

                    foreach (var item in Clients)
                    {
                        item.Dispose();
                    }
                }
            }
            private void CheckDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().ToString());
                }
            }
        }
        public static async Task<HttpResponseMessage> GetAsync(Uri url, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            return await Handler.StartClient(url).GetAsync(url, head);
        }
        public static async Task<HttpResponseMessage> GetStreamAsync(Uri url, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            return await Handler.StartClient(url).GetAsync(url, HttpCompletionOption.ResponseHeadersRead, head);
        }
        public static async Task<HttpResponseMessage> PostAsync(Uri url, HttpContent content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            return await Handler.StartClient(url).PostAsync(url, content, head);
        }
        public static async Task<HttpResponseMessage> PatchAsync(Uri url, HttpContent content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            return await Handler.StartClient(url).PatchAsync(url, content, head);
        }
        public static async Task<HttpResponseMessage> PutAsync(Uri url, HttpContent content, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            return await Handler.StartClient(url).PutAsync(url, content, head);
        }
        
        public static async Task DownloadAsync(Uri url, string path, DownloadTask task, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? head = null)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                long ReceivedBytes = 0;

                head ??= new List<KeyValuePair<string, IEnumerable<string>>>();
                List<KeyValuePair<string, IEnumerable<string>>>? getLength = new() { new("Range", new List<string>() { $"bytes=0-16" }) };
                HttpResponseMessage httpResponse = await GetAsync(url, getLength.Concat(head.Where(i => i.Key.ToLower() != "range")));
                if (httpResponse.IsSuccessStatusCode && Env.MainConfig.ParallelCount != 0)
                {
                    long fileLength = httpResponse.Content.Headers.ContentRange?.Length ?? httpResponse.Content.Headers.ContentLength ?? -1;
                    long chunkSize = fileLength / Env.MainConfig.ParallelCount;
                    if (fileLength > 0)
                    {
                        async Task chunk(int p, int tryCount)
                        {
                            Log($"Task: {task.Video.Name} chunk: {p} try: {tryCount} ");
                            long rangeStart = p * chunkSize;
                            long rangeEnd = ((p + 1) != Env.MainConfig.ParallelCount) ? (rangeStart + chunkSize) : fileLength;
                            try
                            {
                                byte[] buffer = new byte[Env.MainConfig.BufferBlockSize];
                                List<KeyValuePair<string, IEnumerable<string>>>? Range = new() { new("Range", new List<string>() { $"bytes={rangeStart}-{rangeEnd}" }) };
                                Stream ResponseStream = await (await GetStreamAsync(url, Range.Concat(head.Where(i => i.Key.ToLower() != "range")))).Content.ReadAsStreamAsync();
                                int bytesRead;
                                long chunkSeek = rangeStart;
                                using (FileStream destination = new(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                                {
                                    while ((bytesRead = await ResponseStream.ReadAsync(buffer)) != 0)
                                    {
                                        destination.Seek(chunkSeek, SeekOrigin.Begin);
                                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                                        ReceivedBytes += bytesRead;
                                        chunkSeek = destination.Position;
                                        task.OnDownloadProgressChanged((double)ReceivedBytes / fileLength * 100);
                                    }
                                };
                            }
                            catch (HttpRequestException ex)
                            {
                                if (tryCount < 5)
                                {
                                    Log($"Task: {task.Video.Name} HttpRequestException try:{tryCount} Delay10s");
                                    await Task.Delay(1000 * 10);
                                    await chunk(p, tryCount++);
                                }
                                else
                                {
                                    Log($"Task: {task.Video.Name} tryCount Max throw");
                                    throw ex;
                                }
                            }
                        }
                        Task.WaitAll(Enumerable.Range(0, Env.MainConfig.ParallelCount).Select(p => chunk(p, 0)).ToArray());
                        return;
                    }
                }
                byte[] buffer = new byte[Env.MainConfig.BufferBlockSize];
                Stream ResponseStream = await (await GetStreamAsync(url, head)).Content.ReadAsStreamAsync();
                int bytesRead;
                using (FileStream destination = new(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    while ((bytesRead = await ResponseStream.ReadAsync(buffer)) != 0)
                    {
                        ReceivedBytes += bytesRead;
                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                        task.OnDownloadProgressChanged((double)ReceivedBytes / ResponseStream.Length * 100);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"DownloadException {url.ToString() ?? path}");
                Warn($"----------- Errer info -----------{Environment.NewLine}{ex}");
            }
        }
    }
}
