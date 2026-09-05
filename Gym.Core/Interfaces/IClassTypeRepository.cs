using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IClassTypeRepository : IRepository<ClassType>
{
    /// <summary>Loads a class type together with its tags (tracked, for updates).</summary>
    Task<ClassType?> GetWithTagsAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ClassType>> ListWithTagsAsync(CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);

    /// <summary>Returns existing tags for the given names and creates the missing ones.</summary>
    Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> names, CancellationToken ct = default);
}
