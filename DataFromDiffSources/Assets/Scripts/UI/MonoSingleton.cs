using UnityEngine;

namespace Core
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance = null;

        public static T Instance
        {
            get
            {
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null) Debug.LogError(name + "error: already initialized", this);

            _instance = (T)this;
        }

    }
}

