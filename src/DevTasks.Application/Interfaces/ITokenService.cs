namespace DevTasks.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, IList<string> roles);
}