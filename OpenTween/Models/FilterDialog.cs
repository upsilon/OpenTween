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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTween.Models
{
    public class FilterDialog
    {
        public TabInformations TabInfo
            => TabInformations.GetInstance();

        public TabModel SelectedTab { get; private set; }
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

        public void SetSelectedTabName(string selectedTabName)
        {
            if (selectedTabName == null)
            {
                this.SelectedTab = null;
            }
            else
            {
                var tab = this.TabInfo.GetTabByName(selectedTabName) ?? throw new ArgumentException();
                this.SelectedTab = tab;
            }

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
    }
}
