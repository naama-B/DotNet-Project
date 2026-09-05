using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class ClassTypeRepository : Repository<ClassType>, IClassTypeRepository
{
    public ClassTypeRepository(GymDbContext db) : base(db) { }

    public Task<ClassType?> GetWithTagsAsync(int id, CancellationToken ct = default) =>
        Set.Include(ct2 => ct2.Tags).FirstOrDefaultAsync(ct2 => ct2.Id == id, ct);

    public async Task<IReadOnlyList<ClassType>> ListWithTagsAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(c => c.Tags)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default) =>
        Set.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), ct);

    public async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> names, CancellationToken ct = default)
    {
        var wanted = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wanted.Count == 0)
            return new List<Tag>();

        var existing = await Db.Tags
            .Where(t => wanted.Contains(t.Name))
            .ToListAsync(ct);

        var result = new List<Tag>(existing);

        foreach (var name in wanted)
        {
            if (existing.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var created = new Tag { Name = name };
            await Db.Tags.AddAsync(created, ct);
            result.Add(created);
        }

        return result;
    }
}
