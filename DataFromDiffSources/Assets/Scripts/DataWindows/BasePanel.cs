using System;
using UnityEngine;

namespace DataFromDiffSources.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        protected Action onEnabled;
        protected Action onDisabled;

        public void SetEnabledCallback(Action enabledCallback)
        {
            onEnabled = enabledCallback;
        }
        public void SetDisabledCallback(Action disabledCallback)
        {
            onDisabled = disabledCallback;
        }

        protected virtual void OnEnable()
        {
            onEnabled?.Invoke();
        }

        protected virtual void OnDisable()
        {
            onDisabled?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            onEnabled = null;
            onDisabled = null;
        }
    }
}
