namespace Codeo.CQRS
{
    /// <summary>
    /// Provide some seconds values for common cache times
    /// </summary>
    public static class CacheTimes
    {
        /// <summary>
        /// 1 minute - 60 seconds
        /// </summary>
        public const int ONE_MINUTE = 60;

        /// <summary>
        /// 2 minutes - 120 seconds
        /// </summary>
        public const int TWO_MINUTES = 2 * ONE_MINUTE;

        /// <summary>
        /// 5 minutes - 300 seconds
        /// </summary>
        public const int FIVE_MINUTES = 5 * ONE_MINUTE;

        /// <summary>
        /// 10 minutes - 600 seconds
        /// </summary>
        public const int TEN_MINUTES = 10 * ONE_MINUTE;

        /// <summary>
        /// 15 minutes - 900 seconds
        /// </summary>
        public const int FIFTEEN_MINUTES = ONE_MINUTE * 15;

        /// <summary>
        /// 30 minutes - 1800 seconds
        /// </summary>
        public const int THIRTY_MINUTES = 30 * ONE_MINUTE;

        /// <summary>
        /// 1 hour - 3600 seconds
        /// </summary>
        public const int ONE_HOUR = 60 * ONE_MINUTE;

        /// <summary>
        /// 1 day - 86400 seconds
        /// </summary>
        public const int ONE_DAY = 24 * ONE_HOUR;

        /// <summary>
        /// 1 week - 604800 seconds
        /// </summary>
        public const int ONE_WEEK = 7 * ONE_DAY;
        
        /// <summary>
        /// ~1 year - 31622400 seconds (366 days)
        /// (effectively, cache for the lifetime of the system, since it's very likely
        /// the system will be updated / restarted in between)
        /// </summary>
        public const int ONE_YEAR_IN_SECONDS = 31622400;
    }
}