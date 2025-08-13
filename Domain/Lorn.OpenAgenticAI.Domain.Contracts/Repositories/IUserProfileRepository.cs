using System.Threading;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Shared.Contracts.Repositories;

namespace Lorn.OpenAgenticAI.Domain.Contracts.Repositories;

/// <summary>
/// 用户档案仓储接口（领域层契约）
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile>, IAsyncRepository<UserProfile>
{
    UserProfile? GetByUserName(string userName);
    Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}
