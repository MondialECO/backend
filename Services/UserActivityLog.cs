using WebApp.DbContext;
using WebApp.Services.Interface;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services
{
    public class UserActivityLog : IAsyncActionFilter
    {
        private readonly IUserLogsRepository _userLogsRepository;
        private readonly GeolocationService _geolocationService;
        private readonly IUserGeolocationRepository _userGeolocation;

        public UserActivityLog(MongoDbContext context, GeolocationService geolocationService, IUserGeolocationRepository userGeolocation, IUserLogsRepository userLogsRepository)
        {
            _userLogsRepository = userLogsRepository;
            _geolocationService = geolocationService;
            _userGeolocation = userGeolocation;

        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();
            var url = $"{controllerName}/{actionName}";

            var forwardedForHeader = context.HttpContext.Request.Headers["X-Forwarded-For"].ToString();

            //var ipAddress = !string.IsNullOrEmpty(forwardedForHeader)
            //    ? forwardedForHeader.Split(',')[0].Trim() // Take the first IP in the list
            //    : context.HttpContext.Connection.RemoteIpAddress?.ToString();
            string ipAddress = "205.25.90.30"; // test ip address


            string data = !string.IsNullOrEmpty(context.HttpContext.Request.QueryString.Value)
                ? context.HttpContext.Request.QueryString.Value
                : JsonConvert.SerializeObject(context.ActionArguments.FirstOrDefault());

            var userName = context.HttpContext.User.Identity?.Name ?? "Anonymous";
            var localIpAddress = context.HttpContext.Connection.LocalIpAddress?.ToString();
            var clientPort = context.HttpContext.Connection.RemotePort;
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
            var requestMethod = context.HttpContext.Request.Method;
            var referrerUrl = context.HttpContext.Request.Headers["Referer"].ToString();
            var browserLanguage = context.HttpContext.Request.Headers["Accept-Language"].ToString();
            var responseStatusCode = context.HttpContext.Response.StatusCode;
            var isSecureConnection = context.HttpContext.Request.IsHttps;

            var location = await _geolocationService.GetGeolocationAsync(ipAddress);

            var geolocation = new GeolocationModel
            {
                IpAddress = ipAddress,
                UserName = userName,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                Loc = location.Loc,
                Org = location.Org,
                Postal = location.Postal,
                TimeZone = location.TimeZone
            };

            var geolocationFromDb = await _userGeolocation.GetByUserNameAndIpAddressAsync(userName, ipAddress);

            if (geolocationFromDb == null)
            {
                // Insert new record if not found
                await _userGeolocation.AddAsync(geolocation);
            }
            else
            {
                // Update record if it exists
                bool isMismatch =
                    geolocationFromDb.City != geolocation.City ||
                    geolocationFromDb.Region != geolocation.Region ||
                    geolocationFromDb.Country != geolocation.Country ||
                    geolocationFromDb.Loc != geolocation.Loc ||
                    geolocationFromDb.Org != geolocation.Org ||
                    geolocationFromDb.Postal != geolocation.Postal ||
                    geolocationFromDb.TimeZone != geolocation.TimeZone;

                if (isMismatch)
                {
                    // Update only if fields mismatch
                    geolocationFromDb.City = geolocation.City;
                    geolocationFromDb.Region = geolocation.Region;
                    geolocationFromDb.Country = geolocation.Country;
                    geolocationFromDb.Loc = geolocation.Loc;
                    geolocationFromDb.Org = geolocation.Org;
                    geolocationFromDb.Postal = geolocation.Postal;
                    geolocationFromDb.TimeZone = geolocation.TimeZone;

                    await _userGeolocation.UpdateAsync(geolocationFromDb);
                }
            }

            var locationIdGeo = await _userGeolocation.GetByUserNameAndIpAddressAsync(userName, ipAddress);
            var locationId = locationIdGeo?.Id.ToString();
            StoreUserActivity(data, url, userName, ipAddress, localIpAddress, clientPort, userAgent, requestMethod, referrerUrl, browserLanguage, responseStatusCode, locationId, isSecureConnection);
            await next();
        }

        private void StoreUserActivity(string data, string url, string userName, string ipAddress, string localIpAddress, int clientPort,
                                           string userAgent, string requestMethod, string referrerUrl, string browserLanguage,
                                           int responseStatusCode, string locationId, bool isSecureConnection)
        {
            var deviceType = DetermineDeviceType(userAgent);
            var operatingSystem = GetOperatingSystem(userAgent);
            var browserName = GetBrowserName(userAgent);

            var activityLog = new ActivityLogsModel
            {
                UserName = userName,
                IpAddress = ipAddress,
                url = url,
                data = data,
                LocalIpAddress = localIpAddress,
                Port = clientPort,
                ActivityDate = DateTime.Now,
                UserAgent = userAgent,
                RequestMethod = requestMethod,
                ResponseStatusCode = responseStatusCode,
                ReferrerUrl = referrerUrl,
                BrowserLanguage = browserLanguage,
                GeoLocation = locationId,
                DeviceType = deviceType,
                OperatingSystem = operatingSystem,
                BrowserName = browserName,
                IsSecureConnection = isSecureConnection
            };

            _userLogsRepository.AddAsync(activityLog);
        }

        private string DetermineDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return "Unknown";
            }

            if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return "Mobile";
            }
            if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            {
                return "Tablet";
            }
            return "Desktop";
        }

        private string GetOperatingSystem(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";

            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
            if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) return "MacOS";
            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
            if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iOS";

            return "Unknown";
        }

        private string GetBrowserName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";

            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) return "Chrome";
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome")) return "Safari";
            if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase)) return "Edge";
            if (userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Trident", StringComparison.OrdinalIgnoreCase)) return "Internet Explorer";

            return "Unknown";
        }
    }
}

