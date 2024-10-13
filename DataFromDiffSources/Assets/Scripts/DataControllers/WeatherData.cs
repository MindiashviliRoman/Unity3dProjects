using System;

namespace DataFromDiffSources
{
    [Serializable]
    public struct WeatherData
    {
        public float latitude;
        public float longitude;
        public WeatherItemData current;

    }
    [Serializable]
    public struct WeatherItemData
    {
        public string time;
        public float interval;
        public float temperature_2m;
        public float wind_speed_10m;

        public DateTime GetTime()
        {
            return DateTime.Parse(time);
        }
    }
}
