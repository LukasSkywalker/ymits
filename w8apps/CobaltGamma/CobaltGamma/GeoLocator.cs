using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace CobaltGamma
{
    class GeoLocator
    {
        private static Geolocator _geolocator = null;
        public static async Task<Geocoordinate> GetGeolocation()
        {
            _geolocator = new Geolocator();
            try
            {
                Geoposition pos = await _geolocator.GetGeopositionAsync();
                return pos.Coordinate;
            }
            catch (System.UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
