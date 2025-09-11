-- Fix Azure AD B2C User ID Mismatch
-- This script helps resolve authentication issues where the seeded user's Azure ID
-- doesn't match their actual Azure AD B2C authentication ID.
--
-- IMPORTANT: Replace 'YOUR_ACTUAL_AZURE_ID' with your real Azure AD B2C Object ID
-- You can find this by:
-- 1. Enable debugging in ResolveCurrentUserMiddleware (set log level to Debug)
-- 2. Login to the application 
-- 3. Check the console/logs for "No ApplicationUser found for external id [YOUR_ID]"
-- 4. Copy that ID and use it in the script below

-- Option 1: Update the seeded user's Azure ID (recommended)
-- Replace 'YOUR_ACTUAL_AZURE_ID' with your real Azure AD B2C Object ID
UPDATE ApplicationUsers 
SET AzureEntraB2CId = 'YOUR_ACTUAL_AZURE_ID'
WHERE Email = 'darrin@buckscience.com' AND Id = 1;

-- Option 2: Verify the update worked
SELECT Id, AzureEntraB2CId, Email, DisplayName 
FROM ApplicationUsers 
WHERE Email = 'darrin@buckscience.com';

-- Option 3: Alternative - Create new user with correct Azure ID
-- (Only use if Option 1 doesn't work)
/*
INSERT INTO ApplicationUsers (AzureEntraB2CId, FirstName, LastName, DisplayName, Email, CreatedDate)
VALUES ('YOUR_ACTUAL_AZURE_ID', 'Darrin', 'Brandon', 'Darrin B', 'darrin@buckscience.com', GETUTCDATE());

-- Then update the subscription to point to the new user
UPDATE Subscriptions 
SET UserId = (SELECT Id FROM ApplicationUsers WHERE Email = 'darrin@buckscience.com' AND AzureEntraB2CId = 'YOUR_ACTUAL_AZURE_ID')
WHERE UserId = 1;
*/

-- Verification query - should show your user with correct Azure ID
SELECT 
    u.Id,
    u.AzureEntraB2CId,
    u.Email,
    u.DisplayName,
    s.Tier as SubscriptionTier,
    s.Status as SubscriptionStatus
FROM ApplicationUsers u
LEFT JOIN Subscriptions s ON u.Id = s.UserId
WHERE u.Email = 'darrin@buckscience.com';