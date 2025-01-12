using CrudGenerator.Controllers;
using CrudGenerator.Services;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CrudGeneratorTest
{
    public class CodeGenerationControllerTests
    {
        private readonly Mock<ICodeGenerationService> _mockService;
        private readonly CodeGenerationController _controller;

        public CodeGenerationControllerTests()
        {
            _mockService = new Mock<ICodeGenerationService>();
            _controller = new CodeGenerationController(_mockService.Object);
        }

        [Fact]
        public async Task GenerateCode_ReturnsZipFile_WhenResponseTypeIsZip()
        {
            // Arrange
            var request = new CodeGenerationRequest
            {
                Models = new List<ModelDefinition>
        {
            new ModelDefinition
            {
                Name = "TestModel",
                Attributes = new List<AttributeDefinition>
                {
                    new AttributeDefinition { Name = "Id", Type = "int" },
                    new AttributeDefinition { Name = "Name", Type = "string" }
                },
                Relationships = new List<Relationship>()
            }
        },
                ResponseType = "zip",
                IncludeJwtAuthentication = false
            };

            _mockService.Setup(s => s.GenerateModelCode(It.IsAny<string>(), It.IsAny<List<(string, string)>>(), It.IsAny<List<Relationship>>()))
                        .ReturnsAsync("public class TestModel { }");

            _mockService.Setup(s => s.GenerateDbContextCode(It.IsAny<List<ModelDefinition>>(), It.IsAny<bool>()))
                        .ReturnsAsync("public class AppDbContext { }");

            _mockService.Setup(s => s.GenerateServiceCode(It.IsAny<string>()))
                        .ReturnsAsync("public interface ITestModelService { }");

            _mockService.Setup(s => s.GenerateRepositoryCode(It.IsAny<string>()))
                        .ReturnsAsync("public interface ITestModelRepository { }");

            _mockService.Setup(s => s.GenerateControllerCode(It.IsAny<string>()))
                        .ReturnsAsync("public class TestModelController : ControllerBase { }");

            _mockService.Setup(s => s.GenerateProgramCs(It.IsAny<List<string>>(), It.IsAny<bool>()))
                        .ReturnsAsync("public class Program { static void Main() { } }");

            _mockService.Setup(s => s.GenerateProjectFile(It.IsAny<string>()))
                        .ReturnsAsync("<Project></Project>");

            _mockService.Setup(s => s.GenerateAppSettingsJson())
                        .ReturnsAsync("{}");

            // Act
            var result = await _controller.GenerateCode(request);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/zip", fileResult.ContentType);


            // Read and validate the zip stream
            using (var memoryStream = new MemoryStream())
            {
                await fileResult.FileStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    Assert.NotEmpty(archive.Entries);
                    Assert.Contains(archive.Entries, entry => entry.FullName == "Models/TestModel.cs");
                }
            }
        }

    }
}
