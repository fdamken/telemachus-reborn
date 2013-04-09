﻿//Author: Richard Bunt
using System;
using UnityEngine;
using KSP.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using MinimalHTTPServer;

namespace Telemachus
{
    public class TelemachusPart : Part
    {
        protected override void onFlightStart()
        {
            PluginLogger.Log("Flight start");
            base.onFlightStart();
        }
    }

    public class TelemachusDataLink : PartModule, IBasicFlightControl
    {
        #region Fields

        [KSPEvent(guiActive = true, guiName = "Display Browser")]
        public void openBrowser()
        {
            Application.OpenURL("http://" + TelemachusBehaviour.getServerPrimaryIPAddress() + ":"
                + TelemachusBehaviour.getServerPort() + "/telemachus/information");
        }

        #endregion

        #region Part Events

        public override void OnAwake()
        {
            if (TelemachusBehaviour.instance == null)
            {
                TelemachusBehaviour.instance = GameObject.Find("TelemachusBehaviour") 
                    ?? new GameObject("TelemachusBehaviour", typeof(TelemachusBehaviour));
            }

            base.OnAwake();
        }

        public override void OnLoad(ConfigNode node)
        {
            PluginLogger.Log("Loading Partmodule");
            base.OnLoad(node);
        }

        #endregion

        #region IBasicFlightControl

        public void activateNextStage()
        {
            Staging.ActivateNextStage();
        }

        public void throttleUp()
        {
            FlightInputHandler.state.mainThrottle += 0.1f;

            if (FlightInputHandler.state.mainThrottle > 1)
            {
                FlightInputHandler.state.mainThrottle = 1;
            }
        }

        public void throttleDown()
        {
            FlightInputHandler.state.mainThrottle -= 0.1f;
 
            if (FlightInputHandler.state.mainThrottle < 0)
            {
                FlightInputHandler.state.mainThrottle = 0;
            }
        }

        #endregion
    }

    public class TelemachusPowerDrain : PartModule
    {
        #region Fields

        static string[] dataUnits = new string[] { "Error", " bit/s", " kbit/s", " Mbit/s", "Gbit/s" };

        static public bool isActive = true, activeToggle = true;

        static public float powerConsumption = 0f;

        [KSPField]
        public float powerConsumptionIncrease = 0.02f;
        
        [KSPField]
        public float powerConsumptionBase = 0.02f;

        [KSPField(guiActive = true, guiName = "Power Consumption")]
        string activeReading = "";

        [KSPField(guiActive = true, guiName = "Uplink Rate")]
        string uplinkReading = "";

        [KSPField(guiActive = true, guiName = "Downlink Rate")]
        string downlinkReading = "";

        [KSPEvent(guiActive = true, guiName = "Enable/Disable Data Link")]
        public void togglePower()
        {
            if (activeToggle)
            {
                activeToggle = false;
            }
            else
            {
                activeToggle = true;
            }
        }

        #endregion

        #region Part Events

        public override void OnUpdate()
        {
            if (activeToggle)
            {
                float requiredPower = powerConsumption * TimeWarp.deltaTime;
                float availPower = part.RequestResource("ElectricCharge", requiredPower);

                if (availPower < requiredPower)
                {
                    isActive = false;
                    telemachusInactive();
                }
                else
                {
                    isActive = true;
                    telemachusActive();
                }
            }
            else
            {
                 telemachusInactive();
            }
        }

        #endregion

        #region GUI Update

        private void telemachusInactive()
        {
            activeReading = "0 units";
            uplinkReading = "0" + dataUnits[1];
            downlinkReading = "0" + dataUnits[1];
        }

        private void telemachusActive()
        {
            activeReading = powerConsumption + " units";
            uplinkReading = formatBitRate(TelemachusBehaviour.getUpLinkRate());
            downlinkReading = formatBitRate(TelemachusBehaviour.getDownLinkRate());
        }

        private string formatBitRate(double bitRate)
        {
            int index = 1;
            powerConsumption = powerConsumptionBase;

            while(bitRate > 1000)
            {
                bitRate = bitRate / 1000;
                index++;
                powerConsumption += powerConsumptionIncrease;
            }

            if(index >= dataUnits.Length)
            {
                index = 0;
            }

            return Math.Round(bitRate, 2) + dataUnits[index];
        }

        #endregion
    }
}
