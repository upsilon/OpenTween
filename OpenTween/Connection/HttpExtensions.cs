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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenTween.Connection
{
    public static class HttpExtensions
    {
        /// <summary>
        /// レスポンスのステータスコードが 4xx, 5xx であるか否かを返す
        /// </summary>
        public static bool IsErrorStatus(this HttpWebResponse response)
        {
            var statusCode = (int)response.StatusCode;
            return statusCode >= 400 && statusCode <= 599;
        }

        /// <summary>
        /// 4xx, 5xx のステータスコードの場合に WebException をスローします
        /// </summary>
        /// <exception cref="WebException"></exception>
        public static void ThrowIfErrorStatus(this HttpWebResponse response)
        {
            if (response.IsErrorStatus())
            {
                var message = string.Format("{0} ({1})", response.StatusDescription, response.StatusCode);
                throw new WebException(message, null, WebExceptionStatus.ProtocolError, response);
            }
        }

        /// <summary>
        /// レスポンス本文を文字列として取得します
        /// </summary>
        public static string GetResponseString(this HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// レスポンス本文をバイト型配列として取得します
        /// </summary>
        public static async Task<byte[]> GetResponseBytesAsync(this HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var memstream = new MemoryStream())
            {
                await stream.CopyToAsync(memstream);
                return memstream.ToArray();
            }
        }
    }
}
