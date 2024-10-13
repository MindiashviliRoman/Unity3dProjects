using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataFromDiffSources.UI
{
    public class VideoPanel : BasePanel
    {
        [SerializeField]
        private TextMeshProUGUI headerText;

        [SerializeField]
        private RawImage contentImage;

        private Vector2 _startSize;
        private float _startArImage = 1f;

        private void Awake()
        {
            _startSize = contentImage.rectTransform.rect.size;
            _startArImage = _startSize.x / _startSize.y;
        }

        public void SetImage(Texture rt, string hText)
        {
            headerText.text = hText;

            contentImage.texture = rt;

            var arRt = (float)rt.width / rt.height;
            var nwSz = Vector2.zero;
            if (arRt > _startArImage)
            {
                nwSz = new Vector2(_startSize.x, _startSize.x / arRt);
                contentImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nwSz.x);
                contentImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nwSz.y);
            }
            else
            {
                nwSz = new Vector2(_startSize.y * arRt, _startSize.y);
                contentImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nwSz.x);
                contentImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nwSz.y);
            }
        }
    }
}
