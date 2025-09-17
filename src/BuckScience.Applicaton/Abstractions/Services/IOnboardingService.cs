using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions.Services;

public interface IOnboardingService
{
    Task<(OnboardingState state, int? primaryPropertyId, int? firstCameraId)> GetStateAsync(
        int userId,
        CancellationToken ct = default);
}