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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using ReeperCommon;

namespace AlignedCurrencyIndicator
{
    /// <summary>
    /// Replace the funds widget with a calculator-style widget. Format
    /// the numbers in the cost widget (and drop the decimal).
    /// </summary>
    public class CalculatorWidget : MonoBehaviour
    {
        CostWidget realCostWidget;
        FundsWidget realFundsWidget;
        CostWidget newFundsWidget;
        NumberFormatInfo formatter;
        


        public static void Create(GameObject owner, CostWidget stockCostWidget, FundsWidget stockFundsWidget)
        {
            var cw = owner.AddComponent<CalculatorWidget>();

            cw.realCostWidget = stockCostWidget;
            cw.realFundsWidget = stockFundsWidget;

            Log.Debug("stock cost widget layer = {0}", stockCostWidget.gameObject.layer);
            Log.Debug("stock funds widget layer = {0}", stockFundsWidget.gameObject.layer);

            Log.Debug("CalculatorWidget.Create");
        }



        /// <summary>
        /// Use calculator style widget for both cost and funds
        /// </summary>
        private void Start()
        {
            Log.Normal("Using calculator widget style");

            Log.Debug("Cloning stock cost widget...");

            var copy = (GameObject)GameObject.Instantiate(realCostWidget.gameObject);
            copy.name = "AlignedCurrencyIndicator.FundsWidget";
            copy.transform.parent = realFundsWidget.transform.parent;
            copy.transform.position = realFundsWidget.transform.position;

            Log.Normal("Cloned stock cost widget.");

            newFundsWidget = copy.GetComponent<CostWidget>();

            // hide the stock funds widget
            Log.Normal("Hiding stock funds widget renderers");
#if (!DEBUG)
            realFundsWidget.gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r =>
            {
                r.enabled = false;
            });
#else
            // for debug purposes, just move the real one off to the side so
            // we can make sure they always agree
            realFundsWidget.transform.Translate(new Vector3(300f, 0f, 0f));

            // ... and add another copy of the stock cost widget that we
            // won't overwrite the value of. This should always agree
            // with our version

            copy = (GameObject)GameObject.Instantiate(realCostWidget.gameObject);
            copy.transform.parent = realCostWidget.transform.parent;
            copy.transform.position = realCostWidget.transform.position;
            copy.transform.Translate(new Vector3(300f, 0f, 0f));
#endif

            //copy.GetComponentsInChildren<Renderer>().ToList().ForEach(r =>
            //{
            //    if (r.sharedMaterial != null)
            //    if (r.sharedMaterial.mainTexture != null)
            //    ((Texture2D)r.sharedMaterial.mainTexture).CreateReadable().SaveToDisk(string.Format("cost_{0}", r.gameObject.name));
            //});

            // there's a little more to be done in the rendering department; the 
            // cost widget we cloned has that red funds icon and we just hid the green
            // one so fix that real quick..
            Log.Normal("Fixing fund icons");

            // hide the red cost icon 
            newFundsWidget.transform.Find("costIcon").renderer.enabled = false;

            // show the green cost icon
            realFundsWidget.transform.Find("fundsGreen").renderer.enabled = true;


            // culture setting
            Log.Normal("Configuring NumberFormatInfo for current locale");
            formatter = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            formatter.CurrencySymbol = string.Empty;
            formatter.CurrencyDecimalDigits = 0;

            copy.SetActive(true);



            Log.Normal("CalculatorWidget: Subscribing to events");
            GameEvents.onGUILaunchScreenVesselSelected.Add(VesselSelected);
            GameEvents.onEditorShipModified.Add(ShipModified);
            GameEvents.OnFundsChanged.Add(FundsChanged);

            CameraListener.Instance.Enqueue(RefreshCost);

            StartCoroutine(WaitForStart());
        }



        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                realFundsWidget.gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r =>
                {
                    r.enabled = !r.enabled;
                    Log.Normal("{0} renderer in {1}", r.enabled ? "enabled" : "disabled", r.gameObject.name);
                });
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

            Log.Normal("CalculatorWidget ready");
        }




        private void OnDestroy()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Remove(VesselSelected);
            GameEvents.onEditorShipModified.Remove(ShipModified);
        }



        void VesselSelected(ShipTemplate template)
        {
            Log.Debug("CalculatorWidget.VesselSelected");
            CameraListener.Instance.Enqueue(RefreshCost);
        }



        void ShipModified(ShipConstruct ship)
        {
            Log.Debug("CalculatorWidget.ShipModified");
            CameraListener.Instance.Enqueue(RefreshCost);
        }



        /// <summary>
        /// strictly speaking, this isn't necessary unless another mod is 
        /// updating funds available while in the editor. 
        /// </summary>
        /// <param name="value"></param>
        void FundsChanged(double value)
        {
            CameraListener.Instance.Enqueue(RefreshCost); // refreshes funds anyway
        }



        void RefreshCost()
        {
            float dryCost = 0f;
            float fuelCost = 0f;
            float total = EditorLogic.fetch.ship.GetShipCosts(out dryCost, out fuelCost);

            Log.Debug("RefreshCost");
            SetCost(total);
        }



        private void SetCost(double cost)
        {
            var tm = realCostWidget.GetComponentInChildren<TextMesh>();

            tm.text = cost.ToString("c", formatter);

            if (cost > Funding.Instance.Funds)
            {
                tm.color = realCostWidget.unaffordableColor;
            }
            else tm.color = realCostWidget.affordableColor;

            Log.Debug("CalculatorWidget: Set cost to {0}", cost);

            // don't forget, the widget we're using for funds is a CostWidget
            // which has automatically registered itself for cost changes 
            // (somehow it does anyway, probably with the same events we're
            // using ourselves)
            // 
            // so on any cost update, we need to overwrite the copied
            // widget's value with current funds again
            SetFunds(Funding.Instance.Funds);
        }

        public void SetFunds(double funds)
        {
            var mesh = newFundsWidget.GetComponentInChildren<TextMesh>();
                
            mesh.text = funds.ToString("c", formatter);
            mesh.gameObject.renderer.material.color = realCostWidget.affordableColor;

            Log.Debug("stock cost affordable = {0}", realCostWidget.affordableColor);


            Log.Debug("CalculatorWidget: Set funds to {0}", funds);
        }
    }
}
