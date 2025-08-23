using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

public interface IOnboardingService
{
    Task<(OnboardingState state, int? primaryPropertyId, int? firstCameraId)> GetStateAsync(
        int userId,
        CancellationToken ct = default);
}