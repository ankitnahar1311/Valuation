/// <author>
/// Alastair Wilkins, Sebastian Steinfeld, Andy Hudson
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Class definition for forward deals with average strike and average rates,
/// where the underlying itself can be a forward of fixed tenor.
/// </summary>
using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Base deal class for average strike forwards.
    /// </summary>
    [Serializable]
    public abstract class AverageForwardExplicitDealBase : AssetDeal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AverageForwardExplicitDealBase"/> class.
        /// </summary>
        protected AverageForwardExplicitDealBase()
        {
            Sampling_Data = new SamplingEntryList<SamplingEntryAsset>();
        }

        /// <summary>
        /// Gets or sets buy / sell.
        /// </summary>
        public BuySell Buy_Sell
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the sampling data.
        /// </summary>
        public SamplingEntryList<SamplingEntryAsset> Sampling_Data
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
        /// Gets or sets the tenor.
        /// </summary>
        public Period Tenor
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the holiday calendars.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

        /// <summary>
        /// Get termination date for the deal.
        /// </summary>
        /// <returns>Deal end date</returns>
        public override double EndDate()
        {
            return Maturity_Date;
        }

        /// <summary>
        /// Validate deal parameters.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            Sampling_Data.Validate(errors, false, true);

            int nSamples = Sampling_Data.Count;
            if (nSamples > 0 && Sampling_Data[nSamples - 1].Date > Maturity_Date)
            {
                AddToErrors(errors, "last sample date in Sampling_Data cannot be after the Maturity_Date");
            }

            if (Tenor < 0)
                AddToErrors(errors, "Underlying forward tenor can not be negative");

            if (GetUnits() < 0.0)
                AddToErrors(errors, ErrorLevel.Warning, "Units/Underlying_Amount is negative");
        }

        /// <summary>
        /// Get number of units by which deal pv should be scaled.
        /// </summary>
        /// <returns>Number of units</returns>
        public abstract double GetUnits();
    }

    /// <summary>
    /// Base valuation class for average rate and average strike deals.
    /// </summary>
    [Serializable]
    public abstract class AverageForwardValuationBase : AssetValuation
    {
        protected double                                fScale                  = 0.0;
        [NonSerialized] protected IAssetPrice           fBaseAssetPrice         = null;
        protected double[]                              fSamplingTimes          = null;
        protected double[]                              fSamplingTimesPlusTenor = null;
        [NonSerialized] protected ISpotProcessVol       fAssetPriceVol          = null;
        protected SamplingEntryList<SamplingEntryAsset> fSamplingData           = null;

        /// <summary>
        /// Gets or sets the deal.
        /// </summary>
        public override Deal Deal
        {
            get
            {
                return fDeal;
            }
            set
            {
                fDeal = (AverageForwardExplicitDealBase)value;
            }
        }

        /// <summary>
        /// Full pricing always required as this deal is strongly path-dependent.
        /// </summary>
        public override bool FullPricing()
        {
            return true;
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.HeadNodeInitialize(factors, baseTimes, resultsRequired);
            var deal = (AverageForwardExplicitDealBase)Deal;

            fScale = (deal.Buy_Sell == BuySell.Buy ? +1 : -1) * deal.GetUnits();

            fSamplingTimes = new double[deal.Sampling_Data.Count];
            fSamplingTimesPlusTenor = new double[deal.Sampling_Data.Count];

            int index = 0;
            Term tenorAsTerm = Period.ValueToTerm(deal.Tenor);

            // Loop over sampling dates and generate relevant sampling times.
            foreach (SamplingEntryAsset sample in deal.Sampling_Data)
            {
                double endDate = DateAdjuster.Add(sample.Date, tenorAsTerm, deal.GetHolidayCalendar());
                double sampleTime = CalcUtils.DaysToYears(sample.Date - factors.BaseDate);

                // Store the start time and the end time.
                fSamplingTimes[index] = sampleTime; // Discount Factor and Forward Factor times are in Act365.
                fSamplingTimesPlusTenor[index] = CalcUtils.DaysToYears(endDate - factors.BaseDate);

                index++;
            }

            // Create a deep copy of the sampling data list and replace missing values with data from the rate fixings file
            var assetPrice = ((BaseAssetFxDealHelper)deal.GetDealHelper()).GetAssetPrice(factors);
            string assetCurrency = fPayoffType == PayoffType.Compo ? fPayoffCurrency : fCurrency;
            fSamplingData = deal.Sampling_Data.FillMissingDataFromFixings(factors.RateFixings, factors, assetPrice, assetCurrency, deal, "calculation of asset average");

            // Add to valuation time grid
            fT.AddPayDate(deal.Maturity_Date, resultsRequired.CashRequired());
        }

        /// <summary>
        /// Initialise valuation object ready for valuation.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);
            ((BaseAssetFxDealHelper)GetDealHelper()).PreValueAsset(out fBaseAssetPrice, out fAssetPriceVol, out fBasketPricer, ref fQuantoCompo, factors);
        }

        /// <summary>
        /// Value the deal on all dates within the valuation grid (vectorised).
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            var deal = (AverageForwardExplicitDealBase)Deal;
            double tMaturity = CalcUtils.DaysToYears(deal.Maturity_Date - factors.BaseDate); // Discount Factor and Forward Factor times are in Act365.
            int numSamplingDates = fSamplingTimes.Length;
            PreValue(factors);

            PVProfiles result = valuationResults.Profile;
            CashAccumulators cash = valuationResults.Cash;
            var tgi = new TimeGridIterator(fT);

            using (var pricerCache = Vector.Cache(factors.NumScenarios))
            {
                double  historicalObservations;
                double  futureObservations;
                int     currentDateCount;
                Vector  realisedSum             = pricerCache.GetClear(); // sum to value date.
                Vector forecastSum              = pricerCache.Get();
                Vector spotPrice                = pricerCache.Get();
                Vector forwardPrice             = pricerCache.Get();

                double tSpotPrice = double.NegativeInfinity;
                GetForwardPrice(0.0, deal.Tenor, true, ref tSpotPrice, spotPrice, forwardPrice);
                fSamplingData.GetInitialSum(null, factors.BaseDate, forwardPrice, realisedSum, out historicalObservations, out futureObservations, out currentDateCount);

                VectorEngine.For(tgi, () =>
                    {
                        using (var cache = Vector.Cache(factors.NumScenarios))
                        {
                            Vector overallAverage = cache.Get();
                            Vector value          = cache.Get();
                            Vector payoffRate     = cache.Get();

                            UpdateSum(tgi.Date, realisedSum, ref tSpotPrice, spotPrice, ref historicalObservations, ref futureObservations, ref currentDateCount);
                            forecastSum.Clear();

                            // all the sampling dates that are in the future (compared to our valuation date)
                            VectorEngine.For(currentDateCount, numSamplingDates, i =>
                                {
                                    GetForwardPrice(tgi.T, fSamplingTimesPlusTenor[i], false, ref tSpotPrice, spotPrice,
                                                    forwardPrice);
                                    forecastSum.AddProduct(fSamplingData[i].Weight, forwardPrice);

                                    return LoopAction.Continue;
                                });

                            forecastSum.MultiplyBy(spotPrice);
                            double totalWeight = historicalObservations + futureObservations;
                            if (totalWeight > 0.0)
                                overallAverage.Assign((realisedSum + forecastSum) / totalWeight);
                            else
                                overallAverage.Clear();

                            PayoffRate(payoffRate, overallAverage, ref tSpotPrice, spotPrice, tgi.T, tMaturity);
                            value.Assign(fScale * fDiscountRate.Get(tgi.T, tMaturity) * payoffRate);

                            if (tgi.Date == deal.Maturity_Date)
                                cash.Accumulate(fPayoffFxRate, tgi.Date, value);

                            result.AppendVector(tgi.Date, value * fPayoffFxRate.Get(tgi.T));
                        }
                    });

                result.Complete(fT);
            }
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Calculates either forwardPrice(t, tMaturity) when includeSpot == true, or 
        /// forwardPrice(t, tMaturity) / spotPrice(t) when includeSpot == false.
        /// Also calculates spotPrice(t) when called with t != tSpotPrice.
        /// </summary>
        /// <remarks>Includes Compo and Quanto adjustments.</remarks>
        protected void GetForwardPrice(double t, double tMaturity, bool includeSpot, ref double tSpotPrice, Vector spotPrice, Vector forwardPrice)
        {
            if (t != tSpotPrice)
            {
                // Calculate new spotPrice and update tSpotPrice
                if (fPayoffCurrency != fCurrency && fPayoffType == PayoffType.Compo)
                    spotPrice.Assign(fBaseAssetPrice.Get(t) / fPayoffFxRate.Get(t));
                else
                    spotPrice.Assign(fBaseAssetPrice.Get(t) / fFxRate.Get(t));

                tSpotPrice = t;
            }

            forwardPrice.Assign(fBaseAssetPrice.ForwardFactor(t, tMaturity, fFxRate));
            if (includeSpot)
                forwardPrice.MultiplyBy(spotPrice);

            if (fPayoffCurrency != fCurrency)
            {
                if (fPayoffType == PayoffType.Compo)
                {
                    // Composite price: forward equity price * forward FX rate
                    fQuantoCompo.AdjustForwardFactor(forwardPrice, t, tMaturity, fFxRate, fPayoffFxRate);
                }
                else if (fPayoffType == PayoffType.Quanto)
                {
                    // Quanto-adjusted forward equity price
                    fQuantoCompo.AdjustForwardPrice(forwardPrice, t, tMaturity, fAssetPriceVol);
                }
            }
        }

        /// <summary>
        /// Update sum to date.
        /// </summary>
        protected void UpdateSum(double valueDate, Vector sum, ref double tSpotPrice, Vector spotPrice, ref double n, ref double m, ref int counter)
        {
            var deal = (AverageForwardExplicitDealBase)fDeal;
            if (counter == fSamplingData.Count) return;

            using (var cache = Vector.CacheLike(sum))
            {
                Vector forwardPrice = cache.Get();

                while (counter < fSamplingData.Count && fSamplingData[counter].Date <= valueDate)
                {
                    double tSample          = fSamplingTimes[counter];
                    double tSamplePlusTenor = fSamplingTimesPlusTenor[counter];
                    double weight           = fSamplingData[counter].Weight;

                    GetForwardPrice(tSample, tSamplePlusTenor, true, ref tSpotPrice, spotPrice, forwardPrice);
                    sum.Add(weight * forwardPrice);
                    n += weight;
                    m -= weight;
                    ++counter;
                }
            }
        }

        /// <summary>
        /// Calculate payoff rate, which is either price(T) - averagePrice - Spread for average strike deals, or
        /// averagePrice - Forward_Price for average rate deals.
        /// </summary>
        protected abstract void PayoffRate(Vector vout, Vector overallAverage, ref double tSpotPrice, Vector spotPrice, double t, double tMaturity);
    }

    /// <summary>
    /// Average strike deal base class.
    /// </summary>
    [Serializable]
    public abstract class AverageStrikeExplicitDealBase : AverageForwardExplicitDealBase
    {
        /// <summary>
        /// Gets or sets the spread.
        /// </summary>
        public double Spread
        {
            get; set;
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2} {3} {4}", Buy_Sell, GetDealHelper().UnderlyingSummary(), Currency, GetUnits(), Maturity_Date);
        }
    }

    /// <summary>
    /// Average rate deal base class.
    /// </summary>
    [Serializable]
    public abstract class AverageRateExplicitDealBase : AverageForwardExplicitDealBase
    {
        /// <summary>
        /// Gets or sets the forward price.
        /// </summary>
        public double Forward_Price
        {
            get; set;
        }

        /// <summary>
        /// Summary of deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2} Average Rate Forward {3} {4} {5}", Buy_Sell, GetDealHelper().UnderlyingSummary(), Currency, Forward_Price, Tenor, Maturity_Date);
        }
    }

    /// <summary>
    /// Average strike valuation base class.
    /// </summary>
    [Serializable]
    public abstract class AverageStrikeValuationBase : AverageForwardValuationBase
    {
        /// <summary>
        /// Calculate payoff rate, which is either price(T) - averagePrice - Spread for average strike deals, or
        /// averagePrice - Forward_Price for average rate deals.
        /// </summary>
        protected override void PayoffRate(Vector vout, Vector overallAverage, ref double tSpotPrice, Vector spotPrice, double t, double tMaturity)
        {
            var deal = (AverageStrikeExplicitDealBase)fDeal;
            GetForwardPrice(t, tMaturity, true, ref tSpotPrice, spotPrice, vout);
            vout.Subtract(overallAverage + deal.Spread);
        }
    }

    /// <summary>
    /// Average rate valuation base class
    /// </summary>
    [Serializable]
    public abstract class AverageRateValuationBase : AverageForwardValuationBase
    {
        /// <summary>
        /// Calculate payoff rate, which is either price(T) - averagePrice - Spread for average strike deals, or
        /// averagePrice - Forward_Price for average rate deals.
        /// </summary>
        protected override void PayoffRate(Vector vout, Vector overallAverage, ref double tSpotPrice, Vector spotPrice, double t, double tMaturity)
        {
            var deal = (AverageRateExplicitDealBase)fDeal;
            vout.Assign(overallAverage);
            vout.Subtract(deal.Forward_Price);
        }
    }

    /// <summary>
    /// Average-strike forward with FX underlying
    /// </summary>
    [Serializable]
    [DisplayName("FX Average Strike Forward (Explicit)")]
    public class FXAverageStrikeForwardExplicit : AverageStrikeExplicitDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FXAverageStrikeForwardExplicit"/> class.
        /// </summary>
        public FXAverageStrikeForwardExplicit()
        {
            Underlying_Currency = string.Empty;
        }

        /// <summary>
        /// Gets or sets the underlying currency.
        /// </summary>
        public string Underlying_Currency
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying amount.
        /// </summary>
        public double Underlying_Amount
        {
            get; set;
        }

        /// <summary>
        /// Get number of units of underlying asset.
        /// </summary>
        /// <returns>Underlying amount</returns>
        public override double GetUnits()
        {
            return Underlying_Amount;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleFxForwardDealHelper(Underlying_Currency, Currency);
        }
    }

    /// <summary>
    /// Average-strike forward valuation with FX underlying
    /// </summary>
    [Serializable]
    [DisplayName("FX Average Strike Forward Valuation")]
    public class FXAverageStrikeForwardValuation : AverageStrikeValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of FX Average Strike Forward</returns>
        public override Type DealType()
        {
            return typeof(FXAverageStrikeForwardExplicit);
        }
    }

    /// <summary>
    /// Average-rate forward with FX underlying
    /// </summary>
    [Serializable]
    [DisplayName("FX Average Rate Forward (Explicit)")]
    public class FXAverageRateForwardExplicit : AverageRateExplicitDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FXAverageRateForwardExplicit"/> class.
        /// </summary>
        public FXAverageRateForwardExplicit()
        {
            Underlying_Currency = string.Empty;
        }

        /// <summary>
        /// Gets or sets the underlying currency.
        /// </summary>
        public string Underlying_Currency
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying amount.
        /// </summary>
        public double Underlying_Amount
        {
            get; set;
        }

        /// <summary>
        /// Get number of units of underlying asset.
        /// </summary>
        /// <returns>Underlying amount</returns>
        public override double GetUnits()
        {
            return Underlying_Amount;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleFxForwardDealHelper(Underlying_Currency, Currency);
        }
    }

    /// <summary>
    /// Average-Rate forward valuation with FX underlying
    /// </summary>
    [Serializable]
    [DisplayName("FX Average Rate Forward Valuation")]
    public class FXAverageRateForwardValuation : AverageRateValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of FX Average Rate Forward</returns>
        public override Type DealType()
        {
            return typeof(FXAverageRateForwardExplicit);
        }
    }

    /// <summary>
    /// Average-strike forward with equity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Equity Average Strike Forward (Explicit)")]
    public class EquityAverageStrikeForwardExplicit : AverageStrikeExplicitDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EquityAverageStrikeForwardExplicit"/> class.
        /// </summary>
        public EquityAverageStrikeForwardExplicit()
        {
            Equity            = string.Empty;
            Equity_Volatility = string.Empty;
        }

        /// <summary>
        /// Gets or sets the equity ID.
        /// </summary>
        public string Equity
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Equity_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the payoff currency.
        /// </summary>
        public string Payoff_Currency
        {
            get { return fPayoffCurrency; } set { fPayoffCurrency = value; }
        }

        /// <summary>
        /// Gets or sets the payoff type.
        /// </summary>
        public PayoffType Payoff_Type
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
        /// Get number of units of underlying asset.
        /// </summary>
        public override double GetUnits()
        {
            return Units;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleEquityForwardDealHelper(Equity, Equity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Average-strike forward valuation with equity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Equity Average Strike Forward Valuation")]
    public class EquityAverageStrikeForwardValuation : AverageStrikeValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of Equity Average Strike Forward</returns>
        public override Type DealType()
        {
            return typeof(EquityAverageStrikeForwardExplicit);
        }
    }

    /// <summary>
    /// Average-Rate forward with equity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Equity Average Rate Forward (Explicit)")]
    public class EquityAverageRateForwardExplicit : AverageRateExplicitDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EquityAverageRateForwardExplicit"/> class.
        /// </summary>
        public EquityAverageRateForwardExplicit()
        {
            Equity            = string.Empty;
            Equity_Volatility = string.Empty;
        }

        /// <summary>
        /// Gets or sets the equity ID.
        /// </summary>
        public string Equity
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Equity_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the payoff currency.
        /// </summary>
        public string Payoff_Currency
        {
            get { return fPayoffCurrency; } set { fPayoffCurrency = value; }
        }

        /// <summary>
        /// Gets or sets the payoff type.
        /// </summary>
        public PayoffType Payoff_Type
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
        /// Get number of units of underlying asset.
        /// </summary>
        public override double GetUnits()
        {
            return Units;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleEquityForwardDealHelper(Equity, Equity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Average-Rate forward valuation with equity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Equity Average Rate Forward Valuation")]
    public class EquityAverageRateForwardValuation : AverageRateValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of Equity Average Rate Forward</returns>
        public override Type DealType()
        {
            return typeof(EquityAverageRateForwardExplicit);
        }
    }

    /// <summary>
    /// Average-strike forward with commodity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Average Strike Forward (Explicit)")]
    public class CommodityAverageStrikeForwardExplicit : AverageStrikeExplicitDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommodityAverageStrikeForwardExplicit"/> class.
        /// </summary>
        public CommodityAverageStrikeForwardExplicit()
        {
            Commodity            = string.Empty;
            Commodity_Volatility = string.Empty;
        }

        /// <summary>
        /// Gets or sets the commodity ID.
        /// </summary>
        public string Commodity
        {
            get; set;
        }

         /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Commodity_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the payoff currency.
        /// </summary>
        public string Payoff_Currency
        {
            get { return fPayoffCurrency; } set { fPayoffCurrency = value; }
        }

        /// <summary>
        /// Gets or sets the payoff type.
        /// </summary>
        public PayoffType Payoff_Type
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
        /// Get number of units of underlying asset.
        /// </summary>
        public override double GetUnits()
        {
            return Units;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleCommodityForwardDealHelper(Commodity, Commodity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Average-strike forward valuation with commodity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Average Strike Forward Valuation")]
    public class CommodityAverageStrikeForwardValuation : AverageStrikeValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of Commodity Average Strike Forward</returns>
        public override Type DealType()
        {
            return typeof(CommodityAverageStrikeForwardExplicit);
        }
    }

    /// <summary>
    /// Average-Rate forward with commodity or basket commodity underlying
    /// </summary>
    [Serializable]
    public abstract class CommodityAverageRateForwardExplicitBase : AverageRateExplicitDealBase
    {
        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        public double Units
        {
            get; set;
        }

        /// <summary>
        /// Get number of units of underlying asset.
        /// </summary>
        public override double GetUnits()
        {
            return Units;
        }
    }

    /// <summary>
    /// Average-Rate forward with commodity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Average Rate Forward (Explicit)")]
    public class CommodityAverageRateForwardExplicit : CommodityAverageRateForwardExplicitBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommodityAverageRateForwardExplicit"/> class.
        /// </summary>
        public CommodityAverageRateForwardExplicit()
        {
            Commodity            = string.Empty;
            Commodity_Volatility = string.Empty;
        }

        /// <summary>
        /// Gets or sets the commodity ID.
        /// </summary>
        public string Commodity
        {
            get; set;
        }

         /// <summary>
        /// Gets or sets the equity volatility ID.
        /// </summary>
        public string Commodity_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the payoff currency.
        /// </summary>
        public string Payoff_Currency
        {
            get { return fPayoffCurrency; } set { fPayoffCurrency = value; }
        }

        /// <summary>
        /// Gets or sets the payoff type.
        /// </summary>
        public PayoffType Payoff_Type
        {
            get; set;
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleCommodityForwardDealHelper(Commodity, Commodity_Volatility, Currency, Payoff_Currency, Payoff_Type);
        }
    }

    /// <summary>
    /// Average-Rate forward with commodity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Basket Average Rate Forward (Explicit)")]
    public class CommodityBasketAverageRateForwardExplicit : CommodityAverageRateForwardExplicitBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommodityBasketAverageRateForwardExplicit"/> class.
        /// </summary>
        public CommodityBasketAverageRateForwardExplicit()
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
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new CommodityBasketPricesForwardDealHelper<CommodityBasketPricesComponent>(Basket, Currency);
        }
    }

    /// <summary>
    /// Average-Rate forward valuation with commodity underlying
    /// </summary>
    [Serializable]
    [DisplayName("Commodity Average Rate Forward Valuation")]
    public class CommodityAverageRateForwardValuation : AverageRateValuationBase
    {
        /// <summary>
        /// Get deal type associated with this valuation.
        /// </summary>
        /// <returns>Type of Commodity Average Rate Forward</returns>
        public override Type DealType()
        {
            return typeof(CommodityAverageRateForwardExplicitBase);
        }
    }
}