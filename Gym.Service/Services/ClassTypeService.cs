using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

public sealed class ClassTypeService : IClassTypeService
{
    private readonly IClassTypeRepository _classTypes;
    private readonly IMapper _mapper;
    private readonly ILogger<ClassTypeService> _logger;

    public ClassTypeService(IClassTypeRepository classTypes, IMapper mapper, ILogger<ClassTypeService> logger)
    {
        _classTypes = classTypes;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ClassTypeResponse>> ListAsync(CancellationToken ct = default)
    {
        var items = await _classTypes.ListWithTagsAsync(ct);
        return _mapper.Map<IReadOnlyList<ClassTypeResponse>>(items);
    }

    public async Task<Result<ClassTypeResponse>> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await _classTypes.GetWithTagsAsync(id, ct);
        return entity is null
            ? Result<ClassTypeResponse>.NotFound($"Class type {id} was not found.")
            : Result<ClassTypeResponse>.Success(_mapper.Map<ClassTypeResponse>(entity));
    }

    public async Task<Result<ClassTypeResponse>> CreateAsync(CreateClassTypeRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _classTypes.NameExistsAsync(name, excludeId: null, ct))
            return Result<ClassTypeResponse>.Conflict($"A class type named '{name}' already exists.");

        var entity = new ClassType
        {
            Name = name,
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            DefaultCapacity = request.DefaultCapacity,
            Tags = await _classTypes.ResolveTagsAsync(request.Tags, ct)
        };

        await _classTypes.AddAsync(entity, ct);
        await _classTypes.SaveChangesAsync(ct);

        _logger.LogInformation("Class type created: {ClassTypeId} {Name}", entity.Id, entity.Name);
        return Result<ClassTypeResponse>.Success(_mapper.Map<ClassTypeResponse>(entity));
    }

    public async Task<Result<ClassTypeResponse>> UpdateAsync(int id, UpdateClassTypeRequest request, CancellationToken ct = default)
    {
        var entity = await _classTypes.GetWithTagsAsync(id, ct);
        if (entity is null)
            return Result<ClassTypeResponse>.NotFound($"Class type {id} was not found.");

        var name = request.Name.Trim();
        if (await _classTypes.NameExistsAsync(name, excludeId: id, ct))
            return Result<ClassTypeResponse>.Conflict($"A class type named '{name}' already exists.");

        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.DurationMinutes = request.DurationMinutes;
        entity.DefaultCapacity = request.DefaultCapacity;

        var tags = await _classTypes.ResolveTagsAsync(request.Tags, ct);
        entity.Tags.Clear();
        foreach (var tag in tags)
            entity.Tags.Add(tag);

        await _classTypes.SaveChangesAsync(ct);
        return Result<ClassTypeResponse>.Success(_mapper.Map<ClassTypeResponse>(entity));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _classTypes.GetByIdAsync(id, ct);
        if (entity is null)
            return Result.NotFound($"Class type {id} was not found.");

        try
        {
            _classTypes.Remove(entity);
            await _classTypes.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // FK restrict: sessions still reference this class type.
            _logger.LogWarning(ex, "Blocked delete of class type {ClassTypeId} with existing sessions", id);
            return Result.Conflict("This class type still has scheduled sessions and cannot be deleted.");
        }

        return Result.Success();
    }
}
