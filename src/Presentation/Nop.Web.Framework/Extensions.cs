using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Infrastructure;
using Nop.Services.Helpers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        public static IEnumerable<T> PagedForCommand<T>(this IEnumerable<T> current, DataSourceRequest command)
        {
            return current.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize);
        }

        /// <summary>
        /// Returns a value indicating whether real selection is not possible
        /// </summary>
        /// <param name="items">Items</param>
        /// <param name="ignoreZeroValue">A value indicating whether we should ignore items with "0" value</param>
        /// <returns>A value indicating whether real selection is not possible</returns>
        public static bool SelectionIsNotPossible(this IList<SelectListItem> items, bool ignoreZeroValue = true)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            //we ignore items with "0" value? Usually it's something like "Select All", "etc
            return items.Count(x => !ignoreZeroValue || !x.Value.ToString().Equals("0")) < 2;
        }

        /// <summary>
        /// Relative formatting of DateTime (e.g. 2 hours ago, a month ago)
        /// </summary>
        /// <param name="source">Source (UTC format)</param>
        /// <returns>Formatted date and time string</returns>
        public static string RelativeFormat(this DateTime source)
        {
            return RelativeFormat(source, string.Empty);
        }

        /// <summary>
        /// Relative formatting of DateTime (e.g. 2 hours ago, a month ago)
        /// </summary>
        /// <param name="source">Source (UTC format)</param>
        /// <param name="defaultFormat">Default format string (in case relative formatting is not applied)</param>
        /// <returns>Formatted date and time string</returns>
        public static string RelativeFormat(this DateTime source, string defaultFormat)
        {
            return RelativeFormat(source, false, defaultFormat);
        }

        /// <summary>
        /// Relative formatting of DateTime (e.g. 2 hours ago, a month ago)
        /// </summary>
        /// <param name="source">Source (UTC format)</param>
        /// <param name="convertToUserTime">A value indicating whether we should convert DateTime instance to user local time</param>
        /// <param name="defaultFormat">Default format string (in case relative formatting is not applied)</param>
        /// <returns>Formatted date and time string</returns>
        public static string RelativeFormat(this DateTime source,
            bool convertToUserTime, string defaultFormat)
        {
            string result = "";

            var ts = new TimeSpan(DateTime.UtcNow.Ticks - source.Ticks);
            double delta = ts.TotalSeconds;

            if (delta > 0)
            {
                if (delta < 60)
                {
                    result = ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
                }
                else if (delta < 120)
                {
                    result = "a minute ago";
                }
                else if (delta < 2700)
                {
                    result = ts.Minutes + " minutes ago";
                }
                else if (delta < 5400)
                {
                    result = "an hour ago";
                }
                else if (delta < 86400)
                {
                    int hours = ts.Hours;
                    if (hours == 1)
                        hours = 2;
                    result = hours + " hours ago";
                }
                else if (delta < 172800)
                {
                    result = "yesterday";
                }
                else if (delta < 2592000)
                {
                    result = ts.Days + " days ago";
                }
                else if (delta < 31104000)
                {
                    int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    result = months <= 1 ? "one month ago" : months + " months ago";
                }
                else
                {
                    int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                    result = years <= 1 ? "one year ago" : years + " years ago";
                }
            }
            else
            {
                DateTime tmp1 = source;
                if (convertToUserTime)
                {
                    tmp1 = EngineContext.Current.Resolve<IDateTimeHelper>().ConvertToUserTime(tmp1, DateTimeKind.Utc);
                }
                if (!string.IsNullOrEmpty(defaultFormat))
                {
                    result = tmp1.ToString(defaultFormat);
                }
                else
                {
                    result = tmp1.ToString();
                }
            }
            return result;
        }
    }
}
