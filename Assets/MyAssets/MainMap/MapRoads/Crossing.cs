using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace MainMap.Roads
{
    /// <summary>
    /// 交差点オブジェクトにアタッチする
    /// </summary>
    public class Crossing : MonoBehaviour
    {


        [SerializeField] private GUIStyle passGuiStyle;

        public Vector3 position
        {
            get => transform.position;
        }

        private void OnDrawGizmos()
        {
            Handles.Label(position, this.name, passGuiStyle);
        }

        public override string ToString()
        {
            return $"Crossing.{name}";
        }
    }
}