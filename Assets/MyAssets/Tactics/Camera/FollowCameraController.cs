using UnityEngine;

namespace Tactics.Control
{
    /// <summary>
    /// This camera controller is adapted from here: https://ruhrnuklear.de/fcc/ and provided here as a convenience.
    /// </summary>
    public class FollowCameraController : MonoBehaviour
    {

        void Start()
        {
        }

        //Only Move camera after everything else has been updated 
        void LateUpdate()
        {

        }
    }
}