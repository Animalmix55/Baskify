using GeoCoordinatePortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace BaskifyClient.Utilities
{
    public static class SearchUtils
    {
        public static double getMiles(float lat1, float lng1, float lat2, float lng2)
        {
            var lat1Coord = new GeoCoordinate(lat1, lng1);
            var lat2Coord = new GeoCoordinate(lat2, lng2);

            return lat1Coord.GetDistanceTo(lat2Coord)/1609.34; //conversion from meters to miles
        }
    }
}
