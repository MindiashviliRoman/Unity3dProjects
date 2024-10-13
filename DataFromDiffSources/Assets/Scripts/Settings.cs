using Core;
using UnityEngine;

namespace DataFromDiffSources
{
    public class Settings : MonoSingleton<Settings>
    {
        [SerializeField]
        private Canvas mainCanvas;

        [SerializeField]
        private string videoFileName;

        [SerializeField]
        private string videoURL;

        [SerializeField]
        private string htmlFilePath;

        [SerializeField]
        private string dataFilePath;

        [SerializeField]
        private string dataFilePath2;

        public Canvas MainCanvas => mainCanvas;
        public string VideoFileName => videoFileName;
        public string VideoURL => videoURL;
        public string HtmlFilePath => htmlFilePath;
        public string DataFileName => dataFilePath;
        public string DataFileName2 => dataFilePath2;

    }
}

