// OpenTween - Client of Twitter
// Copyright (c) 2013 kim_upsilon <https://upsilo.net/~upsilon/>
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
using System.Security.Cryptography;
using System.Text;

namespace OpenTween.Connection
{
    internal class OAuthUtil
    {
        public static void AddCommonParameters(IDictionary<string, string> param, string consumerKey)
        {
            var unixtime = ConvertToUnixTime(GetDateTimeUtcNow());

            param["oauth_consumer_key"] = consumerKey;
            param["oauth_nonce"] = GenerateNonce();
            param["oauth_signature_method"] = "HMAC-SHA1";
            param["oauth_timestamp"] = unixtime.ToString();
            param["oauth_version"] = "1.0";
        }

        /// <summary>
        /// UTC での現在時刻を取得します
        /// </summary>
        internal static Func<DateTime> GetDateTimeUtcNow = () => DateTime.UtcNow;

        /// <summary>
        /// Unix time の基準となる日付 (1970/01/01 00:00:00 UTC)
        /// </summary>
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 指定された DateTime オブジェクトを Unix 時間に変換します
        /// </summary>
        public static long ConvertToUnixTime(DateTime datetime)
        {
            return (long)(datetime.ToUniversalTime() - UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// oauth_nonce を生成するための乱数クラス
        /// </summary>
        [ThreadStatic]
        private static Random NonceRandom; // ThreadStatic なフィールドには初期値を設定しない

        /// <summary>
        /// oauth_nonce に使用するランダムな 32 バイトの Base64 文字列を生成する
        /// </summary>
        public static string GenerateNonce()
        {
            if (NonceRandom == null)
                NonceRandom = new Random();

            var bytes = new byte[32];
            NonceRandom.NextBytes(bytes);

            return Convert.ToBase64String(bytes);
        }

        public static string GenerateSignature(string requestMethod, Uri requestUri,
            IEnumerable<KeyValuePair<string, string>> signingParams,
            string signingKey, string signatureMethod = "HMAC-SHA1")
        {
            if (signatureMethod != "HMAC-SHA1")
                throw new NotImplementedException(string.Format("Signature method \"{0}\" is not supported.", signatureMethod));

            // 署名対象のパラメータをソートする必要がある
            IEnumerable<KeyValuePair<string, string>> sortedParams;

            if (signingParams is SortedDictionary<string, string>)
                sortedParams = signingParams;
            else
                sortedParams = signingParams.OrderBy(x => x.Key);

            using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey)))
            {
                var bytes = Encoding.UTF8.GetBytes(
                    requestMethod + "&" +
                    Uri.EscapeDataString(requestUri.GetLeftPart(UriPartial.Path).ToString()) + "&" +
                    Uri.EscapeDataString(HttpAsync.CreateQueryString(sortedParams)));

                return Convert.ToBase64String(hmac.ComputeHash(bytes));
            }
        }

        public static string CreateAuthorization(IEnumerable<KeyValuePair<string, string>> oauthParams, Uri realm)
        {
            var authorization = new StringBuilder();

            authorization.Append("OAuth ");

            if (realm != null)
                authorization.AppendFormat("realm=\"{0}\",", Uri.EscapeDataString(realm.ToString()));

            authorization.Append(string.Join(",", oauthParams.Select(x => string.Format("{0}=\"{1}\"", x.Key, Uri.EscapeDataString(x.Value)))));

            return authorization.ToString();
        }

        /// <summary>
        /// application/x-www-form-urlencoded 形式のクエリ文字列を IDictionary に変換します
        /// </summary>
        public static IDictionary<string, string> ParseQueryString(string queryString)
        {
            return queryString.Split('&')
                .Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(x => Uri.UnescapeDataString(x[0]), x => x.Length == 2 ? Uri.UnescapeDataString(x[1]) : "");
        }
    }
}
