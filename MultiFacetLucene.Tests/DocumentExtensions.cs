using System;
using Lucene.Net.Documents;

namespace MultiFacetLucene.Tests
{
	public static class DocumentExtensions
	{
		public static Document AddField(this Document document, string fieldKey, string value, bool tokenize = true, bool store = true, float boostWeight = 0f)
		{
			var fieldType = new FieldType
			{
				IsIndexed = true,
				IsStored = store,
				IsTokenized = tokenize,
				StoreTermVectors = tokenize,
				StoreTermVectorPositions = tokenize
			};
			var f = new Field(fieldKey, value ?? String.Empty, fieldType)
			{
				Boost = boostWeight
			};
			document.Add(f);
			return document;
		}
	}
}