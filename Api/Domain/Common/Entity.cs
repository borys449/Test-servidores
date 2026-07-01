namespace Api.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }
}
