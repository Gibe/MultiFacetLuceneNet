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
			var values = new Dictionary<string, FacetSearcher.FacetValues.FacetValueBitSet>();
			foreach (var leaf in indexReader.Leaves)
			{
				var termReader = leaf.AtomicReader.GetTerms(info.FieldName).GetEnumerator();
				do
				{
					var bitset = CalculateOpenBitSetDisi(indexReader, info.FieldName, termReader.Term.Utf8ToString());
					var cnt = bitset.Cardinality;
					if (cnt >= _facetSearcherConfiguration.MinimumCountInTotalDatasetForFacet)
					{
						if (values.ContainsKey(termReader.Term.Utf8ToString()))
						{
							var existingValue = values[termReader.Term.Utf8ToString()];
							existingValue.Bitset.InPlaceOr(bitset.GetIterator());
							existingValue.Count += cnt;
						}
						else
						{
							values.Add(termReader.Term.Utf8ToString(),
								new FacetSearcher.FacetValues.FacetValueBitSet
									{Value = termReader.Term.Utf8ToString(), Bitset = bitset, Count = cnt});
						}
					}
					else
					{
						bitset = null;
					}
				} while (termReader.MoveNext());
			}
			return values.Values;
		}

		public OpenBitSetDISI GetFacetBitSet(IndexReader indexReader, FacetFieldInfo info, string value)
		{
			return CalculateOpenBitSetDisi(indexReader, info.FieldName, value);
		}


		protected OpenBitSetDISI CalculateOpenBitSetDisi(IndexReader indexReader, string facetAttributeFieldName, string value)
		{
			var facetQuery = new TermQuery(new Term(facetAttributeFieldName, value));
			var facetQueryFilter = new QueryWrapperFilter(facetQuery);
			var disi = new OpenBitSetDISI(indexReader.MaxDoc);
			foreach (var leaf in indexReader.Leaves)
			{
				var docSet = facetQueryFilter.GetDocIdSet(leaf.AtomicReader.AtomicContext, leaf.AtomicReader.LiveDocs);
				var iterator = docSet.GetIterator();
				if (iterator != null)
				{
					disi.InPlaceOr(iterator);
				}
			}
			return disi;
		}
	}
}
