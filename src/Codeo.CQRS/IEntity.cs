namespace Codeo.CQRS
{
    /// <summary>
    /// Convenience interface: make your database entities implement this
    /// to take advantage of the shorter
    /// Fluently.Configure().WithEntitiesFrom(assembly)
    /// (ie, no need to provide your own entity discriminator)
    /// </summary>
    public interface IEntity
    {
    }
}
