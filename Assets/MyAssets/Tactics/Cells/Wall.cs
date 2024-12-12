using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility;

namespace Tactics.Map
{
    public class Wall : MonoBehaviour
    {
        /// <summary>
        /// Wallからアクセスするため
        /// </summary>
        internal List<TileCell> contactCells = new List<TileCell>();

        internal new BoxCollider collider;

        internal GameObject navigationStatic;

        internal GameObject wallObject;

        void Awake()
        {
            wallObject = gameObject;
            collider = GetComponent<BoxCollider>();

            foreach (Transform child in transform)
            {
                var childObject = child.gameObject;
                navigationStatic = childObject;
                break;
            }
        }

        public override string ToString()
        {
            return gameObject.ToString();
        }
    }
}