using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ReeperCommon;

namespace AlignedCurrencyIndicator
{
    public delegate void ListenerCallback();


    /// <summary>
    /// There's an annoying flicker when updating the replacement widgets
    /// if the timing isn't right. I tried to use a coroutine to solve the
    /// problem but waiting a frame is too long and EndOfFrame doesn't work
    /// because it happens after the camera is rendered. A solution that lets
    /// us wait until just before the ui widgets begin rendering is needed
    /// </summary>
    class CameraListener : MonoBehaviour
    {
        Queue<ListenerCallback> queue = new Queue<ListenerCallback>();

        private static CameraListener _instance;



        public static CameraListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    Log.Normal("CameraListener: Adding to EZGUI camera");

                    // find gui camera
                    var cam = Camera.allCameras.ToList().Find(c => c.name == "EZGUI Cam");

                    if (cam == null)
                    {
                        Log.Error("Failed to find EZGUI camera!");
                    }
                    else
                    {
                        _instance = cam.gameObject.AddComponent<CameraListener>();
                    }
                }

                return _instance;
            }
        }



        public void Enqueue(ListenerCallback cb)
        {
            queue.Enqueue(cb);
        }



        private void OnSceneChange(GameScenes scene)
        {
            Log.Normal("Removing CameraListener from {0}", gameObject.name);
            _instance = null;
            GameEvents.onGameSceneLoadRequested.Remove(OnSceneChange);
            Component.Destroy(this);
      
            Log.Normal("Done");
        }



        private void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(OnSceneChange);
        }



        void OnPreRender()
        {
            if (queue.Count > 0)
                Log.Debug("{0} callbacks in queue for {1}", queue.Count, gameObject.name);

            while (queue.Count > 0)
                queue.Dequeue()();
        }
    }
}
