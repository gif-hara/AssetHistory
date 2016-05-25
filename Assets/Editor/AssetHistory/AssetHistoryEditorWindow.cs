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

		private static Vector2 scrollPosition = new Vector2();

        private static bool filterToggle = false;

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
			if(data == null)
			{
				var loadObject = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(FileDirectory + FileName);
				if(loadObject.Length > 0)
				{
					data = loadObject[0] as Data;
				}
				else
				{
					data = ScriptableObject.CreateInstance<Data>();
					Save();
				}
			}

            DrawHistory();

            filterAnimation.target = EditorGUILayout.Foldout( filterAnimation.target, "Filter" );
            if( EditorGUILayout.BeginFadeGroup( filterAnimation.faded ) )
            {
                for(var i=0; i<data.filters.Count; i++)
                {
                    var oldValid = data.filters[i].valid;
                    data.filters[i].valid = EditorGUILayout.ToggleLeft( data.filters[i].name, data.filters[i].valid );
                    if( oldValid != data.filters[i].valid )
                    {
                        Save();
                    }
                }
            }
            EditorGUILayout.EndFadeGroup();

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

		void OnInspectorUpdate()
		{
			this.Repaint();
		}

        private void DrawHistory()
        {
			if(data.mode == Mode.History)
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				data.guids.ForEach( g =>
				{
                    var obj = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( g ), typeof( UnityEngine.Object ) );
                    var filter = data.filters.Find( f => f.name == obj.GetType().Name );
                    if( filter == null || filter.valid )
                    {
                        EditorGUILayout.ObjectField( obj, typeof( UnityEngine.Object ), false );
                    }
				});
				EditorGUILayout.EndScrollView();
			}
			else if(data.mode == Mode.AccessCount)
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				data.accessCounts.ForEach( a =>
				{
                    if( data.filters.Find( f => f.name == a.GetType().Name ).valid )
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField( AssetDatabase.LoadAssetAtPath( a.path, typeof( UnityEngine.Object ) ), typeof( UnityEngine.Object ), false );
                        EditorGUILayout.LabelField( a.accessCount.ToString(), GUILayout.Width( 20 ) );
                        EditorGUILayout.EndHorizontal();
                    }
				});
				EditorGUILayout.EndScrollView();
			}
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
			System.Array.ForEach(Selection.objects, o =>
			{
				if(CanInsert(o))
				{
					var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(o) );
					data.guids.Insert(0, guid);
					var accessCount = data.accessCounts.Find(a => a.path == guid);
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

				return AssetDatabase.AssetPathToGUID(a.path).CompareTo(AssetDatabase.AssetPathToGUID(b.path));
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
