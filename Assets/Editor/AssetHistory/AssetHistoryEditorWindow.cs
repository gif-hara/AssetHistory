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
		private static Data data;

        private static bool allFilter;

		private static Vector2 historyScrollPosition = new Vector2();

        private static AnimBool filterAnimation = new AnimBool();

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
            DrawFooter();
		}

		void OnInspectorUpdate()
		{
			this.Repaint();
		}

        private void LoadData()
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
            if( data.mode == Mode.History )
            {
                EditorGUILayout.PrefixLabel( "History" );
            }
            else if( data.mode == Mode.AccessCount )
            {
                EditorGUILayout.PrefixLabel( "AccessCount" );
            }
        }

        private void DrawHistory()
        {
            historyScrollPosition = EditorGUILayout.BeginScrollView( historyScrollPosition );
            if( data.mode == Mode.History )
			{
                data.guids.ForEach( g =>
				{
                    var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( g ), typeof( UnityEngine.Object ) );
                    if( data.filters.Find( f => f.name == obj.GetType().Name ).valid )
                    {
                        EditorGUILayout.ObjectField( obj, typeof( UnityEngine.Object ), false );
                    }
				});
            }
			else if(data.mode == Mode.AccessCount)
			{
                data.accessCounts.ForEach( a =>
				{
                    var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( a.guid ), typeof( UnityEngine.Object ) );
                    if( data.filters.Find( f => f.name == obj.GetType().Name ).valid )
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField( obj, typeof( UnityEngine.Object ), false );
                        EditorGUILayout.LabelField( a.accessCount.ToString(), GUILayout.Width( 20 ) );
                        EditorGUILayout.EndHorizontal();
                    }
				});
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawFilter()
        {
            filterAnimation.target = EditorGUILayout.Foldout( filterAnimation.target, "Filter" );
            if( EditorGUILayout.BeginFadeGroup( filterAnimation.faded ) )
            {
                EditorGUI.indentLevel++;
                var oldAllFilter = allFilter;
                allFilter = EditorGUILayout.ToggleLeft( "All", allFilter );
                if( oldAllFilter != allFilter )
                {
                    for( var i = 0; i < data.filters.Count; i++ )
                    {
                        data.filters[i].valid = allFilter;
                    }
                    Save();
                }
                for( var i = 0; i < data.filters.Count; i++ )
                {
                    var oldValid = data.filters[i].valid;
                    data.filters[i].valid = EditorGUILayout.ToggleLeft( data.filters[i].name, data.filters[i].valid );
                    if( oldValid != data.filters[i].valid )
                    {
                        Save();
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            if( GUILayout.Button( "History" ) )
            {
                data.mode = Mode.History;
                Save();
            }
            if( GUILayout.Button( "Access" ) )
            {
                data.mode = Mode.AccessCount;
                Save();
            }
            if( GUILayout.Button( "Delete" ) )
            {
                if( EditorUtility.DisplayDialog( "AssetHistory", "本当に削除しますか？", "Yes", "No" ) )
                {
                    data = null;
                    Save();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

		[InitializeOnLoadMethod()]
		private static void InitializeOnLoad()
		{
			Selection.selectionChanged -= OnChangeSelected;
			Selection.selectionChanged += OnChangeSelected;
            filterAnimation.valueChanged.RemoveListener( RepaintCurrentWindow );
            filterAnimation.valueChanged.AddListener( RepaintCurrentWindow );
		}

        private static void RepaintCurrentWindow()
        {
            EditorWindow.GetWindow<AssetHistoryEditorWindow>().Repaint();
        }

		private static void OnChangeSelected()
		{
            data.guids.RemoveAll( g => AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( g ), typeof( UnityEngine.Object ) ) == null );
            data.accessCounts.RemoveAll( a => AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( a.guid ), typeof( UnityEngine.Object ) ) == null );
            System.Array.ForEach( Selection.objects, o =>
			{
				if(CanInsert(o))
				{
					var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(o) );
					data.guids.Insert(0, guid);

					var accessCount = data.accessCounts.Find(a => a.guid == guid);
					if(accessCount != null)
					{
						accessCount.accessCount++;
					}
					else
					{
						accessCount = new AccessCount(guid);
						data.accessCounts.Add(accessCount);
					}

                    var typeName = o.GetType().Name;
                    if( data.filters.FindIndex( s => s.name == typeName ) < 0 )
                    {
                        data.filters.Add( new Filter( typeName ) );
                        data.filters.Sort( ( a, b ) => a.name.CompareTo( b.name ) );
                    }
				}
			});

			data.accessCounts.Sort((a, b) =>
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
			if(data == null)
			{
				data = ScriptableObject.CreateInstance<Data>();
			}
			Directory.CreateDirectory(FileDirectory);
			File.Delete(FileDirectory + FileName);
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{data}, FileDirectory + FileName, true);
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
	}
}
