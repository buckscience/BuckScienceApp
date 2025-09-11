# Authentication and User Management Consistency Fix

## Issue
During subscription system development, the user tracking middleware was enhanced with complex logic including:
- Email fallback lookup when Azure ID doesn't match
- Automatic Azure ID updates with database writes in middleware
- Complex error handling and logging

This caused authentication issues, Azure AD B2C redirect loops, and subscription upgrade failures.

## Solution
Reverted `ResolveCurrentUserMiddleware` back to its original simple implementation:
- Simple lookup by Azure ID only
- No database writes in middleware
- No complex fallback logic
- Debug logging only when user not found

## Remaining Issue
The seeded user (`darrin@buckscience.com`) has a hardcoded Azure ID (`b300176c-0f43-4a4d-afd3-d128f8e635a1`) that likely doesn't match the real user's Azure AD B2C Object ID.

## Fix Instructions
1. Enable debug logging temporarily by setting log level to Debug in `appsettings.Development.json`
2. Login to the application
3. Check console/logs for: `"ResolveCurrentUser: No ApplicationUser found for external id [YOUR_REAL_ID]"`
4. Copy your real Azure ID from the log message
5. Run the SQL script in `docs/database-migration/fix-user-azure-id.sql` with your real Azure ID
6. Restore log level to Information

## Why This Approach
- Maintains simple, proven middleware design
- Avoids complex authentication logic that caused issues
- Provides clear fix path for seeded data mismatch
- Consistent with original authentication approach

## Files Changed
- `src/BuckScience.Web/Middleware/ResolveCurrentUserMiddleware.cs` - Reverted to original simple implementation
- `docs/database-migration/fix-user-azure-id.sql` - Helper script to fix seeded user Azure ID

## Testing
- All 386 tests pass including subscription tests
- Build succeeds with no errors
- Authentication flow is now consistent and simple