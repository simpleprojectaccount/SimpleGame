using UnityEngine;

namespace Vertigo.Managers
{
    public abstract class Manager<T> : MonoBehaviour where T : Manager<T>
    {
        public static T Instance { get; private set; }

        private bool isQuitting = false;

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = (T)this;
            else if (this != Instance)
                Destroy(this);
        }

        protected void OnApplicationQuit()
        {
            isQuitting = true;
        }

        protected void OnDestroy()
        {
            if (!isQuitting)
                Destructor();
        }

        protected virtual void Destructor() { }
    }
}