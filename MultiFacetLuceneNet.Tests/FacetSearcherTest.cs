﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFacetLucene;
using Lucene.Net.Util;

namespace MultiFacetLuceneNet.Tests
{
	[TestClass]
	public class FacetSearcherTest
	{
		private FacetSearcher _target;

		[TestInitialize]
		public void TestInitialize()
		{
			_target = new FacetSearcher(SetupIndex());
		}

		[TestMethod]
		public void MatchAllQueryShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color"},
								new FacetFieldInfo{ FieldName = "type"},
						};


			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(5, actual.Hits.TotalHits);

			Assert.AreEqual(3, colorFacets.Count);
			Assert.AreEqual(4, typeFacets.Count);

			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "white").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "drink").Count);
			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "meat").Count);
			Assert.AreEqual(3, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);
		}

		[TestMethod]
		public void DrilldownSingleFacetSingleValueShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"yellow"}},
								new FacetFieldInfo{ FieldName = "type"},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(3, actual.Hits.TotalHits);

			Assert.AreEqual(3, colorFacets.Count);
			Assert.AreEqual(3, typeFacets.Count);

			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "white").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "meat").Count);
			Assert.AreEqual(3, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);
		}

		[TestMethod]
		public void DrilldownSingleFacetMultiValueShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"yellow", "none"}},
								new FacetFieldInfo{ FieldName = "type"},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(4, actual.Hits.TotalHits);

			Assert.AreEqual(3, colorFacets.Count);
			Assert.AreEqual(4, typeFacets.Count);

			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "white").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "meat").Count);
			Assert.AreEqual(3, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);
			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "drink").Count);
		}

		[TestMethod]
		public void MaxFacetRestrictionShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", MaxToFetchExcludingSelections = 1},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.AreEqual(5, actual.Hits.TotalHits);
			Assert.AreEqual(1, colorFacets.Count);
			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
		}

		[TestMethod]
		public void MaxFacetRestrictionShouldStillReturnSelectedFacet()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"none"}, MaxToFetchExcludingSelections = 1},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.AreEqual(2, colorFacets.Count);
			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);
		}


		public void MaxFacetRestrictionShouldReturnSelectedFacetAsWell()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"yellow", "none"}},
								new FacetFieldInfo{ FieldName = "type", MaxToFetchExcludingSelections = 2},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(4, actual.Hits.TotalHits);

			Assert.AreEqual(3, colorFacets.Count);
			Assert.AreEqual(2, typeFacets.Count);

			Assert.AreEqual(3, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "white").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(3, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);
		}


		[TestMethod]
		public void MatchSpecifiedQueryShouldReturnCorrectFacetsAndDocuments()
		{
			var query = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, string.Empty, new KeywordAnalyzer()).Parse("keywords:apa");

			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color"},
								new FacetFieldInfo{ FieldName = "type"},
						};
			var actual = _target.SearchWithFacets(query, 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(2, actual.Hits.TotalHits);
			Assert.AreEqual("Banana", _target.Doc(actual.Hits.ScoreDocs[0].Doc).GetField("title").StringValue);
			Assert.AreEqual("Water", _target.Doc(actual.Hits.ScoreDocs[1].Doc).GetField("title").StringValue);

			Assert.AreEqual(2, colorFacets.Count);
			Assert.AreEqual(3, typeFacets.Count);

			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "drink").Count);
			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "fruit").Count);
		}

		[TestMethod]
		public void DrilldownMultiFacetsShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"yellow"}},
								new FacetFieldInfo{ FieldName = "type", Selections = new List<string>{"fruit"}},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.AreEqual(2, actual.Hits.TotalHits);

			Assert.AreEqual(1, colorFacets.Count);
			Assert.AreEqual(3, typeFacets.Count);

			Assert.AreEqual(2, colorFacets.Single(x => x.Value == "yellow").Count); // only fruits

			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "meat").Count); //only yellow
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);//only yellow
			Assert.AreEqual(3, typeFacets.Single(x => x.Value == "food").Count);//only yellow
		}

		[TestMethod]
		public void IncludeEmptyFacetsShouldIncludeEmptyFacets()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
			{
				new FacetFieldInfo{ FieldName = "type", Selections = new List<string>{"drink"} },
        new FacetFieldInfo{ FieldName = "color"}
			};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos, true);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			
			Assert.AreEqual(1, colorFacets.Count(x => x.Count == 0));	
		}

		[TestMethod]
		public void DoNotIncludeEmptyFacetsShouldNotIncludeEmptyFacets()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
			{
				new FacetFieldInfo{ FieldName = "type", Selections = new List<string>{"drink"} },
				new FacetFieldInfo{ FieldName = "color"}
			};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos, false);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.AreEqual(0, colorFacets.Count(x => x.Count == 0));
		}

		[TestMethod]
		public void RangeFacetsShouldReturnCorrectFacetsAndDocument()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "price", IsRange = true, Ranges = new List<Range>
								{
									new Range { Id = "A", From = "0", To = "10"},
									new Range { Id = "B", From = "0", To = "20"},
									new Range { Id = "C", From = "0", To = "30"}
								},
								Selections = new List<string>
								{
									"B"
								}},
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>()},
								new FacetFieldInfo{ FieldName = "type", Selections = new List<string>()},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();
			var priceFacets = actual.Facets.Where(x => x.FacetFieldName == "price").ToList();

			Assert.AreEqual(3, actual.Hits.TotalHits);

			Assert.AreEqual(2, colorFacets.Count);
			Assert.AreEqual(3, typeFacets.Count);
			Assert.AreEqual(3, priceFacets.Count);

			Assert.AreEqual(2, priceFacets.Single(x => x.Value == "A").Count);
			Assert.AreEqual(3, priceFacets.Single(x => x.Value == "B").Count);
			Assert.AreEqual(4, priceFacets.Single(x => x.Value == "C").Count);

			Assert.AreEqual(2, colorFacets.Single(x => x.Value == "yellow").Count);
			Assert.AreEqual(1, colorFacets.Single(x => x.Value == "none").Count);

			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "food").Count);
			Assert.AreEqual(2, typeFacets.Single(x => x.Value == "fruit").Count);
			Assert.AreEqual(1, typeFacets.Single(x => x.Value == "drink").Count);
		}


		protected static IndexReader SetupIndex()
		{
			var directory = new RAMDirectory();
			var writer = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true,
					IndexWriter.MaxFieldLength.LIMITED);
			writer.AddDocument(new Document()
					.AddField("title", "Banana", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("color", "yellow", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "food", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "fruit", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("keywords", "apa hello whatever", Field.Store.YES, Field.Index.ANALYZED)
					.AddField("price", "10", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.AddDocument(new Document()
					.AddField("title", "Apple", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("color", "yellow", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "food", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "fruit", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("price", "20", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.AddDocument(new Document()
					.AddField("title", "Burger", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("color", "yellow", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "food", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "meat", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("price", "30", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.AddDocument(new Document()
					.AddField("title", "Milk", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("color", "white", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "drink", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("price", "40", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.AddDocument(new Document()
					.AddField("title", "Water", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("color", "none", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("type", "drink", Field.Store.YES, Field.Index.NOT_ANALYZED)
					.AddField("keywords", "apa hello cars", Field.Store.YES, Field.Index.ANALYZED)
					.AddField("price", "0", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.Flush(true, true, true);
			writer.Optimize();
			writer.Commit();
			return IndexReader.Open(directory, true);

		}
	}
}
