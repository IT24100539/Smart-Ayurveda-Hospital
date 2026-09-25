using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

public sealed class WorkflowExecutionTests
{
    [Fact]
    public async Task InternalUpsert_IsKeyGuarded_AndStaffCanSearchTheSnapshot()
    {
        await using var factory = new HospitalApiFactory();
        using var client = factory.CreateClient();

        var missingKey = await client.PostAsJsonAsync("/api/internal/workflow-executions", Payload());
        missingKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Add("X-Internal-Service-Key", "dev-internal-service-key");
        var id = Guid.NewGuid();
        var created = await client.PostAsJsonAsync("/api/internal/workflow-executions", Payload(id));
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        createdBody.GetProperty("agentName").GetString().Should().Be("scheduling_bed");
        createdBody.GetProperty("finalOutcome").GetString().Should().Be("InProgress");
        createdBody.GetProperty("toolResults").GetArrayLength().Should().Be(1);

        var patched = await client.PatchAsJsonAsync($"/api/internal/workflow-executions/{id}", Patch());
        patched.StatusCode.Should().Be(HttpStatusCode.OK);

        var loaded = await client.GetAsync($"/api/internal/workflow-executions/{id}");
        loaded.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadedBody = await loaded.Content.ReadFromJsonAsync<JsonElement>();
        loadedBody.GetProperty("approvalStatus").GetString().Should().Be("Pending");
        loadedBody.GetProperty("finalOutcome").GetString().Should().Be("Success");
        loadedBody.GetProperty("completedSteps").GetArrayLength().Should().Be(1);
        loadedBody.GetProperty("validationResults")[0].GetProperty("passed").GetBoolean().Should().BeTrue();

        client.DefaultRequestHeaders.Remove("X-Internal-Service-Key");
        (await client.GetAsync("/api/workflow-executions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var token = await StaffToken(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listed = await client.GetAsync("/api/workflow-executions?agentName=scheduling_bed&approvalStatus=Pending&search=ward");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listed.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
        page.GetProperty("items")[0].GetProperty("id").GetGuid().Should().Be(id);
        page.GetProperty("items")[0].GetProperty("errors").GetArrayLength().Should().Be(0);

        using var scope = factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<HospitalDbContext>().WorkflowExecutions.SingleAsync();
        row.PlanJson.Should().Contain("check_ward");
        row.RelatedEntityType.Should().Be("AdmissionRequest");
    }

    private static async Task<string> StaffToken(HospitalApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var user = new User
        {
            FullName = "Workflow Admin",
            Email = "workflow-admin@test.local",
            PhoneNumber = "0770000099",
            PasswordHash = "not-used",
            Role = UserRole.Admin,
            IsActive = true
        };
        db.Users.Add(user);
        db.StaffUsers.Add(new StaffUser
        {
            Email = user.Email,
            PasswordHash = "not-used",
            FullName = user.FullName,
            Role = StaffRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return scope.ServiceProvider.GetRequiredService<IJwtTokenService>().Create(user).Token;
    }

    private static object Payload(Guid? id = null) => new
    {
        id = id ?? Guid.NewGuid(),
        agentName = "scheduling_bed",
        objectiveText = "Check the ward and propose admission.",
        planJson = new[] { "check_ward" },
        completedStepsJson = Array.Empty<string>(),
        toolResultsJson = new[]
        {
            new { tool = "check_ward_availability", succeeded = true, output = new { freeCapacity = 1 }, error = (string?)null, calledAt = DateTimeOffset.UtcNow }
        },
        validationResultsJson = Array.Empty<object>(),
        errorsJson = (string[]?)null,
        approvalStatus = "NotRequired",
        finalOutcome = "InProgress",
        relatedEntityType = "AdmissionRequest",
        relatedEntityId = Guid.NewGuid()
    };

    private static object Patch() => new
    {
        agentName = "scheduling_bed",
        objectiveText = "Check the ward and propose admission.",
        planJson = new[] { "check_ward" },
        completedStepsJson = new[] { "check_ward" },
        toolResultsJson = new[]
        {
            new { tool = "check_ward_availability", succeeded = true, output = new { freeCapacity = 1 }, error = (string?)null, calledAt = DateTimeOffset.UtcNow }
        },
        validationResultsJson = new[] { new { check = "scheduling_and_bed", passed = true, detail = "Ward has a free bed." } },
        errorsJson = Array.Empty<string>(),
        approvalStatus = "Pending",
        finalOutcome = "Success",
        relatedEntityType = "AdmissionRequest",
        relatedEntityId = Guid.NewGuid()
    };
}
