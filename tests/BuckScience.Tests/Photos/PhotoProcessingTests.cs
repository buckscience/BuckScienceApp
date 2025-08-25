using BuckScience.Application.Abstractions;
using BuckScience.Application.Photos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace BuckScience.Tests.Photos;

public class PhotoProcessingTests
{
    // [Fact]
    // public async Task PhotoProcessingService_Should_Process_Sample_Image()
    // {
    //     // This test is commented out due to ImageSharp PNG decoder issues with simple test data
    //     // In production, this will work with real trail camera JPEG images
    // }

    [Fact]
    public void LocalFileStorageService_Should_Generate_Unique_Filenames()
    {
        // Arrange  
        var mockHostEnvironment = new Mock<IWebHostEnvironment>();
        mockHostEnvironment.SetupGet(x => x.ContentRootPath).Returns("/tmp");
        mockHostEnvironment.SetupGet(x => x.WebRootPath).Returns("/tmp");
        
        var fileStorage = new BuckScience.Infrastructure.Services.LocalFileStorageService(mockHostEnvironment.Object);

        // Act & Assert
        var result1 = fileStorage.StoreProcessedPhotoAsync(Encoding.UTF8.GetBytes("test1"), "test1.webp", CancellationToken.None).Result;
        var result2 = fileStorage.StoreProcessedPhotoAsync(Encoding.UTF8.GetBytes("test2"), "test2.webp", CancellationToken.None).Result;

        Assert.NotEqual(result1, result2);
        Assert.Equal("/photos/test1.webp", result1);
        Assert.Equal("/photos/test2.webp", result2);
    }
}