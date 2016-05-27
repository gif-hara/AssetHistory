using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using UnityEditor.AnimatedValues;

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

		[MenuItem("Window/AssetHistory")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow<AssetHistoryEditorWindow>("AssetHistory");
		}

		void OnGUI()
		{
            LoadData();

            DrawHeader();
            DrawHistory();
            DrawFilter();
			DrawOption();
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
					Save();
				}
			}
			EditorGUILayout.EndHorizontal();
        }

        private void DrawHistory()
        {
			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(Vector2.one * CurrentData.iconSize);

			EditorGUILayout.BeginVertical("box");
            historyScrollPosition = EditorGUILayout.BeginScrollView( historyScrollPosition );
            if( CurrentData.mode == Mode.History )
			{
                CurrentData.guids.ForEach( g =>
				{
                    var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( g ), typeof( UnityEngine.Object ) );
                    if( CurrentData.filters.Find( f => f.name == obj.GetType().Name ).valid )
                    {
						DrawContent(obj);
                    }
				});
            }
			else if(CurrentData.mode == Mode.Access)
			{
                CurrentData.accessCounts.ForEach( a =>
				{
                    var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( a.guid ), typeof( UnityEngine.Object ) );
                    if( CurrentData.filters.Find( f => f.name == obj.GetType().Name ).valid )
                    {
                        EditorGUILayout.BeginHorizontal();
						GUILayout.Label( string.Format("{0}", a.accessCount), GUILayout.Width( 36 ) );
						DrawContent(obj);
                        EditorGUILayout.EndHorizontal();
                    }
				});
            }
			else if(CurrentData.mode == Mode.Recently)
			{
				CurrentData.recently.ForEach( r =>
				{
					var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( r ), typeof( UnityEngine.Object ) );
					if( CurrentData.filters.Find( f => f.name == obj.GetType().Name ).valid )
					{
						DrawContent(obj);
					}
				});
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
                    Save();
                }
                for( var i = 0; i < CurrentData.filters.Count; i++ )
                {
                    var oldValid = CurrentData.filters[i].valid;
					CurrentData.filters[i].valid = EditorGUILayout.ToggleLeft( CurrentData.filters[i].name, CurrentData.filters[i].valid );
                    if( oldValid != CurrentData.filters[i].valid )
                    {
                        Save();
                    }
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
						Save();
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Icon size", GUILayout.Width(100));
				var _iconSize = EditorGUILayout.IntSlider(CurrentData.iconSize, 0, 128);
				if(_iconSize != CurrentData.iconSize)
				{
					CurrentData.iconSize = _iconSize;
					Save();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Style", GUILayout.Width(100));
				var _style = (Style)EditorGUILayout.EnumPopup(CurrentData.historyStyle);
				if(_style != CurrentData.historyStyle)
				{
					CurrentData.historyStyle = _style;
					Save();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Delete", GUILayout.Width(50)))
				{
					var message = string.Format("データを全て削除します。{0}本当によろしいですか？", System.Environment.NewLine);
					if(EditorUtility.DisplayDialog("AssetHistory", message, "Yes", "No"))
					{
						CurrentData.Reset();
						Save();
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

        private static void RepaintCurrentWindow()
        {
            EditorWindow.GetWindow<AssetHistoryEditorWindow>().Repaint();
        }

		private static void OnChangeSelected()
		{
            CurrentData.guids.RemoveAll( g => AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( g ), typeof( UnityEngine.Object ) ) == null );
            CurrentData.accessCounts.RemoveAll( a => AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( a.guid ), typeof( UnityEngine.Object ) ) == null );
            System.Array.ForEach( Selection.objects, o =>
			{
				if(CanInsert(o))
				{
					var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(o) );
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

                    var typeName = o.GetType().Name;
                    if( CurrentData.filters.FindIndex( s => s.name == typeName ) < 0 )
                    {
                        CurrentData.filters.Add( new Filter( typeName ) );
                        CurrentData.filters.Sort( ( a, b ) => a.name.CompareTo( b.name ) );
                    }
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
	}
}
