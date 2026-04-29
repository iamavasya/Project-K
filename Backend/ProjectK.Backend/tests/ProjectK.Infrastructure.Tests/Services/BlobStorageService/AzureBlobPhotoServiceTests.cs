using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.Infrastructure.Services.BlobStorageService;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.Infrastructure.Tests.Services.BlobStorageService
{
    public class AzureBlobPhotoServiceTests
    {
        private readonly AzureBlobPhotoService _service;
        private readonly Mock<ILogger<AzureBlobPhotoService>> _loggerMock;

        public AzureBlobPhotoServiceTests()
        {
            _loggerMock = new Mock<ILogger<AzureBlobPhotoService>>();
            
            // Provide a dummy connection string to satisfy the constructor
            var options = new BlobStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=dummy;AccountKey=dummykey;EndpointSuffix=core.windows.net",
                ContainerName = "test-photos"
            };

            _service = new AzureBlobPhotoService(options, null, _loggerMock.Object);
        }

        [Fact]
        public async Task CompressImageAsync_ShouldResizeAndCompressLargeImage()
        {
            // Arrange
            // Generate a large 3000x3000 image
            using var rawImage = new Image<Rgba32>(3000, 3000);
            
            // Fill with some data to ensure it has a measurable size
            var rnd = new Random(42);
            rawImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        pixelRow[x] = new Rgba32((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));
                    }
                }
            });

            using var msOriginal = new MemoryStream();
            await rawImage.SaveAsJpegAsync(msOriginal, new JpegEncoder { Quality = 100 });
            byte[] originalBytes = msOriginal.ToArray();

            // Act
            var result = await _service.CompressImageAsync(originalBytes, "test.png", CancellationToken.None);

            // Assert
            Assert.Equal(".jpg", result.FinalExtension);
            Assert.True(result.ProcessedBytes.Length < originalBytes.Length, "The compressed image should be smaller than the original uncompressed image.");

            // Verify the dimensions were resized to max 1920x1920
            using var processedImage = Image.Load(result.ProcessedBytes);
            Assert.True(processedImage.Width <= 1920);
            Assert.True(processedImage.Height <= 1920);
        }

        [Fact]
        public async Task CompressImageAsync_ShouldReturnOriginalBytes_WhenFileIsNotAnImage()
        {
            // Arrange
            byte[] invalidImageBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 }; // Invalid image header
            string fileName = "document.pdf";

            // Act
            var result = await _service.CompressImageAsync(invalidImageBytes, fileName, CancellationToken.None);

            // Assert
            Assert.Equal(".pdf", result.FinalExtension); // Extension shouldn't change
            Assert.Equal(invalidImageBytes, result.ProcessedBytes); // Bytes shouldn't change since it failed to parse
        }

        [Fact]
        public async Task CompressImageAsync_ShouldMaintainSizeIfAlreadySmall()
        {
            // Arrange
            // Generate a small 100x100 image
            using var rawImage = new Image<Rgba32>(100, 100);
            using var msOriginal = new MemoryStream();
            await rawImage.SaveAsJpegAsync(msOriginal, new JpegEncoder { Quality = 75 });
            byte[] originalBytes = msOriginal.ToArray();

            // Act
            var result = await _service.CompressImageAsync(originalBytes, "small.jpg", CancellationToken.None);

            // Assert
            Assert.Equal(".jpg", result.FinalExtension);
            
            // Verify dimensions remained the same
            using var processedImage = Image.Load(result.ProcessedBytes);
            Assert.Equal(100, processedImage.Width);
            Assert.Equal(100, processedImage.Height);
        }
    }
}
