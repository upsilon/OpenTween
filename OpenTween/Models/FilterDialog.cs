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
using System.Threading.Tasks;

namespace OpenTween.Models
{
    public class FilterDialog
    {
        public TabInformations TabInfo
            => TabInformations.GetInstance();

        public BindingList<TabModel> Tabs => this.tabs;

        public TabModel SelectedTab
            => this.SelectedTabIndex != -1 ? this.tabs[this.SelectedTabIndex] : null;

        public int SelectedTabIndex { get; private set; }
        public PostFilterRule[] SelectedFilters { get; private set; }
        public PostFilterRule EditingFilter { get; private set; }
        public EDITMODE FilterEditMode { get; private set; }
        public bool MatchRuleComplex { get; private set; }
        public bool ExcludeRuleComplex { get; private set; }
        public EnabledButtonState FilterEnabledButtonState { get; private set; }

        public event EventHandler SelectedTabChanged;
        public event EventHandler SelectedFiltersChanged;
        public event EventHandler EditingFilterChanged;
        public event EventHandler FilterEditModeChanged;
        public event EventHandler MatchRuleComplexChanged;
        public event EventHandler ExcludeRuleComplexChanged;
        public event EventHandler FilterEnabledButtonStateChanged;

        public event EventHandler<AddNewTabFailedEventArgs> AddNewTabFailed;

        private BindingList<TabModel> tabs;

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
        }

        public void SetSelectedFiltersIndex(IEnumerable<int> selectedIndexes)
        {
            if (this.SelectedTab is FilterTabModel filterTab)
            {
                var filters = filterTab.FilterArray;
                this.SelectedFilters = selectedIndexes.Select(x => filters[x]).ToArray();
            }
            else
            {
                this.SelectedFilters = new PostFilterRule[0];
            }

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
            this.EditingFilter = filter;
            this.EditingFilterChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RestoreEditingFilter()
            => this.SetEditingFilter(this.SelectedFilters.FirstOrDefault());

        public void SetFilterEditMode(EDITMODE editMode)
        {
            this.FilterEditMode = editMode;
            this.FilterEditModeChanged?.Invoke(this, EventArgs.Empty);
        }

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

            var lastTab = this.tabs.LastOrDefault();

            // 末尾がミュートタブであればその手前に追加する
            if (lastTab != null && lastTab.TabType == MyCommon.TabUsageType.Mute)
                this.tabs.Insert(this.tabs.Count - 1, tab);
            else
                this.tabs.Add(tab);
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

        public class AddNewTabFailedEventArgs : EventArgs
        {
            public string TabName { get; }

            public AddNewTabFailedEventArgs(string tabName)
                => this.TabName = tabName;
        }
    }
}
