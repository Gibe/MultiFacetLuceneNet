using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Util;
using MultiFacetLucene.Configuration;

namespace MultiFacetLucene
{
	public interface IFacetBitSetCalculator
	{
		IEnumerable<FacetSearcher.FacetValues.FacetValueBitSet> GetFacetValueBitSets(IndexReader indexReader, FacetFieldInfo info);
		OpenBitSetDISI GetFacetBitSet(IndexReader indexReader, FacetFieldInfo info, string value);
	}
}
