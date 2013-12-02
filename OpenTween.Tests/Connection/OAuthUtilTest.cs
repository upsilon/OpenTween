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
using Xunit;
using Xunit.Extensions;

namespace OpenTween.Connection
{
    public class OAuthUtilTest
    {
        [Fact]
        public void GenerateNonce_LengthTest()
        {
            var nonce = OAuthUtil.GenerateNonce();

            // 32 バイト = 256 ビット なので Base64 では 44 文字になるはず
            // (256 / 6 ≒ 42.666 文字、4 の倍数に切り上げて 44 文字)
            Assert.Equal(44, nonce.Length);
        }

        [Fact]
        public void GenerateNonce_RandomTest()
        {
            var nonce1 = OAuthUtil.GenerateNonce();
            var nonce2 = OAuthUtil.GenerateNonce();

            Assert.NotEqual(nonce1, nonce2);
        }

        public static IEnumerable<object[]> ConvertToUnixTimeTest_TestCases
        {
            get
            {
                yield return new object[] { new DateTime(2013, 12, 1, 0, 0, 0, DateTimeKind.Utc), 1385856000L };

                var localdate = new DateTime(2013, 12, 1, 0, 0, 0, DateTimeKind.Local);
                var offset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(localdate).TotalSeconds;
                yield return new object[] { localdate, 1385856000L - offset };
            }
        }

        [Theory]
        [PropertyData("ConvertToUnixTimeTest_TestCases")]
        public void ConvertToUnixTimeTest(DateTime target, long expected)
        {
            Assert.Equal(expected, OAuthUtil.ConvertToUnixTime(target));
        }

        [Fact]
        public void AddCommonParametersTest()
        {
            OAuthUtil.GetDateTimeUtcNow = () => new DateTime(2013, 12, 1, 0, 0, 0, DateTimeKind.Utc);

            var param = new Dictionary<string, string>();
            OAuthUtil.AddCommonParameters(param, "hogehoge");

            Assert.Equal("hogehoge", param["oauth_consumer_key"]);
            Assert.Equal(44, param["oauth_nonce"].Length);
            Assert.Equal("HMAC-SHA1", param["oauth_signature_method"]);
            Assert.Equal("1385856000", param["oauth_timestamp"]);
            Assert.Equal("1.0", param["oauth_version"]);
        }

        [Fact]
        public void GenerateSignature_UnknownMethodTest()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
                {"oauth_signature_method", "RSA-SHA1"},
                {"oauth_timestamp", "1234567890"},
                {"oauth_token", "access_key"},
                {"status", "test"},
            };

            Assert.Throws<NotImplementedException>(() => OAuthUtil.GenerateSignature(
                "POST",
                new Uri("https://api.twitter.com/1.1/statuses/update.json"),
                param, "consumer_secret&access_secret", "RSA-SHA1"));
        }

        [Fact]
        public void GenerateSignature_HmacSha1Test()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_timestamp", "1234567890"},
                {"oauth_token", "access_key"},
                {"status", "test"},
            };

            Assert.Equal("FhfjSY23Bajs8ankp8ULfPBgMxE=", OAuthUtil.GenerateSignature(
                "POST",
                new Uri("https://api.twitter.com/1.1/statuses/update.json"),
                param, "consumer_secret&access_secret", "HMAC-SHA1"));
        }

        [Fact]
        public void GenerateSignature_HmacSha1_RemoveQueryTest()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_timestamp", "1234567890"},
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_token", "access_key"},
                {"status", "test"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
            };

            Assert.Equal("FhfjSY23Bajs8ankp8ULfPBgMxE=", OAuthUtil.GenerateSignature(
                "POST",
                new Uri("https://api.twitter.com/1.1/statuses/update.json?status=test"), // クエリ部分は署名時には無視される
                param, "consumer_secret&access_secret", "HMAC-SHA1"));
        }

        [Fact]
        public void GenerateSignature_HmacSha1_UnsortedTest()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_timestamp", "1234567890"},
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_token", "access_key"},
                {"status", "test"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
            };

            Assert.Equal("FhfjSY23Bajs8ankp8ULfPBgMxE=", OAuthUtil.GenerateSignature(
                "POST",
                new Uri("https://api.twitter.com/1.1/statuses/update.json"),
                param, "consumer_secret&access_secret", "HMAC-SHA1"));
        }

        [Fact]
        public void GenerateSignature_HmacSha1_SortedTest()
        {
            var param = new SortedDictionary<string, string>
            {
                {"oauth_timestamp", "1234567890"},
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_token", "access_key"},
                {"status", "test"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
            };

            Assert.Equal("FhfjSY23Bajs8ankp8ULfPBgMxE=", OAuthUtil.GenerateSignature(
                "POST",
                new Uri("https://api.twitter.com/1.1/statuses/update.json"),
                param, "consumer_secret&access_secret", "HMAC-SHA1"));
        }

        [Fact]
        public void CreateAuthorization_WithoutRealmTest()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_timestamp", "1234567890"},
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_token", "access_key"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
                {"oauth_signature", "FhfjSY23Bajs8ankp8ULfPBgMxE="},
            };

            var expected = "OAuth " +
                "oauth_timestamp=\"1234567890\"," +
                "oauth_consumer_key=\"consumer_key\"," +
                "oauth_token=\"access_key\"," +
                "oauth_signature_method=\"HMAC-SHA1\"," +
                "oauth_nonce=\"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr\"," +
                "oauth_signature=\"FhfjSY23Bajs8ankp8ULfPBgMxE%3D\"";
            Assert.Equal(expected, OAuthUtil.CreateAuthorization(param, null));
        }

        [Fact]
        public void CreateAuthorization_WithRealmTest()
        {
            var param = new Dictionary<string, string>
            {
                {"oauth_timestamp", "1234567890"},
                {"oauth_consumer_key", "consumer_key"},
                {"oauth_token", "access_key"},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_nonce", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr"},
                {"oauth_signature", "FhfjSY23Bajs8ankp8ULfPBgMxE="},
            };
            var realm = new Uri("http://example.com/");

            var expected = "OAuth " +
                "realm=\"http%3A%2F%2Fexample.com%2F\"," +
                "oauth_timestamp=\"1234567890\"," +
                "oauth_consumer_key=\"consumer_key\"," +
                "oauth_token=\"access_key\"," +
                "oauth_signature_method=\"HMAC-SHA1\"," +
                "oauth_nonce=\"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijhlmnopqr\"," +
                "oauth_signature=\"FhfjSY23Bajs8ankp8ULfPBgMxE%3D\"";
            Assert.Equal(expected, OAuthUtil.CreateAuthorization(param, realm));
        }

        [Fact]
        public void ParseQueryString_SimpleTest()
        {
            var queryString = "aaa=hoge";

            var expected = new Dictionary<string, string>
            {
                {"aaa", "hoge"},
            };
            Assert.Equal(expected, OAuthUtil.ParseQueryString(queryString));
        }

        [Fact]
        public void ParseQueryString_MultipleTest()
        {
            var queryString = "aaa=hoge&bbb=hogehoge";

            var expected = new Dictionary<string, string>
            {
                {"aaa", "hoge"},
                {"bbb", "hogehoge"},
            };
            Assert.Equal(expected, OAuthUtil.ParseQueryString(queryString));
        }

        [Fact]
        public void ParseQueryString_EmptyValueTest()
        {
            var queryString = "aaa=";

            var expected = new Dictionary<string, string>
            {
                {"aaa", ""},
            };
            Assert.Equal(expected, OAuthUtil.ParseQueryString(queryString));
        }

        [Fact]
        public void ParseQueryString_EmptyValue2Test()
        {
            var queryString = "aaa";

            var expected = new Dictionary<string, string>
            {
                {"aaa", ""},
            };
            Assert.Equal(expected, OAuthUtil.ParseQueryString(queryString));
        }

        [Fact]
        public void ParseQueryString_EscapedTest()
        {
            var queryString = "a%2Fb%2Fc=d%3De%3Df";

            var expected = new Dictionary<string, string>
            {
                {"a/b/c", "d=e=f"},
            };
            Assert.Equal(expected, OAuthUtil.ParseQueryString(queryString));
        }
    }
}
