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
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTween.Connection
{
    /// <summary>
    /// OAuth 認証を使用した非同期な HTTP 操作を提供します
    /// </summary>
    public class HttpOAuthAsync : HttpAsync
    {
        public readonly Uri Realm;
        public readonly string ConsumerKey;
        public readonly string ConsumerSecret;

        /// <summary>
        /// 認証済みのアクセストークンがセットされている状態であるかを表します
        /// </summary>
        /// <remarks>
        /// SetAccessToken メソッドや GetAccessTokenAsync メソッドが正常に完了すると true になります。
        /// このフィールドが false の状態で GetResponseAsync 等のメソッドは実行できません (InvalidOperationException がスローされます)。
        /// </remarks>
        public bool Authorized { get; private set; }

        public string AccessToken { get; private set; }
        public string AccessSecret { get; private set; }

        public HttpOAuthAsync(string consumerKey, string consumerSecret)
            : this(null, consumerKey, consumerSecret)
        {
        }

        public HttpOAuthAsync(Uri realm, string consumerKey, string consumerSecret)
        {
            this.Realm = realm;
            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
            this.Authorized = false;
        }

        /// <summary>
        /// 認証済みのアクセストークンをセットします
        /// </summary>
        public virtual void SetAccessToken(string accessToken, string accessSecret)
        {
            this.AccessToken = accessToken;
            this.AccessSecret = accessSecret;
            this.Authorized = true;
        }

        /// <summary>
        /// リクエストトークンを取得します
        /// </summary>
        public virtual Task<IDictionary<string, string>> GetRequestTokenAsync(Uri requestTokenUrl, string callbackUrl = "oob")
        {
            var oauthParams = new Dictionary<string, string>
            {
                {"oauth_callback", callbackUrl},
            };

            return this.GetTokenAsync(requestTokenUrl, new Tuple<string, string>(null, null), oauthParams);
        }

        /// <summary>
        /// ユーザーに認可を求めるページの URL をリクエストトークンから生成します
        /// </summary>
        public virtual Uri MakeAuthorizeUrl(Uri authorizeUrl, Tuple<string, string> requestToken)
        {
            var uriBuilder = new UriBuilder(authorizeUrl);
            uriBuilder.Query = "oauth_token=" + requestToken.Item1;

            return uriBuilder.Uri;
        }

        /// <summary>
        /// ユーザーから入力された PIN コードからアクセストークンを取得します
        /// </summary>
        public virtual async Task<IDictionary<string, string>> GetAccessTokenAsync(Uri accessTokenUrl, Tuple<string, string> requestToken, string pinCode)
        {
            var oauthParams = new Dictionary<string, string>
            {
                {"oauth_verifier", pinCode},
            };

            var token = await this.GetTokenAsync(accessTokenUrl, requestToken, oauthParams);

            this.AccessToken = token["oauth_token"];
            this.AccessSecret = token["oauth_token_secret"];
            this.Authorized = true;

            return token;
        }

        /// <summary>
        /// リクエストトークンやアクセストークンを取得する際の共通処理
        /// </summary>
        protected virtual async Task<IDictionary<string, string>> GetTokenAsync(Uri tokenUrl,
            Tuple<string, string> oauthToken, IDictionary<string, string> oauthExtraParams)
        {
            this.AccessToken = oauthToken.Item1;
            this.AccessSecret = oauthToken.Item2;
            this.Authorized = false;

            var request = await this.CreateRequestAsync(HttpMethod.Get, tokenUrl, null);

            this.SetOAuthHeader(request, null, oauthExtraParams);

            using (var response = await this.SendRequestAsync(request))
            {
                response.ThrowIfErrorStatus();
                return OAuthUtil.ParseQueryString(response.GetResponseString());
            }
        }

        /// <summary>
        /// 指定された URL へ非同期にリクエストを送信し結果を取得します
        /// </summary>
        /// <param name="method">HTTP メソッド</param>
        /// <param name="uri">送信先 URL</param>
        /// <param name="param">送信するパラメーター</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public override async Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, CancellationToken token)
        {
            this.ThrowIfNotAuthorized();

            var request = await this.CreateRequestAsync(method, uri, param, token);

            this.SetOAuthHeader(request, param);

            return await this.SendRequestAsync(request, token);
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
        public override async Task<HttpWebResponse> GetResponseAsync(HttpMethod method, Uri uri,
            IDictionary<string, string> param, IDictionary<string, FileInfo> files, CancellationToken token)
        {
            this.ThrowIfNotAuthorized();

            var request = await this.CreateRequestAsync(method, uri, param, files, token);

            this.SetOAuthHeader(request, null);

            return await this.SendRequestAsync(request, token);
        }

        /// <summary>
        /// 指定された URL へ非同期に HEAD リクエストを送信しリダイレクト先を取得します
        /// </summary>
        /// <param name="uri">送信先 URL</param>
        /// <param name="token">リクエストの中断に使用するトークン</param>
        /// <returns>リクエストの結果を待機するタスク</returns>
        public override async Task<Uri> GetRedirectToAsync(Uri uri, CancellationToken token)
        {
            this.ThrowIfNotAuthorized();

            var request = await this.CreateRequestAsync(HttpMethod.Head, uri, null, token);
            request.AllowAutoRedirect = false;

            this.SetOAuthHeader(request, null);

            using (var response = await this.SendRequestAsync(request, token))
            {
                response.ThrowIfErrorStatus();
                var location = response.Headers["Location"];
                return location != null ? new Uri(location) : null;
            }
        }

        /// <summary>
        /// Authorized プロパティが true であるかを確認し、false であれば InvalidOperationException をスローします
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        protected void ThrowIfNotAuthorized()
        {
            if (!this.Authorized)
                throw new InvalidOperationException("認証済みのアクセストークンがセットされていません");
        }

        /// <summary>
        /// HTTP リクエストの内容から OAuth で使用するパラメータを生成します
        /// </summary>
        protected string BuildAuthorizationValue(string requestMethod, Uri requestUri,
            IDictionary<string, string> requestParams,
            IDictionary<string, string> oauthExtraParams = null)
        {
            var oauthParams = new Dictionary<string, string>();

            OAuthUtil.AddCommonParameters(oauthParams, this.ConsumerKey);

            if (this.AccessToken != null)
                oauthParams["oauth_token"] = this.AccessToken;

            if (oauthExtraParams != null)
            {
                foreach (var kvp in oauthExtraParams)
                    oauthParams.Add(kvp.Key, kvp.Value);
            }

            // リクエストの署名
            var signingParams = requestParams != null ? oauthParams.Concat(requestParams) : oauthParams;
            var signingKey = this.ConsumerSecret + "&" + (this.AccessSecret ?? "");
            oauthParams["oauth_signature"] = OAuthUtil.GenerateSignature(requestMethod, requestUri, signingParams, signingKey, "HMAC-SHA1");

            return OAuthUtil.CreateAuthorization(oauthParams, this.Realm);
        }

        /// <summary>
        /// Authorization ヘッダに OAuth の認証情報を設定します
        /// </summary>
        protected virtual void SetOAuthHeader(HttpWebRequest request, IDictionary<string, string> requestParams,
            IDictionary<string, string> oauthExtraParams = null)
        {
            var authorization = this.BuildAuthorizationValue(request.Method, request.RequestUri, requestParams, oauthExtraParams);
            request.Headers[HttpRequestHeader.Authorization] = authorization;
        }
    }
}
