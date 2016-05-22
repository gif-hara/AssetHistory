﻿using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace AssetHistory
{
	/// <summary>
	/// .
	/// </summary>
	public class AssetHistoryEditorWindow : EditorWindow
	{
		private static Data data;

		private static Vector2 scrollPosition = new Vector2();

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

			if(data.mode == Mode.History)
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				data.histries.ForEach( p =>
				{
					EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath(p, typeof(UnityEngine.Object)), typeof(UnityEngine.Object), false);
				});
				EditorGUILayout.EndScrollView();
			}
			else if(data.mode == Mode.AccessCount)
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				data.accessCounts.ForEach( a =>
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath(a.path, typeof(UnityEngine.Object)), typeof(UnityEngine.Object), false);
					EditorGUILayout.LabelField(a.accessCount.ToString(), GUILayout.Width(20));
					EditorGUILayout.EndHorizontal();
				});
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("History"))
			{
				data.mode = Mode.History;
				Save();
			}
			if(GUILayout.Button("Access"))
			{
				data.mode = Mode.AccessCount;
				Save();
			}
			if(GUILayout.Button("Delete"))
			{
				if(EditorUtility.DisplayDialog("AssetHistory", "本当に削除しますか？", "Yes", "No"))
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

		[InitializeOnLoadMethod()]
		private static void InitializeOnLoad()
		{
			Selection.selectionChanged -= OnChangeSelected;
			Selection.selectionChanged += OnChangeSelected;
		}

		private static void OnChangeSelected()
		{
			System.Array.ForEach(Selection.objects, o =>
			{
				if(CanInsert(o))
				{
					var path = AssetDatabase.GetAssetPath(o);
					data.histries.Insert(0, path);
					var accessCount = data.accessCounts.Find(a => a.path == path);
					if(accessCount != null)
					{
						accessCount.accessCount++;
					}
					else
					{
						accessCount = new AccessCount(path);
						data.accessCounts.Add(accessCount);
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