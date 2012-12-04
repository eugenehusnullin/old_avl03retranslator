using System;

namespace OnlineMonitoring.ServerCore.Helpers
{
    public static class DateTimeHelper
    {
        public static long ToUDateTime(this DateTime time)
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var span = time.Subtract(unixEpoch);
            return (long)span.TotalSeconds;
        }

        public static DateTime FromUDateTime(this int timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        } 
    }
}