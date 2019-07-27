using Light.Utilities.Grouping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light.Model;

namespace Light.Utilities.EntityIndexer
{
    class NoGroupingIndexer : IEntityIndexer
    {
        private string _groupName;

        public string Identifier => nameof(NoGroupingIndexer);

        public NoGroupingIndexer(string groupName)
        {
            _groupName = groupName;
        }

        public string GetIndex(CommonViewItemModel item)
        {
            return item.Title;
        }

        public string GetIndexForGroup(CommonViewItemModel item)
        {
            return _groupName;
        }
    }
}
