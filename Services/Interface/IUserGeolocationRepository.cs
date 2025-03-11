using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IUserGeolocationRepository
    {
        Task<List<GeolocationModel>> GetGeolocationsNearby(double Loc, double maxDistanceInMeters);
        Task<GeolocationModel> GetByIpAddressAsync(string ipAddress);
        Task AddAsync(GeolocationModel geolocation);
        Task UpdateAsync(GeolocationModel geolocation);
        Task DeleteAsync(string id);
        Task<GeolocationModel> GetByUserName(string userName);
        Task<GeolocationModel> GetByUserNameAndIpAddressAsync(string userName, string ipAddress);
        Task<GeolocationModel> GetByIdAsync(string Id);
    }
}
