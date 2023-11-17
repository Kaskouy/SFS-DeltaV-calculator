using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using SFS.World.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeltaV_Calculator
{
    public class DeltaV_UI: MonoBehaviour
    {
        private static TextAdapter _deltaV_textAdapter = null;

        public static void createUI()
        {
            //UnityEngine.Debug.Log("createUI called");
            new GameObject("AltairWorldObject", typeof(DeltaV_UI));
        }

        private void Awake()
        {
            AddToVanillaGUI();
        }

        private void Update()
        {
            Rocket theRocket = GetPlayerRocket();

            if ((theRocket == null) || (theRocket.hasControl == false))
            {
                // No rocket under control, or the rocket is incontrollable
                SetDeltaV_invalid();
            }
            else if(SandboxSettings.main.settings.infiniteFuel)
            {
                // Player is cheating
                SetDeltaV_infinity();
            }
            else
            {
                // Calculate ΔV
                double dv = DeltaV_Simulator.CalculateDV(theRocket);
                SetDeltaV_Value(dv);
            }
        }

        private Rocket GetPlayerRocket()
        {
            MapPlayer mapPlayer = PlayerController.main.player.Value?.mapPlayer;
            MapRocket mapRocket = null;
            Rocket theRocket = null;

            if (PlayerController.main.player.Value?.mapPlayer != null)
            {
                mapRocket = PlayerController.main.player.Value?.mapPlayer as MapRocket;

                if (mapRocket != null)
                {
                    theRocket = mapRocket.rocket;
                }
            }

            return theRocket;
        }

        private static void AddToVanillaGUI()
        {
            GameObject thrust = GameObject.Find("Thrust (1)");
            GameObject separator = GameObject.Find("Separator (1)");
            GameObject holder = thrust.transform.parent.gameObject;

            // Adding a separator and a text field to the vanilla GUI
            GameObject sep = GameObject.Instantiate(separator, holder.transform, true);
            GameObject Object = GameObject.Instantiate(thrust, holder.transform, true);

            // Resize stat zone
            var rect = Object.transform.GetChild(0).GetComponent<RectTransform>();
            Object.GetComponent<VerticalLayoutGroup>().childControlWidth = false;
            rect.sizeDelta = new Vector2(100, rect.sizeDelta.y);
            Object.transform.GetChild(1).GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;

            Object.transform.GetChild(0).gameObject.GetComponent<TextAdapter>().Text = "ΔV";

            // Keeping the reference to the text field
            _deltaV_textAdapter = Object.transform.GetChild(1).gameObject.GetComponent<TextAdapter>();

            SetDeltaV_Value(0.0);
        }

        public static void SetDeltaV_Value(double deltaV)
        {
            _deltaV_textAdapter.Text = Units.ToVelocityString(deltaV, true);
        }

        public static void SetDeltaV_invalid()
        {
            _deltaV_textAdapter.Text = "-";
        }

        public static void SetDeltaV_infinity()
        {
            _deltaV_textAdapter.Text = "∞";
        }
    }
}
