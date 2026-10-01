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

    public Task<TreatmentInfoAgentResponse> AskTreatmentInfoAsync(
        TreatmentInfoAgentRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<TreatmentInfoAgentRequest, TreatmentInfoAgentResponse>(
            "/internal/agents/treatment-info",
            request,
            cancellationToken);

    public Task<PatientInfoAgentResponse> AskPatientInfoAsync(
        PatientInfoAgentRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<PatientInfoAgentRequest, PatientInfoAgentResponse>(
            "/internal/agents/patient-info",
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

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            var fallbackUri = GetAlternateUri(path);
            if (fallbackUri is not null)
            {
                using var fallbackMessage = new HttpRequestMessage(HttpMethod.Post, fallbackUri)
                {
                    Content = JsonContent.Create(body, options: Json)
                };
                fallbackMessage.Headers.Add("X-Internal-Secret", _options.SharedSecret);
                try
                {
                    response = await _http.SendAsync(fallbackMessage, cancellationToken);
                }
                catch
                {
                    _logger.LogWarning(
                        ex,
                        "Agent request failed on primary and fallback ports. ExceptionType={ExceptionType}",
                        ex.GetType().Name);
                    throw;
                }
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Agent request failed. ExceptionType={ExceptionType}",
                    ex.GetType().Name);
                throw;
            }
        }
        catch (Exception ex)
        {
            var status = ex is HttpRequestException http ? http.StatusCode : null;
            _logger.LogWarning(
                ex,
                "Agent request failed. ExceptionType={ExceptionType} HttpStatus={HttpStatus}",
                ex.GetType().Name,
                status is null ? "none" : ((int)status).ToString());
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Agent service returned HttpStatus={HttpStatus}",
                (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(Json, cancellationToken)
            ?? throw new InvalidOperationException("Agent service returned an empty payload.");
        return payload;
    }

    private Uri? GetAlternateUri(string path)
    {
        var baseUri = _http.BaseAddress;
        if (baseUri is null) return null;
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        if (baseUri.Port == 8001)
        {
            return new Uri($"http://{baseUri.Host}:8100{normalizedPath}");
        }
        if (baseUri.Port == 8100)
        {
            return new Uri($"http://{baseUri.Host}:8001{normalizedPath}");
        }
        return null;
    }
}
