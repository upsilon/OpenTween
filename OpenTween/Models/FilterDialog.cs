// OpenTween - Client of Twitter
// Copyright (c) 2007-2011 kiri_feather (@kiri_feather) <kiri.feather@gmail.com>
//           (c) 2008-2011 Moz (@syo68k)
//           (c) 2008-2011 takeshik (@takeshik) <http://www.takeshik.org/>
//           (c) 2010-2011 anis774 (@anis774) <http://d.hatena.ne.jp/anis774/>
//           (c) 2010-2011 fantasticswallow (@f_swallow) <http://twitter.com/f_swallow>
//           (c) 2017      kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenTween.Models
{
    public class FilterDialog
    {
        public TabInformations TabInfo
            => TabInformations.GetInstance();

        public BindingList<TabModel> Tabs => this.tabs;
        public BindingList<PostFilterRule> Filters => this.filters;

        public TabModel SelectedTab
            => this.SelectedTabIndex != -1 ? this.tabs[this.SelectedTabIndex] : null;

        public IEnumerable<PostFilterRule> SelectedFilters
            => this.SelectedFilterIndices.Select(x => this.filters[x]);

        public int SelectedTabIndex { get; private set; } = -1;
        public int[] SelectedFilterIndices { get; private set; } = new int[0];
        public PostFilterRule EditingFilter { get; private set; }
        public EDITMODE FilterEditMode { get; private set; }
        public bool MatchRuleComplex { get; private set; }
        public bool ExcludeRuleComplex { get; private set; }
        public EnabledButtonState FilterEnabledButtonState { get; private set; }

        public event EventHandler SelectedTabChanged;
        public event EventHandler FiltersChanged;
        public event EventHandler SelectedFiltersChanged;
        public event EventHandler EditingFilterChanged;
        public event EventHandler FilterEditModeEnter;
        public event EventHandler FilterEditModeExit;
        public event EventHandler MatchRuleComplexChanged;
        public event EventHandler ExcludeRuleComplexChanged;
        public event EventHandler FilterEnabledButtonStateChanged;

        public event EventHandler<AddNewTabFailedEventArgs> AddNewTabFailed;
        public event EventHandler<FilterEditErrorEventArgs> FilterEditError;

        private BindingList<TabModel> tabs;
        private BindingList<PostFilterRule> filters;

        public FilterDialog()
        {
            this.InitializeTabs();
        }

        private void InitializeTabs()
        {
            var tabs = this.TabInfo.Tabs.Values
                .Where(x => x.TabType != MyCommon.TabUsageType.Mute)
                .ToList();

            // ミュートタブは末尾に追加する
            var muteTab = this.TabInfo.GetTabByType(MyCommon.TabUsageType.Mute);
            if (muteTab != null)
                tabs.Add(muteTab);

            this.tabs = new BindingList<TabModel>(tabs);
        }

        public void SetSelectedTabIndex(int selectedTabIndex)
        {
            this.SelectedTabIndex = selectedTabIndex;
            this.SelectedTabChanged?.Invoke(this, EventArgs.Empty);

            var tab = this.SelectedTab;

            var filters = new List<PostFilterRule>();
            if (tab is FilterTabModel filterTab)
                filters.AddRange(filterTab.GetFilters());

            this.filters = new BindingList<PostFilterRule>(filters);
            this.FiltersChanged?.Invoke(this, EventArgs.Empty);

            var selectedIndices = this.filters.Count != 0 ? new[] { 0 } : new int[0];
            this.SetSelectedFiltersIndex(selectedIndices);
        }

        public void SetSelectedFiltersIndex(IEnumerable<int> selectedIndexes)
        {
            this.SelectedFilterIndices = selectedIndexes.ToArray();
            this.SelectedFiltersChanged?.Invoke(this, EventArgs.Empty);

            var firstFilter = this.SelectedFilters.FirstOrDefault();
            if (firstFilter == null)
                this.SetFilterEnabledButtonState(EnabledButtonState.NotSelected);
            else
                this.SetFilterEnabledButtonState(firstFilter.Enabled ? EnabledButtonState.Disable : EnabledButtonState.Enable);

            this.SetEditingFilter(firstFilter);
        }

        public void SetEditingFilter(PostFilterRule filter)
        {
            this.EditingFilter = filter?.Clone();
            this.EditingFilterChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RestoreEditingFilter()
            => this.SetEditingFilter(this.SelectedFilters.FirstOrDefault());

        public void SetFilterEditMode(EDITMODE editMode)
            => this.FilterEditMode = editMode;

        public void SetMatchRuleComplex(bool isComplex)
        {
            this.MatchRuleComplex = isComplex;
            this.MatchRuleComplexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetExcludeRuleComplex(bool isComplex)
        {
            this.ExcludeRuleComplex = isComplex;
            this.ExcludeRuleComplexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetFilterEnabledButtonState(EnabledButtonState mode)
        {
            this.FilterEnabledButtonState = mode;
            this.FilterEnabledButtonStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ActionAddNewTab(string tabName, MyCommon.TabUsageType tabType, ListElement list = null)
        {
            TabModel tab;
            switch (tabType)
            {
                case MyCommon.TabUsageType.UserDefined:
                    tab = new FilterTabModel(tabName);
                    break;
                case MyCommon.TabUsageType.PublicSearch:
                    tab = new PublicSearchTabModel(tabName);
                    break;
                case MyCommon.TabUsageType.Lists:
                    tab = new ListTimelineTabModel(tabName, list);
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(tabType), (int)tabType, typeof(MyCommon.TabUsageType));
            }

            if (!this.TabInfo.AddTab(tab))
            {
                this.AddNewTabFailed?.Invoke(this, new AddNewTabFailedEventArgs(tabName));
                return;
            }

            var insertIndex = this.tabs.Count;

            // 末尾がミュートタブであればその手前に追加する
            var lastTab = this.tabs.LastOrDefault();
            if (lastTab != null && lastTab.TabType == MyCommon.TabUsageType.Mute)
                insertIndex--;

            this.tabs.Insert(insertIndex, tab);

            this.SetSelectedTabIndex(insertIndex);
        }

        public void ActionRemoveSelectedTab(TweenMain tweenMain)
        {
            var tab = this.SelectedTab;
            if (tab == null)
                return;

            if (!tweenMain.RemoveSpecifiedTab(tab.TabName, true))
                return;

            var index = this.SelectedTabIndex;
            this.tabs.RemoveAt(index);

            this.SetSelectedTabIndex(Math.Max(0, index - 1));
        }

        public void ActionRenameSelectedTab(TweenMain tweenMain)
        {
            var tab = this.SelectedTab;
            if (tab == null)
                return;

            var origTabName = tab.TabName;

            if (!tweenMain.TabRename(origTabName, out var newTabName))
                return;

            tab.TabName = newTabName;

            var index = this.SelectedTabIndex;
            this.tabs.ResetItem(index);
        }

        public void ActionMoveUpSelectedTab(TweenMain tweenMain)
        {
            var selectedTab = this.SelectedTab;
            if (selectedTab == null)
                return;

            // 先頭のタブは上に移動できない
            var selectedIndex = this.SelectedTabIndex;
            if (selectedIndex == 0)
                return;

            var selectedTabName = selectedTab.TabName;

            var targetTab = this.tabs[selectedIndex - 1];
            var targetTabName = targetTab.TabName;

            // ミュートタブは移動禁止
            if (selectedTab.TabType == MyCommon.TabUsageType.Mute || targetTab.TabType == MyCommon.TabUsageType.Mute)
                return;

            tweenMain.ReOrderTab(selectedTab.TabName, targetTab.TabName, true);

            // アイテム並び替え
            this.tabs.RemoveAt(selectedIndex - 1);
            this.tabs.Insert(selectedIndex, targetTab);

            // 移動前と同じタブが選択された状態にする
            this.SetSelectedTabIndex(selectedIndex - 1);
        }

        public void ActionMoveDownSelectedTab(TweenMain tweenMain)
        {
            var selectedTab = this.SelectedTab;
            if (selectedTab == null)
                return;

            // 末尾のタブは下に移動できない
            var selectedIndex = this.SelectedTabIndex;
            if (selectedIndex == this.tabs.Count - 1)
                return;

            var targetTab = this.tabs[selectedIndex + 1];

            // ミュートタブは移動禁止
            if (selectedTab.TabType == MyCommon.TabUsageType.Mute || targetTab.TabType == MyCommon.TabUsageType.Mute)
                return;

            tweenMain.ReOrderTab(selectedTab.TabName, targetTab.TabName, false);

            // ListTab のアイテム並び替え
            this.tabs.RemoveAt(selectedIndex + 1);
            this.tabs.Insert(selectedIndex, targetTab);

            // 移動前と同じタブが選択された状態にする
            this.SetSelectedTabIndex(selectedIndex + 1);
        }

        public void ActionNewFilter()
        {
            this.SetEditingFilter(new PostFilterRule());
            this.SetFilterEditMode(EDITMODE.AddNew);

            this.FilterEditModeEnter?.Invoke(this, EventArgs.Empty);
        }

        public void ActionEditSelectedFilter()
        {
            // 複数のフィルタが選択されている場合は先頭の1つのみ選択する
            if (this.SelectedFilterIndices.Length > 1)
                this.SetSelectedFiltersIndex(new[] { this.SelectedFilterIndices[0] });

            var filter = this.SelectedFilters.SingleOrDefault();
            if (filter == null)
                return;

            this.SetEditingFilter(filter);
            this.SetFilterEditMode(EDITMODE.Edit);

            this.FilterEditModeEnter?.Invoke(this, EventArgs.Empty);
        }

        public void ActionEditFilterCancel()
        {
            this.SetFilterEditMode(EDITMODE.None);
            this.RestoreEditingFilter();

            this.FilterEditModeExit?.Invoke(this, EventArgs.Empty);
        }

        public void ActionEditFilterComplete(TweenMain tweenMain)
        {
            var filter = this.EditingFilter;

            //入力チェック
            if (this.IsFilterBlank(filter))
            {
                this.FilterEditError?.Invoke(this, new FilterEditErrorEventArgs(FilterEditErrorType.BlankRule));
                return;
            }

            if (!this.IsFilterRegexValid(filter, out var regexError))
            {
                this.FilterEditError?.Invoke(this, new FilterEditErrorEventArgs(FilterEditErrorType.InvalidRegex, regexError));
                return;
            }

            if (!this.IsFilterLambdaExpValid(filter, out var lambdaExpError))
            {
                this.FilterEditError?.Invoke(this, new FilterEditErrorEventArgs(FilterEditErrorType.InvalidLambdaExp, lambdaExpError));
                return;
            }

            if (this.MatchRuleComplex)
            {
                int cnt = tweenMain.AtIdSupl.ItemCount;
                tweenMain.AtIdSupl.AddItem("@" + filter.FilterName);
                if (cnt != tweenMain.AtIdSupl.ItemCount)
                {
                    tweenMain.ModifySettingAtId = true;
                }
            }

            var tab = (FilterTabModel)this.SelectedTab;
            int newSelectedIndex;

            if (this.FilterEditMode == EDITMODE.AddNew)
            {
                if (!tab.AddFilter(filter))
                {
                    this.FilterEditError?.Invoke(this, new FilterEditErrorEventArgs(FilterEditErrorType.Duplicated));
                    return;
                }

                this.filters.Add(filter);
                newSelectedIndex = this.filters.Count - 1;
            }
            else
            {
                var selectedIndex = this.SelectedFilterIndices.Single();
                var tabFilter = tab.GetFilters()[selectedIndex];

                tabFilter.FilterName = filter.FilterName;
                tabFilter.FilterBody = filter.FilterBody;
                tabFilter.FilterSource = filter.FilterSource;
                tabFilter.UseNameField = filter.UseNameField;
                tabFilter.UseRegex = filter.UseRegex;
                tabFilter.UseLambda = filter.UseLambda;

                tabFilter.ExFilterName = filter.ExFilterName;
                tabFilter.ExFilterBody = filter.ExFilterBody;
                tabFilter.ExFilterSource = filter.ExFilterSource;
                tabFilter.ExUseNameField = filter.ExUseNameField;
                tabFilter.ExUseRegex = filter.ExUseRegex;
                tabFilter.ExUseLambda = filter.ExUseLambda;

                this.filters[selectedIndex] = tabFilter;
                newSelectedIndex = selectedIndex;
            }

            this.SetSelectedFiltersIndex(new[] { newSelectedIndex });
            this.SetFilterEditMode(EDITMODE.None);

            this.FilterEditModeExit?.Invoke(this, EventArgs.Empty);
        }

        public void ActionDeleteSelectedFilters()
        {
            var tab = (FilterTabModel)this.SelectedTab;

            // インデックスの指す要素がずれないように降順に削除する
            var selectedIndices = this.SelectedFilterIndices.OrderByDescending(x => x);

            foreach (var index in selectedIndices)
            {
                var filter = this.filters[index];
                tab.RemoveFilter(filter);
                this.filters.RemoveAt(index);
            }
        }

        public void ActionMoveUpSelectedFilters()
            => this.ActionMoveUpDownSelectedFilters(diffIndex: -1);

        public void ActionMoveDownSelectedFilters()
            => this.ActionMoveUpDownSelectedFilters(diffIndex: 1);

        public void ActionMoveUpDownSelectedFilters(int diffIndex)
        {
            var indices = this.SelectedFilterIndices;
            if (indices.Length == 0)
                return;

            // 移動後のインデックスが要素数を超えないかチェック
            if (diffIndex < 0)
            {
                indices = indices.OrderBy(x => x).ToArray();

                if (indices[0] + diffIndex < 0)
                    return;
            }
            else
            {
                // インデックスの指す要素がずれないように降順に処理する
                indices = indices.OrderByDescending(x => x).ToArray();

                if (indices[0] + diffIndex > this.filters.Count - 1)
                    return;
            }

            foreach (var index in indices)
            {
                var filter = this.filters[index];
                this.filters.RemoveAt(index);
                this.filters.Insert(index + diffIndex, filter);
            }

            var tab = (FilterTabModel)this.SelectedTab;
            tab.FilterArray = this.filters.ToArray();

            this.SetSelectedFiltersIndex(indices.Select(x => x + diffIndex));
        }

        public bool IsFilterBlank(PostFilterRule filter)
        {
            if (filter.UseNameField && !string.IsNullOrEmpty(filter.FilterName))
                return false;

            if (filter.FilterBody.Any(x => !string.IsNullOrEmpty(x)))
                return false;

            if (!string.IsNullOrEmpty(filter.FilterSource))
                return false;

            if (filter.FilterRt)
                return false;

            if (filter.ExUseNameField && !string.IsNullOrEmpty(filter.ExFilterName))
                return false;

            if (filter.ExFilterBody.Any(x => !string.IsNullOrEmpty(x)))
                return false;

            if (!string.IsNullOrEmpty(filter.ExFilterSource))
                return false;

            if (filter.ExFilterRt)
                return false;

            return true;
        }

        public bool IsFilterRegexValid(PostFilterRule filter, out string errorMessage)
        {
            if (filter.UseRegex)
            {
                if (filter.UseNameField)
                {
                    if (!this.IsRegexValid(filter.FilterName, out errorMessage))
                        return false;
                }

                foreach (var bodyItem in filter.FilterBody)
                {
                    if (!this.IsRegexValid(bodyItem, out errorMessage))
                        return false;
                }

                if (!this.IsRegexValid(filter.FilterSource, out errorMessage))
                    return false;
            }

            if (filter.ExUseRegex)
            {
                if (filter.ExUseNameField)
                {
                    if (!this.IsRegexValid(filter.ExFilterName, out errorMessage))
                        return false;
                }

                foreach (var bodyItem in filter.ExFilterBody)
                {
                    if (!this.IsRegexValid(bodyItem, out errorMessage))
                        return false;
                }

                if (!this.IsRegexValid(filter.ExFilterSource, out errorMessage))
                    return false;
            }

            errorMessage = "";
            return true;
        }

        private bool IsRegexValid(string text, out string errorMessage)
        {
            try
            {
                new Regex(text);

                errorMessage = "";
                return true;
            }
            catch (ArgumentException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool IsFilterLambdaExpValid(PostFilterRule filter, out string errorMessage)
        {
            if (filter.UseLambda || filter.ExUseLambda)
            {
                // TODO: DynamicQuery相当のGPLv3互換なライブラリで置換する
                errorMessage = "ラムダ式は非対応のため使用できません";
                return false;
            }

            errorMessage = "";
            return true;
        }

        public enum EDITMODE
        {
            AddNew,
            Edit,
            None,
        }

        public enum EnabledButtonState
        {
            NotSelected,
            Enable,
            Disable,
        }

        public enum FilterEditErrorType
        {
            InvalidRegex,
            InvalidLambdaExp,
            BlankRule,
            Duplicated,
        }

        public class AddNewTabFailedEventArgs : EventArgs
        {
            public string TabName { get; }

            public AddNewTabFailedEventArgs(string tabName)
                => this.TabName = tabName;
        }

        public class FilterEditErrorEventArgs : EventArgs
        {
            public FilterEditErrorType ErrorType { get; }
            public string Message { get; }

            public FilterEditErrorEventArgs(FilterEditErrorType errorType)
                => this.ErrorType = errorType;

            public FilterEditErrorEventArgs(FilterEditErrorType errorType, string message)
                => (this.ErrorType, this.Message) = (errorType, message);
        }
    }
}
