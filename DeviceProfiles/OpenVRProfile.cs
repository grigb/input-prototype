﻿using UnityEngine.InputNew;
using System.Collections;
using Assets.Utilities;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif

namespace UnityEngine.InputNew
{
	public class OpenVRProfile : InputDeviceProfile
	{
		public const int kViveAxisCount = 10; // 5 axes in openVR, each with X and Y.
		public OpenVRProfile()
		{
			AddDeviceName("XRInputDevice");
			AddSupportedPlatform("OpenVR");
			AddSupportedPlatform("Vive");
		}

		public override void Remap(InputEvent inputEvent)
		{

			var controlEvent = inputEvent as GenericControlEvent;

            if (controlEvent != null)
			{
                // Debug.Log("Remapping: " + inputEvent + " Control Index: " + controlEvent.controlIndex);

                switch (controlEvent.controlIndex) {
                    // Axes
                    case 0: // Axis 0.X / trackpad X
                        controlEvent.controlIndex = (int) XRInputDevice.XRControl.LeftStickX;
                        break;
                    case 1: // Axis 0.Y / trackpad Y
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.LeftStickY;
                        break;
                    case 2: // Trigger (Axis 1.X)
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Trigger1;
                        break;
                    case 3:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog4;
                        break;
                    case 4:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog5;
                        break;
                    case 5:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog6;
                        break;
                    case 6:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog7;
                        break;
                    case 7:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog8;
                        break;
                    case 8:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog9;
                        break;
                    case 9:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Analog9;
                        break;
#if ENABLE_STEAMVR_INPUT
					// Buttons
					case kViveAxisCount + (int)EVRButtonId.k_EButton_SteamVR_Trigger:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Trigger1;
                        break;
	                case kViveAxisCount + (int)EVRButtonId.k_EButton_Axis2: // Grip on Oculus Touch
					case kViveAxisCount + (int)EVRButtonId.k_EButton_Grip:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Trigger2;
                        break;
                    case kViveAxisCount + (int)EVRButtonId.k_EButton_SteamVR_Touchpad:
                        controlEvent.controlIndex = (int) XRInputDevice.XRControl.LeftStickButton;
                        break;
                    case kViveAxisCount + (int)EVRButtonId.k_EButton_ApplicationMenu:
                        controlEvent.controlIndex = (int)XRInputDevice.XRControl.Action2;
                        break;
#endif
                }
            }
		}
	}

}