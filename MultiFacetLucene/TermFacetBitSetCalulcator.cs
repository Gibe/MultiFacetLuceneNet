using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using MultiFacetLucene.Configuration;

namespace MultiFacetLucene
{
	public class TermFacetBitSetCalulcator : IFacetBitSetCalculator
	{
		private readonly FacetSearcherConfiguration _facetSearcherConfiguration;

		public TermFacetBitSetCalulcator(FacetSearcherConfiguration configuration)
		{
			_facetSearcherConfiguration = configuration;
		} 
		
		public IEnumerable<FacetSearcher.FacetValues.FacetValueBitSet> GetFacetValueBitSets(IndexReader indexReader, FacetFieldInfo info)
		{
			var termReader = indexReader.Terms(new Term(info.FieldName, String.Empty));
			do
			{
				if (termReader.Term.Field != info.FieldName)
					yield break;

				var bitset = CalculateOpenBitSetDisi(indexReader, info.FieldName, termReader.Term.Text);
				var cnt = bitset.Cardinality();
				if (cnt >= _facetSearcherConfiguration.MinimumCountInTotalDatasetForFacet)
					yield return new FacetSearcher.FacetValues.FacetValueBitSet { Value = termReader.Term.Text, Bitset = bitset, Count = cnt };
				else
				{
					bitset = null;
				}
			} while (termReader.Next());
			termReader.Close();
		}

		public OpenBitSetDISI GetFacetBitSet(IndexReader indexReader, FacetFieldInfo info, string value)
		{
			return CalculateOpenBitSetDisi(indexReader, info.FieldName, value);
		}


		protected OpenBitSetDISI CalculateOpenBitSetDisi(IndexReader indexReader, string facetAttributeFieldName, string value)
		{
			var facetQuery = new TermQuery(new Term(facetAttributeFieldName, value));
			var facetQueryFilter = new QueryWrapperFilter(facetQuery);
			return new OpenBitSetDISI(facetQueryFilter.GetDocIdSet(indexReader).Iterator(), indexReader.MaxDoc);
		}
	}
}
