using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<TripDTO>> GetTripsForClient(int clientId);
    Task<int> CreateClient(ClientCreateDTO client);
    Task<bool> ClientExists(int clientId);
    Task<bool> TripExists(int tripId);
    Task<bool> RegisterClientForTrip(int clientId, int tripId);
    Task<bool> UnregisterClientFromTrip(int clientId, int tripId);
}