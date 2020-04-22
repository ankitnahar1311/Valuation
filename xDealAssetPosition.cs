/// <author>
/// Sasanka Vanga, Alastair Wilkins
/// </author>
/// <owner>
/// Sasanka Vanga
/// </owner>
/// <summary>
/// Base deal and valuation classes for equity positions and ETF deals.
/// </summary>
using System;
using System.ComponentModel;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Base class for asset positions.
    /// </summary>
    [Serializable]
    public abstract class AssetPositionDeal : Deal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPositionDeal"/> class.
        /// </summary>
        protected AssetPositionDeal()
        {
            Buy_Sell = BuySell.Buy;
            Currency = string.Empty;
        }

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
        /// Gets or sets the currency.
        /// </summary>
        public string Currency
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the investment horizon.
        /// </summary>
        public TDate Investment_Horizon
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Investment_Horizon;
        }

        /// <summary>
        /// Currency of deal.
        /// </summary>
        public override string DealCurrency()
        {
            return Currency;
        }

        /// <summary>
        /// Deal description.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1}", AssetName(), Currency);
        }

        /// <summary>
        /// Underlying asset name.
        /// </summary>
        public abstract string AssetName();
    }

    /// <summary>
    /// Base valuation class for asset positions.
    /// </summary>
    [Serializable]
    public abstract class AssetPositionValuation : Valuation
    {
        protected AssetPositionDeal fDeal;

        /// <summary>
        /// Gets/sets the deal this valuation model will value.
        /// </summary>
        public override Deal Deal
        {
            get { return fDeal; } set { fDeal = (AssetPositionDeal)value; }
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            factors.RegisterInterface<IFxRate>(fDeal.Currency);
        }

        /// <summary>
        /// Calculate a valuation profile for a range of scenarios.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            IAssetPrice price = GetAssetPrice(factors);
            PVProfiles result = valuationResults.Profile;

            double scale = fDeal.Units * (fDeal.Buy_Sell == BuySell.Buy ? +1 : -1);

            var tgi = new TimeGridIterator(fT);

            VectorEngine.For(tgi, () => result.AppendVector(tgi.Date, scale * price.Get(tgi.T)));

            result.Complete(fT);

            CashAccumulators cashAccumulators = valuationResults.Cash;
            double endDate = Deal.EndDate();
            if (!cashAccumulators.Ignore && endDate <= fT.fHorizon)
            {
                double tEnd = CalcUtils.DaysToYears(endDate - factors.BaseDate);
                IFxRate fxRate = factors.GetInterface<IFxRate>(fDeal.Currency);
                cashAccumulators.Accumulate(fxRate, endDate, scale * price.Get(tEnd) / fxRate.Get(tEnd));
            }
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Gets the asset price factor.
        /// </summary>
        protected abstract IAssetPrice GetAssetPrice(PriceFactorList factors);
    }
}