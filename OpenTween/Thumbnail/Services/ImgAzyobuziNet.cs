﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Json;
// OpenTween - Client of Twitter
// Copyright (c) 2012      kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
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

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace OpenTween.Thumbnail.Services
{
    class ImgAzyobuziNet : IThumbnailService
    {
        private readonly string[] ApiHosts = { "http://img.azyobuzi.net/api/", "http://img.opentween.org/api/" };

        private string ApiBase;
        private IEnumerable<Regex> UrlRegex = new Regex[] {};

        private object LockObj = new object();

        public ImgAzyobuziNet()
        {
            this.LoadRegex();

            Task.Factory.StartNew(() =>
            {
                for (;;)
                {
                    Thread.Sleep(30 * 60 * 1000); // 30分おきに更新
                    this.LoadRegex();
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void LoadRegex()
        {
            foreach (var host in this.ApiHosts)
            {
                try
                {
                    var result = this.LoadRegex(host);
                    if (result) return;
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
            }
        }

        public bool LoadRegex(string apiBase)
        {
            try
            {
                using (var client = new OTWebClient() { Timeout = 1000 })
                using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(client.DownloadData(apiBase + "regex.json"), XmlDictionaryReaderQuotas.Max))
                {
                    var xElm = XElement.Load(jsonReader);

                    lock (this.LockObj)
                    {
                        this.UrlRegex = xElm.Elements("item")
                            .Select(e => new Regex(e.Element("regex").Value, RegexOptions.IgnoreCase));
                        this.ApiBase = apiBase;
                    }
                }

                return true;
            }
            catch (WebException) { }

            return false;
        }

        public override ThumbnailInfo GetThumbnailInfo(string url, PostClass post)
        {
            lock (this.LockObj)
            {
                foreach (var regex in this.UrlRegex)
                {
                    if (regex.IsMatch(url))
                    {
                        return new ThumbnailInfo()
                        {
                            ImageUrl = url,
                            ThumbnailUrl = this.ApiBase + "redirect?uri=" + Uri.EscapeDataString(url),
                            TooltipText = null,
                        };
                    }
                }
            }

            return null;
        }
    }
}
