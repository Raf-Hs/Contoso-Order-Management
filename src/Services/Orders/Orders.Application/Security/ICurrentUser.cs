namespace Orders.Application.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
}
