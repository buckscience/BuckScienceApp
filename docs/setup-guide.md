# Quick Setup Guide - Trail Camera Photo Processing

This guide will help you set up and use the new trail camera photo processing pipeline in your BuckScience app.

## What's New

Your app now automatically processes trail camera photos according to this workflow:

1. **Upload large photo** (JPEG, PNG, TIFF up to 50MB)
2. **Temporary storage** of original 
3. **Automatic processing** creates:
   - WebP display image (~300KB, max 1920x1080)
   - WebP thumbnail (~15KB, max 300x200)
4. **Original deleted** immediately after processing
5. **Optimized images stored** for fast loading

## Setup Steps

### 1. Database Update
Run this command to add the new photo fields:
```bash
dotnet ef database update
```

### 2. Create Required Directories
Ensure these directories exist with proper permissions:
```bash
mkdir -p wwwroot/photos    # For processed images (web accessible)
mkdir -p temp              # For temporary files (not web accessible)
```

### 3. Configuration (Optional)
If you want to customize the processing settings, edit `PhotoProcessingService.cs`:

```csharp
// Display image settings (targets ~300KB file size)
private const int DisplayMaxWidth = 1920;
private const int DisplayMaxHeight = 1080; 
private const int DisplayQuality = 85;

// Thumbnail settings (targets ~15KB file size)
private const int ThumbnailMaxWidth = 300;
private const int ThumbnailMaxHeight = 200;
private const int ThumbnailQuality = 75;
```

## How to Use

### For Users
1. Navigate to a camera's upload page: `/cameras/{id}/upload`
2. Select a trail camera photo (JPEG, PNG, or TIFF)
3. Click "Upload"
4. Photo is automatically processed - you'll see success message when complete
5. View processed photos in the camera's photo gallery

### File Size Limits
- **Maximum upload**: 50MB per photo
- **Supported formats**: JPEG, PNG, TIFF
- **Output format**: WebP (better compression than JPEG)

## What Happens During Processing

1. **Validation**: File type and size checked
2. **Temporary Storage**: Original saved to temp directory
3. **Display Image Creation**: 
   - Resized to max 1920x1080 (maintains aspect ratio)
   - Converted to WebP format with 85% quality
   - Target size: ~300KB
4. **Thumbnail Creation**:
   - Resized to max 300x200 (maintains aspect ratio) 
   - Converted to WebP format with 75% quality
   - Target size: ~15KB
5. **Database Storage**: Photo record created with both image URLs
6. **Cleanup**: Original file deleted from temp storage
7. **User Notification**: Success message displayed

## Updated Photo Properties

Photos now have these additional properties:

- `ThumbnailUrl`: URL to the small thumbnail image
- `FileSizeBytes`: Size of the display image in bytes
- `ThumbnailSizeBytes`: Size of the thumbnail in bytes

The existing `PhotoUrl` property contains the display image URL.

## Error Handling

The system handles common issues gracefully:

- **Unsupported file type**: Clear error message shown
- **File too large**: Size limit displayed to user
- **Processing failure**: Error logged, user notified
- **Camera not found**: Access denied message
- **Storage issues**: Automatic cleanup and error reporting

## Performance Benefits

- **Faster page loads**: Thumbnails load instantly (~15KB vs several MB)
- **Reduced bandwidth**: Display images ~300KB vs original multi-MB files
- **Storage savings**: Originals deleted immediately after processing
- **Better user experience**: Quick feedback, optimized viewing

## Monitoring Processing

Check your application logs for processing information:
```
[Info] Photo processing started: 8.2MB original
[Info] Photo processing completed: Display=287KB, Thumbnail=12KB, Reduction=96%
```

## Troubleshooting

### "File too large" error
- Check the 50MB limit in `UploadPhoto.cs`
- Increase if needed for your trail cameras

### Processing takes too long
- Check available disk space in temp directory
- Monitor CPU usage during processing
- Consider background processing for high-volume scenarios

### Images not displaying
- Verify `wwwroot/photos` directory exists and is web-accessible
- Check file permissions on the photos directory
- Ensure static file serving is enabled in your app

### Temp files accumulating
- Check temp directory cleanup in `PhotoProcessingService`
- Verify `LocalFileStorageService.DeleteTempFileAsync` is working
- Add monitoring for temp directory size

## Next Steps

Consider these enhancements for production:

1. **Cloud Storage**: Implement Azure Blob Storage or AWS S3 instead of local files
2. **Background Processing**: Use message queues for high-volume photo processing
3. **EXIF Data**: Extract date/GPS information from camera metadata
4. **Animal Detection**: Add AI-powered wildlife identification
5. **Batch Upload**: Allow multiple photo uploads at once

## Support

If you encounter issues:
1. Check application logs for processing errors
2. Verify directory permissions and disk space
3. Test with smaller images first
4. Review the detailed documentation in `docs/photo-processing-pipeline.md`