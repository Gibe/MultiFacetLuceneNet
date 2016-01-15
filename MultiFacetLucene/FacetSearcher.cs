using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

		public FacetSearcher(IndexReader reader, IndexReader[] subReaders, int[] docStarts, FacetSearcherConfiguration facetSearcherConfiguration = null)
			: base(reader, subReaders, docStarts)
		{
			Initialize(facetSearcherConfiguration);
		}

		public FacetSearcherConfiguration FacetSearcherConfiguration { get; protected set; }

		private void Initialize(FacetSearcherConfiguration facetSearcherConfiguration)
		{
			FacetSearcherConfiguration = facetSearcherConfiguration ?? FacetSearcherConfiguration.Default();
		}

		public virtual FacetSearchResult SearchWithFacets(Query baseQueryWithoutFacetDrilldown, int topResults, IList<FacetFieldInfo> facetFieldInfos, bool includeEmptyFacets = false, Query filter = null, Dictionary<int, int> docIdMappingTable = null)
		{
			var hits = Search(CreateFacetedQuery(baseQueryWithoutFacetDrilldown, facetFieldInfos, null), topResults);
			
			var facets = GetAllFacetsValues(baseQueryWithoutFacetDrilldown, facetFieldInfos, filter, docIdMappingTable);
			if (!includeEmptyFacets)
			{
				facets = facets.Where(x => x.Count > 0);
			}

			return new FacetSearchResult()
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

			facetValues.FacetValueBitSetList.AddRange(GetFacetBitSetCalculator(fieldInfo).GetFacetValueBitSets(IndexReader, fieldInfo).OrderByDescending(x => x.Count));

			if (FacetSearcherConfiguration.MemoryOptimizer == null) return facetValues;
			foreach (var facetValue in FacetSearcherConfiguration.MemoryOptimizer.SetAsLazyLoad(_facetBitSetDictionary.Values.ToList()))
				facetValue.Bitset = null;

			return facetValues;
		}
		
		private IEnumerable<FacetMatch> GetAllFacetsValues(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> facetFieldInfos, Query filter, Dictionary<int, int> docIdMappingTable)
		{
			return
					facetFieldInfos.SelectMany(
							facetFieldInfo =>
									FindMatchesInQuery(baseQueryWithoutFacetDrilldown, facetFieldInfos, facetFieldInfo, filter, docIdMappingTable));
		}

		private IEnumerable<FacetMatch> FindMatchesInQuery(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> allFacetFieldInfos, FacetFieldInfo facetFieldInfoToCalculateFor, Query filter, Dictionary<int, int> docIdMappingTable)
		{
			var calculations = 0;
			var query = CombineQueryWithFilter(CreateFacetedQuery(baseQueryWithoutFacetDrilldown, allFacetFieldInfos, facetFieldInfoToCalculateFor.FieldName), filter);
			var queryFilter = new CachingWrapperFilter(new QueryWrapperFilter(query));
			var bitsQueryWithoutFacetDrilldown = new OpenBitSetDISI(queryFilter.GetDocIdSet(IndexReader).Iterator(), IndexReader.MaxDoc);
			var baseQueryWithoutFacetDrilldownCopy = new OpenBitSetDISI(bitsQueryWithoutFacetDrilldown.Bits.Length)
			{
				Bits = new long[bitsQueryWithoutFacetDrilldown.Bits.Length]
			};

			var calculatedFacetCounts = new ResultCollection(facetFieldInfoToCalculateFor);
			foreach (var facetValueBitSet in GetOrCreateFacetBitSet(facetFieldInfoToCalculateFor).FacetValueBitSetList)
			{
				var isSelected = calculatedFacetCounts.IsSelected(facetValueBitSet.Value);

				if (!isSelected && facetValueBitSet.Count < calculatedFacetCounts.MinCountForNonSelected) //Impossible to get a better result
				{
					if (calculatedFacetCounts.HaveEnoughResults)
						break;
				}

				bitsQueryWithoutFacetDrilldown.Bits.CopyTo(baseQueryWithoutFacetDrilldownCopy.Bits, 0);
				baseQueryWithoutFacetDrilldownCopy.NumWords = bitsQueryWithoutFacetDrilldown.NumWords;

				var bitset = facetValueBitSet.Bitset ?? GetFacetBitSetCalculator(facetFieldInfoToCalculateFor).GetFacetBitSet(IndexReader, facetFieldInfoToCalculateFor, facetValueBitSet.Value);
				baseQueryWithoutFacetDrilldownCopy.And(bitset);

				if (docIdMappingTable != null)
				{
					CascadeVariantValuesToParent(baseQueryWithoutFacetDrilldownCopy, docIdMappingTable);
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

		private void CascadeVariantValuesToParent(OpenBitSetDISI bitset, Dictionary<int, int> docIdMappingTable)
		{
			for (int i = 0; i < bitset.Capacity(); i++)
			{
				if (docIdMappingTable.ContainsKey(i) && docIdMappingTable[i] != i)
				{
					if (bitset.Get(i))
					{
						bitset.FastSet(docIdMappingTable[i]);
					}
					bitset.FastClear(i);
				}
			}
		}
		private Query CombineQueryWithFilter(Query query, Query filter)
		{
			if (filter == null)
			{
				return query;
			}
			
			var combinedQuery = new BooleanQuery();
			combinedQuery.Add(query, Occur.MUST);
			combinedQuery.Add(filter, Occur.MUST);
			return combinedQuery;
		}

		protected Query CreateFacetedQuery(Query baseQueryWithoutFacetDrilldown, IList<FacetFieldInfo> facetFieldInfos, string facetAttributeFieldName)
		{
			var facetsToAdd = facetFieldInfos.Where(x => x.FieldName != facetAttributeFieldName && x.HasSelections).ToList();
			if (!facetsToAdd.Any()) return baseQueryWithoutFacetDrilldown;
			var booleanQuery = new BooleanQuery { { baseQueryWithoutFacetDrilldown, Occur.MUST } };
			foreach (var facetFieldInfo in facetsToAdd)
			{
				if (facetFieldInfo.IsRange)
				{
					var selectedRanges = facetFieldInfo.Ranges.Where(r => facetFieldInfo.Selections.Contains(r.Id)).ToList();
					if (selectedRanges.Count == 1)
					{
						booleanQuery.Add(
							new TermRangeQuery(facetFieldInfo.FieldName, selectedRanges[0].From, selectedRanges[0].To, true, true), Occur.MUST);
					}
					else
					{
						var valuesQuery = new BooleanQuery();
						foreach (var range in selectedRanges)
						{
							valuesQuery.Add(new TermRangeQuery(facetFieldInfo.FieldName, range.From, range.To, true, true),
								Occur.SHOULD);
						}
						booleanQuery.Add(valuesQuery, Occur.MUST);
					}
				}
				else
				{
					if (facetFieldInfo.Selections.Count == 1)
					{
						booleanQuery.Add(new TermQuery(new Term(facetFieldInfo.FieldName, facetFieldInfo.Selections[0])), Occur.MUST);
					}
					else
					{
						var valuesQuery = new BooleanQuery();
						foreach (var value in facetFieldInfo.Selections)
						{
							valuesQuery.Add(new TermQuery(new Term(facetFieldInfo.FieldName, value)), Occur.SHOULD);
						}
						booleanQuery.Add(valuesQuery, Occur.MUST);
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