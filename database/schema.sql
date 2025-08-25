-- Photos table for the Azure-first photo upload pipeline
-- Stores metadata for uploaded photos without storing originals
CREATE TABLE Photos (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId nvarchar(450) NOT NULL,                    -- User identifier
    CameraId int NOT NULL,                            -- Camera that took the photo
    ContentHash nvarchar(64) NOT NULL,                -- SHA-256 hash of original bytes
    ThumbBlobName nvarchar(500) NOT NULL,             -- {userId}/{hash}_thumb.webp
    DisplayBlobName nvarchar(500) NOT NULL,           -- {userId}/{hash}_1200x932.webp
    TakenAtUtc datetime2 NULL,                        -- When photo was taken (optional)
    Latitude decimal(10,8) NULL,                      -- GPS latitude (optional)
    Longitude decimal(11,8) NULL,                     -- GPS longitude (optional)
    WeatherJson nvarchar(max) NULL,                   -- Cached weather data from API
    Status nvarchar(50) NOT NULL DEFAULT 'processing', -- 'processing', 'ready', 'failed'
    CreatedAtUtc datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes for common queries
    INDEX IX_Photos_UserId (UserId),
    INDEX IX_Photos_CameraId (CameraId),
    INDEX IX_Photos_ContentHash (ContentHash),
    INDEX IX_Photos_Status (Status),
    INDEX IX_Photos_CreatedAtUtc (CreatedAtUtc),
    
    -- Unique constraint to prevent duplicate uploads
    UNIQUE INDEX UX_Photos_UserCamera_Hash (UserId, CameraId, ContentHash)
);

-- Weather cache table to avoid duplicate API calls
-- One entry per camera per calendar day
CREATE TABLE WeatherCache (
    Id int IDENTITY(1,1) PRIMARY KEY,
    CameraId int NOT NULL,                            -- Camera identifier
    LocalDate date NOT NULL,                          -- Calendar date (UTC date is acceptable for now)
    WeatherJson nvarchar(max) NOT NULL,               -- Raw JSON from Open-Meteo API
    CreatedAtUtc datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Unique constraint: one weather entry per camera per day
    UNIQUE INDEX UX_WeatherCache_CameraDate (CameraId, LocalDate),
    INDEX IX_WeatherCache_CameraId (CameraId),
    INDEX IX_WeatherCache_LocalDate (LocalDate)
);