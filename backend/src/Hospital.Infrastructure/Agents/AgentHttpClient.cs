using System.Net.Http.Json;
using System.Text.Json;
using Hospital.Application.Agents;
using Hospital.Application.Agents.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hospital.Infrastructure.Agents;

public sealed class AgentServiceOptions
{
    public const string SectionName = "AgentService";
    public string BaseUrl { get; set; } = "http://127.0.0.1:8100";
    public string SharedSecret { get; set; } = string.Empty;
}

public sealed class AgentHttpClient : IAgentClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly AgentServiceOptions _options;
    private readonly ILogger<AgentHttpClient> _logger;

    public AgentHttpClient(HttpClient http, IOptions<AgentServiceOptions> options, ILogger<AgentHttpClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public Task<FeedbackSupportAgentResponse> DraftFeedbackSupportAsync(
        FeedbackSupportAgentRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<FeedbackSupportAgentRequest, FeedbackSupportAgentResponse>(
            "/internal/agents/feedback-support",
            request,
            cancellationToken);

    public Task<CoordinatorAgentResponse> CoordinateAsync(
        StartAgentWorkflowRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<StartAgentWorkflowRequest, CoordinatorAgentResponse>(
            "/internal/agents/coordinate",
            request,
            cancellationToken);

    public async Task<AgentInvokeResponse> InvokeAsync(AgentInvokeRequest request, CancellationToken cancellationToken)
    {
        var coordinated = await CoordinateAsync(
            new StartAgentWorkflowRequest(request.Prompt, request.Context),
            cancellationToken);
        return new AgentInvokeResponse(
            coordinated.DelegatedTo,
            coordinated.Summary,
            new Dictionary<string, string>
            {
                ["workflowId"] = coordinated.WorkflowId.ToString(),
                ["approvalStatus"] = coordinated.ApprovalStatus ?? "",
                ["finalOutcome"] = coordinated.FinalOutcome ?? ""
            });
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: Json)
        };
        message.Headers.Add("X-Internal-Secret", _options.SharedSecret);

        var response = await _http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Agent service returned {Status}: {Body}", (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(Json, cancellationToken)
            ?? throw new InvalidOperationException("Agent service returned an empty payload.");
        return payload;
    }
}
