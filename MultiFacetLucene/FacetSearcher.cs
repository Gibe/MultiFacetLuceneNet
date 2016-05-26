using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using MultiFacetLucene.Configuration;

namespace MultiFacetLucene
{
	public class FacetSearcher : IndexSearcher
	{
		private readonly ConcurrentDictionary<string, FacetValues> _facetBitSetDictionary = new ConcurrentDictionary<string, FacetValues>();

		public FacetSearcher(Directory path, FacetSearcherConfiguration facetSearcherConfiguration = null)
			: base(path)
		{
			Initialize(facetSearcherConfiguration);
		}

		public FacetSearcher(Directory path, bool readOnly, FacetSearcherConfiguration facetSearcherConfiguration = null)
			: base(path, readOnly)
		{
			Initialize(facetSearcherConfiguration);
		}

		public FacetSearcher(IndexReader r, FacetSearcherConfiguration facetSearcherConfiguration = null)
			: base(r)
		{
			Initialize(facetSearcherConfiguration);
		}
		
		public FacetSearcherConfiguration FacetSearcherConfiguration { get; protected set; }

		private void Initialize(FacetSearcherConfiguration facetSearcherConfiguration)
		{
			FacetSearcherConfiguration = facetSearcherConfiguration ?? FacetSearcherConfiguration.Default();
		}

		public virtual FacetSearchResult SearchWithFacets(Query baseQueryWithoutFacetDrilldown, int topResults, IList<FacetFieldInfo> facetFieldInfos, bool includeEmptyFacets = false, Filter filter = null, Dictionary<int, int> docIdMappingTable = null)
		{
			var hits = Search(CreateFacetedQuery(baseQueryWithoutFacetDrilldown, facetFieldInfos, null), topResults);
			
			var facets = GetAllFacetsValues(baseQueryWithoutFacetDrilldown, facetFieldInfos, filter, docIdMappingTable);
			if (!includeEmptyFacets)
			{
				facets = facets.Where(x => x.Count > 0);
			}

			return new FacetSearchResult
			{
				Facets = facets.ToList(),
				Hits = hits
			};
		}
		
		private FacetValues GetOrCreateFacetBitSet(FacetFieldInfo fieldInfo)
		{
			return _facetBitSetDictionary.GetOrAdd(fieldInfo.FieldName, ReadBitSetsForValues(fieldInfo));
		}

		private IFacetBitSetCalculator GetFacetBitSetCalculator(FacetFieldInfo fieldInfo)
		{
			return new FacetBitSetCalculatorProvider().GetFacetBitSetCalculator(fieldInfo);
		}
		
		private FacetValues ReadBitSetsForValues(FacetFieldInfo fieldInfo)
		{
			var facetValues = new FacetValues { Term = fieldInfo.FieldName };

			facetValues.FacetValueBitSetList.AddRange(GetFacetBitSetCalculator(fieldInfo).GetFacetValueBitSets(GetIndexReader(), fieldInfo).OrderByDescending(x => x.Count));

			if (FacetSearcherConfiguration.MemoryOptimizer == null) return facetValues;
			foreach (var facetValue in FacetSearcherConfiguration.MemoryOptimizer.SetAsLazyLoad(_facetBitSetDictionary.Values.ToList()))
				facetValue.Bitset = null;

			return facetValues;
		}
		
		private IEnumerable<FacetMatch> GetAllFacetsValues(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> facetFieldInfos, Filter filter, Dictionary<int, int> docIdMappingTable)
		{
			return
					facetFieldInfos.SelectMany(
							facetFieldInfo =>
									FindMatchesInQuery(baseQueryWithoutFacetDrilldown, facetFieldInfos, facetFieldInfo, filter, docIdMappingTable));
		}

		private IEnumerable<FacetMatch> FindMatchesInQuery(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> allFacetFieldInfos, FacetFieldInfo facetFieldInfoToCalculateFor, Filter filter, Dictionary<int, int> docIdMappingTable)
		{
			var calculations = 0;
			var queryFilter = new CachingWrapperFilter(CombineQueryWithFilter(CreateFacetedQuery(baseQueryWithoutFacetDrilldown, allFacetFieldInfos, facetFieldInfoToCalculateFor.FieldName), filter));
			var bitsQueryWithoutFacetDrilldown = new OpenBitSetDISI(queryFilter.GetDocIdSet(GetIndexReader()).Iterator(), GetIndexReader().MaxDoc());
			var baseQueryWithoutFacetDrilldownCopy = new OpenBitSetDISI(bitsQueryWithoutFacetDrilldown.GetBits().Length);
			baseQueryWithoutFacetDrilldownCopy.SetBits(new long[bitsQueryWithoutFacetDrilldown.GetBits().Length]);

			var docIdMappingArray = GetDocIdMappingArray(docIdMappingTable);

			var calculatedFacetCounts = new ResultCollection(facetFieldInfoToCalculateFor);
			foreach (var facetValueBitSet in GetOrCreateFacetBitSet(facetFieldInfoToCalculateFor).FacetValueBitSetList)
			{
				var isSelected = calculatedFacetCounts.IsSelected(facetValueBitSet.Value);

				if (!isSelected && facetValueBitSet.Count < calculatedFacetCounts.MinCountForNonSelected) //Impossible to get a better result
				{
					if (calculatedFacetCounts.HaveEnoughResults)
						break;
				}

				bitsQueryWithoutFacetDrilldown.GetBits().CopyTo(baseQueryWithoutFacetDrilldownCopy.GetBits(), 0);
				baseQueryWithoutFacetDrilldownCopy.SetNumWords(bitsQueryWithoutFacetDrilldown.GetNumWords());

				var bitset = facetValueBitSet.Bitset ?? GetFacetBitSetCalculator(facetFieldInfoToCalculateFor).GetFacetBitSet(GetIndexReader(), facetFieldInfoToCalculateFor, facetValueBitSet.Value);
				baseQueryWithoutFacetDrilldownCopy.And(bitset);

				if (docIdMappingArray != null)
				{
					CascadeVariantValuesToParent(baseQueryWithoutFacetDrilldownCopy, docIdMappingArray);
				}
				var count = baseQueryWithoutFacetDrilldownCopy.Cardinality();
				
				var match = new FacetMatch
				{
					Count = count,
					Value = facetValueBitSet.Value,
					FacetFieldName = facetFieldInfoToCalculateFor.FieldName
				};

				calculations++;
				if (isSelected)
					calculatedFacetCounts.AddToSelected(match);
				else
					calculatedFacetCounts.AddToNonSelected(match);
			}

			return calculatedFacetCounts.GetList();
		}

		private int[] GetDocIdMappingArray(Dictionary<int, int> docIdMappingTable)
		{
			if (docIdMappingTable == null)
			{
				return null;
			}
			var mappingArray = new int[docIdMappingTable.Keys.Max() + 1];
			foreach (var kvp in docIdMappingTable)
			{
				mappingArray[kvp.Key] = kvp.Value;
			}

			return mappingArray;
		}

		private void CascadeVariantValuesToParent(OpenBitSetDISI bitset, int[] docIdMappingTable)
		{
			var capacity = Math.Min(bitset.Capacity(), docIdMappingTable.Length);
			if (bitset.IsEmpty())
				return;
			for (int i = 0; i < capacity; i++)
			{
				if (docIdMappingTable[i] != i)
				{
					if (bitset.FastGet(i))
					{
						bitset.FastSet(docIdMappingTable[i]);
					}
					bitset.FastClear(i);
				}
			}
		}
		private Filter CombineQueryWithFilter(Query query, Filter filter)
		{
			if (filter == null)
			{
				return new QueryWrapperFilter(query);
			}

			return new ChainedFilter(new []
				{
					new QueryWrapperFilter(query),
					filter
				},
				ChainedFilter.Logic.AND
			);

		}

		protected Query CreateFacetedQuery(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> facetFieldInfos, string facetAttributeFieldName)
		{
			var facetsToAdd = facetFieldInfos.Where(x => x.FieldName != facetAttributeFieldName && x.HasSelections).ToList();
			if (!facetsToAdd.Any()) return baseQueryWithoutFacetDrilldown;
			var booleanQuery = new BooleanQuery();
			booleanQuery.Add(baseQueryWithoutFacetDrilldown, BooleanClause.Occur.MUST);
			foreach (var facetFieldInfo in facetsToAdd)
			{
				if (facetFieldInfo.IsRange)
				{
					var selectedRanges = facetFieldInfo.Ranges.Where(r => facetFieldInfo.Selections.Contains(r.Id)).ToList();
					if (selectedRanges.Count == 1)
					{
						booleanQuery.Add(
							new TermRangeQuery(facetFieldInfo.FieldName, selectedRanges[0].From, selectedRanges[0].To, true, true), BooleanClause.Occur.MUST);
					}
					else
					{
						var valuesQuery = new BooleanQuery();
						foreach (var range in selectedRanges)
						{
							valuesQuery.Add(new TermRangeQuery(facetFieldInfo.FieldName, range.From, range.To, true, true),
								BooleanClause.Occur.SHOULD);
						}
						booleanQuery.Add(valuesQuery, BooleanClause.Occur.MUST);
					}
				}
				else
				{
					if (facetFieldInfo.Selections.Count == 1)
					{
						booleanQuery.Add(new TermQuery(new Term(facetFieldInfo.FieldName, facetFieldInfo.Selections[0])), BooleanClause.Occur.MUST);
					}
					else
					{
						var valuesQuery = new BooleanQuery();
						foreach (var value in facetFieldInfo.Selections)
						{
							valuesQuery.Add(new TermQuery(new Term(facetFieldInfo.FieldName, value)), BooleanClause.Occur.SHOULD);
						}
						booleanQuery.Add(valuesQuery, BooleanClause.Occur.MUST);
					}
				}
			}
			return booleanQuery;
		}

		public class FacetValues
		{
			public FacetValues()
			{
				FacetValueBitSetList = new List<FacetValueBitSet>();
			}

			public string Term { get; set; }

			public List<FacetValueBitSet> FacetValueBitSetList { get; set; }

			public class FacetValueBitSet
			{
				public string Value { get; set; }
				public OpenBitSetDISI Bitset { get; set; }
				public long Count { get; set; }
			}
		}
	}
}