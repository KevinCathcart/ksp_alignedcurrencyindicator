using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ReeperCommon;

namespace AlignedCurrencyIndicator
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class AlignedCurrencyIndicator : MonoBehaviour
    {
        enum WidgetStyle : int
        {
            Stock = 0,          // effectively disables this addon
            Tumbler,            // Cost widget is turned into an altimeter-style widget
            Calculator          // Funds widget is turned into a text-style widget
        }

        void Start()
        {
            Log.Debug("Current game mode: {0}", HighLogic.fetch.currentGame.Mode.ToString());
            Log.Debug("Listing cameras...");
            Camera.allCameras.ToList().ForEach(cam => Log.Debug("Camera: {0}", cam.name));



            StartCoroutine(DelayedStart());
        }



        T FindWidget<T>(string transformPath) where T : Component
        {
            var possible = UIManager.instance.transform.Find(transformPath);

            if (possible == null || possible.GetComponent(typeof(T)) == null)
                throw new Exception(string.Format("AlignedCurrencyIndicator: Failed to find component '{0}' at path {1}", typeof(T).Name, transformPath));

            return (T)possible.GetComponent(typeof(T));
        }


        private ConfigNode LoadSettings(bool createdDefault = false)
        {
            string path = ConfigUtil.GetDllDirectoryPath() + "/settings.cfg"; Log.Debug("dllPath = {0}", path);
            ConfigNode settings = ConfigNode.Load(path);

            if (settings == null || !settings.HasValue("Style"))
            {
                // just to prevent an infinite loop if we failed to create
                // the default settings ConfigNode due to whatever unknown 
                // reason (permissions?)
                if (createdDefault)
                    throw new Exception("LoadSettings: Failed to find settings.cfg and failed to create default.");

                
                ConfigNode defaultSettings = new ConfigNode("Settings");

                if (settings == null) Log.Warning("No config settings found. Will create default.");
                if (settings != null) Log.Warning("No value called 'Style' found in config");


                defaultSettings.AddValue("Style", WidgetStyle.Calculator);

                if (settings == null)
                {
                    defaultSettings.AddValue("// Style", WidgetStyle.Tumbler);
                    defaultSettings.AddValue("// Style", WidgetStyle.Stock);
                }

                defaultSettings.Save(path, "Aligned Currency Indicator settings");

                return LoadSettings(true);
            }
            else return settings;
        }

        System.Collections.IEnumerator DelayedStart()
        {
            var spawner = FindWidget<NestedPrefabSpawner>("PanelPartList/EditorFooter/CurrencyWidgets_Editor");

            if (spawner == null)
            {
                Log.Error("Failed to find prefab spawner!");
                yield break;
            }

            while (!spawner.Spawned)
                yield return 0;

            try
            {
                // load settings
                var settings = LoadSettings();
                WidgetStyle style = settings.ParseEnum<WidgetStyle>("Style", WidgetStyle.Calculator);


                // retrieve original widgets
                Log.Normal("Locating stock widgets...");

                // note: I avoid GameObject.FindOfType here to avoid causing a
                // mess should any mod in the future include their own FundsWidget
                // or CostWidget in the scene. I'd rather fail outright than cause
                // unintended behaviour
                FundsWidget funds = FindWidget<FundsWidget>("PanelPartList/EditorFooter/CurrencyWidgets_Editor/FundsWidget");
                CostWidget cost = FindWidget<CostWidget>("PanelPartList/EditorFooter/CurrencyWidgets_Editor/CostWidget");

                Log.Normal("Creating replacement widget...");

                
                switch (style)
                {
                    case WidgetStyle.Calculator:
                        Log.Normal("Replacement widget is calculator-style funds");
                        CalculatorWidget.Create(gameObject, cost, funds);
                        break;

                    case WidgetStyle.Tumbler:
                        Log.Normal("Replacement widget is tumbler-style cost");
                        TumblerWidget.Create(gameObject, cost, funds);
                        break;

                    default:
                        Log.Warning("AlignedCurrencyIndicator: disabled");
                        break;
                }

                //Log.Debug("Subscribing to events");
                //GameEvents.debugEvents = true;
                //GameEvents.OnFundsChanged.Add(FundsChanged);
                //GameEvents.onPartDestroyed.Add(PartDestroyed);


                //GameEvents.onGameSceneLoadRequested.Add(SceneChange);
                //GameEvents.onLevelWasLoaded.Add(SceneLoaded);
                //GameEvents.onGameStateCreated.Add(GameCreated);
                //GameEvents.onGameStateSaved.Add(GameSaved);
                //GameEvents.onGUIPrefabLauncherReady.Add(PrefabLauncherReady);
                //GameEvents.OnVesselRollout.Add(VesselRollout);
                //GameEvents.onInputLocksModified.Add(InputLocksModified);

                //GameEvents.onGUILaunchScreenVesselSelected.Add(VesselSelected);
                //GameEvents.onEditorShipModified.Add(ShipModified);

                // note to self: I include the above commented code to remind
                // all the various things I tried in order to get the widgets
                // to update as soon as possible when the editor starts. None
                // of them (include coroutines that waited on editor locks, 
                // various object states, etc) worked reliably with the exception
                // of a time delay. 
                //
                // Ultimately I went with the solution in CalculatorWidget.WaitOnStart, 
                // where I grab the funds widget value immediately and 
                // then wait for the game to change it
            }
            catch (Exception e)
            {
                Log.Error("AlignedCurrencyIndicator: Failed with exception {0}", e);
            }

        }


/******************************************************************************
 * DONT FORGET TO REMOVE FROM RELEASE VERSION
 ******************************************************************************/
#if DEBUG
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Funding.Instance.Funds += 123; // confirming that modifying funds this
                                               // way does in fact trigger 
                                               // GameEvents.onFundsChanged

                Log.Write("Adding to funds");
                Log.Write("Funds is now {0}", Funding.Instance.Funds);
            }
        }
#endif
    }
}


/* Relevant info
All signs point to us being unable to directly modif
[LOG 20:27:27.438] AlignedMoneyIndicator, CurrencyWidgets_Editor has components:
[LOG 20:27:27.439] AlignedMoneyIndicator, ...c: UnityEngine.Transform
[LOG 20:27:27.440] AlignedMoneyIndicator, ...c: NestedPrefabSpawner
[LOG 20:27:27.441] AlignedMoneyIndicator, --->FundsWidget has components:
[LOG 20:27:27.442] AlignedMoneyIndicator, ......c: UnityEngine.Transform
[LOG 20:27:27.443] AlignedMoneyIndicator, ......c: FundsWidget
[LOG 20:27:27.443] AlignedMoneyIndicator, ------>fundsGreen has components:
[LOG 20:27:27.444] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.445] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.446] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.446] AlignedMoneyIndicator, ------>fundsRef has components:
[LOG 20:27:27.447] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.448] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.448] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.449] AlignedMoneyIndicator, ------>Bg has components:
[LOG 20:27:27.450] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.450] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.451] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.452] AlignedMoneyIndicator, ------>Frame has components:
[LOG 20:27:27.452] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.453] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.453] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.454] AlignedMoneyIndicator, ------>tumblerMaskBottom has components:
[LOG 20:27:27.455] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.456] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.456] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.457] AlignedMoneyIndicator, ------>tumblerMaskTop has components:
[LOG 20:27:27.458] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.459] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.460] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.461] AlignedMoneyIndicator, ------>tumblers has components:
[LOG 20:27:27.462] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.462] AlignedMoneyIndicator, .........c: Tumblers
[LOG 20:27:27.463] AlignedMoneyIndicator, --------->tumbler0 has components:
[LOG 20:27:27.463] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.464] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.465] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.465] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.466] AlignedMoneyIndicator, --------->tumbler1 has components:
[LOG 20:27:27.466] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.467] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.468] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.468] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.469] AlignedMoneyIndicator, --------->tumbler2 has components:
[LOG 20:27:27.470] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.470] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.471] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.471] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.472] AlignedMoneyIndicator, --------->tumbler3 has components:
[LOG 20:27:27.473] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.474] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.475] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.475] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.476] AlignedMoneyIndicator, --------->tumbler4 has components:
[LOG 20:27:27.476] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.477] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.478] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.478] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.479] AlignedMoneyIndicator, --------->tumbler5 has components:
[LOG 20:27:27.480] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.481] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.481] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.482] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.483] AlignedMoneyIndicator, --------->tumbler6 has components:
[LOG 20:27:27.483] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.484] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.484] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.485] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.486] AlignedMoneyIndicator, --------->tumbler7 has components:
[LOG 20:27:27.486] AlignedMoneyIndicator, ............c: UnityEngine.Transform
[LOG 20:27:27.487] AlignedMoneyIndicator, ............c: UnityEngine.MeshFilter
[LOG 20:27:27.487] AlignedMoneyIndicator, ............c: UnityEngine.MeshRenderer
[LOG 20:27:27.488] AlignedMoneyIndicator, ............c: Tumbler
[LOG 20:27:27.489] AlignedMoneyIndicator, --->CostWidget has components:
[LOG 20:27:27.489] AlignedMoneyIndicator, ......c: UnityEngine.Transform
[LOG 20:27:27.490] AlignedMoneyIndicator, ......c: CostWidget
[LOG 20:27:27.490] AlignedMoneyIndicator, ------>Frame has components:
[LOG 20:27:27.491] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.492] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.492] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.493] AlignedMoneyIndicator, ------>valueText has components:
[LOG 20:27:27.493] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.494] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
[LOG 20:27:27.495] AlignedMoneyIndicator, .........c: UnityEngine.TextMesh
[LOG 20:27:27.495] AlignedMoneyIndicator, ------>costIcon has components:
[LOG 20:27:27.496] AlignedMoneyIndicator, .........c: UnityEngine.Transform
[LOG 20:27:27.497] AlignedMoneyIndicator, .........c: UnityEngine.MeshFilter
[LOG 20:27:27.497] AlignedMoneyIndicator, .........c: UnityEngine.MeshRenderer
*/