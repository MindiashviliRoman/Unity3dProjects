using DataFromDiffSources.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DataFromDiffSources.DataControllers
{
    public class WeatherDataController : BaseDataController
    {
        public string urlRequest { get; private set; } = "https://api.open-meteo.com/v1/forecast?latitude=52.52&longitude=13.41&current=temperature_2m,wind_speed_10m";

        #region Base abstract interface
        protected override bool DataPrepare()
        {
            StartCoroutine(GetRequest(urlRequest));
            return true;
        }
        #endregion

        IEnumerator GetRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        ParseData(webRequest.downloadHandler.text);
                        break;
                }
            }
        }

        private void ParseData(string textData)
        {
            var data = JsonUtility.FromJson<WeatherData>(textData);

            var weatherPanel = _panelInstance as WeatherPanel;
            if (weatherPanel != null)
            {
                weatherPanel.UpdateView(data);
            }
        }
    }
}
