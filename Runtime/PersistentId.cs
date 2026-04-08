using UnityEngine;

namespace SkyWalker.Craft
{
    /// <summary>
    /// Assigns a stable GUID to a GameObject that persists across sessions.
    /// Used by CRAFT to reliably identify scene objects between MCP calls.
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        string id;

        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(id))
                {
                    id = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
                return id;
            }
        }

        void Reset()
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
}
