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
using System.Threading;
using System.Threading.Tasks;

namespace OpenTween
{
    public static class AsyncBackports
    {
        #region Stream.CopyToAsync (.NET 4.0 implement)

        /// <summary>
        /// 現在のストリームからデータを読み込み別のストリームへ書き込む操作を非同期に行います
        /// </summary>
        public static Task CopyToAsync(this Stream from, Stream to)
        {
            return from.CopyToAsync(to, 4096);
        }

        /// <summary>
        /// 現在のストリームからデータを読み込み別のストリームへ書き込む操作を非同期に行います
        /// </summary>
        public static Task CopyToAsync(this Stream from, Stream to, CancellationToken token)
        {
            return from.CopyToAsync(to, 4096, token);
        }

        /// <summary>
        /// 現在のストリームからデータを読み込み別のストリームへ書き込む操作を非同期に行います
        /// </summary>
        public static Task CopyToAsync(this Stream from, Stream to, int bufferSize)
        {
            return from.CopyToAsync(to, bufferSize, CancellationToken.None);
        }

        /// <summary>
        /// 現在のストリームからデータを読み込み別のストリームへ書き込む操作を非同期に行います
        /// </summary>
        public static async Task CopyToAsync(this Stream from, Stream to, int bufferSize, CancellationToken token)
        {
            var buffer = new byte[bufferSize];
            int readLength;
            while ((readLength = await from.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                token.ThrowIfCancellationRequested();

                await to.WriteAsync(buffer, 0, readLength);

                token.ThrowIfCancellationRequested();
            }
        }

        #endregion

        #region Stream.ReadAsync (.NET 4.0 implement)

        /// <summary>
        /// 現在のストリームから非同期にデータを読み込みます
        /// </summary>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            return Task<int>.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, null);
        }

        #endregion

        #region Stream.WriteAsync (.NET 4.0 implement)

        /// <summary>
        /// 現在のストリームに非同期にデータを書き込みます
        /// </summary>
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
        }

        #endregion

        #region WebRequest.GetRequestStreamAsync (.NET 4.0 implement)

        /// <summary>
        /// リクエストデータを書き込むためのストリームを非同期で取得します
        /// </summary>
        public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
        }

        #endregion

        #region WebRequest.GetResponseAsync (.NET 4.0 implement)

        /// <summary>
        /// レスポンスデータを非同期に取得します
        /// </summary>
        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        }

        #endregion

        /// <summary>
        /// 指定された値を返すだけのタスクを生成します
        /// </summary>
        /// <remarks>
        /// .NET 4.5 では Task.FromResult() ですが、拡張メソッドの制約のため Task.Factory.FromResult() となっています
        /// </remarks>
        public static Task<T> FromResult<T>(this TaskFactory factory, T result)
        {
            var task = new TaskCompletionSource<T>(factory.CreationOptions);
            task.SetResult(result);
            return task.Task;
        }
    }
}
