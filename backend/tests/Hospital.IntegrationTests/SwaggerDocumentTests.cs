using System.Net;
using FluentAssertions;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class SwaggerDocumentTests
{
    private readonly HospitalApiFactory _factory;

    public SwaggerDocumentTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SwaggerJson_ReturnsOk_WithHttpBearerScheme()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("\"openapi\"");
        body.Should().Contain("/api/doctors/{id}/photo");
        body.Should().Contain("/api/medical-documents");
        body.Should().Contain("\"Bearer\"");
        body.Should().Contain("\"scheme\":\"bearer\"");
        body.Should().Contain("/api/feedback/mine");
        body.Should().Contain("/api/feedback/staff");
    }
}
