using UnityEngine;
using UnityEngine.InputNew;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(ActionMap))]
public class ActionMapEditor : Editor
{
	static class Styles
	{
		public static GUIContent iconToolbarPlus =	EditorGUIUtility.IconContent("Toolbar Plus", "Add to list");
		public static GUIContent iconToolbarMinus =	EditorGUIUtility.IconContent("Toolbar Minus", "Remove from list");
		public static GUIContent iconToolbarPlusMore =	EditorGUIUtility.IconContent("Toolbar Plus More", "Choose to add to list");
	}
	
	ActionMap m_ActionMap;
	
	int m_SelectedScheme = 0;
	[System.NonSerialized]
	InputAction m_SelectedEntry = null;
	List<string> m_PropertyNames = new List<string>();
	HashSet<string> m_PropertyBlacklist  = new HashSet<string>();
	Dictionary<string, string> m_PropertyErrors = new Dictionary<string, string>();
	InputControlDescriptor m_SelectedSource = null;
	ButtonAxisSource m_SelectedButtonAxisSource = null;
	
	int selectedScheme
	{
		get { return m_SelectedScheme; }
		set
		{
			if (m_SelectedScheme == value)
				return;
			m_SelectedScheme = value;
		}
	}
	
	InputAction selectedEntry
	{
		get { return m_SelectedEntry; }
		set
		{
			if (m_SelectedEntry == value)
				return;
			m_SelectedEntry = value;
		}
	}
	
	public void OnEnable()
	{
		m_ActionMap = (ActionMap)serializedObject.targetObject;
		RefreshPropertyNames();
		CalculateBlackList();
	}
	
	void RefreshPropertyNames()
	{
		// Calculate property names.
		m_PropertyNames.Clear();
		for (int i = 0; i < m_ActionMap.entries.Count; i++)
			m_PropertyNames.Add(GetCamelCaseString(m_ActionMap.entries[i].name, false));
		
		// Calculate duplicates.
		HashSet<string> duplicates = new HashSet<string>(m_PropertyNames.GroupBy(x => x).Where(group => group.Count() > 1).Select(group => group.Key));
		
		// Calculate errors.
		m_PropertyErrors.Clear();
		for (int i = 0; i < m_PropertyNames.Count; i++)
		{
			string name = m_PropertyNames[i];
			if (m_PropertyBlacklist.Contains(name))
				m_PropertyErrors[name] = "Invalid action name: "+name+".";
			else if (duplicates.Contains(name))
				m_PropertyErrors[name] = "Duplicate action name: "+name+".";
		}
	}
	
	void CalculateBlackList()
	{
		m_PropertyBlacklist = new HashSet<string>(typeof(PlayerInput).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(e => e.Name));
	}
	
	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		
		if (selectedScheme >= m_ActionMap.schemes.Count)
			selectedScheme = m_ActionMap.schemes.Count - 1;
		
		// Show schemes
		EditorGUIUtility.GetControlID(FocusType.Passive);
		for (int i = 0; i < m_ActionMap.schemes.Count; i++)
		{
			Rect rect = EditorGUILayout.GetControlRect();
			
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
				selectedScheme = i;
			
			if (selectedScheme == i)
				GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
			
			EditorGUI.BeginChangeCheck();
			string schemeName = EditorGUI.TextField(rect, "Control Scheme " + i, m_ActionMap.schemes[i]);
			if (EditorGUI.EndChangeCheck())
				m_ActionMap.schemes[i] = schemeName;
			
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
				Event.current.Use();
		}
		
		// Remove an add buttons
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(15 * EditorGUI.indentLevel);
		if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
		{
			m_ActionMap.schemes.RemoveAt(selectedScheme);
			
			for (int i = 0; i < m_ActionMap.entries.Count; i++)
			{
				InputAction entry = m_ActionMap.entries[i];
				if (entry.bindings.Count > selectedScheme)
					entry.bindings.RemoveAt(selectedScheme);
				while (entry.bindings.Count > m_ActionMap.schemes.Count)
					entry.bindings.RemoveAt(entry.bindings.Count - 1);
			}
			if (selectedScheme >= m_ActionMap.schemes.Count)
				selectedScheme = m_ActionMap.schemes.Count - 1;
		}
		if (GUILayout.Button(Styles.iconToolbarPlus, GUIStyle.none))
		{
			m_ActionMap.schemes.Add("New Control Scheme");
			for (int i = 0; i < m_ActionMap.entries.Count; i++)
			{
				InputAction entry = m_ActionMap.entries[i];
				while (entry.bindings.Count < m_ActionMap.schemes.Count)
					entry.bindings.Add(new ControlBinding());
			}
			selectedScheme = m_ActionMap.schemes.Count - 1;
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		
		// Show high level controls
		EditorGUILayout.LabelField("Actions", m_ActionMap.schemes[selectedScheme] + " Bindings");
		EditorGUILayout.BeginVertical("Box");
		foreach (var entry in m_ActionMap.entries)
		{
			DrawEntry(entry, selectedScheme);
		}
		EditorGUILayout.EndVertical();
		
		// Remove an add buttons
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(15 * EditorGUI.indentLevel);
		if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
		{
			m_ActionMap.entries.Remove(selectedEntry);
			if (!m_ActionMap.entries.Contains(selectedEntry))
				selectedEntry = m_ActionMap.entries[m_ActionMap.entries.Count - 1];
		}
		if (GUILayout.Button(Styles.iconToolbarPlus, GUIStyle.none))
		{
			var entry = new InputAction();
			entry.controlData = new InputControlData() { name = "New Control" };
			entry.name = entry.controlData.name;
			entry.bindings = new List<ControlBinding>();
			while (entry.bindings.Count < m_ActionMap.schemes.Count)
				entry.bindings.Add(new ControlBinding());
			m_ActionMap.entries.Add(entry);
			selectedEntry = m_ActionMap.entries[m_ActionMap.entries.Count - 1];
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		
		if (selectedEntry != null)
			DrawEntryGUI();
		
		if (EditorGUI.EndChangeCheck())
			EditorUtility.SetDirty(m_ActionMap);
		
		EditorGUILayout.Space();
		
		bool valid = true;
		if (m_PropertyErrors.Count > 0)
		{
			valid = false;
			EditorGUILayout.HelpBox(string.Join("\n", m_PropertyErrors.Values.ToArray()), MessageType.Error);
		}
		EditorGUI.BeginDisabledGroup(!valid);
		if (GUILayout.Button("Update Script"))
			UpdateActionMapScript();
		EditorGUI.EndDisabledGroup();
	}
	
	void DrawEntry(InputAction entry, int selectedScheme)
	{
		ControlBinding binding = (entry.bindings.Count > selectedScheme ? entry.bindings[selectedScheme] : null);
		
		int sourceCount = 0;
		int buttonAxisSourceCount = 0;
		if (binding != null)
		{
			sourceCount += binding.sources.Count;
			buttonAxisSourceCount += binding.buttonAxisSources.Count;
		}
		int totalSourceCount = sourceCount + buttonAxisSourceCount;
		int lines = Mathf.Max(1, totalSourceCount);
		
		float height = EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1) + 8;
		Rect totalRect = GUILayoutUtility.GetRect(1, height);
		
		Rect baseRect = totalRect;
		baseRect.yMin += 4;
		baseRect.yMax -= 4;
		
		if (selectedEntry == entry)
			GUI.DrawTexture(totalRect, EditorGUIUtility.whiteTexture);
		
		// Show control fields
		
		Rect rect = baseRect;
		rect.height = EditorGUIUtility.singleLineHeight;
		rect.width = EditorGUIUtility.labelWidth - 4;
		
		EditorGUI.LabelField(rect, entry.controlData.name);
		
		// Show binding fields
		
		if (binding != null)
		{
			rect = baseRect;
			rect.height = EditorGUIUtility.singleLineHeight;
			rect.xMin += EditorGUIUtility.labelWidth;
			
			if (binding.primaryIsButtonAxis)
			{
				DrawButtonAxisSources(ref rect, binding);
				DrawSources(ref rect, binding);
			}
			else
			{
				DrawSources(ref rect, binding);
				DrawButtonAxisSources(ref rect, binding);
			}
		}
		
		if (Event.current.type == EventType.MouseDown && totalRect.Contains(Event.current.mousePosition))
		{
			selectedEntry = entry;
			Event.current.Use();
		}
	}
	
	void DrawSources(ref Rect rect, ControlBinding binding)
	{
		for (int i = 0; i < binding.sources.Count; i++)
		{
			DrawSourceSummary(rect, binding.sources[i]);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
	
	void DrawButtonAxisSources(ref Rect rect, ControlBinding binding)
	{
		for (int i = 0; i < binding.buttonAxisSources.Count; i++)
		{
			DrawButtonAxisSourceSummary(rect, binding.buttonAxisSources[i]);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
	
	void DrawSourceSummary(Rect rect, InputControlDescriptor source)
	{
		EditorGUI.LabelField(rect, GetSourceString(source));
	}
	
	void DrawButtonAxisSourceSummary(Rect rect, ButtonAxisSource source)
	{
		if (source.negative.deviceType == source.positive.deviceType)
			EditorGUI.LabelField(rect,
				string.Format("{0} {1} & {2}",
					InputDeviceGUIUtility.GetDeviceName(source.negative),
					InputDeviceGUIUtility.GetDeviceControlName(source.negative),
					InputDeviceGUIUtility.GetDeviceControlName(source.positive)
				)
			);
		else
			EditorGUI.LabelField(rect, string.Format("{0} & {1}", GetSourceString(source.negative), GetSourceString(source.positive)));
	}
	
	string GetSourceString(InputControlDescriptor source)
	{
		return string.Format("{0} {1}", InputDeviceGUIUtility.GetDeviceName(source), InputDeviceGUIUtility.GetDeviceControlName(source));
	}
	
	void UpdateActionMapScript () {
		string className = GetCamelCaseString(m_ActionMap.name, true);
		StringBuilder str = new StringBuilder();
		
		str.AppendFormat(@"using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
public class {0} : PlayerInput {{
	public {0} (ActionMap actionMap) : base (actionMap) {{ }}
	public {0} (SchemeInput schemeInput) : base (schemeInput) {{ }}
	
", className);
		
		for (int i = 0; i < m_ActionMap.entries.Count; i++)
			str.AppendFormat("	public InputControl @{0} {{ get {{ return this[{1}]; }} }}\n", GetCamelCaseString(m_ActionMap.entries[i].name, false), i);
		
		str.AppendLine(@"}");
		
		string path = AssetDatabase.GetAssetPath(m_ActionMap);
		path = path.Substring(0, path.Length - Path.GetExtension(path).Length) + ".cs";
		File.WriteAllText(path, str.ToString());
		AssetDatabase.ImportAsset(path);
	}
	
	string GetCamelCaseString(string input, bool capitalFirstLetter)
	{
		string output = string.Empty;
		bool capitalize = capitalFirstLetter;
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == ' ')
			{
				capitalize = true;
				continue;
			}
			if (char.IsLetter(c))
			{
				if (capitalize)
					output += char.ToUpper(c);
				else if (output.Length == 0)
					output += char.ToLower(c);
				else
					output += c;
				capitalize = false;
				continue;
			}
			if (char.IsDigit(c))
			{
				if (output.Length > 0)
				{
					output += c;
					capitalize = false;
				}
				continue;
			}
			if (c == '_')
			{
				output += c;
				capitalize = true;
				continue;
			}
		}
		return output;
	}
	
	void DrawEntryGUI()
	{
		EditorGUI.BeginChangeCheck();
		
		EditorGUI.BeginChangeCheck();
		string name = EditorGUILayout.TextField("Name", selectedEntry.controlData.name);
		if (EditorGUI.EndChangeCheck())
		{
			InputControlData data = selectedEntry.controlData;
			data.name = name;
			selectedEntry.controlData = data;
			selectedEntry.name = name;
		}
		
		EditorGUI.BeginChangeCheck();
		var type = (InputControlType)EditorGUILayout.EnumPopup("Type", selectedEntry.controlData.controlType);
		if (EditorGUI.EndChangeCheck())
		{
			InputControlData data = selectedEntry.controlData;
			data.controlType = type;
			selectedEntry.controlData = data;
		}
		
		EditorGUILayout.Space();
		
		if (selectedScheme >= 0 && selectedScheme < selectedEntry.bindings.Count)
			DrawBinding(selectedEntry.bindings[selectedScheme]);
	}
	
	void DrawBinding(ControlBinding binding)
	{
		if (binding.primaryIsButtonAxis)
		{
			DrawButtonAxisSources(binding);
			DrawSources(binding);
		}
		else
		{
			DrawSources(binding);
			DrawButtonAxisSources(binding);
		}
		
		// Remove and add buttons
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(15 * EditorGUI.indentLevel);
		if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
		{
			if (m_SelectedSource != null)
				binding.sources.Remove(m_SelectedSource);
			if (m_SelectedButtonAxisSource != null)
				binding.buttonAxisSources.Remove(m_SelectedButtonAxisSource);
		}
		Rect r = GUILayoutUtility.GetRect(Styles.iconToolbarPlusMore, GUIStyle.none);
		if (GUI.Button(r, Styles.iconToolbarPlusMore, GUIStyle.none))
		{
			ShowAddOptions(r, binding);
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}
	
	void ShowAddOptions(Rect rect, ControlBinding binding)
	{
		GenericMenu menu = new GenericMenu();
		menu.AddItem(new GUIContent("Regular Source"), false, AddSource, binding);
		menu.AddItem(new GUIContent("Button Axis Source"), false, AddButtonAxisSource, binding);
		menu.DropDown(rect);
	}
	
	void AddSource(object data)
	{
		ControlBinding binding = (ControlBinding)data;
		var source = new InputControlDescriptor();
		binding.sources.Add(source);
		
		m_SelectedButtonAxisSource = null;
		m_SelectedSource = source;
	}
	
	void AddButtonAxisSource(object data)
	{
		ControlBinding binding = (ControlBinding)data;
		var source = new ButtonAxisSource(new InputControlDescriptor(), new InputControlDescriptor());
		binding.buttonAxisSources.Add(source);
		
		m_SelectedSource = null;
		m_SelectedButtonAxisSource = source;
	}
	
	void DrawSources(ControlBinding binding)
	{
		for (int i = 0; i < binding.sources.Count; i++)
		{
			DrawSourceSummary(binding.sources[i]);
		}
	}
	
	void DrawButtonAxisSources(ControlBinding binding)
	{
		for (int i = 0; i < binding.buttonAxisSources.Count; i++)
		{
			DrawButtonAxisSourceSummary(binding.buttonAxisSources[i]);
		}
	}
	
	void DrawSourceSummary(InputControlDescriptor source)
	{
		Rect rect = EditorGUILayout.GetControlRect();
		
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
		{
			m_SelectedButtonAxisSource = null;
			m_SelectedSource = source;
			Repaint();
		}
		if (m_SelectedSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		DrawSourceSummary(rect, "Source", source);
		
		EditorGUILayout.Space();
	}
	
	void DrawButtonAxisSourceSummary(ButtonAxisSource source)
	{
		Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
		
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
		{
			m_SelectedSource = null;
			m_SelectedButtonAxisSource = source;
			Repaint();
		}
		if (m_SelectedButtonAxisSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		rect.height = EditorGUIUtility.singleLineHeight;
		DrawSourceSummary(rect, "Source (negative)", source.negative);
		rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		DrawSourceSummary(rect, "Source (positive)", source.positive);
		
		EditorGUILayout.Space();
	}
	
	void DrawSourceSummary(Rect rect, string label, InputControlDescriptor source)
	{
		if (m_SelectedSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
		rect.width = (rect.width - 4) * 0.5f;
		
		int indentLevel = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		
		string[] deviceNames = InputDeviceGUIUtility.GetDeviceNames();
		EditorGUI.BeginChangeCheck();
		int deviceIndex = EditorGUI.Popup(rect, InputDeviceGUIUtility.GetDeviceIndex(source.deviceType), deviceNames);
		if (EditorGUI.EndChangeCheck())
			source.deviceType = InputDeviceGUIUtility.GetDeviceType(deviceIndex);
		
		rect.x += rect.width + 4;
		
		string[] controlNames = InputDeviceGUIUtility.GetDeviceControlNames(source.deviceType);
		EditorGUI.BeginChangeCheck();
		int controlIndex = EditorGUI.Popup(rect, source.controlIndex, controlNames);
		if (EditorGUI.EndChangeCheck())
			source.controlIndex = controlIndex;
		
		EditorGUI.indentLevel = indentLevel;
	}
}