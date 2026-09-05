using Gym.Core.Entities;
using Gym.Core.Interfaces;

namespace Gym.Data.Repositories;

public sealed class InstructorRepository : Repository<Instructor>, IInstructorRepository
{
    public InstructorRepository(GymDbContext db) : base(db) { }
}
