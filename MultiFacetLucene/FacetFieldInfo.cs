using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFacetLucene
{
	public class FacetFieldInfo
	{
		public FacetFieldInfo()
		{
			Selections = new List<string>();
			Ranges = new List<Range>();
			MaxToFetchExcludingSelections = 20;
		}
		public string FieldName { get; set; }
		public List<string> Selections { get; set; }
		public int MaxToFetchExcludingSelections { get; set; }
		public bool IsRange { get; set; }
		public List<Range> Ranges { get; set; }

		public virtual bool HasSelections
		{
			get
			{
				return Selections.Any();
			}
		}
	}

	public class Range
	{
		public string Id { get; set; }
		public string From { get; set; }
		public string To { get; set; }
	}


}