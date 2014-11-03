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
            MaxToFetchExcludingSelections = 20;
        }
        public string FieldName { get; set; }
        public List<string> Selections { get; set; }
        public int MaxToFetchExcludingSelections { get; set; }

	    public virtual bool HasSelections
	    {
		    get { return Selections.Any(); }
	    }
    }

	  public class RangeFacetFieldInfo : FacetFieldInfo
	  {
			public string From { get; set; }
			public string To { get; set; }

			public override bool HasSelections
			{
				get { return !String.IsNullOrEmpty(From) && !String.IsNullOrEmpty(To); }
			}
	  }
}