/// <author>
/// Nick Lea, Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for commodity forward deals.
/// </summary>
using System;
using System.ComponentModel;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Base class for commodity forward deals.
    /// </summary>
    [Serializable]
    public abstract class CommodityForwardDealBase : AssetDeal
    {
        /// <summary>
        /// Gets or sets whether the holder is long (buy) or short (sell) the deal.
        /// </summary>
        public BuySell Buy_Sell
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        public double Units
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the forward strike price.
        /// </summary>
        public double Forward_Price
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the maturity date.
        /// </summary>
        public TDate Maturity_Date
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Maturity_Date;
        }
    }

    /// <summary>
    /// Deal class for commodity forward deals.
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Forward")]
    public class CommodityForwardDeal : CommodityForwardDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommodityForwardDeal"/> class.
        /// </summary>
        public CommodityForwardDeal()
        {
            Commodity = string.Empty;
        }

        /// <summary>
        /// Gets or sets the commodity.
        /// </summary>
        public string Commodity
        {
            get; set;
        }

        /// <summary>
        /// Summary of deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} for {2} {3:N}", Buy_Sell, GetDealHelper().UnderlyingSummary(), Currency, Units * Forward_Price);
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleCommodityForwardDealHelper(Commodity, Currency);
        }
    }

    /// <summary>
    /// Deal class for commodity forward deals.
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Basket Forward")]
    public class CommodityBasketForwardDeal : CommodityForwardDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommodityBasketForwardDeal"/> class.
        /// </summary>
        public CommodityBasketForwardDeal()
        {
            Basket = new CommodityBasketPrices();
        }

        /// <summary>
        /// Gets or sets the basket.
        /// </summary>
        public CommodityBasketPrices Basket
        {
            get; set;
        }

        /// <summary>
        /// Summary of deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} for {2} {3:N}", Buy_Sell, GetDealHelper().UnderlyingSummary(), Currency, Units * Forward_Price);
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new CommodityBasketPricesForwardDealHelper<CommodityBasketPricesComponent>(Basket, Currency);
        }
    }

    /// <summary>
    /// Valuation class for commodity forward deals.
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Forward Valuation")]
    public class CommodityForwardValuation : AssetValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CommodityForwardDealBase);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CommodityForwardDealBase deal = (CommodityForwardDealBase)fDeal;

            // Add to valuation time grid
            fT.AddPayDate(deal.Maturity_Date, requiredResults.CashRequired());
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            base.PreValue(factors);

            IAssetPrice commodityPrice;
            ISpotProcessVol dummyAssetPriceVol;
            ((BaseAssetFxDealHelper)GetDealHelper()).PreValueAsset(out commodityPrice, out dummyAssetPriceVol, out fBasketPricer, ref fQuantoCompo, factors);

            CommodityForwardDealBase deal = (CommodityForwardDealBase)fDeal;

            double scale = (deal.Buy_Sell == BuySell.Buy ? +1 : -1) * deal.Units;
            double tMaturity = CalcUtils.DaysToYears(deal.Maturity_Date - factors.BaseDate);

            TimeGridIterator tgi = new TimeGridIterator(fT);
            CashAccumulators cash = valuationResults.Cash;
            PVProfiles result = valuationResults.Profile;

            VectorEngine.For(tgi, () =>
                {
                    using (var cache = Vector.Cache(factors.NumScenarios))
                    {
                        Vector pv = cache.Get();

                        if (tgi.Date <= deal.Maturity_Date)
                        {
                            pv.Assign(commodityPrice.ForwardFactor(tgi.T, tMaturity, fFxRate) * commodityPrice.Get(tgi.T)); // assign forward * fxRate to pv
                            pv.Assign((pv - deal.Forward_Price * fFxRate.Get(tgi.T)) * fDiscountRate.Get(tgi.T, tMaturity) * scale);
                        }
                        else
                        {
                            pv.Clear();
                        }

                        result.AppendVector(tgi.Date, pv);

                        if (tgi.Date == deal.Maturity_Date)
                            cash.Accumulate(fFxRate, deal.Maturity_Date, (commodityPrice.Get(tMaturity) / fFxRate.Get(tMaturity) - deal.Forward_Price) * scale);
                    }
                });

            result.Complete(fT);
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }
    }
}