using System.Net;

namespace BiznetX.Customer;

public sealed class CustomerApiException(
    HttpStatusCode statusCode,
    string title,
    string? detail,
    string? traceId
) : HttpRequestException(title, null, statusCode)
{
    public string? Detail { get; } = detail;
    public string? TraceId { get; } = traceId;
}
