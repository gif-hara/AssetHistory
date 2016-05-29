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

		public Dictionary<string, Filter> FilterDictionary
		{
			get
			{
				if(this.filterDictionary == null)
				{
					this.filterDictionary = new Dictionary<string, Filter>();
					for(int i=0, imax=this.filters.Count; i<imax; i++)
					{
						this.filterDictionary.Add(this.filters[i].name, this.filters[i]);
					}
				}

				return this.filterDictionary;
			}
		}
		private Dictionary<string, Filter> filterDictionary = null;

		public Mode mode = Mode.History;

		public int historyCount = 100;

		public Style style;

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
			this.filterDictionary = null;
			this.mode = Mode.History;
			this.historyCount = 100;
			this.style.iconSize = 13;
			this.style.styleType = StyleType.ObjectField;
		}

		public void AddFilter(string name)
		{
			if( this.FilterDictionary.ContainsKey( name ))
			{
				return;
			}
			var filter = new Filter( name );
			this.filters.Add( filter );
			this.filters.Sort( ( a, b ) => a.name.CompareTo( b.name ) );
			this.filterDictionary.Add(name, filter);
		}

		public GUIStyle GetStyle()
		{
			switch(this.style.styleType)
			{
			case StyleType.Label:
				return EditorStyles.label;
			case StyleType.LargeLabel:
				return EditorStyles.largeLabel;
			case StyleType.MiniLabel:
				return EditorStyles.miniLabel;
			case StyleType.BoldLabel:
				return EditorStyles.boldLabel;
			case StyleType.CenteredGreyMiniLabel:
				return EditorStyles.centeredGreyMiniLabel;
			case StyleType.MiniButton:
				return EditorStyles.miniButton;
			case StyleType.ToolbarButton:
				return EditorStyles.toolbarButton;
			case StyleType.RadioButton:
				return EditorStyles.radioButton;
			case StyleType.Toggle:
				return EditorStyles.toggle;
			case StyleType.ObjectField:
				return EditorStyles.objectField;
			case StyleType.ObjectFieldThumb:
				return EditorStyles.objectFieldThumb;
			case StyleType.HelpBox:
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

	public enum StyleType : int
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

	[System.Serializable]
	public class Style
	{
		public int iconSize = 13;

		public StyleType styleType = StyleType.ObjectField;

		public Style(int iconSize, StyleType styleType)
		{
			this.iconSize = iconSize;
			this.styleType = styleType;
		}

		public Style(Style other)
		{
			this.iconSize = other.iconSize;
			this.styleType = other.styleType;
		}

		public bool IsMatch(Style other)
		{
			return (this.iconSize == other.iconSize) && (this.styleType == other.styleType);
		}
	}

	[System.Serializable]
	public class RecommendStyle
	{
		public string name;

		public Style style;

		public RecommendStyle(string name, Style style)
		{
			this.name = name;
			this.style = style;
		}
	}
}
