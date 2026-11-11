using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BiznetX.Customer.Tests;

public sealed class CustomerClientTests
{
    private const string TEST_KEY = "test-project-key-with-at-least-32-characters";

    [Fact]
    public async Task Identify_uses_versioned_route_auth_and_camel_case_json()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(
                new
                {
                    externalId = "user@42",
                    email = "person@example.test",
                    attributes = new { tier = "gold" },
                    createdAt = DateTimeOffset.UtcNow,
                    updatedAt = DateTimeOffset.UtcNow,
                }
            ),
        });
        using var services = CreateServices(handler);
        var result = await services
            .GetRequiredService<ICustomerClient>()
            .IdentifyAsync(
                "user@42",
                new("person@example.test", new Dictionary<string, object?> { ["tier"] = "gold" })
            );
        Assert.Equal("user@42", result.ExternalId);
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal(
            "https://customer.example.test/prefix/v1/customers/user%4042",
            handler.Uri!.AbsoluteUri
        );
        Assert.Equal($"Bearer {TEST_KEY}", handler.Authorization);
        var body = JsonSerializer.Deserialize<JsonElement>(handler.Body!);
        Assert.Equal("gold", body.GetProperty("attributes").GetProperty("tier").GetString());
        Assert.Equal("person@example.test", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Track_preserves_caller_id_and_reports_duplicate_receipts()
    {
        var eventId = Guid.NewGuid();
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { eventId, duplicate = true }),
        });
        using var services = CreateServices(handler);
        var result = await services
            .GetRequiredService<ICustomerClient>()
            .TrackAsync(
                new(
                    eventId,
                    "player-1",
                    "casino.deposit.completed",
                    Properties: new Dictionary<string, object?> { ["amountMinor"] = 1500 }
                )
            );
        Assert.True(result.Duplicate);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(
            eventId,
            JsonSerializer.Deserialize<JsonElement>(handler.Body!).GetProperty("eventId").GetGuid()
        );
        Assert.Equal(HttpMethod.Post, handler.Method);
    }

    [Fact]
    public async Task Conflict_retains_status_and_problem_details()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(
            HttpStatusCode.Conflict
        )
        {
            Content = JsonContent.Create(
                new
                {
                    title = "Event ID already used",
                    detail = "Use the original payload.",
                    traceId = "trace-42",
                }
            ),
        });
        using var services = CreateServices(handler);
        var exception = await Assert.ThrowsAsync<CustomerApiException>(
            () =>
                services
                    .GetRequiredService<ICustomerClient>()
                    .TrackAsync(new(Guid.NewGuid(), "user-1", "signup"))
        );
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("trace-42", exception.TraceId);
        Assert.Equal("Use the original payload.", exception.Detail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Permanent_processing_block_preserves_410_and_never_retries(bool track)
    {
        const string TITLE = "Customer processing is permanently blocked.";
        var calls = 0;
        using var handler = new RecordingHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.Gone)
            {
                Content = JsonContent.Create(
                    new
                    {
                        status = 410,
                        title = TITLE,
                        traceId = "processing-block-trace",
                    }
                ),
            };
        });
        using var services = CreateServices(handler);
        var client = services.GetRequiredService<ICustomerClient>();
        var exception = await Assert.ThrowsAsync<CustomerApiException>(async () =>
        {
            if (track)
                await client.TrackAsync(new(Guid.NewGuid(), "user-1", "signup"));
            else
                await client.IdentifyAsync("user-1", new());
        });
        Assert.Equal(HttpStatusCode.Gone, exception.StatusCode);
        Assert.Equal(TITLE, exception.Message);
        Assert.Equal("processing-block-trace", exception.TraceId);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Non_json_errors_still_expose_http_status()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(
            HttpStatusCode.BadGateway
        )
        {
            Content = new StringContent("upstream unavailable"),
        });
        using var services = CreateServices(handler);
        var exception = await Assert.ThrowsAsync<CustomerApiException>(
            () => services.GetRequiredService<ICustomerClient>().IdentifyAsync("user-1", new())
        );
        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
    }

    [Theory]
    [InlineData("http://customer.example.test/")]
    [InlineData("https://customer.example.test/prefix")]
    [InlineData("https://customer.example.test/?token=secret")]
    public void Invalid_base_urls_are_rejected(string url)
    {
        var services = new ServiceCollection();
        services.AddBxCustomer(options =>
        {
            options.BaseAddress = new Uri(url);
            options.ApiKey = TEST_KEY;
        });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<ICustomerClient>()
        );
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        using var handler = new RecordingHandler(token =>
        {
            token.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var services = CreateServices(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () =>
                services
                    .GetRequiredService<ICustomerClient>()
                    .IdentifyAsync("user-1", new(), cancellation.Token)
        );
    }

    [Fact]
    public async Task Empty_event_id_is_rejected_before_transport()
    {
        using var handler = new RecordingHandler(_ =>
            throw new InvalidOperationException("Must not send")
        );
        using var services = CreateServices(handler);
        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                services
                    .GetRequiredService<ICustomerClient>()
                    .TrackAsync(new(Guid.Empty, "user-1", "signup"))
        );
    }

    private static ServiceProvider CreateServices(RecordingHandler handler)
    {
        var services = new ServiceCollection();
        services
            .AddBxCustomer(options =>
            {
                options.BaseAddress = new Uri("https://customer.example.test/prefix/");
                options.ApiKey = TEST_KEY;
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private sealed class RecordingHandler(Func<CancellationToken, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response(cancellationToken);
        }
    }
}
