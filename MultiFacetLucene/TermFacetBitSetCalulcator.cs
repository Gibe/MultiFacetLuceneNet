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

            foreach (var leaf in indexReader.Leaves)
            {

                var termReader = leaf.AtomicReader.GetTerms(info.FieldName).GetEnumerator();
                do
                {
                    var bitset = CalculateOpenBitSetDisi(indexReader, info.FieldName, termReader.Term.Utf8ToString());
                    var cnt = bitset.Cardinality;
                    if (cnt >= _facetSearcherConfiguration.MinimumCountInTotalDatasetForFacet)
                        yield return new FacetSearcher.FacetValues.FacetValueBitSet
                        { Value = termReader.Term.Utf8ToString(), Bitset = bitset, Count = cnt };
                    else
                    {
                        bitset = null;
                    }
                } while (termReader.MoveNext());

            }
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
