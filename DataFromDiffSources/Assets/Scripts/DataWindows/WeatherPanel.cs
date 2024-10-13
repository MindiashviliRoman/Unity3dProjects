using TMPro;
using UnityEngine;

namespace DataFromDiffSources.UI
{
    public class WeatherPanel : BasePanel
    {
        [SerializeField]
        private TextMeshProUGUI LatitudeText;
        [SerializeField]
        private TextMeshProUGUI LongitudeText;
        [SerializeField]
        private TextMeshProUGUI TimeText;

        [SerializeField]
        private TextMeshProUGUI Temperature2mText;
        [SerializeField]
        private TextMeshProUGUI WindSpeed10m;

        public void UpdateView(WeatherData data)
        {
            LatitudeText.text = data.latitude.ToString();
            LongitudeText.text = data.longitude.ToString();
            TimeText.text = data.current.GetTime().ToString();
            Temperature2mText.text = data.current.temperature_2m.ToString();
            WindSpeed10m.text = data.current.wind_speed_10m.ToString();
        }
    }
}
