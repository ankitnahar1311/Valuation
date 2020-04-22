/// <author>
/// Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for commodity futures and commodity futures options.
/// </summary>
using System;
using System.ComponentModel;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Deal class for commodity futures.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commodity Future")]
    public class CommodityFuture : Future
    {
        public CommodityFuture()
            : base()
        {
            Commodity = string.Empty;
        }

        public string Commodity
        {
            get; set;
        }

        /// <inheritdoc />
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            // Validate the existence of commodity.
            if (string.IsNullOrWhiteSpace(Commodity))
            {
                AddToErrors(errors, "Commodity must be specified on the deal.");
            }
        }
    }

    /// <summary>
    /// Valuation class for commodity futures. 
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commodity Future Valuation")]
    public class CommodityFutureValuation : FutureValuation
    {
        [NonSerialized]
        protected ICommodityPrice fCommodityPrice = null; // commodity price in base currency

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CommodityFuture);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CommodityFuture deal = (CommodityFuture)Deal;

            factors.RegisterInterface<ICommodityPrice>(deal.Commodity);
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CommodityFuture deal = (CommodityFuture)Deal;

            fCommodityPrice = factors.GetInterface<ICommodityPrice>(deal.Commodity);
        }

        /// <summary>
        /// Calculate forward price.
        /// </summary>
        protected override void ForwardPrice(double baseDate, double valueDate, Vector forwardPrice)
        {
            CommodityFuture deal = (CommodityFuture)Deal;

            double t = CalcUtils.DaysToYears(valueDate - baseDate);
            double tSettle = CalcUtils.DaysToYears(deal.Settlement_Date - baseDate);

            forwardPrice.Assign((fCommodityPrice.ForwardFactor(t, tSettle, fFxRate) * fCommodityPrice.Get(t)) / fFxRate.Get(t));
        }
    }

    /// <summary>
    /// Deal class for commodity futures options.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commodity Future Option")]
    public class CommodityFutureOption : FutureOption
    {
        public CommodityFutureOption()
            : base()
        {
            Commodity = string.Empty;
            Commodity_Volatility = string.Empty;
        }

        public string Commodity
        {
            get; set;
        }

        public string Commodity_Volatility
        {
            get; set;
        }

        public SingleCommodityDealHelper GetCommodityDealHelper()
        {
            return new SingleCommodityDealHelper(Commodity, Commodity_Volatility, Currency);
        }

        /// <summary>
        /// Validates that a commodity ID has been entered.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            GetCommodityDealHelper().Validate(errors);
        }
    }

    /// <summary>
    /// Valuation class for commodity futures options.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commodity Future Option Valuation")]
    public class CommodityFutureOptionValuation : FutureOptionValuation
    {
        [NonSerialized]
        protected IAssetPrice fCommodityPrice = null; // commodity price in base currency
        [NonSerialized]
        protected ISpotProcessVol fCommodityPriceVol = null;

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>        
        public override Type DealType()
        {
            return typeof(CommodityFutureOption);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CommodityFutureOption deal = (CommodityFutureOption)Deal;
            deal.GetCommodityDealHelper().RegisterFactors(factors, errors);
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CommodityFutureOption deal = (CommodityFutureOption)Deal;
            deal.GetCommodityDealHelper().PreValueAsset(out fCommodityPrice, out fCommodityPriceVol, factors);
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Calculate forward price, discount factor and volatility.
        /// </summary>
        protected override void PriceAndVolatility(double baseDate, double valueDate, Vector forwardPrice, Vector discountFactor, Vector volatility)
        {
            CommodityFutureOption deal = (CommodityFutureOption)Deal;

            double t = CalcUtils.DaysToYears(valueDate - baseDate);
            double tSettle = CalcUtils.DaysToYears(deal.Settlement_Date - baseDate);

            // Get spot price in contract currency
            forwardPrice.Assign(fCommodityPrice.Get(t) / fFxRate.Get(t));

            if (volatility != null)
            {
                double tExpiry = CalcUtils.DaysToYears(deal.Expiry_Date - baseDate);
                if (tExpiry > t)
                {
                    // temporary use of discountFactor vector to store strike
                    discountFactor.Assign(deal.Strike);
                    // Get volatility using spot price and strike
                    volatility.Assign(fCommodityPriceVol.Get(t, forwardPrice, discountFactor, tExpiry));
                }
                else
                {
                    volatility.Clear();
                }
            }

            fDiscountRate.GetValue(discountFactor, t, tSettle);

            // Get forward factor and complete calculation of forward price
            forwardPrice.MultiplyBy(fCommodityPrice.ForwardFactor(t, tSettle, fFxRate));
        }

        /// <inheritdoc />
        protected override bool UnderlyingIsAssetPrice
        {
            get { return true; }
        }
    }
}