using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using MultiFacetLucene.Configuration;

namespace MultiFacetLucene
{
    public class RangeFacetBitSetCalculator : IFacetBitSetCalculator
    {
        private FacetSearcherConfiguration _facetSearcherConfiguration;

        public RangeFacetBitSetCalculator(FacetSearcherConfiguration configuration)
        {
            _facetSearcherConfiguration = configuration;
        }

        public IEnumerable<FacetSearcher.FacetValues.FacetValueBitSet> GetFacetValueBitSets(IndexReader indexReader, FacetFieldInfo info)
        {
            foreach (var range in info.Ranges)
            {
                var bitset = CalculateOpenBitSetDisi(indexReader, info.FieldName, range.From, range.To);
                var cnt = bitset.Cardinality;
                if (cnt >= _facetSearcherConfiguration.MinimumCountInTotalDatasetForFacet)
                {
                    yield return
                        new FacetSearcher.FacetValues.FacetValueBitSet { Value = range.Id, Bitset = bitset, Count = cnt };
                }
                else
                {
                    bitset = null;
                }
            }
        }

        public OpenBitSetDISI GetFacetBitSet(IndexReader indexReader, FacetFieldInfo info, string value)
        {
            var range = info.Ranges.FirstOrDefault(r => r.Id == value);
            return CalculateOpenBitSetDisi(indexReader, info.FieldName, range.From, range.To);
        }

        protected OpenBitSetDISI CalculateOpenBitSetDisi(IndexReader indexReader, string facetAttributeFieldName, string from, string to)
        {
            var facetQuery = new TermRangeQuery(facetAttributeFieldName, new BytesRef(from), new BytesRef(to), true, true);
            var facetQueryFilter = new QueryWrapperFilter(facetQuery);
            var disi = new OpenBitSetDISI(indexReader.MaxDoc);
            foreach (var leaf in indexReader.Leaves)
            {
                disi.InPlaceOr(facetQueryFilter.GetDocIdSet(leaf.AtomicReader.AtomicContext, leaf.AtomicReader.LiveDocs).GetIterator());
            }
            return disi;
        }
    }
}
