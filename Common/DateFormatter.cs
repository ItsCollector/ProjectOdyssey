using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Common
{
    public static class DateFormatter
    {
        public static string FormatDate(long utcTicks)
        {
            if (utcTicks <= 0) return "-";
            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().ToString("dd MMM yyyy HH:mm");
        }
    }
}
