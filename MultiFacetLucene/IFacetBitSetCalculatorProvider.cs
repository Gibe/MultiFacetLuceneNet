namespace MultiFacetLucene
{
	public interface IFacetBitSetCalculatorProvider
	{
		IFacetBitSetCalculator GetFacetBitSetCalculator(FacetFieldInfo info);
	}
}
