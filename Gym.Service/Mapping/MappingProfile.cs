using AutoMapper;
using Gym.Core.DTOs;
using Gym.Core.Entities;

namespace Gym.Service.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Instructor, InstructorResponse>();

        CreateMap<ClassType, ClassTypeResponse>()
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags.Select(t => t.Name).OrderBy(n => n).ToList()));

        CreateMap<ClassSession, ClassSessionResponse>()
            .ForMember(d => d.ClassTypeName, o => o.MapFrom(s => s.ClassType.Name))
            .ForMember(d => d.InstructorName, o => o.MapFrom(s => s.Instructor.FullName))
            .ForMember(d => d.DurationMinutes, o => o.MapFrom(s => s.ClassType.DurationMinutes))
            .ForMember(d => d.AvailableSpots, o => o.MapFrom(s => s.Capacity - s.BookedCount > 0 ? s.Capacity - s.BookedCount : 0))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.RatingCount, o => o.MapFrom(s => s.Ratings.Count))
            .ForMember(d => d.AverageStars, o => o.MapFrom(s =>
                s.Ratings.Count > 0 ? (double?)Math.Round(s.Ratings.Average(r => r.Stars), 2) : null));

        CreateMap<Booking, BookingResponse>()
            .ForMember(d => d.ClassTypeName, o => o.MapFrom(s => s.ClassSession.ClassType.Name))
            .ForMember(d => d.StartsAtUtc, o => o.MapFrom(s => s.ClassSession.StartsAtUtc))
            .ForMember(d => d.MemberName, o => o.MapFrom(s => s.Member.FullName))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.WaitlistPosition, o => o.Ignore());

        CreateMap<WaitlistEntry, WaitlistEntryResponse>()
            .ForMember(d => d.MemberName, o => o.MapFrom(s => s.Member.FullName));

        CreateMap<ClassRating, ClassRatingResponse>()
            .ForMember(d => d.MemberName, o => o.MapFrom(s => s.Member.FullName));
    }
}
