﻿// OpenTween - Client of Twitter
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
using System.Text;
using System.Xml.Serialization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace OpenTween
{
    /// <summary>
    /// タブで使用する振り分けルールを表すクラス
    /// </summary>
    [Serializable]
    public class FiltersClass
    {
        /// <summary>
        /// Compile() メソッドの呼び出しが必要な状態か否か
        /// </summary>
        [XmlIgnore]
        public bool IsDirty { get; protected set; }

        /// <summary>
        /// ExecFilter() メソッドの実行時に自動でコンパイルを実行する
        /// </summary>
        /// <remarks>
        /// テスト用途以外では AutoCompile に頼らず事前に Compile() メソッドを呼び出すこと
        /// </remarks>
        internal static bool AutoCompile { get; set; }

        /// <summary>
        /// コンパイルされた振り分けルール
        /// </summary>
        [XmlIgnore]
        protected Func<PostClass, MyCommon.HITRESULT> FilterDelegate;

        /// <summary>
        /// 振り分けルールの概要
        /// </summary>
        /// <remarks>
        /// Compile() メソッドを実行するまで更新されません
        /// </remarks>
        [XmlIgnore]
        public string SummaryText { get; protected set; }

        [XmlElement("NameFilter")]
        public string FilterName
        {
            get { return this._FilterName; }
            set
            {
                this.IsDirty = true;
                this._FilterName = value;
            }
        }
        private string _FilterName;

        [XmlElement("ExNameFilter")]
        public string ExFilterName
        {
            get { return this._ExFilterName; }
            set
            {
                this.IsDirty = true;
                this._ExFilterName = value;
            }
        }
        private string _ExFilterName;

        [XmlArray("BodyFilterArray")]
        public string[] FilterBody
        {
            get { return this._FilterBody; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                this.IsDirty = true;
                this._FilterBody = value;
            }
        }
        private string[] _FilterBody = new string[0];

        [XmlArray("ExBodyFilterArray")]
        public string[] ExFilterBody
        {
            get { return this._ExFilterBody; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                this.IsDirty = true;
                this._ExFilterBody = value;
            }
        }
        private string[] _ExFilterBody = new string[0];

        [XmlElement("SearchBoth")]
        public bool UseNameField
        {
            get { return this._UseNameField; }
            set
            {
                this.IsDirty = true;
                this._UseNameField = value;
            }
        }
        private bool _UseNameField;

        [XmlElement("ExSearchBoth")]
        public bool ExUseNameField
        {
            get { return this._ExUseNameField; }
            set
            {
                this.IsDirty = true;
                this._ExUseNameField = value;
            }
        }
        private bool _ExUseNameField;

        [XmlElement("MoveForm")]
        public bool MoveMatches
        {
            get { return this._MoveMatches; }
            set
            {
                this.IsDirty = true;
                this._MoveMatches = value;
            }
        }
        private bool _MoveMatches;

        [XmlElement("SetMark")]
        public bool MarkMatches
        {
            get { return this._MarkMatches; }
            set
            {
                this.IsDirty = true;
                this._MarkMatches = value;
            }
        }
        private bool _MarkMatches;

        [XmlElement("SearchUrl")]
        public bool FilterByUrl
        {
            get { return this._FilterByUrl; }
            set
            {
                this.IsDirty = true;
                this._FilterByUrl = value;
            }
        }
        private bool _FilterByUrl;

        [XmlElement("ExSearchUrl")]
        public bool ExFilterByUrl
        {
            get { return this._ExFilterByUrl; }
            set
            {
                this.IsDirty = true;
                this._ExFilterByUrl = value;
            }
        }
        private bool _ExFilterByUrl;

        public bool CaseSensitive
        {
            get { return this._CaseSensitive; }
            set
            {
                this.IsDirty = true;
                this._CaseSensitive = value;
            }
        }
        private bool _CaseSensitive;

        public bool ExCaseSensitive
        {
            get { return this._ExCaseSensitive; }
            set
            {
                this.IsDirty = true;
                this._ExCaseSensitive = value;
            }
        }
        private bool _ExCaseSensitive;

        public bool UseLambda
        {
            get { return this._UseLambda; }
            set
            {
                this.IsDirty = true;
                this._UseLambda = value;
            }
        }
        private bool _UseLambda;

        public bool ExUseLambda
        {
            get { return this._ExUseLambda; }
            set
            {
                this.IsDirty = true;
                this._ExUseLambda = value;
            }
        }
        private bool _ExUseLambda;

        public bool UseRegex
        {
            get { return this._UseRegex; }
            set
            {
                this.IsDirty = true;
                this._UseRegex = value;
            }
        }
        private bool _UseRegex;

        public bool ExUseRegex
        {
            get { return this._ExUseRegex; }
            set
            {
                this.IsDirty = true;
                this._ExUseRegex = value;
            }
        }
        private bool _ExUseRegex;

        [XmlElement("IsRt")]
        public bool FilterRt
        {
            get { return this._FilterRt; }
            set
            {
                this.IsDirty = true;
                this._FilterRt = value;
            }
        }
        private bool _FilterRt;

        [XmlElement("IsExRt")]
        public bool ExFilterRt
        {
            get { return this._ExFilterRt; }
            set
            {
                this.IsDirty = true;
                this._ExFilterRt = value;
            }
        }
        private bool _ExFilterRt;

        [XmlElement("Source")]
        public string FilterSource
        {
            get { return this._FilterSource; }
            set
            {
                this.IsDirty = true;
                this._FilterSource = value;
            }
        }
        private string _FilterSource;

        [XmlElement("ExSource")]
        public string ExFilterSource
        {
            get { return this._ExFilterSource; }
            set
            {
                this.IsDirty = true;
                this._ExFilterSource = value;
            }
        }
        private string _ExFilterSource;

        public FiltersClass()
        {
            this.IsDirty = true;
            this.SummaryText = "";

            this.MarkMatches = true;
            this.UseNameField = true;
            this.ExUseNameField = true;
        }

        static FiltersClass()
        {
            // TODO: TabsClass とかの改修が終わるまでデフォルト有効
            FiltersClass.AutoCompile = true;
        }

        /// <summary>
        /// 振り分けルールをコンパイルします
        /// </summary>
        public void Compile()
        {
            var postParam = Expression.Parameter(typeof(PostClass), "x");

            var matchExpr = this.MakeFiltersExpr(
                postParam,
                this.FilterName, this.FilterBody, this.FilterSource, this.FilterRt,
                this.UseRegex, this.CaseSensitive, this.UseNameField, this.UseLambda, this.FilterByUrl);

            var excludeExpr = this.MakeFiltersExpr(
                postParam,
                this.ExFilterName, this.ExFilterBody, this.ExFilterSource, this.ExFilterRt,
                this.ExUseRegex, this.ExCaseSensitive, this.ExUseNameField, this.ExUseLambda, this.ExFilterByUrl);

            Expression<Func<PostClass, MyCommon.HITRESULT>> filterExpr;

            if (matchExpr != null)
            {
                MyCommon.HITRESULT hitResult;

                if (this.MoveMatches)
                    hitResult = MyCommon.HITRESULT.Move;
                else if (this.MarkMatches)
                    hitResult = MyCommon.HITRESULT.CopyAndMark;
                else
                    hitResult = MyCommon.HITRESULT.Copy;

                if (excludeExpr != null)
                {
                    // x => matchExpr ? (!excludeExpr ? hitResult : None) : None
                    filterExpr =
                        Expression.Lambda<Func<PostClass, MyCommon.HITRESULT>>(
                            Expression.Condition(
                                matchExpr,
                                Expression.Condition(
                                    Expression.Not(excludeExpr),
                                    Expression.Constant(hitResult),
                                    Expression.Constant(MyCommon.HITRESULT.None)),
                                Expression.Constant(MyCommon.HITRESULT.None)),
                            postParam);
                }
                else
                {
                    // x => matchExpr ? hitResult : None
                    filterExpr =
                        Expression.Lambda<Func<PostClass, MyCommon.HITRESULT>>(
                            Expression.Condition(
                                matchExpr,
                                Expression.Constant(hitResult),
                                Expression.Constant(MyCommon.HITRESULT.None)),
                            postParam);
                }
            }
            else if (excludeExpr != null)
            {
                // x => excludeExpr ? Exclude : None
                filterExpr =
                    Expression.Lambda<Func<PostClass, MyCommon.HITRESULT>>(
                        Expression.Condition(
                            excludeExpr,
                            Expression.Constant(MyCommon.HITRESULT.Exclude),
                            Expression.Constant(MyCommon.HITRESULT.None)),
                        postParam);
            }
            else // matchExpr == null && excludeExpr == null
            {
                filterExpr = x => MyCommon.HITRESULT.None;
            }

            this.FilterDelegate = filterExpr.Compile();
            this.SummaryText = this.MakeSummary();
            this.IsDirty = false;
        }

        protected virtual Expression MakeFiltersExpr(
            ParameterExpression postParam,
            string filterName, string[] filterBody, string filterSource, bool filterRt,
            bool useRegex, bool caseSensitive, bool useNameField, bool useLambda, bool filterByUrl)
        {
            var filterExprs = new List<Expression>();

            if (useNameField && !string.IsNullOrEmpty(filterName))
            {
                filterExprs.Add(this.MakeNameFilter(postParam, filterName, useRegex, caseSensitive));
            }
            foreach (var body in filterBody)
            {
                Expression bodyExpr;
                if (useLambda)
                {
                    // TODO DynamicQuery相当のGPLv3互換なライブラリで置換する
                    Expression<Func<PostClass, bool>> lambdaExpr = x => false;
                    bodyExpr = lambdaExpr.Body;
                }
                else
                {
                    if (filterByUrl)
                        bodyExpr = this.MakeGenericFilter(postParam, "Text", body, useRegex, caseSensitive);
                    else
                        bodyExpr = this.MakeGenericFilter(postParam, "TextFromApi", body, useRegex, caseSensitive);

                    if (!useNameField)
                    {
                        // useNameField = false の場合は ScreenName も filterBody のマッチ対象となる
                        bodyExpr = Expression.OrElse(
                            bodyExpr,
                            this.MakeGenericFilter(postParam, "ScreenName", body, useRegex, caseSensitive));
                    }
                }

                filterExprs.Add(bodyExpr);
            }
            if (!string.IsNullOrEmpty(filterSource))
            {
                if (filterByUrl)
                    filterExprs.Add(this.MakeGenericFilter(postParam, "SourceHtml", filterSource, useRegex, caseSensitive));
                else
                    filterExprs.Add(this.MakeGenericFilter(postParam, "Source", filterSource, useRegex, caseSensitive, exactMatch: true));
            }
            if (filterRt)
            {
                // x.RetweetedId != null
                filterExprs.Add(Expression.NotEqual(
                    Expression.Property(
                        postParam,
                        typeof(PostClass).GetProperty("RetweetedId")),
                    Expression.Constant(null)));
            }

            if (filterExprs.Count == 0)
            {
                return null;
            }
            else
            {
                // filterExpr[0] && filterExpr[1] && ...
                var filterExpr = filterExprs[0];
                foreach (var expr in filterExprs.Skip(1))
                {
                    filterExpr = Expression.AndAlso(filterExpr, expr);
                }

                return filterExpr;
            }
        }

        protected Expression MakeNameFilter(ParameterExpression postParam, string pattern, bool useRegex, bool caseSensitive)
        {
            Expression<Func<PostClass, bool>> nameLambda;

            if (useRegex)
            {
                var regex = new Regex(pattern, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

                nameLambda = x => regex.IsMatch(x.ScreenName) || regex.IsMatch(x.RetweetedBy);
            }
            else
            {
                if (caseSensitive)
                    nameLambda = x => pattern == x.ScreenName || pattern == x.RetweetedBy;
                else
                    nameLambda = x => pattern.Equals(x.ScreenName, StringComparison.OrdinalIgnoreCase) ||
                        pattern.Equals(x.RetweetedBy, StringComparison.OrdinalIgnoreCase);
            }

            return Expression.Invoke(nameLambda, postParam);
        }

        protected Expression MakeGenericFilter(
            ParameterExpression postParam, string targetFieldName, string pattern,
            bool useRegex, bool caseSensitive, bool exactMatch = false)
        {
            if (useRegex)
            {
                var regex = new Regex(pattern, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

                // regex.IsMatch(x.<targetFieldName>)
                return Expression.Call(
                    Expression.Constant(regex),
                    typeof(Regex).GetMethod("IsMatch", new[] { typeof(string) }),
                    Expression.Property(
                        postParam,
                        typeof(PostClass).GetProperty(targetFieldName)));
            }
            else
            {
                var compOpt = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (exactMatch)
                {
                    // 完全一致
                    // pattern.Equals(x.<targetFieldName>, compOpt)
                    return Expression.Call(
                        Expression.Constant(pattern),
                        typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(StringComparison) }),
                        Expression.Property(
                            postParam,
                            typeof(PostClass).GetProperty(targetFieldName)),
                        Expression.Constant(compOpt));

                }
                else
                {
                    // 部分一致
                    // x.<targetFieldName>.IndexOf(pattern, compOpt) != -1
                    return Expression.NotEqual(
                        Expression.Call(
                            Expression.Property(
                                postParam,
                                typeof(PostClass).GetProperty(targetFieldName)),
                            typeof(string).GetMethod("IndexOf", new[] { typeof(string), typeof(StringComparison) }),
                            Expression.Constant(pattern),
                            Expression.Constant(compOpt)),
                        Expression.Constant(-1));
                }
            }
        }

        /// <summary>
        /// ツイートの振り分けを実行する
        /// </summary>
        /// <param name="post">振り分けるツイート</param>
        /// <returns>振り分け結果</returns>
        public MyCommon.HITRESULT ExecFilter(PostClass post)
        {
            if (this.IsDirty)
            {
                if (!FiltersClass.AutoCompile)
                    throw new InvalidOperationException("振り分け実行前に Compile() を呼び出す必要があります");

                this.Compile();
            }

            return this.FilterDelegate(post);
        }

        public FiltersClass Clone()
        {
            return new FiltersClass
            {
                FilterBody = this.FilterBody,
                FilterName = this.FilterName,
                FilterSource = this.FilterSource,
                FilterRt = this.FilterRt,
                FilterByUrl = this.FilterByUrl,
                UseLambda = this.UseLambda,
                UseNameField = this.UseNameField,
                UseRegex = this.UseRegex,
                CaseSensitive = this.CaseSensitive,
                ExFilterBody = this.ExFilterBody,
                ExFilterName = this.ExFilterName,
                ExFilterSource = this.ExFilterSource,
                ExFilterRt = this.ExFilterRt,
                ExFilterByUrl = this.ExFilterByUrl,
                ExUseLambda = this.ExUseLambda,
                ExUseNameField = this.ExUseNameField,
                ExUseRegex = this.ExUseRegex,
                ExCaseSensitive = this.ExCaseSensitive,
                MoveMatches = this.MoveMatches,
                MarkMatches = this.MarkMatches,
            };
        }

        /// <summary>
        /// 振り分けルールの文字列表現を返します
        /// </summary>
        /// <returns>振り分けルールの概要</returns>
        public override string ToString()
        {
            return this.SummaryText;
        }

        #region from Tween v1.1.0.0

        // The code in this region block is based on code written by the following authors:
        //   (C) 2007 kiri_feather (@kiri_feather) <kiri.feather@gmail.com>
        //   (C) 2008 Moz (@syo68k)

        /// <summary>
        /// フィルタ一覧に表示する文言生成
        /// </summary>
        protected virtual string MakeSummary()
        {
            var fs = new StringBuilder();
            if (!string.IsNullOrEmpty(this.FilterName) || this.FilterBody.Length > 0 || this.FilterRt || !string.IsNullOrEmpty(this.FilterSource))
            {
                if (this.UseNameField)
                {
                    if (!string.IsNullOrEmpty(this.FilterName))
                    {
                        fs.AppendFormat(Properties.Resources.SetFiltersText1, this.FilterName);
                    }
                    else
                    {
                        fs.Append(Properties.Resources.SetFiltersText2);
                    }
                }
                if (this.FilterBody.Length > 0)
                {
                    fs.Append(Properties.Resources.SetFiltersText3);
                    foreach (var bf in this.FilterBody)
                    {
                        fs.Append(bf);
                        fs.Append(" ");
                    }
                    fs.Length--;
                    fs.Append(Properties.Resources.SetFiltersText4);
                }
                fs.Append("(");
                if (this.UseNameField)
                {
                    fs.Append(Properties.Resources.SetFiltersText5);
                }
                else
                {
                    fs.Append(Properties.Resources.SetFiltersText6);
                }
                if (this.UseRegex)
                {
                    fs.Append(Properties.Resources.SetFiltersText7);
                }
                if (this.FilterByUrl)
                {
                    fs.Append(Properties.Resources.SetFiltersText8);
                }
                if (this.CaseSensitive)
                {
                    fs.Append(Properties.Resources.SetFiltersText13);
                }
                if (this.FilterRt)
                {
                    fs.Append("RT/");
                }
                if (this.UseLambda)
                {
                    fs.Append("LambdaExp/");
                }
                if (!string.IsNullOrEmpty(this.FilterSource))
                {
                    fs.AppendFormat("Src…{0}/", this.FilterSource);
                }
                fs.Length--;
                fs.Append(")");
            }
            if (!string.IsNullOrEmpty(this.ExFilterName) || this.ExFilterBody.Length > 0 || this.ExFilterRt || !string.IsNullOrEmpty(this.ExFilterSource))
            {
                //除外
                fs.Append(Properties.Resources.SetFiltersText12);
                if (this.ExUseNameField)
                {
                    if (!string.IsNullOrEmpty(this.ExFilterName))
                    {
                        fs.AppendFormat(Properties.Resources.SetFiltersText1, this.ExFilterName);
                    }
                    else
                    {
                        fs.Append(Properties.Resources.SetFiltersText2);
                    }
                }
                if (this.ExFilterBody.Length > 0)
                {
                    fs.Append(Properties.Resources.SetFiltersText3);
                    foreach (var bf in this.ExFilterBody)
                    {
                        fs.Append(bf);
                        fs.Append(" ");
                    }
                    fs.Length--;
                    fs.Append(Properties.Resources.SetFiltersText4);
                }
                fs.Append("(");
                if (this.ExUseNameField)
                {
                    fs.Append(Properties.Resources.SetFiltersText5);
                }
                else
                {
                    fs.Append(Properties.Resources.SetFiltersText6);
                }
                if (this.ExUseRegex)
                {
                    fs.Append(Properties.Resources.SetFiltersText7);
                }
                if (this.ExFilterByUrl)
                {
                    fs.Append(Properties.Resources.SetFiltersText8);
                }
                if (this.ExCaseSensitive)
                {
                    fs.Append(Properties.Resources.SetFiltersText13);
                }
                if (this.ExFilterRt)
                {
                    fs.Append("RT/");
                }
                if (this.ExUseLambda)
                {
                    fs.Append("LambdaExp/");
                }
                if (!string.IsNullOrEmpty(this.ExFilterSource))
                {
                    fs.AppendFormat("Src…{0}/", this.ExFilterSource);
                }
                fs.Length--;
                fs.Append(")");
            }

            fs.Append("(");
            if (this.MoveMatches)
            {
                fs.Append(Properties.Resources.SetFiltersText9);
            }
            else
            {
                fs.Append(Properties.Resources.SetFiltersText11);
            }
            if (!this.MoveMatches && this.MarkMatches)
            {
                fs.Append(Properties.Resources.SetFiltersText10);
            }
            else if (!this.MoveMatches)
            {
                fs.Length--;
            }

            fs.Append(")");

            return fs.ToString();
        }
        #endregion
    }
}
