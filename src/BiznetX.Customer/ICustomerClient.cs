namespace BiznetX.Customer;

public interface ICustomerClient
{
    Task<CustomerProfile> IdentifyAsync(
        string customerId,
        IdentifyCustomerRequest request,
        CancellationToken cancellationToken = default
    );
    Task<EventReceipt> TrackAsync(
        TrackEventRequest request,
        CancellationToken cancellationToken = default
    );
}
