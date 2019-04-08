using System;
using System.Data;
using Dapper;

namespace Codeo.CQRS
{
    /// <summary>
    /// Dapper Date Time Handler
    /// </summary>
    public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="value">The value.</param>
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.ToUniversalTime();
        }

        /// <summary>
        /// Parses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override DateTime Parse(object value)
        {
            return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
        }

    }

    /// <summary>
    /// Dapper Date Time Handler
    /// </summary>
    public class DateTimeNullableHandler : SqlMapper.TypeHandler<DateTime?>
    {
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="value">The value.</param>
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value?.ToUniversalTime();
        }

        /// <summary>
        /// Parses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override DateTime? Parse(object value)
        {
            return (value == null) ? (DateTime?)null : DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
        }
    }
}
