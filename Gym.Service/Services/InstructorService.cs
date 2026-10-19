using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

public sealed class InstructorService : IInstructorService
{
    private readonly IInstructorRepository _instructors;
    private readonly IMapper _mapper;
    private readonly ILogger<InstructorService> _logger;

    public InstructorService(IInstructorRepository instructors, IMapper mapper, ILogger<InstructorService> logger)
    {
        _instructors = instructors;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InstructorResponse>> ListAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var items = await _instructors.ListAsync(includeInactive, ct);
        return _mapper.Map<IReadOnlyList<InstructorResponse>>(items);
    }

    public async Task<Result<InstructorResponse>> CreateAsync(CreateInstructorRequest request, CancellationToken ct = default)
    {
        var entity = new Instructor
        {
            FullName = request.FullName.Trim(),
            Bio = request.Bio?.Trim()
        };

        await _instructors.AddAsync(entity, ct);
        await _instructors.SaveChangesAsync(ct);

        return Result<InstructorResponse>.Success(_mapper.Map<InstructorResponse>(entity));
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _instructors.GetByIdAsync(id, ct);
        if (entity is null)
            return Result.NotFound($"Instructor {id} was not found.");

        if (!entity.IsActive)
            return Result.Success();

        if (await _instructors.HasUpcomingSessionsAsync(id, DateTime.UtcNow, ct))
            return Result.Conflict("This instructor still has upcoming sessions. Cancel them before deactivating the instructor.");

        entity.IsActive = false;
        await _instructors.SaveChangesAsync(ct);

        _logger.LogInformation("Instructor deactivated: {InstructorId}", id);
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _instructors.GetByIdAsync(id, ct);
        if (entity is null)
            return Result.NotFound($"Instructor {id} was not found.");

        if (entity.IsActive)
            return Result.Success();

        entity.IsActive = true;
        await _instructors.SaveChangesAsync(ct);

        _logger.LogInformation("Instructor reactivated: {InstructorId}", id);
        return Result.Success();
    }
}
