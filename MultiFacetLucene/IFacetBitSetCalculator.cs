using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace MultiFacetLucene
{
	public interface IFacetBitSetCalculator
	{
		IEnumerable<FacetSearcher.FacetValues.FacetValueBitSet> GetFacetValueBitSets(AtomicReader indexReader, FacetFieldInfo info);
		OpenBitSetDISI GetFacetBitSet(IndexReader indexReader, FacetFieldInfo info, string value);
	}
}
