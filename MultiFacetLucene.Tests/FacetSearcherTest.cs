using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;

namespace MultiFacetLucene.Tests
{
	[TestFixture]
	public class FacetSearcherTest
	{
		private FacetSearcher _target;

		[SetUp]
		public void TestInitialize()
		{
			_target = new FacetSearcher(SetupIndex());
		}

		[Test]
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(5));

			Assert.That(colorFacets.Count, Is.EqualTo(3));
			Assert.That(typeFacets.Count, Is.EqualTo(4));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
			Assert.That(colorFacets.Single(x => x.Value == "white").Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "drink").Count, Is.EqualTo(2));
			Assert.That(typeFacets.Single(x => x.Value == "meat").Count, Is.EqualTo(1));
			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(3));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));
		}

		[Test]
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(3));

			Assert.That(colorFacets.Count, Is.EqualTo(3));
			Assert.That(typeFacets.Count, Is.EqualTo(3));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
			Assert.That(colorFacets.Single(x => x.Value == "white").Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "meat").Count, Is.EqualTo(1));
			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(3));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));
		}

		[Test]
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(4));

			Assert.That(colorFacets.Count, Is.EqualTo(3));
			Assert.That(typeFacets.Count, Is.EqualTo(4));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
			Assert.That(colorFacets.Single(x => x.Value == "white").Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "meat").Count, Is.EqualTo(1));
			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(3));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));
			Assert.That(typeFacets.Single(x => x.Value == "drink").Count, Is.EqualTo(1));
		}

		[Test]
		public void MaxFacetRestrictionShouldReturnCorrectFacetsAndDocuments()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", MaxToFetchExcludingSelections = 1},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(5));
			Assert.That(colorFacets.Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
		}

		[Test]
		public void MaxFacetRestrictionShouldStillReturnSelectedFacet()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color", Selections = new List<string>{"none"}, MaxToFetchExcludingSelections = 1},
						};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.That(colorFacets.Count, Is.EqualTo(2));
			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));
		}

		[Test]
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(4));

			Assert.That(colorFacets.Count, Is.EqualTo(3));
			Assert.That(typeFacets.Count, Is.EqualTo(2));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(3));
			Assert.That(colorFacets.Single(x => x.Value == "white").Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(3));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));
		}


		[Test]
		public void MatchSpecifiedQueryShouldReturnCorrectFacetsAndDocuments()
		{
			var query = new QueryParser(LuceneVersion.LUCENE_48, string.Empty, new KeywordAnalyzer()).Parse("keywords:apa");

			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "color"},
								new FacetFieldInfo{ FieldName = "type"},
						};
			var actual = _target.SearchWithFacets(query, 100, facetFieldInfos);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();
			var typeFacets = actual.Facets.Where(x => x.FacetFieldName == "type").ToList();

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(2));
			Assert.That(_target.Doc(actual.Hits.ScoreDocs[0].Doc).GetField("title").GetStringValue(), Is.EqualTo("Banana"));
			Assert.That(_target.Doc(actual.Hits.ScoreDocs[1].Doc).GetField("title").GetStringValue(), Is.EqualTo("Water"));

			Assert.That(colorFacets.Count, Is.EqualTo(2));
			Assert.That(typeFacets.Count, Is.EqualTo(3));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(1));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "drink").Count, Is.EqualTo(1));
			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(1));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(1));
		}

		[Test]
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(2));

			Assert.That(colorFacets.Count, Is.EqualTo(1));
			Assert.That(typeFacets.Count, Is.EqualTo(3));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(2)); // only fruits

			Assert.That(typeFacets.Single(x => x.Value == "meat").Count, Is.EqualTo(1)); //only yellow
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));//only yellow
			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(3));//only yellow
		}

		[Test]
		public void IncludeEmptyFacetsShouldIncludeEmptyFacets()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
			{
				new FacetFieldInfo{ FieldName = "type", Selections = new List<string>{"drink"} },
        new FacetFieldInfo{ FieldName = "color"}
			};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos, true);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.That(colorFacets.Count(x => x.Count == 0), Is.EqualTo(1));
		}

		[Test]
		public void DoNotIncludeEmptyFacetsShouldNotIncludeEmptyFacets()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
			{
				new FacetFieldInfo{ FieldName = "type", Selections = new List<string>{"drink"} },
				new FacetFieldInfo{ FieldName = "color"}
			};

			var actual = _target.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos, false);

			var colorFacets = actual.Facets.Where(x => x.FacetFieldName == "color").ToList();

			Assert.That(colorFacets.Count(x => x.Count == 0), Is.EqualTo(0));
		}

		[Test]
		public void RangeFacetsShouldReturnCorrectFacetsAndDocument()
		{
			var facetFieldInfos = new List<FacetFieldInfo>
						{
								new FacetFieldInfo{ FieldName = "price", IsRange = true, Ranges = new List<MultiFacetLucene.Range>
								{
									new MultiFacetLucene.Range { Id = "A", From = "0", To = "10"},
									new MultiFacetLucene.Range { Id = "B", From = "0", To = "20"},
									new MultiFacetLucene.Range { Id = "C", From = "0", To = "30"}
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

			Assert.That(actual.Hits.TotalHits, Is.EqualTo(3));

			Assert.That(colorFacets.Count, Is.EqualTo(2));
			Assert.That(typeFacets.Count, Is.EqualTo(3));
			Assert.That(priceFacets.Count, Is.EqualTo(3));

			Assert.That(priceFacets.Single(x => x.Value == "A").Count, Is.EqualTo(2));
			Assert.That(priceFacets.Single(x => x.Value == "B").Count, Is.EqualTo(3));
			Assert.That(priceFacets.Single(x => x.Value == "C").Count, Is.EqualTo(4));

			Assert.That(colorFacets.Single(x => x.Value == "yellow").Count, Is.EqualTo(2));
			Assert.That(colorFacets.Single(x => x.Value == "none").Count, Is.EqualTo(1));

			Assert.That(typeFacets.Single(x => x.Value == "food").Count, Is.EqualTo(2));
			Assert.That(typeFacets.Single(x => x.Value == "fruit").Count, Is.EqualTo(2));
			Assert.That(typeFacets.Single(x => x.Value == "drink").Count, Is.EqualTo(1));
		}


		protected static IndexReader SetupIndex()
		{
			var directory = new RAMDirectory();
			var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48)));
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
			writer.Flush(true, true);
			writer.Commit();
			return DirectoryReader.Open(directory);

		}
	}
}
