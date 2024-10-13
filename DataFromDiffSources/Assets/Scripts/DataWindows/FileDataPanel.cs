using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DataFromDiffSources.UI
{
    public class FileDataPanel : BasePanel
    {
        [SerializeField]
        private ScrollRect scrollRect; //TODO: создать скролл вью на ограниченное число элементов (занимающее весь экран)

        [SerializeField]
        private DataItem itemPrefab;

        [SerializeField]
        private List<DataItem> dataItems = new();

        public void UpdateView(FileItemData[] data)
        {
            if(scrollRect != null && dataItems.Count < data.Length)
            {
                var nwCount = data.Length - dataItems.Count;
                for(var i = 0; i < nwCount; i++)
                {
                    var curItem = GameObject.Instantiate(itemPrefab);
                    curItem.transform.SetParent(scrollRect.content.transform);
                    dataItems.Add(curItem);
                }
            }

            for (var i = 0; i < dataItems.Count; i++)
            {
                if(i < data.Length)
                {
                    dataItems[i].FieldName.text = data[i].NameField;
                    dataItems[i].Value.text = data[i].Value;
                    dataItems[i].gameObject.SetActive(true);
                }
                else
                {
                    dataItems[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
