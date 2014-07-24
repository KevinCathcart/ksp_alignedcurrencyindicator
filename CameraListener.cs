/******************************************************************************
               AlignedCurrencyIndicator for Kerbal Space Program                    
 ******************************************************************************
Copyright (c) 2014 Allen Mrazek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
******************************************************************************/
using System.Collections.Generic;
using System.Linq;
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
