using System;
using Light.Common;
using Light.Model;

namespace Light.Utilities
{
    static class DateTimeHelper
    {
        public static string GetItemDateYearString(CommonViewItemModel item)
        {
            var dateString = string.Empty;
            if (!string.IsNullOrEmpty(item.ReleaseDate))
            {
                dateString = GetItemDateYearString(item.ReleaseDate);
            }
            return dateString;
        }

        public static string GetItemDateYearString(string dateStringInput)
        {
            var dateString = string.Empty;
            if (!string.IsNullOrEmpty(dateStringInput))
            {
                DateTime date;
                if (DateTime.TryParse(dateStringInput, out date))
                {
                    dateString = date.Year.ToString();
                }
                else
                {
                    dateString = dateStringInput;
                }
            }
            return dateString;
        }
    }
}
