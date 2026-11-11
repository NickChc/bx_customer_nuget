using System.Net.Http.Json;
using System.Text.Json;

namespace BiznetX.Customer;

public sealed class CustomerClient(HttpClient httpClient) : ICustomerClient
{
    public Task<CustomerProfile> IdentifyAsync(
        string customerId,
        IdentifyCustomerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<CustomerProfile>(
            HttpMethod.Put,
            $"v1/customers/{Uri.EscapeDataString(customerId)}",
            request,
            cancellationToken
        );
    }

    public Task<EventReceipt> TrackAsync(
        TrackEventRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.EventId == Guid.Empty)
            throw new ArgumentException(
                "Generate an event ID once and preserve it when retrying.",
                nameof(request)
            );
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CustomerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        return SendAsync<EventReceipt>(HttpMethod.Post, "v1/events", request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object payload,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(payload),
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Problem? problem = null;
            try
            {
                problem = await response.Content.ReadFromJsonAsync<Problem>(cancellationToken);
            }
            catch (JsonException)
            { /* Reverse proxies may return non-JSON errors. */
            }
            throw new CustomerApiException(
                response.StatusCode,
                problem?.Title ?? "BX Customer request failed.",
                problem?.Detail,
                problem?.TraceId
            );
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new JsonException("BX Customer returned an empty response.");
    }

    private sealed record Problem(string? Title, string? Detail, string? TraceId);
}
