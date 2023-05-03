namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// Types of operations, typically to identify SQL-based
    /// operations, though they can make sense in other
    /// scenarios too.
    /// </summary>
    public enum Operation
    {
        /// <summary>
        /// Fetching zero or more items from a data store
        /// </summary>
        Select,
        /// <summary>
        /// Updating zero or more items in a data store
        /// </summary>
        Update,
        /// <summary>
        /// Deleting zero or more items in a data store
        /// </summary>
        Delete,
        /// <summary>
        /// Inserting one or more items in a data store
        /// </summary>
        Insert
    }
}