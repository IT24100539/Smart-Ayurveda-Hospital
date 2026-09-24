using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hospital.Api.Security;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class InternalFeedbackEndpointTests
{
    private readonly HospitalApiFactory _factory;

    public InternalFeedbackEndpointTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_WithoutServiceKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/internal/feedback/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithServiceKey_ReturnsCommentRatingAndPatientContext()
    {
        Guid id;
        string comment;
        int rating;
        Guid patientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var feedback = await db.Feedbacks.AsNoTracking().FirstAsync(x => x.Comment.Contains("abhyanga"));
            id = feedback.Id;
            comment = feedback.Comment;
            rating = feedback.Rating;
            patientId = feedback.PatientId;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalServiceKeyFilter.HeaderName, "dev-internal-service-key");
        var response = await client.GetAsync($"/api/internal/feedback/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("comment").GetString().Should().Be(comment);
        payload.GetProperty("rating").GetInt32().Should().Be(rating);
        payload.GetProperty("patientId").GetGuid().Should().Be(patientId);
        payload.GetProperty("prakriti").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Similar_CountsOtherRecentFeedbackInTheCategory()
    {
        Guid excludePatientId;
        int expected;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var sample = await db.Feedbacks.AsNoTracking()
                .FirstAsync(x => x.Category == FeedbackCategory.WaitingTime);
            excludePatientId = sample.PatientId;
            var since = DateTimeOffset.UtcNow.AddDays(-30);
            expected = await db.Feedbacks.CountAsync(x =>
                x.Category == FeedbackCategory.WaitingTime
                && x.PatientId != excludePatientId
                && x.CreatedAt >= since);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalServiceKeyFilter.HeaderName, "dev-internal-service-key");
        var response = await client.GetAsync(
            $"/api/internal/feedback/similar?category=WaitingTime&excludePatientId={excludePatientId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("count").GetInt32().Should().Be(expected);
        payload.GetProperty("category").GetString().Should().Be("WaitingTime");
    }
}
