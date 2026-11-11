namespace BiznetX.Customer;

public sealed class CustomerClientOptions
{
    public Uri BaseAddress { get; set; } = new("http://localhost:5080/");
    public string ApiKey { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    internal bool IsValid() =>
        BaseAddress is { IsAbsoluteUri: true }
        && (
            BaseAddress.Scheme == Uri.UriSchemeHttps
            || (BaseAddress.Scheme == Uri.UriSchemeHttp && BaseAddress.IsLoopback)
        )
        && string.IsNullOrEmpty(BaseAddress.UserInfo)
        && string.IsNullOrEmpty(BaseAddress.Query)
        && string.IsNullOrEmpty(BaseAddress.Fragment)
        && BaseAddress.AbsolutePath.EndsWith('/')
        && ApiKey.Length is >= 32 and <= 256
        && !ApiKey.Any(char.IsWhiteSpace)
        && Timeout > TimeSpan.Zero
        && Timeout <= TimeSpan.FromMinutes(5);
}
