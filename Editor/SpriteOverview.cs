/*	This script can help to manage your SpriteRenderer's Layer and Order.
 * 	version: 1.1
 * 
 * 	todo:
 * 	- calculate the draw call in gameobject(or scene).
 *	- show sprite texture.
 *	- use list to temp the all order data.
 * 
 * 	done:
 * 	1.1:
 * 	- get all layers name(use System.Reflection).
 * 	- modify the class name LayerValue to SRLayer.
 * 	1.0:
 * 	- show same layer, sorted order in layer.
 * 	- quick to modify layer, order.
 * 	- select, revert, save GameObject to asset.
 * 
 * 	referance:
 *	- UIDrawCallOverview: http://www.tasharen.com/forum/index.php?topic=6166.0
 * 	- Sortlayer Renderer Extension: http://answers.unity3d.com/questions/604703/sortlayer-renderer-extension.html
 * */
/*
sprite draw call算法
ex: texture a: @, texture b: #, null: ~
從左到右遞增order(same layer)
@@##~~@@ => draw call: 3
@@~@@##~ => draw call: 3
@@~@@#@~ => draw call: 4
~~@@##~~ => draw call: 2
~@~ => draw call: 1
@~@ => draw call: 2
兩個同texture上的sprite如果中間有一個空的sprite會另外算一個draw call.

bug?
step 1:
@@@ => draw call: 1
step 2:
@~@ => draw call: 1
step 3:
@#@ => draw call: 2
step 4:
@~@ => draw call: 2
*/
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEditorInternal;
using System.Reflection;

public class SpriteOverview : EditorWindow
{
	#region member
	private Vector2 _scroll = Vector2.zero;
	private GameObject goTarget = null;
	private int _instanceID;
	// save key and layerID/order
	private Dictionary<string, int> dicKeys = new Dictionary<string, int>();
	private bool _dirty = false;
	private List<SRInfo> tempOrders;
	static readonly private Color32 defaultColor = GUI.backgroundColor;
	static readonly private Color32[] allColors = new Color32[]
	{
		new Color32(124, 252, 0, 255), //lawn green
		new Color32(30, 144, 255, 255), //dodger blue
		new Color32(255, 215, 32, 255),  //gold
		new Color32(255, 0, 255, 255), //magenta
		new Color32(244, 164, 96, 255), //sandy brown
		new Color32(0, 191, 255, 255), //sky blue
		new Color32(0, 128, 0, 255), //green
		new Color32(186, 85, 211, 255), //medium orchid
	//add more colors for more differentiation
	//see http://www.rapidtables.com/web/color/RGB_Color.htm#rgb-format
	};
	#endregion
	
	[MenuItem("Tools/Sprite Overview")]
	static void SpriteOverviewWin()
	{
		EditorWindow wnd = EditorWindow.GetWindow<SpriteOverview>(false, "Sprite", true);
		wnd.autoRepaintOnSceneChange = true;
	}

	void OnFocus()
	{
		// update layer name by order
		SpriteManager.UpdateLayerOrder();
	}
	
	void OnGUI()
	{
		if(_dirty)
		{
			dicKeys.Clear();
			_dirty = false;
		}
		
		EditorGUILayout.BeginVertical();
		_scroll = GUILayout.BeginScrollView(_scroll);
		
		#region target
		// set, select gameobject
		EditorGUILayout.BeginHorizontal();
		goTarget = EditorGUILayout.ObjectField("Target", goTarget, typeof(GameObject), true) as GameObject;
		EditorGUILayout.EndHorizontal();
		
		// revert, apply prefab
		DrawGOBehavior(goTarget);
		#endregion target
		
		if(goTarget)
		{
			// delete key if not same valid GameObject
			if(_instanceID != goTarget.GetInstanceID())
			{
				_instanceID = goTarget.GetInstanceID();
				foreach(string tmpkey in dicKeys.Keys)
				{
					if(EditorPrefs.HasKey(tmpkey))
						EditorPrefs.DeleteKey(tmpkey);
				}
			}
			
			// update sprites inforamtion
			SpriteManager.Update(goTarget);

			#region draw call information
			GUILayout.BeginHorizontal();
			GUILayout.Label(string.Format("Draw calls: {0}", SpriteManager.DrawCalls), GUILayout.Width(200f));
			if(GUILayout.Button("Get Draw Call"))
			{
				SpriteManager.GetDrawCalls(true);
			}
			GUILayout.EndHorizontal();
			#endregion draw call information
			
			for(int i = 0; i < SpriteManager.Count; i++)
			{
				#region layer main
				SRLayer layer = SpriteManager.list[i];
				
				// set color
				GUI.backgroundColor = allColors[i % allColors.Length];
				GUI.color = Color.white;
				Color oldColor = defaultColor;
				
				#region layer field
				GUILayout.BeginHorizontal();
				
				// layer field
				int currentLayer = layer.Index;
				string keyStr = string.Format("L_{0}", currentLayer);
				int massLayer = currentLayer;
				if(dicKeys.ContainsKey(keyStr))
				{
					massLayer = dicKeys[keyStr];
				}
				GUILayout.Label("Layer ID", GUILayout.Width(60f));
				dicKeys[keyStr] = EditorGUILayout.IntField(massLayer, GUILayout.Width(50f));
				
				GUILayout.EndHorizontal();
				#endregion layer field
				
				#region layer apply/reset
				if(massLayer != currentLayer)
				{
					GUILayout.BeginHorizontal();
					oldColor = GUI.backgroundColor;
					
					// apply
					GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
					if(GUILayout.Button("Apply", GUILayout.Width(100f)))
					{
						// set new layer
						for(int o = 0; o < layer.Orders.Count; o++)
						{
							tempOrders = layer.Orders[o];
							for(int iO = 0; iO < tempOrders.Count; iO++)
							{
								tempOrders[iO].SetLayerID(massLayer);
							}
						}
						
						// delete key
						EditorPrefs.DeleteKey(keyStr);
						for(int o = 0; o < layer.Orders.Count; o++)
						{
							tempOrders = layer.Orders[o];
							if(tempOrders.Count == 0)
								continue;
							
							string okey = string.Format("L_{0}O_{1}", currentLayer, tempOrders[0].Index);
							EditorPrefs.DeleteKey(okey);
						}
						
						_dirty = true;
						GUI.FocusControl(null);
						EditorUtility.SetDirty(goTarget);
					}
					
					// reset
					GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
					if(GUILayout.Button("Reset", GUILayout.Width(100f)))
					{
						dicKeys[keyStr] = currentLayer;
						GUI.FocusControl(null);
						EditorUtility.SetDirty(goTarget);
					}
					
					GUI.backgroundColor = oldColor;
					GUILayout.EndHorizontal();
				}
				#endregion layer apply/reset
				
				// layer fold
				string headerStr = string.Format("<b>Sorting Layer {0} ({1}) - Count: {2}</b>", layer.Index, layer.LayerName, layer.Count);
				bool layerFoldedOut = DrawHeader(headerStr, keyStr);
				if(!layerFoldedOut)
					continue;
				#endregion layer main
				
				StyleEx.BeginContent();
				
				#region orders main
				for(int o = 0; o < layer.Orders.Count; o++)
				{
					tempOrders = layer.Orders[o];
					if(tempOrders.Count == 0)
						continue;
					
					#region order field, fold
					GUILayout.BeginHorizontal();
					
					// order field
					int currentOrder = tempOrders[0].Index;
					keyStr = string.Format("L_{0}O_{1}", currentLayer, currentOrder);
					bool orderFoldedOut = EditorPrefs.GetBool(keyStr, false);
					int massOrder = currentOrder;
					if(dicKeys.ContainsKey(keyStr))
					{
						massOrder = dicKeys[keyStr];
					}
					GUILayout.Label("Order", GUILayout.Width(40f));
					dicKeys[keyStr] = EditorGUILayout.IntField(massOrder, GUILayout.Width(50f));
					
					// order fold
					string collapserName = string.Format("<b>Cont: {0}</b> - Click to {1}", tempOrders.Count, (orderFoldedOut ? "collapse" : "expand"));
					bool foldedOut = DrawOrderCollapser(collapserName, keyStr, orderFoldedOut);
					
					GUILayout.EndHorizontal();
					#endregion order field, fold
					
					#region order apply/reset
					if(massOrder != currentOrder)
					{
						GUILayout.BeginHorizontal();
						oldColor = GUI.backgroundColor;
						
						// apply
						GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
						if(GUILayout.Button("Apply", GUILayout.Width(100f)))
						{
							// set new order
							for(int iO = 0; iO < tempOrders.Count; iO++)
							{
								tempOrders[iO].SetOrder(massOrder);
							}
							
							// delete key
							EditorPrefs.DeleteKey(keyStr);
							
							_dirty = true;
							GUI.FocusControl(null);
							EditorUtility.SetDirty(goTarget);
						}
						
						// reset
						GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
						if(GUILayout.Button("Reset", GUILayout.Width(100f)))
						{
							dicKeys[keyStr] = currentOrder;
							GUI.FocusControl(null);
							EditorUtility.SetDirty(goTarget);
						}
						
						GUI.backgroundColor = oldColor;
						GUILayout.EndHorizontal();
					}
					#endregion order apply/reset
					
					#region same set of order
					if(foldedOut)
					{
						var alignBefore = GUI.skin.button.alignment;
						oldColor = GUI.backgroundColor;
						GUI.skin.button.alignment = TextAnchor.MiddleLeft;
						// draw GameObject button
						for(int iW = 0; iW < tempOrders.Count; iW++)
						{
							GUILayout.BeginHorizontal();
							GUILayout.Space(10f);
							
							// button to select gameobject
							GUI.backgroundColor = defaultColor;
							if(GUILayout.Button(tempOrders[iW].Name, GUILayout.ExpandWidth(false)))
							{
								Selection.activeGameObject = tempOrders[iW].gameObject;
							}
							
							// order field
							GUI.backgroundColor = oldColor;
							GUILayout.Label("Order", GUILayout.Width(40f));
							keyStr = tempOrders[iW].SpriteRender.GetInstanceID().ToString();
							currentOrder = tempOrders[iW].Index;
							int singleOrder = currentOrder;
							if(dicKeys.ContainsKey(keyStr))
							{
								singleOrder = dicKeys[keyStr];
							}
							dicKeys[keyStr] = EditorGUILayout.IntField(singleOrder, GUILayout.Width(50f));
							
							#region single order apply/reset, show texture
							GUI.skin.button.alignment = alignBefore;
							if(singleOrder != currentOrder)
							{
								oldColor = GUI.backgroundColor;
								
								// apply
								GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
								if(GUILayout.Button("Apply", GUILayout.Width(100f)))
								{
									// set new order
									tempOrders[iW].SetOrder(singleOrder);									
									// delete key
									EditorPrefs.DeleteKey(keyStr);
									
									_dirty = true;
									GUI.FocusControl(null);
									EditorUtility.SetDirty(goTarget);
								}
								
								// reset
								GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
								if(GUILayout.Button("Reset", GUILayout.Width(100f)))
								{
									dicKeys[keyStr] = currentOrder;
									GUI.FocusControl(null);
									EditorUtility.SetDirty(goTarget);
								}
								
								GUI.backgroundColor = oldColor;
							} else
							{
								// todo show texture
							}
							#endregion single order apply/reset, show texture
							
							GUILayout.EndHorizontal();
						}
						GUI.backgroundColor = oldColor;
						GUI.skin.button.alignment = alignBefore;
					}
					#endregion same set of order
					
					GUILayout.Space(3f);
				}
				#endregion orders main
				
				StyleEx.EndContent();
			}
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}
		
		GUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	#region function
	static private void ApplyToTarget(GameObject go)
	{
		UnityEngine.Object obj = PrefabUtility.GetPrefabParent(go);
		if(obj)
		{
			if(go.transform.root)
			{
				PrefabUtility.ReplacePrefab(go.transform.root.gameObject, obj, ReplacePrefabOptions.ConnectToPrefab);
			} else
				PrefabUtility.ReplacePrefab(go, obj, ReplacePrefabOptions.ConnectToPrefab);
		} else
			Debug.LogWarning("The prefab has no parent!");
	}
	#endregion function
	
	#region draw function
	
	static private bool DrawOrderCollapser(string text, string key, bool forceOn)
	{
		bool state = EditorPrefs.GetBool(key, forceOn);
		
		GUILayout.Space(3f);
		Color oldColor = GUI.backgroundColor;
		if(!forceOn && !state)
			GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
		else
			GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
		GUILayout.Space(3f);
		
		GUI.changed = false;
		if(!GUILayout.Toggle(true, text, "dragtab", GUILayout.ExpandWidth(false)))
			state = !state;
		if(GUI.changed)
			EditorPrefs.SetBool(key, state);
		
		GUILayout.Space(2f);
		GUI.backgroundColor = oldColor;
		if(!forceOn && !state)
			GUILayout.Space(3f);
		
		return state;
	}
	
	static private bool DrawHeader(string text, string key)
	{
		bool state = EditorPrefs.GetBool(key, true);
		
		GUILayout.Space(3f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(3f);
		
		GUI.changed = false;
		if(!GUILayout.Toggle(true, text, "dragtab"))
			state = !state;
		if(GUI.changed)
			EditorPrefs.SetBool(key, state);
		
		GUILayout.Space(2f);
		GUILayout.EndHorizontal();
		
		if(!state)
			GUILayout.Space(3f);
		
		return state;
	}
	
	static private void DrawGOBehavior(GameObject go)
	{
		if(!go)
			return;
		
		EditorGUILayout.BeginHorizontal();
		
		if(GUILayout.Button("Select Target", GUILayout.Width(150f)))
		{
			Selection.activeGameObject = go;
		}
		if(GUILayout.Button("Revert Target", GUILayout.Width(150f)))
		{
			PrefabUtility.RevertPrefabInstance(go);
		}
		if(GUILayout.Button("Apply Target", GUILayout.Width(150f)))
		{
			ApplyToTarget(go);
		}
		
		EditorGUILayout.EndHorizontal();
	}
	#endregion draw function
}

#region sub area

static public class StyleEx
{
	static public void BeginContent()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(4f);
		EditorGUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(10f));
		GUILayout.BeginVertical();
		GUILayout.Space(2f);
	}
	
	static public void EndContent()
	{
		GUILayout.Space(3f);
		GUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(3f);
		GUILayout.EndHorizontal();
		GUILayout.Space(3f);
	}
}

public class SRInfo : IEquatable<SRInfo>, IComparable<SRInfo>
{
	private int mIndex;
	public SpriteRenderer SpriteRender;
	public string Name;
	
	public int Index
	{
		get
		{
			return mIndex;
		}
	}
	
	public string LayerName
	{
		get
		{
			if(SpriteRender)
				return (string.IsNullOrEmpty(SpriteRender.sortingLayerName) ? SpriteManager.DEFAULTLAYERNAME : SpriteRender.sortingLayerName);
			else
				return string.Empty;
		}
	}
	
	public GameObject gameObject
	{
		get
		{
			if(SpriteRender)
				return SpriteRender.gameObject;
			else
				return null;
		}
	}

	public string TextureName
	{
		get
		{
			if(SpriteRender)
			{
				if(SpriteRender.sprite)
					return SpriteRender.sprite.texture.name;
				else
					return string.Empty;
			} else
				return string.Empty;
		}
	}
	
	public SRInfo(int idx, string name, SpriteRenderer sr)
	{
		SpriteRender = sr;
		Name = name;
		mIndex = idx;
	}
	
	public void SetOrder(int order)
	{
		if(SpriteRender)
			SpriteRender.sortingOrder = order;
	}
	
	public void SetLayerID(int layerID)
	{
		if(SpriteRender)
			SpriteRender.sortingLayerID = layerID;
	}
	
	#region interface
	
	public override bool Equals(object obj)
	{
		if(obj == null || !(obj is SRInfo))
			return false;
		
		SRInfo other = (SRInfo)obj;
		return (this.Index == other.Index) && (this.Name == other.Name);
	}
	
	public override int GetHashCode()
	{
		return Index.GetHashCode();
	}
	
	// Default comparer for Part type. 
	public int CompareTo(SRInfo comparePart)
	{
		// A null value means that this object is greater. 
		if(comparePart == null)
			return 1;
		else 
			return this.Name.CompareTo(comparePart.Name);
	}
	
	public bool Equals(SRInfo other)
	{
		if(other == null)
			return false;
		return (this.Index == other.Index) && (this.Name == other.Name);
	}
	
	#endregion
}

public class SRLayer : SRInfo, IEquatable<SRLayer>, IComparable<SRLayer>
{
	// <object name, order>
	public List<List<SRInfo>> Orders;
	
	public SRLayer(int idx, string name, SpriteRenderer sr) : base(idx, name, sr)
	{
		Orders = new List<List<SRInfo>>();
	}
	
	public int Count
	{
		get { return Orders.Count; }
	}
	
	public void Add(int value, string name, SpriteRenderer sr)
	{
		SRInfo tmp = new SRInfo(value, name, sr);
		int idx = -1;
		for(int i = 0; i < Orders.Count; i++)
		{
			List<SRInfo> ovs = Orders[i];
			if(ovs.Count == 0)
				continue;
			if(ovs[0].Index != tmp.Index)
				continue;
			
			idx = i;
			break;
		}
		
		if(idx == -1)
		{
			List<SRInfo> tmpOVs = new List<SRInfo>();
			tmpOVs.Add(tmp);
			Orders.Add(tmpOVs);
		} else
		{
			Orders[idx].Add(tmp);
		}
	}
	
	public void Clear()
	{
		Orders.Clear();
	}
	
	#region interface
	
	public override bool Equals(object obj)
	{
		if(obj == null || !(obj is SRInfo))
			return false;
		
		SRInfo other = (SRInfo)obj;
		return (this.Index == other.Index) && (this.Name == other.Name);
	}
	
	public override int GetHashCode()
	{
		return Index.GetHashCode();
	}
	
	// Default comparer for Part type. 
	public int CompareTo(SRLayer comparePart)
	{
		// A null value means that this object is greater. 
		if(comparePart == null)
			return 1;
		else 
			return this.Index.CompareTo(comparePart.Index);
	}
	
	public bool Equals(SRLayer other)
	{
		if(other == null)
			return false;
		return (this.Index == other.Index) && (this.Name == other.Name);
	}
	
	#endregion
}

static public class SpriteManager
{
	public const  string DEFAULTLAYERNAME = "Default";
	// layer name by draw order
	static private List<string> orderLayerNames = new List<string>();
//	static private Dictionary<int, LayerValue> _Layers = new Dictionary<int, LayerValue>();
	static private Dictionary<string, SRLayer> _Layers = new Dictionary<string, SRLayer>();
	static private List<SRLayer> listLayers = new List<SRLayer>();

	// Get the sorting layer names
	static private string[] GetSortingLayerNames()
	{
		Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
		return (string[])sortingLayersProperty.GetValue(null, new object[0]);
	}

	/*
	// Get the unique sorting layer IDs -- tossed this in for good measure
	static private int[] GetSortingLayerUniqueIDs()
	{
		Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
		return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
	}
	*/

	static public int GetDrawCalls(bool log)
	{
		if(orderLayerNames.Count == 0)
			return 0;

		List<SRInfo> allsr = new List<SRInfo>();
		foreach(string l in orderLayerNames)
		{
			if(!_Layers.ContainsKey(l))
				continue;

			SRLayer srl = _Layers[l];
			foreach(List<SRInfo> srlist in srl.Orders.AsReadOnly())
			{
				srlist.Sort();
				allsr.AddRange(srlist);
			}
		}

		if(allsr.Count == 0)
			return 0;

		int dc = 0;
		string curTxtNM = string.Empty;
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		foreach(SRInfo sr in allsr)
		{
			sb.AppendLine(string.Format("{0} - {1} - {2}", sr.LayerName, sr.Index, sr.TextureName));
			if((dc > 0) && (string.IsNullOrEmpty(sr.TextureName)))
				continue;

			if(string.Compare(curTxtNM, sr.TextureName,
			                  StringComparison.InvariantCultureIgnoreCase) == 0)
				continue;

			curTxtNM = sr.TextureName;
			dc++;
		}
		if(log)
			Debug.Log(sb.ToString());
		return dc;
	}

	static public void UpdateLayerOrder()
	{
		orderLayerNames.Clear();
		string[] layers = GetSortingLayerNames();
		foreach(string l in layers)
			orderLayerNames.Add(l);
	}

	static public void Update(SpriteRenderer[] sr)
	{
		_Layers.Clear();
		// set all SpriteRenderer information
		for(int i = 0; i < sr.Length; i++)
		{
			int layerID = sr[i].sortingLayerID;
			string layerName = sr[i].sortingLayerName.ToLower();
			if((layerID == 0) && (string.IsNullOrEmpty(layerName)))
				layerName = DEFAULTLAYERNAME;

			SRLayer tmp = null;
//			if(!_layers.ContainsKey(layerID))
			if(!_Layers.ContainsKey(layerName))
			{
				tmp = new SRLayer(layerID, sr[i].gameObject.name, sr[i]);
//				_layers.Add(layerID, tmp);
				_Layers.Add(layerName, tmp);
			}
			
//			tmp = _layers[layerID];
			tmp = _Layers[layerName];
			string name = string.Format("{0}/{1}", sr[i].transform.parent.name, sr[i].gameObject.name);
			tmp.Add(sr[i].sortingOrder, name, sr[i]);
		}
		
		// sorting Orders data
		foreach(SRLayer data in _Layers.Values)
		{
			data.Orders.Sort(delegate(List<SRInfo> x, List<SRInfo> y)
			{
				if(x == null && y == null)
					return 0;
				else if(x.Count == 0 && y.Count == 0)
					return 0;
				else if(x.Count == 0)
					return -1;
				else if(y.Count == 0)
					return 1;
				else if(x[0].Index == y[0].Index)
					return 0;
				else if(x[0].Index > y[0].Index)
					return 1;
				else if(x[0].Index < y[0].Index)
					return -1;
				else
					return 0;
			});
			
			foreach(List<SRInfo> o in data.Orders)
				o.Sort();
		}
	}
	
	static public void Update(GameObject go)
	{
		SpriteRenderer[] sr = go.GetComponentsInChildren<SpriteRenderer>();
		Update(sr);
	}
	
	static public List<SRLayer> list
	{
		get
		{
			// to list and sotring Layers data
			listLayers.Clear();
			foreach(SRLayer data in _Layers.Values)
				listLayers.Add(data);
			
			listLayers.Sort();
			return listLayers;
		}
	}
	
	static private SRLayer Layer(string layername)
	{
		if(_Layers.ContainsKey(layername))
			return _Layers[layername];
		else
			return null;
	}
	
	static public int Count
	{
		get
		{
			if(listLayers == null)
				return 0;
			else
				return _Layers.Count;
		}
	}

	static public int DrawCalls
	{
		get
		{
			return GetDrawCalls(false);
		}
	}
}

#endregion
