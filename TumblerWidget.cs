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
using System.Linq;
using ReeperCommon;
using UnityEngine;

namespace AlignedCurrencyIndicator
{
    /// <summary>
    /// Replace the calculator-style cost widget with a tumbler-style
    /// widget like the stock funds
    /// 
    /// Leave stock funds widget as-is; nothing special needed for it
    /// </summary>
    class TumblerWidget : MonoBehaviour
    {
        CostWidget realCostWidget;
        FundsWidget realFundsWidget;
        FundsWidget newCostWidget;


        public static void Create(GameObject owner, CostWidget stockCostWidget, FundsWidget stockFundsWidget)
        {
            var tw = owner.AddComponent<TumblerWidget>();

            tw.realCostWidget = stockCostWidget;
            tw.realFundsWidget = stockFundsWidget;

            Log.Debug("TumblerWidget.Create");
        }



        private void Start()
        {
            Log.Normal("Using tumbler style widget");


            // make a copy of the funds widget
            Log.Normal("Cloning stock funds widget");
            var copy = (GameObject)GameObject.Instantiate(realFundsWidget.gameObject);
            copy.name = "AlignedCurrencyIndicator.TumblerWidget";

            copy.transform.position = realCostWidget.transform.position;
            copy.transform.parent = realCostWidget.transform.parent;

            Log.Normal("Cloned stock widget");
            newCostWidget = copy.GetComponent<FundsWidget>();

#if !DEBUG
            Log.Normal("Hiding stock cost widget");

            // hide the stock cost widget
            realCostWidget.gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r => r.enabled = false);

#else
            // move the stock cost widget off to the side for comparison
            realCostWidget.transform.Translate(new Vector3(300f, 0f, 0f));
#endif
            Log.Debug("Fixing currency icon");

            newCostWidget.transform.Find("fundsGreen").renderer.enabled = false;
            //newCostWidget.transform.Find("fundsRef").renderer.enabled = true;


            Log.Debug("Modifying TumblerWidget shaders");
            var tumblers = newCostWidget.GetComponentInChildren<Tumblers>();

            foreach (var t in tumblers.GetComponentsInChildren<Tumbler>())
            {
                var mat = t.renderer.material;

                // this is good enough to do what we want
                mat.shader = Shader.Find("Transparent/Specular");
                mat.SetColor("_Color", Color.black);
                mat.SetColor("_SpecColor", Color.red);
                mat.SetFloat("_Shininess", 0f);
            }

            Log.Normal("TumblerWidget: Subscribing to events");
            GameEvents.onGUILaunchScreenVesselSelected.Add(VesselSelected);
            GameEvents.onEditorShipModified.Add(ShipModified);
            GameEvents.OnFundsChanged.Add(FundsChanged);

            CameraListener.Instance.Enqueue(RefreshCost);

            StartCoroutine(WaitForStart());

        }

        private void OnDestroy()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Remove(VesselSelected);
            GameEvents.onEditorShipModified.Remove(ShipModified);
            GameEvents.OnFundsChanged.Remove(FundsChanged);
        }



        private System.Collections.IEnumerator WaitForStart()
        {
            Tumblers tumblers = realFundsWidget.GetComponentInChildren<Tumblers>();
            double current = tumblers.value;

            while (tumblers.value == current)
                yield return 0;
            

            // aaaaaaaand one more frame necessary
            yield return new WaitForEndOfFrame();

            CameraListener.Instance.Enqueue(RefreshCost);

            Log.Normal("TumblerWidget ready");
        }



        void VesselSelected(ShipTemplate template)
        {
            Log.Debug("TumblerWidget.VesselSelected");
            CameraListener.Instance.Enqueue(RefreshCost);
        }



        void ShipModified(ShipConstruct ship)
        {
            Log.Debug("TumblerWidget.ShipModified");
            CameraListener.Instance.Enqueue(RefreshCost);
        }



        /// <summary>
        /// Don't forget that the tumbler-style widget we cloned has
        /// registered itself for this event. If it is fired, the
        /// widget we're trying to use for cost will try and display
        /// funds instead so we need to catch that and overwrite the
        /// value before the player can see it
        /// </summary>
        /// <param name="value"></param>
        void FundsChanged(double value)
        {
            CameraListener.Instance.Enqueue(RefreshCost);
        }



        void RefreshCost()
        {
            float dryCost = 0f;
            float fuelCost = 0f;
            float total = EditorLogic.fetch.ship.GetShipCosts(out dryCost, out fuelCost);

            Log.Debug("TumblerWidget.RefreshCost");
            SetCost(total);
        }

        private void SetCost(double total)
        {
            Tumblers tumblers = newCostWidget.GetComponentInChildren<Tumblers>();
            tumblers.setValue(total);

            // [Log]: AlignedCurrencyIndicator, Tumbler shader: Unlit/Transparent Tint
            //  note to self: tried various methods, couldn't figure out how to change "tint"
            //  so modified shader in start
            tumblers.GetComponentsInChildren<Tumbler>().ToList().ForEach(t =>
            {
                if (t.renderer != null && t.renderer.material != null)
                    t.renderer.material.SetFloat("_Shininess", total > Funding.Instance.Funds ? 0f : 1f);
            });
        }
    }
}
