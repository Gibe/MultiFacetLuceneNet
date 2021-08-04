using MultiFacetLucene.Configuration;

namespace MultiFacetLucene
{
	public class FacetBitSetCalculatorProvider : IFacetBitSetCalculatorProvider
	{
		public IFacetBitSetCalculator GetFacetBitSetCalculator(FacetFieldInfo info)
		{
			if (info.IsRange)
			{
				return new RangeFacetBitSetCalculator(new FacetSearcherConfiguration());
			}
			return new TermFacetBitSetCalulcator(new FacetSearcherConfiguration());
		}
	}
}
