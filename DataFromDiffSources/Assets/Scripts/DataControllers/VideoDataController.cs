using DataFromDiffSources.UI;
using UnityEngine;
using UnityEngine.Video;

namespace DataFromDiffSources.DataControllers
{
    public class VideoDataController : BaseDataController
    {
        private string videoClipName;

        private VideoPlayer videoPlayer;
        private string _headerText = "Видео файл";

        protected override void Start()
        {
            base.Start();

            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("[VideoDataController]. Not found VideoPlayer component");
            }

            switch (videoPlayer.source)
            {
                case VideoSource.VideoClip:
                    videoClipName = System.IO.Path.Combine(Application.streamingAssetsPath, Settings.Instance.VideoFileName);
                    break;
                case VideoSource.Url:
                    videoClipName = Settings.Instance.VideoURL;
                    _headerText = "Видео из интернета";
                    break;
            }
        }

        #region Base abstract interface
        protected override bool DataPrepare()
        {
            if (videoPlayer != null)
            {
                videoPlayer.url = videoClipName;
                videoPlayer.prepareCompleted += Play;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.EnableAudioTrack(0, true);
                videoPlayer.Prepare();
                return true;
            }
            return false;
        }
        #endregion

        private void Play(VideoPlayer source)
        {
            var videoPanelInstance = _panelInstance as VideoPanel;
            videoPanelInstance.SetImage(source.texture, _headerText);
            source.Play();
        }

        protected override void OnPanelDisabled()
        {
            if(videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            base.OnPanelDisabled();
        }

        private void OnDestroy()
        {
            if(videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= Play;
            }
        }
    }
}
