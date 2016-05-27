using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace AssetHistory
{
	public class Data : ScriptableObject
	{
		public List<string> guids = new List<string>();

		public List<AccessCount> accessCounts = new List<AccessCount>();

		public List<string> recently = new List<string>();

		public List<Category> category = new List<Category>();

        public List<Filter> filters = new List<Filter>();

		public Mode mode = Mode.History;

		public int historyCount = 100;

		public int iconSize = 16;

		public Style historyStyle = Style.Label;

		public void ChangeHistoryCount(int historyCount)
		{
			if(this.historyCount > historyCount)
			{
				this.guids = this.guids.GetRange(0, historyCount);
			}
			this.historyCount = historyCount;
		}

		public void Load()
		{
			this.category.ForEach(c => c.animBool.valueChanged.AddListener(AssetHistoryEditorWindow.RepaintCurrentWindow));
		}

		public void Reset()
		{
			this.guids = new List<string>();
			this.accessCounts = new List<AccessCount>();
			this.recently = new List<string>();
			this.category = new List<Category>();
			this.filters = new List<Filter>();
			this.mode = Mode.History;
			this.historyCount = 100;
			this.iconSize = 16;
			this.historyStyle = Style.Label;
		}

		public GUIStyle GetStyle()
		{
			switch(this.historyStyle)
			{
			case Style.Label:
				return EditorStyles.label;
			case Style.LargeLabel:
				return EditorStyles.largeLabel;
			case Style.MiniLabel:
				return EditorStyles.miniLabel;
			case Style.BoldLabel:
				return EditorStyles.boldLabel;
			case Style.CenteredGreyMiniLabel:
				return EditorStyles.centeredGreyMiniLabel;
			case Style.MiniButton:
				return EditorStyles.miniButton;
			case Style.ToolbarButton:
				return EditorStyles.toolbarButton;
			case Style.RadioButton:
				return EditorStyles.radioButton;
			case Style.Toggle:
				return EditorStyles.toggle;
			case Style.ObjectField:
				return EditorStyles.objectField;
			case Style.ObjectFieldThumb:
				return EditorStyles.objectFieldThumb;
			case Style.HelpBox:
				return EditorStyles.helpBox;
			default:
				return EditorStyles.label;
			}
		}
	}

	public enum Mode : int
	{
		History,
		Access,
		Recently,
		Category,
	}

	public enum Style : int
	{
		Label,
		LargeLabel,
		BoldLabel,
		MiniLabel,
		CenteredGreyMiniLabel,
		MiniButton,
		ToolbarButton,
		RadioButton,
		Toggle,
		ObjectField,
		ObjectFieldThumb,
		HelpBox,
	}

	[System.Serializable]
	public class AccessCount
	{
		public string guid;

		public int accessCount;

		public AccessCount(string path)
		{
			this.guid = path;
			this.accessCount = 1;
		}
	}

	[System.Serializable]
	public class Category
	{
		public string filterName;

		public List<string> guids;

		public AnimBool animBool;

		public Category(string filterName)
		{
			this.filterName = filterName;
			this.guids = new List<string>();
			this.animBool = new AnimBool();
			this.animBool.valueChanged.AddListener(AssetHistoryEditorWindow.RepaintCurrentWindow);
		}
	}

    [System.Serializable]
    public class Filter
    {
        public string name;

        public bool valid;

        public Filter(string name)
        {
            this.name = name;
            this.valid = true;
        }
    }
}
