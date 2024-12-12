using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility;

namespace Tactics.UI
{
    public class SearchingPanel : MonoBehaviour
    {
        private Camera mainCamera;

        private bool _enableSearchingCircle = false;

        public bool EnableSearchingCircle
        {
            get => _enableSearchingCircle;
            set
            {
                _enableSearchingCircle = value;
                if (value)
                {
                    // Appear
                }
                else
                {
                    // Disappear
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            mainCamera = Camera.current;
        }

        // Update is called once per frame
        protected void FixedUpdate()
        {
            //mainCamera.
        }
    }
}