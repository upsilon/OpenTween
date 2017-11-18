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

        public event EventHandler SelectedTabChanged;

        public void SetSelectedTabName(string selectedTabName)
        {
            var tab = this.TabInfo.GetTabByName(selectedTabName);
            if (tab == null)
                return;

            this.SelectedTab = tab;
            this.SelectedTabChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
