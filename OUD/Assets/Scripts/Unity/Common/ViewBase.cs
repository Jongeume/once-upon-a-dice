using UnityEngine;

namespace OUD.Unity.Common
{
    /// <summary>모든 View MonoBehaviour의 공통 기반. Show/Hide 추상화.</summary>
    public abstract class ViewBase : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
        public bool IsVisible => gameObject.activeSelf;
    }
}
