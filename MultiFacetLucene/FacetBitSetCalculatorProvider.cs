using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
