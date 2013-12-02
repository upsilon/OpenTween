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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenTween.Connection
{
    public class HttpOAuthEchoAsync : HttpOAuthAsync
    {
        public readonly Uri ServiceProvider;

        public HttpOAuthEchoAsync(string consumerKey, string consumerSecret, Uri serviceProvider)
            : this(null, consumerKey, consumerSecret, serviceProvider)
        {
        }

        public HttpOAuthEchoAsync(Uri realm, string consumerKey, string consumerSecret, Uri serviceProvider)
            : base(realm, consumerKey, consumerSecret)
        {
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Authorization ヘッダに OAuth の認証情報を設定します
        /// </summary>
        protected override void SetOAuthHeader(HttpWebRequest request, IDictionary<string, string> oauthParams,
            IDictionary<string, string> oauthExtraParams = null)
        {
            var authorization = this.BuildAuthorizationValue("GET", this.ServiceProvider, null);
            request.Headers["X-Verify-Credentials-Authorization"] = authorization;
            request.Headers["X-Auth-Service-Provider"] = this.ServiceProvider.ToString();
        }

        /// <summary>
        /// (OAuth Echo では使用しないメソッドです)
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public override Task<IDictionary<string, string>> GetRequestTokenAsync(Uri requestTokenUrl, string callbackUrl = "oob")
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// (OAuth Echo では使用しないメソッドです)
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public override Uri MakeAuthorizeUrl(Uri authorizeUrl, Tuple<string, string> requestToken)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// (OAuth Echo では使用しないメソッドです)
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public override Task<IDictionary<string, string>> GetAccessTokenAsync(Uri accessTokenUrl, Tuple<string, string> requestToken, string pinCode)
        {
            throw new InvalidOperationException();
        }
    }
}
