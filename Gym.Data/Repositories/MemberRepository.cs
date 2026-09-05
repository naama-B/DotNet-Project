using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class MemberRepository : Repository<Member>, IMemberRepository
{
    public MemberRepository(GymDbContext db) : base(db) { }

    public Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(m => m.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Set.AnyAsync(m => m.Email == email, ct);
}
