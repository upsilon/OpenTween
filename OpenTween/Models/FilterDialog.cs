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
        public EDITMODE FilterEditMode { get; private set; }

        public event EventHandler SelectedTabChanged;
        public event EventHandler SelectedFiltersChanged;
        public event EventHandler FilterEditModeChanged;

        public void SetSelectedTabName(string selectedTabName)
        {
            var tab = this.TabInfo.GetTabByName(selectedTabName);
            if (tab == null)
                return;

            this.SelectedTab = tab;
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
        }

        public void SetFilterEditMode(EDITMODE editMode)
        {
            this.FilterEditMode = editMode;
            this.FilterEditModeChanged?.Invoke(this, EventArgs.Empty);
        }

        public enum EDITMODE
        {
            AddNew,
            Edit,
            None,
        }
    }
}
