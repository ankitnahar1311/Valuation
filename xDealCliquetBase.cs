/// <author>
/// Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Base deal and valuation classes for cliquet options.
/// </summary>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Base deal class for cliquet options.
    /// </summary>
    [Serializable]
    public abstract class BaseCliquetOption : AssetDeal
    {
        protected BaseCliquetOption()
        {
            Moneyness    = 1.0;
        }

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

        public BuySell Buy_Sell
        {
            get; set;
        }

        public OptionType Option_Type
        {
            get; set;
        }

        public TDate Effective_Date
        {
            get; set;
        }

        public TDate Maturity_Date
        {
            get; set;
        }

        public Period Frequency
        {
            get; set;
        }

        public double Moneyness
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

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Effective_Date, Maturity_Date, true);

            if (Moneyness <= 0.0)
            {
                AddToErrors(errors, "Moneyness must be greater than zero");
            }
        }

        /// <summary>
        /// Summary of deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2} {3}", Buy_Sell, Option_Type, Maturity_Date, DealCurrency());
        }

        /// <summary>
        /// Number of units of asset.
        /// </summary>
        public abstract double GetUnits();

        /// <summary>
        /// Construct the array of known prices.
        /// </summary>
        public abstract double[] GetKnownPrices(DateList resetDates, PriceFactorList factors, IAssetPrice assetPrice);

        /// <summary>
        /// Fills missing known prices with values from the rate fixings.
        /// </summary>
        protected void FillMissingKnownPricesFromRateFixings(double[] knownPrices, DateList resetDates, PriceFactorList factors, IAssetPrice assetPrice, string assetCurrency)
        {
            List<int> missingIndices;
            List<DateTime> missingDates;
            if (!GetMissingIndicesAndDates(knownPrices, resetDates, factors.BaseDate, out missingIndices, out missingDates))
                return;

            var rateFixings = factors.RateFixings.GetAssetPriceFixings(factors, assetPrice, assetCurrency, missingDates, factors.BaseDate, this, string.Empty).ToList();

            ApplyFixings(missingIndices, rateFixings, knownPrices);
        }

        /// <summary>
        /// Build lists of indices i and corresponding reset dates for which knownRates[i] is missing.
        /// </summary>
        private static bool GetMissingIndicesAndDates(double[] knownRates, DateList resetDates, double baseDate, out List<int> missingIndices, out List<DateTime> missingDates)
        {
            if (knownRates == null)
                throw new ArgumentNullException("knownRates");

            if (resetDates == null)
                throw new ArgumentNullException("resetDates");

            if (knownRates.Length != resetDates.Count)
                throw new ArgumentException("knownRates and resetDates must have the same number of elements");

            missingIndices = new List<int>();
            missingDates = new List<DateTime>();
            for (int index = 0; index < resetDates.Count; ++index)
            {
                if (resetDates[index] > baseDate)
                    break; // this and subsequent reset dates are in the future

                if (knownRates[index] > 0.0)
                    continue; // already have a known price

                if (index < resetDates.Count - 1 && resetDates[index + 1] < baseDate)
                    continue; // do not need a known price for past cashflows (cashflow payment date = resetDates[index + 1] for Cliquet options)

                missingIndices.Add(index);
                missingDates.Add(DateTime.FromOADate(resetDates[index]));
            }

            return missingIndices.Any();
        }

        /// <summary>
        /// Set knownRates[missingIndices[i]] = rateFixings[i] whenever rateFixings[i] exists (is not NaN) and is positive.
        /// </summary>
        private static void ApplyFixings(List<int> missingIndices, List<double> rateFixings, double[] knownRates)
        {
            if (missingIndices == null)
                throw new ArgumentNullException("missingIndices");

            if (rateFixings == null)
                throw new ArgumentNullException("resetDates");

            if (knownRates == null)
                throw new ArgumentNullException("knownRates");

            if (missingIndices.Count != rateFixings.Count)
                throw new ArgumentException("missingIndices and rateFixings must have the same number of elements");

            if (knownRates.Length < missingIndices.Count)
                throw new ArgumentException("knownRates cannot have fewer elements than missingIndices");

            for (int i = 0; i < missingIndices.Count; ++i)
            {
                if (!Double.IsNaN(rateFixings[i]))
                    knownRates[missingIndices[i]] = rateFixings[i];
            }
        }
    }

    /// <summary>
    /// Base valuation class for cliquet options.
    /// </summary>
    [Serializable]
    public abstract class BaseCliquetOptionValuation : AssetValuation
    {
        [NonSerialized]
        protected IAssetPrice fAssetPrice = null;
        [NonSerialized]
        protected ISpotProcessVol fAssetPriceVol = null;
        protected double[] fTimes = null; // cashflow and reset times
        
        // This field is potentially constructed from rate fixings, so it needs to be created on the
        // head node and serialized to grid nodes, as rate fixings aren't available on grid nodes.
        protected double[] fKnownPrices;

        /// <inheritdoc />
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);

            BaseCliquetOption deal = (BaseCliquetOption)fDeal;

            DateList accrualDates = CashflowGeneration.GenerateStripOfDates(deal.Effective_Date, deal.Maturity_Date, deal.Frequency, deal.GetHolidayCalendar());

            fTimes = new double[accrualDates.Count];
            for (int i = 0; i < accrualDates.Count; ++i)
                fTimes[i] = CalcUtils.DaysToYears(accrualDates[i] - factors.BaseDate);

            // Get the asset price from the deal helper
            var dealHelper = (BaseAssetFxDealHelper)deal.GetDealHelper();
            IAssetPrice assetPrice = dealHelper.GetAssetPrice(factors);

            fKnownPrices = deal.GetKnownPrices(accrualDates, factors, assetPrice);

            // Add expiry dates to valuation time grid.
            if (accrualDates.Count > 1)
            {
                DateList expiryDates = new DateList(accrualDates);
                expiryDates.RemoveAt(0);
                
                fT.AddPayDates(expiryDates, requiredResults.CashRequired());
            }
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            TimeGridIterator tgi = new TimeGridIterator(fT);
            PVProfiles result = valuationResults.Profile;
            CashAccumulators cashAccumulators = valuationResults.Cash;

            BaseCliquetOption deal = (BaseCliquetOption)Deal;

            double scale = (deal.Buy_Sell == BuySell.Buy ? +1 : -1) * deal.GetUnits();

            VectorEngine.For(tgi, () =>
                {
                    using (var cache = Vector.Cache(factors.NumScenarios))
                    {
                        Vector pv   = cache.Get();
                        Vector cash = cache.Get();

                        PricingFunctions.CliquetOption(pv, cash, deal.Option_Type, tgi.T, fTimes, deal.Moneyness,
                                                        fKnownPrices, fAssetPrice, fFxRate, fPayoffFxRate,
                                                        fDiscountRate, fAssetPriceVol, fQuantoCompo, fPayoffType,
                                                        factors.PathDependent);
                        cashAccumulators.Accumulate(fPayoffFxRate, tgi.Date, scale * cash);
                        result.AppendVector(tgi.Date, scale * pv * fPayoffFxRate.Get(tgi.T));
                    }
                }
            );

            result.Complete(fT);
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }
    }

    /// <summary>
    /// Deal base class for equity and commodity cliquet options.
    /// </summary>
    [Serializable]
    public abstract class CliquetOption : BaseCliquetOption
    {
        protected string fAsset    = string.Empty;
        protected string fAssetVol = string.Empty;

        protected CliquetOption()
        {
            Known_Prices = new AssetPriceFxRateList();
        }

        public double Units
        {
            get; set;
        }

        [NonMandatory]
        public AssetPriceFxRateList Known_Prices
        {
            get; set;
        }

        [NonMandatory]
        public string Payoff_Currency
        {
            get { return fPayoffCurrency; } set { fPayoffCurrency = value; }
        }

        public PayoffType Payoff_Type
        {
            get; set;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            bool payoffInAssetCurrency = string.IsNullOrEmpty(fPayoffCurrency) || fPayoffCurrency == Currency;
            Known_Prices.Validate(errors, !payoffInAssetCurrency && Payoff_Type == PayoffType.Compo, "Known prices");
        }

        /// <summary>
        /// Summary of deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1}", GetDealHelper().UnderlyingSummary(), base.Summary());
        }

        /// <summary>
        /// Number of units of asset.
        /// </summary>
        public override double GetUnits()
        {
            return Units;
        }

        /// <summary>
        /// Construct the array of known prices.
        /// </summary>
        public override double[] GetKnownPrices(DateList resetDates, PriceFactorList factors, IAssetPrice assetPrice)
        {
            bool isCompo = !string.IsNullOrEmpty(fPayoffCurrency) && fPayoffCurrency != Currency && Payoff_Type == PayoffType.Compo;
            string assetCurrency = isCompo ? fPayoffCurrency : Currency;

            double[] knownPrices;
            double[] knownFxRates;
            AssetPriceFxRateList.GetRates(resetDates, factors.BaseDate, Known_Prices, out knownPrices, out knownFxRates);
            if (isCompo)
            {
                for (int i = 0; i < knownPrices.Length; ++i)
                    knownPrices[i] *= knownFxRates[i];
            }

            FillMissingKnownPricesFromRateFixings(knownPrices, resetDates, factors, assetPrice, assetCurrency);

            return knownPrices;
        }
    }

    /// <summary>
    /// Deal class for equity cliquet options.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Equity Cliquet Option")]
    public class EquityCliquetOption : CliquetOption
    {
        public string Equity
        {
            get { return fAsset; } set { fAsset = value; }
        }

         /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Equity_Volatility
        {
            get { return fAssetVol; } set { fAssetVol = value; }
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleEquityDealHelper(Equity, Equity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Deal class for commodity cliquet options.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commodity Cliquet Option")]
    public class CommodityCliquetOption : CliquetOption
    {
        public string Commodity
        {
            get { return fAsset; } set { fAsset = value; }
        }

         /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Commodity_Volatility
        {
            get { return fAssetVol; } set { fAssetVol = value; }
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleCommodityDealHelper(Commodity, Commodity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Valuation class for equity and commodity cliquet options.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Cliquet Option Valuation")]
    public class CliquetOptionValuation : BaseCliquetOptionValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CliquetOption);
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);
            ((BaseAssetFxDealHelper)GetDealHelper()).PreValueAsset(out fAssetPrice, out fAssetPriceVol, out fBasketPricer, ref fQuantoCompo, factors);
        }
    }
}
