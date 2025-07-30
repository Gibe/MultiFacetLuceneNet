using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace MultiFacetLucene.Tests
{
    //[Ignore]
    [TestFixture]
    public class PerformanceTest
    {
        private static FacetSearcher? _target;
        private static readonly Random Rnd = new Random(Guid.NewGuid().GetHashCode());

        public void Warmup()
        {
            //Warmup to prefetch facet bitset
            var facetFieldInfos = new List<FacetFieldInfo>
            {
                new FacetFieldInfo{ FieldName = "color"},
                new FacetFieldInfo{ FieldName = "type"},
            };
            _target?.SearchWithFacets(new TermQuery(new Term("Price", "5")), 100, facetFieldInfos);
        }

        [SetUp]
        public void TestInitialize()
        {
            _target = new FacetSearcher(SetupIndex());
            Warmup();
        }

        [Test]
        public void MatchAllDocsAndCalculateFacetsPerformanceTest()
        {
            Warmup();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var facetFieldInfos = new List<FacetFieldInfo>
            {
                new() { FieldName = "color"},
                new() { FieldName = "type"},
            };
            var actual = _target?.SearchWithFacets(new MatchAllDocsQuery(), 100, facetFieldInfos);


            stopwatch.Stop();
            var vs = stopwatch.ElapsedMilliseconds;
            Trace.WriteLine("Took " + vs + " ms");
        }

        [Test]
        public void MatchAllDocsPerformanceTest()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var actual = _target?.SearchWithFacets(new MatchAllDocsQuery(), 100, new List<FacetFieldInfo>());


            stopwatch.Stop();
            var vs = stopwatch.ElapsedMilliseconds;
            Trace.WriteLine("Took " + vs + " ms");
        }


        protected static IndexReader SetupIndex()
        {
            var directory = new RAMDirectory();
            var writer = new IndexWriter(directory, new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)));
            for (var i = 0; i < 50000; i++)
                writer.AddDocument(new Document()
                    .AddField("title", Guid.NewGuid().ToString(), false)
                    .AddField("color", GenerateColor(), false)
                    .AddField("type", GenerateFood(), false)
                    .AddField("type", GenerateFruit(), false)
                    .AddField("price", "10", false));
            writer.Flush(true, true);
            writer.Commit();
            return DirectoryReader.Open(directory);
        }


        private static string GenerateFruit()
        {
            return "fruit" + GetRandom(1000, 2000); // 1000 different values
        }

        private static string GenerateFood()
        {
            return "food" + GetRandom(1000, 1100); // 100 different values
        }

        private static string GenerateColor()
        {
            return "color" + GetRandom(1000, 1100); // 30 different values
        }

        private static string GetRandom(int i, int i1)
        {
            return Rnd.Next(i, i1).ToString("00000");
        }
    }
}