// OpenTween - Client of Twitter
// Copyright (c) 2013 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
//
// This file is part of OpenTween.
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTween;

namespace OpenTween.Connection
{
    /// <summary>
    /// 非同期に HTTP リクエストの送信やレスポンスの受信を行う機能を提供する
    /// </summary>
    public class HttpAsync
    {
        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// HttpAsync から生成されるリクエストのデフォルトのプロキシ設定
        /// </summary>
        public static IWebProxy DefaultProxy { get; set; }

        /// <summary>
        /// HttpAsync から生成されるリクエストのデフォルトのタイムアウト時間
        /// </summary>
        public static TimeSpan DefaultTimeout { get; set; }

        static HttpAsync()
        {
            DefaultProxy = null;
            DefaultTimeout = new TimeSpan(0, 0, 20); // 20 sec
        }

        /// <summary>
        /// リクエスト間で Cookie を保持するか否か
        /// </summary>
        public bool UseCookie { get; set; }

        /// <summary>
        /// インスタンスごとに設定されるタイムアウト時間
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// クッキー保存用コンテナ
        /// </summary>
        private CookieContainer cookieContainer = new CookieContainer();

        /// <summary>
        /// HttpWebRequest に対する共通のパラメータ設定を行う
        /// </summary>
        /// <param name="request">設定を施す HttpWebRequest インスタンス</param>
        /// <param name="method">HTTP メソッド</param>
        private void InitRequest(HttpWebRequest request, HttpMethod method)
        {
            switch (method)
            {
                case HttpMethod.Get:
                    request.Method = WebRequestMethods.Http.Get;
                    break;
                case HttpMethod.Head:
                    request.Method = WebRequestMethods.Http.Head;
                    break;
                case HttpMethod.Post:
                    request.Method = WebRequestMethods.Http.Post;
                    break;
                case HttpMethod.Put:
                    request.Method = WebRequestMethods.Http.Put;
                    break;
                default:
                    throw new ArgumentException("Invalid method.");
            }

            request.UserAgent = MyCommon.GetUserAgentString();
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            request.ReadWriteTimeout = 90 * 1000; // Streamの読み込みは90秒でタイムアウト（デフォルト5分）
            // 非同期メソッドでは request.Timeout の値は無視されるため設定しない

            // プロキシ設定
            request.Proxy = DefaultProxy;

            // Cookie設定
            if (this.UseCookie)
                request.CookieContainer = this.cookieContainer;
        }

        /// <summary>
        /// 指定された URL に対するリクエストを作成する
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <returns>リクエストの作成を待機するタスク</returns>
        protected Task<HttpWebRequest> CreateRequestAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param)
        {
            return this.CreateRequestAsync(method, uri, param, CancellationToken.None);
        }

        /// <summary>
        /// 指定された URL に対するリクエストを作成する
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの作成を待機するタスク</returns>
        protected async Task<HttpWebRequest> CreateRequestAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            if (!Initialized)
                throw new InvalidOperationException("Sequence error.(not initialized)");

            // クエリの送信にメッセージボディを使用するかどうか
            var useMessageBody = (method == HttpMethod.Post || method == HttpMethod.Put);

            var queryString = param != null ? CreateQueryString(param) : null;

            if (!useMessageBody && queryString != null)
            {
                // クエリ文字列を URL に付加
                var ub = new UriBuilder(uri.AbsoluteUri);
                ub.Query = queryString;

                uri = ub.Uri;
            }

            var request = (HttpWebRequest)WebRequest.Create(uri);
            this.InitRequest(request, method);

            if (useMessageBody && queryString != null)
            {
                // クエリ文字列をメッセージボディに書き出し
                request.ContentType = "application/x-www-form-urlencoded";

                using (var reqStream = await request.GetRequestStreamAsync())
                {
                    var bytes = Encoding.UTF8.GetBytes(queryString);
                    await reqStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }

            var timeout = this.Timeout ?? DefaultTimeout;
            this.RegisterCancellationToken(request, token, timeout);

            return request;
        }

        /// <summary>
        /// 指定された URL に対するリクエストを作成する
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="files">送信するファイル</param>
        /// <returns>リクエストの作成を待機するタスク</returns>
        protected Task<HttpWebRequest> CreateRequestAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, IDictionary<string, FileInfo> files)
        {
            return this.CreateRequestAsync(method, uri, param, files, CancellationToken.None);
        }

        /// <summary>
        /// 指定された URL に対するリクエストを作成する
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="files">送信するファイル</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの作成を待機するタスク</returns>
        protected async Task<HttpWebRequest> CreateRequestAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, IDictionary<string, FileInfo> files,
            CancellationToken token)
        {
            if (!Initialized)
                throw new InvalidOperationException("Sequence error.(not initialized)");

            if (method != HttpMethod.Post && method != HttpMethod.Put)
                throw new ArgumentException("Method must be POST or PUT");

            if ((param == null || param.Count == 0) && (files == null || files.Count == 0))
                throw new ArgumentException("Data is empty");

            var request = (HttpWebRequest)WebRequest.Create(uri);
            this.InitRequest(request, method);

            var boundary = Environment.TickCount.ToString(); // マルチパートの境界に使う文字列

            request.ContentType = "multipart/form-data; boundary=" + boundary;

            token.ThrowIfCancellationRequested();

            using (var reqStream = await request.GetRequestStreamAsync())
            {
                if (param != null)
                {
                    var postData = new StringBuilder();
                    foreach (var pair in param)
                    {
                        postData
                            .AppendFormat("--{0}\r\n", boundary)
                            .AppendFormat("Content-Disposition: form-data; name=\"{0}\"\r\n", pair.Key)
                            .Append("\r\n")
                            .AppendFormat("{0}\r\n", pair.Value);
                    }

                    var postBytes = Encoding.UTF8.GetBytes(postData.ToString());
                    await reqStream.WriteAsync(postBytes, 0, postBytes.Length);
                }

                if (files != null)
                {
                    var crlfByte = Encoding.UTF8.GetBytes("\r\n");

                    foreach (var pair in files)
                    {
                        var file = pair.Value;

                        token.ThrowIfCancellationRequested();

                        var postData = new StringBuilder()
                            .AppendFormat("--{0}\r\n", boundary)
                            .AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", pair.Key, file.Name)
                            .AppendFormat("Content-Type: {0}\r\n", GetMimeType(file.Extension))
                            .Append("Content-Transfer-Encoding: binary\r\n")
                            .Append("\r\n");

                        var postBytes = Encoding.UTF8.GetBytes(postData.ToString());
                        await reqStream.WriteAsync(postBytes, 0, postBytes.Length);

                        using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                        {
                            await fs.CopyToAsync(reqStream);
                        }

                        await reqStream.WriteAsync(crlfByte, 0, crlfByte.Length);
                    }
                }

                // 終端
                var endBytes = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");
                await reqStream.WriteAsync(endBytes, 0, endBytes.Length);
            }

            var timeout = this.Timeout ?? DefaultTimeout;
            this.RegisterCancellationToken(request, token, timeout);

            return request;
        }

        /// <summary>
        /// 指定された CancellationToken がキャンセルされたときかタイムアウト時間が
        /// 経過した場合に request.Abort() が呼ばれる状態にします
        /// </summary>
        /// <param name="request">対象となるリクエスト</param>
        /// <param name="token">リクエスト時に渡された CancellationToken</param>
        /// <param name="timeout">タイムアウト時間</param>
        protected void RegisterCancellationToken(HttpWebRequest request, CancellationToken token, TimeSpan timeout)
        {
            var timeoutTokenSource = new CancellationTokenSource();
            var timeoutToken = timeoutTokenSource.Token;

            var timer = new Timer(_ => timeoutTokenSource.Cancel());
            timer.Change(timeout, new TimeSpan(0, 0, 0, 0, -1));

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken);
            linkedTokenSource.Token.Register(() =>
            {
                request.Abort();

                linkedTokenSource.Dispose();
                timer.Dispose();
                timeoutTokenSource.Dispose();
            });
        }

        /// <summary>
        /// リクエストを実行しレスポンスを非同期に取得します
        /// </summary>
        /// <param name="request">実行するリクエスト</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>レスポンスを待機するタスク</returns>
        protected Task<HttpWebResponse> SendRequestAsync(HttpWebRequest request)
        {
            return this.SendRequestAsync(request, CancellationToken.None);
        }

        /// <summary>
        /// リクエストを実行しレスポンスを非同期に取得します
        /// </summary>
        /// <param name="request">実行するリクエスト</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>レスポンスを待機するタスク</returns>
        protected async Task<HttpWebResponse> SendRequestAsync(HttpWebRequest request, CancellationToken token)
        {
            HttpWebResponse response;
            try
            {
               response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                    throw;
                response = (HttpWebResponse)ex.Response;
            }

            if (this.UseCookie)
                this.FixCookies(response.Cookies);

            return response;
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri)
        {
            return this.GetResponseAsync(method, uri, null);
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            CancellationToken token)
        {
            return this.GetResponseAsync(method, uri, null, token);
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param)
        {
            return this.GetResponseAsync(method, uri, param, CancellationToken.None);
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public virtual async Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            var request = await this.CreateRequestAsync(method, uri, param, token);

            return await this.SendRequestAsync(request, token);
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="files">送信するファイル</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, IDictionary<string, FileInfo> files)
        {
            return this.GetResponseAsync(method, uri, param, files, CancellationToken.None);
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="files">送信するファイル</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public virtual async Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, IDictionary<string, FileInfo> files,
            CancellationToken token)
        {
            var request = await this.CreateRequestAsync(method, uri, param, files, token);

            return await this.SendRequestAsync(request, token);
        }

        /// <summary>
        /// 指定された URL へ非同期に HEAD リクエストを送信しリダイレクト先を取得します
        /// </summary>
        /// <param name="uri">送信先 URL</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public Task<Uri> GetRedirectToAsync(Uri uri)
        {
            return this.GetRedirectToAsync(uri, CancellationToken.None);
        }

        /// <summary>
        /// 指定された URL へ非同期に HEAD リクエストを送信しリダイレクト先を取得します
        /// </summary>
        /// <param name="uri">送信先 URL</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public virtual async Task<Uri> GetRedirectToAsync(Uri uri, CancellationToken token)
        {
            var request = await this.CreateRequestAsync(HttpMethod.Head, uri, null, token);
            request.AllowAutoRedirect = false;

            using (var response = await this.SendRequestAsync(request, token))
            {
                response.ThrowIfErrorStatus();
                var location = response.Headers["Location"];

                return location != null ? new Uri(location) : null;
            }
        }

        public Task<string> GetStringAsync(HttpMethod method, Uri uri)
        {
            return this.GetStringAsync(method, uri, null);
        }

        public Task<string> GetStringAsync(HttpMethod method, Uri uri, IDictionary<string, string> param)
        {
            return this.GetStringAsync(method, uri, param, CancellationToken.None);
        }

        public Task<string> GetStringAsync(HttpMethod method, Uri uri, CancellationToken token)
        {
            return this.GetStringAsync(method, uri, null, token);
        }

        public async Task<string> GetStringAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            using (var response = await this.GetResponseAsync(method, uri, param, token))
            {
                response.ThrowIfErrorStatus();
                return response.GetResponseString();
            }
        }

        public Task<byte[]> GetBytesAsync(HttpMethod method, Uri uri)
        {
            return this.GetBytesAsync(method, uri, null);
        }

        public Task<byte[]> GetBytesAsync(HttpMethod method, Uri uri, IDictionary<string, string> param)
        {
            return this.GetBytesAsync(method, uri, param, CancellationToken.None);
        }

        public Task<byte[]> GetBytesAsync(HttpMethod method, Uri uri, CancellationToken token)
        {
            return this.GetBytesAsync(method, uri, null, token);
        }

        public async Task<byte[]> GetBytesAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            using (var response = await this.GetResponseAsync(method, uri, param, token))
            {
                response.ThrowIfErrorStatus();
                return await response.GetResponseBytesAsync();
            }
        }

        public Task<MemoryImage> GetImageAsync(HttpMethod method, Uri uri)
        {
            return this.GetImageAsync(method, uri, null);
        }

        public Task<MemoryImage> GetImageAsync(HttpMethod method, Uri uri, IDictionary<string, string> param)
        {
            return this.GetImageAsync(method, uri, param, CancellationToken.None);
        }

        public Task<MemoryImage> GetImageAsync(HttpMethod method, Uri uri, CancellationToken token)
        {
            return this.GetImageAsync(method, uri, null, token);
        }

        public async Task<MemoryImage> GetImageAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            using (var response = await GetResponseAsync(method, uri, param, token))
            {
                response.ThrowIfErrorStatus();

                using (var stream = response.GetResponseStream())
                {
                    return await MemoryImage.CopyFromStreamAsync(stream);
                }
            }
        }

        /// <summary>
        /// ホスト名なしのドメインはドメイン名から先頭のドットを除去しないと再利用されないため修正して追加する
        /// </summary>
        private void FixCookies(CookieCollection cookieCollection)
        {
            foreach (Cookie ck in cookieCollection)
            {
                if (ck.Domain.StartsWith("."))
                {
                    ck.Domain = ck.Domain.Substring(1);
                    cookieContainer.Add(ck);
                }
            }
        }

        /// <summary>
        /// IDictionary オブジェクトから application/x-www-form-urlencoded 形式に整形されたクエリ文字列に変換します
        /// </summary>
        internal static string CreateQueryString(IEnumerable<KeyValuePair<string, string>> param)
        {
            if (param == null) return "";

            return string.Join("&", param.Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value)));
        }

        private static readonly IDictionary<string, string> mimeTypeDict = new Dictionary<string, string>
        {
            {".jpg", "image/jpeg"},
            {".jpe", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".gif", "image/gif"},
            {".png", "image/png"},
            {".tif", "image/tiff"},
            {".tiff", "image/tiff"},
            {".bmp", "image/x-bmp"},
            {".avi", "video/avi"},
            {".wmv", "video/x-ms-wmv"},
            {".flv", "video/x-flv"},
            {".m4v", "video/x-m4v"},
            {".mov", "video/quicktime"},
            {".mp4", "video/3gpp"},
            {".rm", "application/vnd.rn-realmedia"},
            {".mpg", "video/mpeg"},
            {".mpeg", "video/mpeg"},
            {".3gp", "movie/3gp"},
            {".3g2", "video/3gpp2"},
        };

        /// <summary>
        /// 拡張子から MIME タイプを推測します
        /// </summary>
        /// <param name="extension">拡張子</param>
        /// <returns>MIME タイプ</returns>
        private static string GetMimeType(string extension)
        {
            string mimeType;
            if (!mimeTypeDict.TryGetValue(extension, out mimeType))
                return "application/octet-stream";

            return mimeType;
        }

        /// <summary>
        /// 事前にやっといた方がよさそうな通信周りの準備をちゃちゃっと済ませる
        /// </summary>
        public static void Initialize()
        {
            ServicePointManager.Expect100Continue = false;

            // Windows 8.1 Previewの場合SecurityProtocolを明確に指定する必要がある
            // Preview 版使用期限の 2014 年 1 月を過ぎたら消すよ
            var osVersion = Environment.OSVersion.Version;
            if (osVersion.Major == 6 && osVersion.Minor == 3)
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Ssl3;
            }

            Initialized = true;
        }

        public static void SetProxy(ProxyType proxyType, string host, int port, string user, string password)
        {
            switch (proxyType)
            {
                case ProxyType.Specified:
                    var uriBuilder = new UriBuilder
                    {
                        Scheme = "http",
                        Host = host,
                        Port = port,
                        UserName = user,
                        Password = password,
                    };
                    DefaultProxy = new WebProxy(uriBuilder.ToString());
                    break;
                case ProxyType.None:
                    DefaultProxy = null;
                    break;
                case ProxyType.IE:
                default:
                    DefaultProxy = WebRequest.GetSystemWebProxy();
                    break;
            }
        }

        public static void SetProxy(IWebProxy proxy)
        {
            DefaultProxy = proxy;
        }
    }
}