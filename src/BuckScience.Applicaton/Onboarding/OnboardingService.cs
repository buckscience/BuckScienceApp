using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Services;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Onboarding;

public sealed class OnboardingService : IOnboardingService
{
    private readonly IAppDbContext _db;

    public OnboardingService(IAppDbContext db) => _db = db;

    public async Task<(OnboardingState state, int? primaryPropertyId, int? firstCameraId)> GetStateAsync(
        int userId,
        CancellationToken ct = default)
    {
        var primary = await _db.Properties
            .AsNoTracking()
            .Where(p => p.ApplicationUserId == userId)
            .OrderBy(p => p.CreatedDate)
            .ThenBy(p => p.Id)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);

        if (primary is null)
            return (OnboardingState.NeedsProperty, null, null);

        var primaryId = primary.Id;

        var hasCameraOnPrimary = await _db.Cameras
            .AsNoTracking()
            .AnyAsync(c => c.PropertyId == primaryId, ct);

        if (!hasCameraOnPrimary)
            return (OnboardingState.NeedsCameraOnPrimaryProperty, primaryId, null);

        var firstCameraId = await _db.Cameras
            .AsNoTracking()
            .Where(c => c.PropertyId == primaryId)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .FirstAsync(ct);

        var hasPhotoOnPrimary = await _db.Photos
            .AsNoTracking()
            .Join(_db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { Photo = p, Camera = c })
            .AnyAsync(x => x.Camera.PropertyId == primaryId, ct);

        if (!hasPhotoOnPrimary)
            return (OnboardingState.NeedsPhotoOnPrimaryProperty, primaryId, firstCameraId);

        return (OnboardingState.Complete, primaryId, firstCameraId);
    }
}