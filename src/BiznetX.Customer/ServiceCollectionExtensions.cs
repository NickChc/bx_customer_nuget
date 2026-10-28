using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BiznetX.Customer;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddBxCustomer(this IServiceCollection services, Action<CustomerClientOptions> configure)
    {
        services.AddOptions<CustomerClientOptions>().Configure(configure)
            .Validate(options => options.IsValid(), "Use HTTPS (or loopback HTTP), a base URL ending in /, a 32–256 character API key without whitespace, and a timeout up to five minutes.")
            .ValidateOnStart();
        return services.AddHttpClient<ICustomerClient, CustomerClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CustomerClientOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
    }
}
