using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Interfaces;

namespace Gym.Service.Services;

public sealed class InstructorService : IInstructorService
{
    private readonly IInstructorRepository _instructors;
    private readonly IMapper _mapper;

    public InstructorService(IInstructorRepository instructors, IMapper mapper)
    {
        _instructors = instructors;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<InstructorResponse>> ListAsync(CancellationToken ct = default)
    {
        var items = await _instructors.ListAsync(ct);
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
}
