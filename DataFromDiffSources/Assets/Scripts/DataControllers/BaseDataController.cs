using DataFromDiffSources.UI;
using UnityEngine;

namespace DataFromDiffSources.DataControllers
{
    public abstract class BaseDataController : MonoBehaviour
    {
        [SerializeField]
        private BasePanel panelPrefab;
        protected BasePanel _panelInstance;

        public Transform TargetPos;

        public bool IsOpened { get; private set; }

        protected virtual void Start()
        {
            if (panelPrefab == null)
            {
                Debug.LogError("[BaseDataController]. Not found panelPrefab");
            }
            else
            {
                if (_panelInstance == null)
                {
                    _panelInstance = GameObject.Instantiate<BasePanel>(panelPrefab);
                    var targetParent = Settings.Instance.MainCanvas.transform;
                    if (TargetPos != null)
                    {
                        targetParent = TargetPos;
                    }
                    _panelInstance.transform.SetParent(targetParent);
                    _panelInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    _panelInstance.gameObject.SetActive(false);
                    _panelInstance.SetEnabledCallback(OnPanelEnabled);
                    _panelInstance.SetDisabledCallback(OnPanelDisabled);
                }
            }
        }

        protected abstract bool DataPrepare();

        public void OpenDataContent()
        {
            if (!IsOpened)
            {
                if (DataPrepare())
                {
                    _panelInstance.gameObject.SetActive(true);
                }
            }
            else
            {
                _panelInstance.gameObject.SetActive(false);
            }
        }


        protected virtual void OnPanelEnabled()
        {
            IsOpened = true;
        }
        protected virtual void OnPanelDisabled()
        {
            IsOpened = false;
        }
    }
}
