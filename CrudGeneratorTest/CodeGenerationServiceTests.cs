using CrudGenerator.Controllers;
using CrudGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrudGeneratorTest
{
    public class CodeGenerationServiceTests
    {
        private readonly CodeGenerationService _service;

        public CodeGenerationServiceTests()
        {
            _service = new CodeGenerationService();
        }

        [Fact]
        public async Task GenerateModelCode_HandlesAttributesAndRelationshipsCorrectly()
        {
            // Arrange
            var modelName = "Customer";
            var attributes = new List<(string, string)>
        {
            ("Id", "int"),
            ("Name", "string")
        };
            var relationships = new List<Relationship>
        {
            new Relationship { PropertyName = "Orders", TargetModel = "Order", Type = RelationshipType.OneToMany }
        };

            // Act
            var result = await _service.GenerateModelCode(modelName, attributes, relationships);

            // Assert
            Assert.Contains("public int Id { get; set; }", result);
            Assert.Contains("public string Name { get; set; }", result);
            Assert.Contains("public ICollection<Order> Orders { get; set; }", result);
        }
    }

}
