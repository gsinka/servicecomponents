using System;
using System.Threading;

namespace ServiceComponents.Core.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly bool[] WorkDays = {false, true, true, true, true, true, false};

        public static bool IsWeekday(this DateTime dateTime)
        {
            return WorkDays[(int)dateTime.DayOfWeek];
        }

        public static bool IsHoliday(this DateTime dateTime)
        {
            return !WorkDays[(int)dateTime.DayOfWeek];
        }

        public static DateTime StartOfWeek(this DateTime date)
        {
            return date.StartOfWeek(Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
        }

        public static DateTime StartOfWeek(this DateTime date, DayOfWeek startOfWeek)
        {
            var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

    }
}