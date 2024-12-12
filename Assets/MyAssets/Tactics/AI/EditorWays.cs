using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Tactics
{
    /// <summary>
    /// WayをEditor表示するための
    /// </summary>
    public class EditorWays : MonoBehaviour
    {

        [SerializeField] List<EditorPass> ways;
        [Tooltip("GridPointの番号表示のGUIテキスト")]
        [SerializeField] private GUIStyle passGuiStyle;


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {

            foreach(var w in ways)
            {
                if (w == null)
                    continue;
                if (w.parent == null)
                    continue;
                if (!w.enable)
                    continue;

                Gizmos.color = w.color;
                passGuiStyle.normal.textColor = w.color;

                var count = 0;
                Transform old = null;
                foreach (Transform pass in w.parent.transform)
                {
                    if (old != null)
                    {
                        Gizmos.DrawLine(old.position, pass.position);
                        Handles.Label(old.position, count.ToString(), passGuiStyle);
                        Gizmos.DrawLine(old.position, old.position + (old.forward * 0.3f));
                    }

                    old = pass;
                    count++;
                }

                if (old != null)
                {
                    Gizmos.DrawLine(old.position, old.position + (old.forward * 0.3f));
                    Handles.Label(old.position, count.ToString(), passGuiStyle);
                }
            }
        }
#endif
    }

    [Serializable]
    class EditorPass
    {
        [SerializeField] internal bool enable = true;
        [SerializeField] internal Transform parent;
        [SerializeField] internal Color color = Color.green;
    }
}