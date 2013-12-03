﻿// OpenTween - Client of Twitter
// Copyright (c) 2012 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NSubstitute;
using OpenTween.Thumbnail;
using OpenTween.Thumbnail.Services;
using Xunit;
using Xunit.Extensions;
using System.Threading.Tasks;

namespace OpenTween
{
    public class TweetThumbnailTest
    {
        class TestThumbnailService : SimpleThumbnailService
        {
            protected string tooltip;

            public TestThumbnailService(string pattern, string replacement, string tooltip)
                : base(pattern, replacement)
            {
                this.tooltip = tooltip;
            }

            public override ThumbnailInfo GetThumbnailInfo(string url, PostClass post)
            {
                var thumbinfo = base.GetThumbnailInfo(url, post);

                if (thumbinfo != null && this.tooltip != null)
                {
                    var match = this.regex.Match(url);
                    thumbinfo.TooltipText = match.Result(this.tooltip);
                }

                return thumbinfo;
            }
        }

        public TweetThumbnailTest()
        {
            this.ThumbnailGeneratorSetup();
            this.MyCommonSetup();
        }

        public void ThumbnailGeneratorSetup()
        {
            ThumbnailGenerator.Services.Clear();
            ThumbnailGenerator.Services.AddRange(new[]
            {
                new TestThumbnailService(@"^https?://foo.example.com/(.+)$", @"dot.gif", null),
                new TestThumbnailService(@"^https?://bar.example.com/(.+)$", @"dot.gif", @"${1}"),
            });
        }

        public void MyCommonSetup()
        {
            var mockAssembly = Substitute.For<_Assembly>();
            mockAssembly.GetName().Returns(new AssemblyName("OpenTween"));
            MyCommon.EntryAssembly = mockAssembly;

            MyCommon.fileVersion = "1.0.0.0";
        }

        [Fact]
        public void CreatePictureBoxTest()
        {
            using (var thumbBox = new TweetThumbnail())
            {
                var method = typeof(TweetThumbnail).GetMethod("CreatePictureBox", BindingFlags.Instance | BindingFlags.NonPublic);
                var picbox = method.Invoke(thumbBox, new[] { "pictureBox1" }) as PictureBox;

                Assert.NotNull(picbox);
                Assert.Equal("pictureBox1", picbox.Name);
                Assert.Equal(PictureBoxSizeMode.Zoom, picbox.SizeMode);
                Assert.False(picbox.WaitOnLoad);
                Assert.Equal(DockStyle.Fill, picbox.Dock);

                picbox.Dispose();
            }
        }

        [Fact(Skip = "Mono環境でたまに InvaliOperationException: out of sync で異常終了する")]
        public void CancelAsyncTest()
        {
            using (var thumbbox = new TweetThumbnail())
            {
                var post = new PostClass();

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                var task = thumbbox.ShowThumbnailAsync(post);

                thumbbox.CancelAsync();

                Assert.Throws<AggregateException>(async () => await task);
                Assert.True(task.IsCanceled);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void SetThumbnailCountTest(int count)
        {
            using (var thumbbox = new TweetThumbnail())
            {
                var method = typeof(TweetThumbnail).GetMethod("SetThumbnailCount", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(thumbbox, new[] { (object)count });

                Assert.Equal(count, thumbbox.pictureBox.Count);

                var num = 0;
                foreach (var picbox in thumbbox.pictureBox)
                {
                    Assert.Equal("pictureBox" + num, picbox.Name);
                    num++;
                }

                Assert.Equal(thumbbox.pictureBox, thumbbox.panelPictureBox.Controls.Cast<OTPictureBox>());

                Assert.Equal(0, thumbbox.scrollBar.Minimum);
                Assert.Equal(count, thumbbox.scrollBar.Maximum);
            }
        }

        [Fact]
        public async Task ShowThumbnailAsyncTest()
        {
            var post = new PostClass
            {
                TextFromApi = "てすと http://foo.example.com/abcd",
                Media = new Dictionary<string, string>
                {
                    {"http://foo.example.com/abcd", "http://foo.example.com/abcd"},
                },
            };

            using (var thumbbox = new TweetThumbnail())
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                await thumbbox.ShowThumbnailAsync(post);

                Assert.Equal(0, thumbbox.scrollBar.Maximum);
                Assert.False(thumbbox.scrollBar.Enabled);

                Assert.Equal(1, thumbbox.pictureBox.Count);
                Assert.Equal("dot.gif", thumbbox.pictureBox[0].ImageLocation);

                Assert.IsType<ThumbnailInfo>(thumbbox.pictureBox[0].Tag);
                var thumbinfo = (ThumbnailInfo)thumbbox.pictureBox[0].Tag;

                Assert.Equal("http://foo.example.com/abcd", thumbinfo.ImageUrl);
                Assert.Equal("dot.gif", thumbinfo.ThumbnailUrl);

                Assert.Equal("", thumbbox.toolTip.GetToolTip(thumbbox.pictureBox[0]));
            }
        }

        [Fact]
        public async Task ShowThumbnailAsyncTest2()
        {
            var post = new PostClass
            {
                TextFromApi = "てすと http://foo.example.com/abcd http://bar.example.com/efgh",
                Media = new Dictionary<string, string>
                {
                    {"http://foo.example.com/abcd", "http://foo.example.com/abcd"},
                    {"http://bar.example.com/efgh", "http://bar.example.com/efgh"},
                },
            };

            using (var thumbbox = new TweetThumbnail())
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                await thumbbox.ShowThumbnailAsync(post);

                Assert.Equal(1, thumbbox.scrollBar.Maximum);
                Assert.True(thumbbox.scrollBar.Enabled);

                Assert.Equal(2, thumbbox.pictureBox.Count);
                Assert.Equal("dot.gif", thumbbox.pictureBox[0].ImageLocation);
                Assert.Equal("dot.gif", thumbbox.pictureBox[1].ImageLocation);

                Assert.IsType<ThumbnailInfo>(thumbbox.pictureBox[0].Tag);
                var thumbinfo = (ThumbnailInfo)thumbbox.pictureBox[0].Tag;

                Assert.Equal("http://foo.example.com/abcd", thumbinfo.ImageUrl);
                Assert.Equal("dot.gif", thumbinfo.ThumbnailUrl);

                Assert.IsType<ThumbnailInfo>(thumbbox.pictureBox[1].Tag);
                thumbinfo = (ThumbnailInfo)thumbbox.pictureBox[1].Tag;

                Assert.Equal("http://bar.example.com/efgh", thumbinfo.ImageUrl);
                Assert.Equal("dot.gif", thumbinfo.ThumbnailUrl);

                Assert.Equal("", thumbbox.toolTip.GetToolTip(thumbbox.pictureBox[0]));
                Assert.Equal("efgh", thumbbox.toolTip.GetToolTip(thumbbox.pictureBox[1]));
            }
        }

        [Fact]
        public async Task ThumbnailLoadingEventTest()
        {
            using (var thumbbox = new TweetThumbnail())
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

                bool eventCalled;
                thumbbox.ThumbnailLoading +=
                    (s, e) => { eventCalled = true; };

                var post = new PostClass
                {
                    TextFromApi = "てすと",
                    Media = new Dictionary<string, string>
                    {
                    },
                };
                eventCalled = false;
                await thumbbox.ShowThumbnailAsync(post);

                Assert.False(eventCalled);

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

                var post2 = new PostClass
                {
                    TextFromApi = "てすと http://foo.example.com/abcd",
                    Media = new Dictionary<string, string>
                    {
                        {"http://foo.example.com/abcd", "http://foo.example.com/abcd"},
                    },
                };
                eventCalled = false;
                await thumbbox.ShowThumbnailAsync(post2);

                Assert.True(eventCalled);
            }
        }

        [Fact]
        public async Task ScrollTest()
        {
            var post = new PostClass
            {
                TextFromApi = "てすと http://foo.example.com/abcd http://foo.example.com/efgh",
                Media = new Dictionary<string, string>
                {
                    {"http://foo.example.com/abcd", "http://foo.example.com/abcd"},
                    {"http://foo.example.com/efgh", "http://foo.example.com/efgh"},
                },
            };

            using (var thumbbox = new TweetThumbnail())
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                await thumbbox.ShowThumbnailAsync(post);

                Assert.Equal(0, thumbbox.scrollBar.Minimum);
                Assert.Equal(1, thumbbox.scrollBar.Maximum);

                thumbbox.scrollBar.Value = 0;

                thumbbox.ScrollUp();
                Assert.Equal(1, thumbbox.scrollBar.Value);
                Assert.False(thumbbox.pictureBox[0].Visible);
                Assert.True(thumbbox.pictureBox[1].Visible);

                thumbbox.ScrollUp();
                Assert.Equal(1, thumbbox.scrollBar.Value);
                Assert.False(thumbbox.pictureBox[0].Visible);
                Assert.True(thumbbox.pictureBox[1].Visible);

                thumbbox.ScrollDown();
                Assert.Equal(0, thumbbox.scrollBar.Value);
                Assert.True(thumbbox.pictureBox[0].Visible);
                Assert.False(thumbbox.pictureBox[1].Visible);

                thumbbox.ScrollDown();
                Assert.Equal(0, thumbbox.scrollBar.Value);
                Assert.True(thumbbox.pictureBox[0].Visible);
                Assert.False(thumbbox.pictureBox[1].Visible);
            }
        }
    }
}
