using Light.Utilities.Grouping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light.Model;
using Light.Common;

namespace Light.Utilities.EntityIndexer
{
    class ItemAddedTimeIndexer : IEntityIndexer
    {
        public string Identifier => nameof(ItemAddedTimeIndexer);

        public string GetIndex(CommonViewItemModel item)
        {
            return item?.DatabaseItemAddedDate.ToUnixTimeSeconds().ToString() ?? "0";
        }

        public string GetIndexForGroup(CommonViewItemModel item)
        {
            return item?.DatabaseItemAddedDate.ToString("yyyy-MM-dd") ?? CommonSharedStrings.UnknownIndex;
        }
    }
}
