using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using UnityEditor.AnimatedValues;
using System.Collections.ObjectModel;

namespace AssetHistory
{
	/// <summary>
	/// .
	/// </summary>
	public class AssetHistoryEditorWindow : EditorWindow
	{
		public static Data CurrentData
		{
			get
			{
				if(data == null)
				{
					LoadData();
				}

				return data;
			}
		}
		private static Data data;

        private static bool allFilter = true;

		private static Vector2 historyScrollPosition = new Vector2();

		private static Vector2 filterScrollPosition = new Vector2();

        private static AnimBool filterAnimation = new AnimBool();

		private static AnimBool optionAnimation = new AnimBool();

		private const string FileDirectory = "AssetHistory";

		private const string FileName = "/data.dat";

		private readonly ReadOnlyCollection<RecommendStyle> recommendStyle = Array.AsReadOnly<RecommendStyle>( new RecommendStyle[]
		{
			new RecommendStyle("Standard", new Style(13, StyleType.ObjectField)),
			new RecommendStyle("Label", new Style(13, StyleType.Label)),
			new RecommendStyle("Flat", new Style(0, StyleType.BoldLabel)),
			new RecommendStyle("Flat Label", new Style(0, StyleType.Label)),
			new RecommendStyle("Radio", new Style(13, StyleType.RadioButton))
		});

		[MenuItem("Window/AssetHistory")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow<AssetHistoryEditorWindow>("AssetHistory");
		}

		void OnGUI()
		{
            LoadData();

			EditorGUI.BeginChangeCheck();

            DrawHeader();
            DrawHistory();
            DrawFilter();
			DrawOption();

			if(EditorGUI.EndChangeCheck())
			{
				Save();
			}
		}

		void OnInspectorUpdate()
		{
			this.Repaint();
		}

		private static void LoadData()
        {
            if( data != null )
            {
                return;
            }

            var loadObject = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget( FileDirectory + FileName );
            if( loadObject.Length > 0 )
            {
                data = loadObject[0] as Data;
            }
            else
            {
                data = ScriptableObject.CreateInstance<Data>();
                Save();
            }

			data.Load();
        }

        private void DrawHeader()
        {
			EditorGUILayout.BeginHorizontal();
			var enumNames = System.Enum.GetNames(typeof(Mode));
			for(int i=0; i<enumNames.Length; i++)
			{
				if( GUILayout.Toggle( CurrentData.mode == (Mode)i, enumNames[i], EditorStyles.toolbarButton ) )
				{
					CurrentData.mode = (Mode)i;
				}
			}
			EditorGUILayout.EndHorizontal();
        }

        private void DrawHistory()
        {
			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(Vector2.one * CurrentData.style.iconSize);

			EditorGUILayout.BeginVertical("box");
            historyScrollPosition = EditorGUILayout.BeginScrollView( historyScrollPosition );
            if( CurrentData.mode == Mode.History )
			{
				for(int i=0, imax=CurrentData.guids.Count; i<imax; i++)
				{
					var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( CurrentData.guids[i] ), typeof( UnityEngine.Object ) );
					if( CurrentData.FilterDictionary[obj.GetType().Name].valid )
					{
						DrawContent(obj);
					}
				}
            }
			else if(CurrentData.mode == Mode.Access)
			{
				var accessCounts = CurrentData.accessCounts;
				for(int i=0, imax=accessCounts.Count; i<imax; i++)
				{
					var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( accessCounts[i].guid ), typeof( UnityEngine.Object ) );
					if( CurrentData.FilterDictionary[obj.GetType().Name].valid )
					{
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label( string.Format("{0}", accessCounts[i].accessCount ), GUILayout.Width( 36 ) );
						DrawContent(obj);
						EditorGUILayout.EndHorizontal();
					}
				}
            }
			else if(CurrentData.mode == Mode.Recently)
			{
				var recently = CurrentData.recently;
				for(int i=0, imax=recently.Count; i<imax; i++)
				{
					var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( recently[i] ), typeof( UnityEngine.Object ) );
					if( CurrentData.FilterDictionary[obj.GetType().Name].valid )
					{
						DrawContent(obj);
					}
				}
			}
			else if(CurrentData.mode == Mode.Category)
			{
				var category = CurrentData.category;
				for(int i=0, imax=category.Count; i<imax; i++)
				{
					if( CurrentData.FilterDictionary[category[i].filterName].valid )
					{
						category[i].animBool.target = EditorGUILayout.Foldout( category[i].animBool.target, category[i].filterName );
						if( EditorGUILayout.BeginFadeGroup( category[i].animBool.faded ) )
						{
							EditorGUI.indentLevel++;
							var guids = category[i].guids;
							for(int j=0, jmax=guids.Count; j<jmax; j++)
							{
								var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guids[j] ), typeof( UnityEngine.Object ) );
								DrawContent(obj);
							}
							EditorGUI.indentLevel--;
						}
						EditorGUILayout.EndFadeGroup();
					}
				}
			}
            EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			EditorGUIUtility.SetIconSize(iconSize);
        }

        private void DrawFilter()
        {
			EditorGUILayout.BeginVertical("box");
            filterAnimation.target = EditorGUILayout.Foldout( filterAnimation.target, "Filter" );
            if( EditorGUILayout.BeginFadeGroup( filterAnimation.faded ) )
            {
				filterScrollPosition = EditorGUILayout.BeginScrollView(filterScrollPosition, GUILayout.Height(this.position.height / 3));
                EditorGUI.indentLevel++;
                var oldAllFilter = allFilter;
                allFilter = EditorGUILayout.ToggleLeft( "All", allFilter );
                if( oldAllFilter != allFilter )
                {
                    for( var i = 0; i < CurrentData.filters.Count; i++ )
                    {
                        CurrentData.filters[i].valid = allFilter;
                    }
                }
                for( var i = 0; i < CurrentData.filters.Count; i++ )
                {
					CurrentData.filters[i].valid = EditorGUILayout.ToggleLeft( CurrentData.filters[i].name, CurrentData.filters[i].valid );
                }
                EditorGUI.indentLevel--;
				EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFadeGroup();
			EditorGUILayout.EndVertical();
        }

		private void DrawOption()
		{
			EditorGUILayout.BeginVertical("box");
			optionAnimation.target = EditorGUILayout.Foldout( optionAnimation.target, "Option" );
			if( EditorGUILayout.BeginFadeGroup( optionAnimation.faded ) )
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Leave history", GUILayout.Width(100));
				var _historyCount = EditorGUILayout.DelayedIntField(CurrentData.historyCount);
				if(_historyCount != CurrentData.historyCount)
				{
					var message = string.Format("履歴数を{1}から{2}に変更します。{0}小さくした場合は古い履歴が削除されます。{0}本当によろしいですか？", System.Environment.NewLine, CurrentData.historyCount, _historyCount);
					if(EditorUtility.DisplayDialog("AssetHistory", message, "Yes", "No"))
					{
						CurrentData.ChangeHistoryCount(_historyCount);
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Icon size", GUILayout.Width(100));
				var _iconSize = EditorGUILayout.IntSlider(CurrentData.style.iconSize, 0, 128);
				if(_iconSize != CurrentData.style.iconSize)
				{
					CurrentData.style.iconSize = _iconSize;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Style", GUILayout.Width(100));
				var _style = (StyleType)EditorGUILayout.EnumPopup(CurrentData.style.styleType);
				if(_style != CurrentData.style.styleType)
				{
					CurrentData.style.styleType = _style;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.LabelField("Recommend style");
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(" ", GUILayout.Width(115));
				for(int i=0; i<recommendStyle.Count; i++)
				{
					if(GUILayout.Toggle(CurrentData.style.IsMatch(recommendStyle[i].style), recommendStyle[i].name, EditorStyles.toolbarButton))
					{
						CurrentData.style = new Style(recommendStyle[i].style);
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(50)))
				{
					var message = string.Format("データを全て削除します。{0}本当によろしいですか？", System.Environment.NewLine);
					if(EditorUtility.DisplayDialog("AssetHistory", message, "Yes", "No"))
					{
						CurrentData.Reset();
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.EndVertical();
		}

		private void DrawContent(UnityEngine.Object obj)
		{
			if(GUILayout.Toggle( obj == Selection.activeObject, GetGUIContent(obj), CurrentData.GetStyle() ))
			{
				Selection.activeObject = obj;
			}
		}

		[InitializeOnLoadMethod()]
		private static void InitializeOnLoad()
		{
			Selection.selectionChanged -= OnChangeSelected;
			Selection.selectionChanged += OnChangeSelected;
			filterAnimation.valueChanged.RemoveListener( RepaintCurrentWindow );
			filterAnimation.valueChanged.AddListener( RepaintCurrentWindow );
			optionAnimation.valueChanged.RemoveListener( RepaintCurrentWindow );
			optionAnimation.valueChanged.AddListener( RepaintCurrentWindow );
		}

		public static void RepaintCurrentWindow()
        {
            EditorWindow.GetWindow<AssetHistoryEditorWindow>().Repaint();
        }

		private static void OnChangeSelected()
		{
            CurrentData.guids.RemoveAll( IsRemove );
			CurrentData.accessCounts.RemoveAll( a => IsRemove(a.guid) );
			CurrentData.recently.RemoveAll( IsRemove );
			CurrentData.category.ForEach( c => c.guids.RemoveAll( IsRemove ) );
			System.Array.ForEach( Selection.objects, o =>
			{
				if(CanInsert(o))
				{
					var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(o) );
					var typeName = o.GetType().Name;
					CurrentData.guids.Insert(0, guid);

					var accessCount = CurrentData.accessCounts.Find(a => a.guid == guid);
					if(accessCount != null)
					{
						accessCount.accessCount++;
					}
					else
					{
						accessCount = new AccessCount(guid);
						CurrentData.accessCounts.Add(accessCount);
					}

					var recentlyIndex = CurrentData.recently.FindIndex(r => r == guid);
					if(recentlyIndex >= 0)
					{
						CurrentData.recently.RemoveAt(recentlyIndex);
					}
					CurrentData.recently.Insert(0, guid);

					var category = CurrentData.category.Find(c => c.filterName == typeName);
					if(category == null)
					{
						category = new Category(typeName);
						CurrentData.category.Add(category);
						CurrentData.category.Sort((a, b) =>a.filterName.CompareTo(b.filterName));
					}
					if(category.guids.FindIndex(g => g == guid) == -1)
					{
						category.guids.Add(guid);
					}
					category.guids.Sort((a, b) =>
					{
						var a_fileName = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(a), typeof(UnityEngine.Object)).name;
						var b_fileName = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(b), typeof(UnityEngine.Object)).name;
						var compare = a_fileName.CompareTo(b_fileName);
						if(compare != 0)
						{
							return compare;
						}

						return a.CompareTo(b);
					});
						
					CurrentData.AddFilter(typeName);
				}
			});

			if(CurrentData.guids.Count > CurrentData.historyCount)
			{
				CurrentData.guids = CurrentData.guids.GetRange(0, CurrentData.historyCount);
			}

			CurrentData.accessCounts.Sort((a, b) =>
			{
				var compare = b.accessCount - a.accessCount;
				if(compare != 0)
				{
					return compare;
				}

				return a.guid.CompareTo(b.guid);
			});

			Save();
		}

		private static void Save()
		{
			Directory.CreateDirectory(FileDirectory);
			File.Delete(FileDirectory + FileName);
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{CurrentData}, FileDirectory + FileName, true);
		}

		private static bool CanInsert(UnityEngine.Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if(string.IsNullOrEmpty(path))
			{
				return false;
			}
			var typeName = obj.GetType().Name;
			return typeName != "DefaultAsset";
		}

		private GUIContent GetGUIContent(UnityEngine.Object obj)
		{
			if (obj == null)
			{
				return new GUIContent();
			}

			var content = new GUIContent(EditorGUIUtility.ObjectContent(obj, obj.GetType()));
			content.tooltip = AssetDatabase.GetAssetPath(obj);

			return content;
		}

		private static bool IsRemove(string guid)
		{
			return AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), typeof( UnityEngine.Object ) ) == null;
		}
	}
}
