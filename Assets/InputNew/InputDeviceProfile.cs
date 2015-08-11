using System;
using Assets.Utilities;

namespace UnityEngine.InputNew
{
	public abstract class InputDeviceProfile
		: ScriptableObject
	{
		#region Public Methods

		public abstract void Remap( InputEvent inputEvent );

		public void AddSupportedPlatform( string platform )
		{
			ArrayHelpers.AppendUnique( ref supportedPlatforms, platform );
		}

		public void AddDeviceName( string deviceName )
		{
			ArrayHelpers.AppendUnique( ref deviceNames, deviceName );
		}

		public void AddDeviceRegex( string regex )
		{
			ArrayHelpers.AppendUnique( ref deviceRegexes, regex );
		}
		
		#endregion

		#region Public Properties

		public string[] supportedPlatforms;
		public string[] deviceNames;
		public string[] deviceRegexes;
		public string lastResortDeviceRegex;
		public Version minUnityVersion;
		public Version maxUnityVersion;

		#endregion
	}
}