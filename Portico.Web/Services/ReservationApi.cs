using Portico.Web.Models.Reservations;

namespace Portico.Web.Services;

public interface IReservationApi
{
    Task<ApiResult<List<TripDto>>> ListAsync(string email, CancellationToken cancellationToken);
    Task<ApiResult<TripDto>> GetAsync(string reference, CancellationToken cancellationToken);
    Task<ApiResult<ReservationCreated>> CreateAsync(StayRequest request, CancellationToken cancellationToken);
    Task<ApiResult<TripDto>> CancelAsync(string reference, CancellationToken cancellationToken);
}

public class ReservationApi : PorticoApiClient, IReservationApi
{
    public ReservationApi(HttpClient http, ILogger<ReservationApi> logger) : base(http, logger)
    {
    }

    public Task<ApiResult<List<TripDto>>> ListAsync(string email, CancellationToken cancellationToken) =>
        GetJsonAsync<List<TripDto>>($"/reservations?email={Uri.EscapeDataString(email)}", cancellationToken);

    public Task<ApiResult<TripDto>> GetAsync(string reference, CancellationToken cancellationToken) =>
        GetJsonAsync<TripDto>($"/reservations/{Uri.EscapeDataString(reference)}", cancellationToken);

    public Task<ApiResult<ReservationCreated>> CreateAsync(StayRequest request, CancellationToken cancellationToken) =>
        PostJsonAsync<ReservationCreated>("/reservations", request, cancellationToken);

    public Task<ApiResult<TripDto>> CancelAsync(string reference, CancellationToken cancellationToken) =>
        PostJsonAsync<TripDto>($"/reservations/{Uri.EscapeDataString(reference)}/cancel", null, cancellationToken);
}
