namespace SilkHat.Infrastructure.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
    }
}