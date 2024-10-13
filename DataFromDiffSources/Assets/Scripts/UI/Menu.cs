using DataFromDiffSources.DataControllers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DataFromDiffSources.UI
{
    public class Menu : MonoBehaviour
    {
        [System.Serializable]
        struct ButtonAction
        {
            public Button Butt;
            public BaseDataController DataOpener;
        }

        [SerializeField]
        private List<ButtonAction> buttons;

        void Awake()
        {
            foreach (var b in buttons)
            {
                if (b.DataOpener != null && b.Butt != null)
                {
                    b.Butt.onClick.AddListener(b.DataOpener.OpenDataContent);
                }
            }

#if PLATFORM_ANDROID
            var rt = GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rt.rect.size.y * 2);
#endif
        }

        private void OnDestroy()
        {
            foreach (var b in buttons)
            {
                if(b.DataOpener != null && b.Butt != null)
                {
                    b.Butt.onClick.RemoveListener(b.DataOpener.OpenDataContent);
                }
            }
        }

    }
}
