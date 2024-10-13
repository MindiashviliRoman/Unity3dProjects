using DataFromDiffSources.UI;
using System.IO;
using UnityEngine;
using UnityEngine.TextCore.Text;
using yutokun;

namespace DataFromDiffSources.DataControllers
{
    public class FileDataController : BaseDataController
    {
        [SerializeField]
        private bool createDynamicPanel;
        #region Base abstract interface
        protected override bool DataPrepare()
        {
            var dataFileName = Settings.Instance.DataFileName;
            if (createDynamicPanel)
            {
                dataFileName = Settings.Instance.DataFileName2;
            }

            var filePath = Path.Combine(Application.streamingAssetsPath + Path.DirectorySeparatorChar.ToString(), dataFileName);
            //Debug.LogFormat("[FileDataController]. path  to data is: {0}", filePath);
            GetDataFromFile(filePath);
            return true;
        }
        #endregion

        private async void GetDataFromFile(string path)
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            var reader = new WWW(path);
            while (!reader.isDone) { }
            var result = CSVParser.Parse(reader.text, Delimiter.Semicolon);
#endif 

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            var result = await CSVParser.LoadFromPathAsync(path, Delimiter.Semicolon); 
#endif

            var fileDataPanel = _panelInstance as FileDataPanel;
            if (fileDataPanel != null && result.Count > 0)
            {
                var data = new FileItemData[result.Count];
                for(var i = 0; i < data.Length; i++)
                {
                    var curItem = result[i];
                    if(curItem.Count == 2)
                    {
                        data[i] = new FileItemData() { NameField = curItem[0], Value = curItem[1] };
                    }
                    else
                    {
                        Debug.LogErrorFormat("[FileDataController]. incorrect data in file: {0}", path);
                        return;
                    }
                }
                fileDataPanel.UpdateView(data);
            }
        }
    }
}
