using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tactics.UI.Overlay
{
    public class Compass : MonoBehaviour
    {
        Material compassMaterial;
        Camera camera;

        // Start is called before the first frame update
        void Start()
        {
            var image = GetComponent<Image>();
            compassMaterial = image.material;
            camera = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            var deg = camera.transform.rotation.eulerAngles.y;
            compassMaterial.SetFloat("_Degree", deg);
        }
    }
}