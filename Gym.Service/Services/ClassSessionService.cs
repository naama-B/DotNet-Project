using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

public sealed class ClassSessionService : IClassSessionService
{
    private readonly IClassSessionRepository _sessions;
    private readonly IClassTypeRepository _classTypes;
    private readonly IInstructorRepository _instructors;
    private readonly IMapper _mapper;
    private readonly ILogger<ClassSessionService> _logger;

    public ClassSessionService(
        IClassSessionRepository sessions,
        IClassTypeRepository classTypes,
        IInstructorRepository instructors,
        IMapper mapper,
        ILogger<ClassSessionService> logger)
    {
        _sessions = sessions;
        _classTypes = classTypes;
        _instructors = instructors;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ClassSessionResponse>> ListAsync(ClassSessionQueryParameters parameters, CancellationToken ct = default)
    {
        var (items, total) = await _sessions.QueryAsync(parameters, ct);
        var mapped = _mapper.Map<IReadOnlyList<ClassSessionResponse>>(items);
        return new PagedResult<ClassSessionResponse>(mapped, parameters.Page, parameters.PageSize, total);
    }

    public async Task<Result<ClassSessionResponse>> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await _sessions.GetDetailAsync(id, ct);
        return entity is null
            ? Result<ClassSessionResponse>.NotFound($"Class session {id} was not found.")
            : Result<ClassSessionResponse>.Success(_mapper.Map<ClassSessionResponse>(entity));
    }

    public async Task<Result<ClassSessionResponse>> CreateAsync(CreateClassSessionRequest request, CancellationToken ct = default)
    {
        var classType = await _classTypes.GetByIdAsync(request.ClassTypeId, ct);
        if (classType is null)
            return Result<ClassSessionResponse>.Validation($"Class type {request.ClassTypeId} does not exist.");

        if (await _instructors.GetByIdAsync(request.InstructorId, ct) is null)
            return Result<ClassSessionResponse>.Validation($"Instructor {request.InstructorId} does not exist.");

        var startsAt = DateTime.SpecifyKind(request.StartsAtUtc, DateTimeKind.Utc);
        if (startsAt <= DateTime.UtcNow)
            return Result<ClassSessionResponse>.Validation("A session must start in the future.");

        var entity = new ClassSession
        {
            ClassTypeId = classType.Id,
            InstructorId = request.InstructorId,
            StartsAtUtc = startsAt,
            Capacity = request.Capacity ?? classType.DefaultCapacity,
            BookedCount = 0,
            Status = ClassSessionStatus.Scheduled
        };

        await _sessions.AddAsync(entity, ct);
        await _sessions.SaveChangesAsync(ct);

        _logger.LogInformation("Class session created: {SessionId} at {StartsAt:o}", entity.Id, entity.StartsAtUtc);

        var created = await _sessions.GetDetailAsync(entity.Id, ct);
        return Result<ClassSessionResponse>.Success(_mapper.Map<ClassSessionResponse>(created!));
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        var entity = await _sessions.GetForUpdateAsync(id, ct);
        if (entity is null)
            return Result.NotFound($"Class session {id} was not found.");

        if (entity.Status == ClassSessionStatus.Cancelled)
            return Result.Success();

        entity.Status = ClassSessionStatus.Cancelled;
        await _sessions.SaveChangesAsync(ct);

        _logger.LogInformation("Class session cancelled: {SessionId}", id);
        return Result.Success();
    }
}
