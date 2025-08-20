using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BuckScience.Application.Properties;

public static class DeleteProperty
{
    // In the future, add a userId parameter to enforce ownership checks.
    public static async Task HandleAsync(int id, IAppDbContext db, CancellationToken ct)
    {
        var entity = await db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;

        db.Properties.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}