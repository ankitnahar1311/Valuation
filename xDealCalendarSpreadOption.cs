//----------------------------------------------------------------------------------
// <owner>
// Perukrishnen Vytelingum
// </owner>
// <summary>
// Deal and valuation classes for Calendar Spread Option on Energy futures/forward.
// </summary>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Deal class for Calendar Spread Option on Energy futures/forwards (European CSO with cash settlement).
    /// </summary>
    [Serializable, DisplayName("Energy Calendar Spread Option")]
    public class CalendarSpreadOption : Deal
    {
        /// <summary>
        /// Initializes a new instance of the CalendarSpreadOption class for a European CSO with cash settlement.
        /// </summary>
        public CalendarSpreadOption()
        {
            Reference_Type      = string.Empty;
            Reference_Vol_Type  = string.Empty;
            Sampling_Type       = string.Empty;
            Currency            = string.Empty;
            Payoff_Currency     = string.Empty;
            Discount_Rate       = string.Empty;      
        }

        /// <summary>
        /// Underlying energy product, e.g., GAS.
        /// </summary>
        public string       Reference_Type              { get; set; }

        /// <summary>
        /// Sets the identifier for the reference vol. If not set, the default Reference Type is used instead.
        /// </summary>
        [NonMandatory]
        public string       Reference_Vol_Type          { get; set; }

        /// <summary>
        /// Property specifies whether CSO is a call or a put.
        /// </summary>
        public OptionType   Option_Type                 { get; set; }

        /// <summary>
        /// Property speficies whether we are buying or selling CSO.
        /// </summary>
        public BuySell      Buy_Sell                    { get; set; }

        /// <summary>
        /// Sampling type for reference price of energy deal.
        /// </summary>
        public string       Sampling_Type               { get; set; }

        /// <summary>
        /// Settlement date of option.
        /// </summary>
        public TDate        Settlement_Date             { get; set; }

        /// <summary>
        /// Start of sampling period for first reference price.
        /// </summary>
        public TDate        Period_Start_1              { get; set; }

        /// <summary>
        /// End of sampling period for first reference price - which is also the expiry date of the CSO.
        /// </summary>
        public TDate        Period_End_1                { get; set; }

        /// <summary>
        /// Realized price for first reference price.
        /// </summary>
        public double       Realized_Average_1          { get; set; }

        /// <summary>
        /// Date for realized price for first reference price.
        /// </summary>
        public TDate        Realized_Average_Date_1 { get; set; }

        /// <summary>
        /// Start of sampling period for second reference price.
        /// </summary>
        public TDate        Period_Start_2              { get; set; }

        /// <summary>
        /// End of sampling period for second reference price - which is also the expiry date of the CSO.
        /// </summary>
        public TDate        Period_End_2                { get; set; }

        /// <summary>
        /// Realized price for second reference price.
        /// </summary>
        public double       Realized_Average_2          { get; set; }

        /// <summary>
        /// Date for realized price for second reference price.
        /// </summary>
        public TDate        Realized_Average_Date_2 { get; set; }

        /// <summary>
        /// Size of contract, e.g., 1,000 barrels for a CME CSO.
        /// </summary>
        public double       Contract_Size               { get; set; }

        /// <summary>
        /// Number of units of the underlying product in a contract.
        /// </summary>
        public double       Units                       { get; set; }

        /// <summary>
        /// Strike price of CSO. Note that we allow negative and zero strikes as well.
        /// </summary>
        public double       Strike_Price                { get; set; }

        /// <summary>
        /// Currency CSO is specified in, including strike currency.
        /// </summary>
        public string       Currency                    { get; set; }

        /// <summary>
        /// Payoff Currency.
        /// </summary>
        public string       Payoff_Currency             { get; set; }

        /// <summary>
        /// Discount Rate Currency.
        /// </summary>
        [NonMandatory]
        public string       Discount_Rate               { get; set; }
        
        /// <summary>
        /// Deal end date. Contract expires at the end of the first period.
        /// </summary>
        public override double EndDate()
        {
            return Settlement_Date;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (string.IsNullOrEmpty(Reference_Type))
            {
                AddToErrors(errors, "An invalid or no reference type has been provided.");
            }

            if (Contract_Size <= 0)
            {
                AddToErrors(errors, "The contract size must be positive.");
            }

            if (Units <= 0)
            {
                AddToErrors(errors, "The number of units must be positive.");
            }

            if (Realized_Average_1 < 0 || Realized_Average_2 < 0)
            {
                AddToErrors(errors, "Realized averages must be positive.");
            }

            if (Settlement_Date < Period_End_1)
            {
                errors.Add(ErrorLevel.Error, "Settlement date must lie on or after period end: " + Period_End_1);
            }

            if (Period_End_1 >= Period_End_2)
            {
                errors.Add(ErrorLevel.Error, "End of sampling period 2 must be after end of sampling period 1." + Period_End_2);
            }

            if (Period_End_1 < Period_Start_1)
            {
                errors.Add(ErrorLevel.Error, "Period end date must lie on or after period start: " + Period_Start_1);
            }

            if (Period_End_2 < Period_Start_2)
            {
                errors.Add(ErrorLevel.Error, "Period end date must lie on or after period start: " + Period_Start_2);
            }

            // Check that the reference price does not have a parent, e.g. SPREAD.GAS
            var dummyFactorID = new FactorID(Reference_Type);
            if (dummyFactorID.Count > 1)
            {
                errors.Add(ErrorLevel.Error, string.Format("Calendar Spread Option valid only on contracts with one suffix, e.g., ForwardPrice.GAS and not on ForwardPrice.{0}",
                    Reference_Type));
            }
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2} {3} CSO {4} {5}-{6} Strike {7}", Buy_Sell, Reference_Type, Currency, Units, 
                Option_Type, Period_End_1, Period_End_2, Strike_Price);
        }

        /// <summary>
        /// Returns the deal currency.
        /// </summary>
        public override string DealCurrency()
        {
            return !string.IsNullOrEmpty(Payoff_Currency) ? Payoff_Currency : Currency;
        }

        /// <summary>
        /// Return an enumeration of the contract periods for this deal.
        /// </summary>
        public IEnumerable<ContractPeriod> GetContractPeriods(TDate start, TDate end)
        {
            yield return new ContractPeriod(start, end);
        }
    }

    /// <summary>
    /// Valuation class for Calendar Spread Option.
    /// </summary>
    [Serializable, DisplayName("Energy Calendar Spread Option Valuation")]
    public class CalendarSpreadOptionValuation : Valuation
    {
        [NonSerialized] 
        private IReferencePrice              fReferencePrice1;   // Factors: first reference price in base.
        [NonSerialized] 
        private IReferencePrice              fReferencePrice2;   // Factors: second reference price in base.
        [NonSerialized] 
        private ReferenceVol                fReferenceVol1;     // Factors: first reference volatility in base.
        [NonSerialized] 
        private ReferenceVol                fReferenceVol2;     // Factors: second reference volatility in base.
        [NonSerialized]
        private ForwardPriceCorrelations    fCorrelations;
        [NonSerialized]
        private ForwardPriceSample          fForwardSample;
        [NonSerialized]
        private IFxRate                     fFxRate;
        [NonSerialized]
        private IFxRate                     fFxPayoffRate;
        [NonSerialized]
        private IFxRate                     fPriceFactorFxRate;
        [NonSerialized]
        private IInterestRate               fDiscountRate;
        [NonSerialized]
        private double                      fScale;             // (+1 for a buy and -1 for a sell) * Units * Contract Size
        [NonSerialized]
        private int                         fCallPutSign;       // +1 for a call and -1 for a put option.
        [NonSerialized]
        private double                      fTimeToSettlement;
        [NonSerialized]
        private double                      fTimeToExpiry;
        [NonSerialized]
        private double                      fPeriodEnd1;
        [NonSerialized]
        private double                      fPeriodEnd2;
 
        /// <summary>
        /// Return the underlying deal.
        /// </summary>
        public override Deal Deal { get; set; }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CalendarSpreadOption);
        }

        /// <summary>
        /// No strong path dependency.
        /// </summary>
        public override bool FullPricing()
        {
            return false;
        }

        /// <summary>
        /// Generic Deal Valuation - derived objects override the ValueFn() method.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            var deal                        = (CalendarSpreadOption)Deal;
            var tgi                         = new TimeGridIterator(fT);
            PVProfiles result               = valuationResults.Profile;
            CashAccumulators accumulator    = valuationResults.Cash;

            fTimeToSettlement               = CalcUtils.DaysToYears(deal.Settlement_Date  - factors.BaseDate);
            fPeriodEnd1                     = CalcUtils.DaysToYears(deal.Period_End_1     - factors.BaseDate);
            fPeriodEnd2                     = CalcUtils.DaysToYears(deal.Period_End_2     - factors.BaseDate);

            // The time to expiry is the time to the end of the first expiring contract.
            fTimeToExpiry                   = CalcUtils.DaysToYears(deal.Period_End_1     - factors.BaseDate);

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                EnergySpreadOptionPricer pricer1 = GetEnergyOptionPricer(fForwardSample,
                    fReferencePrice1,
                    fReferenceVol1,
                    factors.BaseDate,
                    fPeriodEnd1,
                    deal.Period_Start_1,
                    deal.Period_End_1,
                    deal.Realized_Average_1,
                    deal.Realized_Average_Date_1,
                    cache);

                EnergySpreadOptionPricer pricer2 = GetEnergyOptionPricer(fForwardSample,
                    fReferencePrice2,
                    fReferenceVol2,
                    factors.BaseDate,
                    fPeriodEnd2,
                    deal.Period_Start_2,
                    deal.Period_End_2,
                    deal.Realized_Average_2,
                    deal.Realized_Average_Date_2,
                    cache);

                Vector pv               = cache.Get();
                Vector realizedPrice1   = cache.Get();
                Vector realizedPrice2   = cache.Get();
                Vector unrealizedPrice1 = cache.Get();
                Vector unrealizedPrice2 = cache.Get();
                Vector vol1             = cache.Get();
                Vector vol2             = cache.Get();
                Vector correlation      = cache.Get();
                Vector discountFactor   = cache.Get();

                // Calculate correlation between forward prices for two different (dereferenced) maturities (e.g. Apr2012 and Jan2013).
                TDate forwardDate1 = fReferencePrice1.GetPriceDate(deal.Period_End_1, 0);
                TDate forwardDate2 = fReferencePrice2.GetPriceDate(deal.Period_End_2, 0);
                fCorrelations.GetValue(correlation, tgi.T, forwardDate1, forwardDate2);

                while (tgi.Next())
                {
                    pv.Clear();

                    if (tgi.T <= fTimeToExpiry)
                    {
                        // Get unrealised reference forward prices (which also age the pricer to time t and update the realized average at time t)
                        pricer1.GetUnrealizedPrice(tgi.T, unrealizedPrice1);
                        pricer2.GetUnrealizedPrice(tgi.T, unrealizedPrice2);

                        // Get the realized averages for both reference prices.
                        pricer1.GetRealizedPrice(realizedPrice1);
                        pricer2.GetRealizedPrice(realizedPrice2);

                        int numSamples1 = pricer1.GetNumSamples();
                        int numRealizedSamples1 = pricer1.GetNumRealizedSamples();
                        int numUnrealizedSamples1 = pricer1.GetNumUnrealizedSamples();

                        int numSamples2 = pricer2.GetNumSamples();
                        int numRealizedSamples2 = pricer2.GetNumRealizedSamples();
                        int numUnrealizedSamples2 = pricer2.GetNumUnrealizedSamples();

                        // Modify the strike
                        Vector kStar            = cache.Get(deal.Strike_Price 
                            - realizedPrice1 * numRealizedSamples1 / numSamples1 
                            + realizedPrice2 * numRealizedSamples2 / numSamples2);
                        Vector refPriceStar1    = cache.Get(unrealizedPrice1 * numUnrealizedSamples1 / numSamples1);
                        Vector refPriceStar2 = cache.Get(unrealizedPrice2 * numUnrealizedSamples2 / numSamples2);

                        // Get ATM volatilities of the forward price at different maturities (given as time in years with respect to base date).
                        pricer1.GetVol(tgi.T, unrealizedPrice2, vol1);
                        pricer2.GetVol(tgi.T, unrealizedPrice1, vol2);

                        // value is intrinsic - pricing with volatility 0 and realized price if there are no future sample.
                        if (numUnrealizedSamples1 == 0)
                            vol1.Clear();

                        // value is intrinsic - pricing with volatility 0 and realized price if there are no future sample.
                        if (numUnrealizedSamples2 == 0)
                            vol2.Clear();

                        // CSO pricing: exp(-rT) * E{max(F1(T) - F2(T) - K, 0)}
                        // For the European CSO,  we set multiplier1 = sign, multiplier2 = -sign, constant = -sign * strike, 
                        // where sign = +1 for a call and -1 for a put in the SpreadOptionBS function.
                        double rootTime = Math.Sqrt(fTimeToExpiry - tgi.T);
                        Vector volStar1 = cache.Get(vol1 * rootTime);
                        Vector volStar2 = cache.Get(vol2 * rootTime);
                        PricingFunctions.SpreadOptionBS(pv, fCallPutSign, -fCallPutSign, cache.Get(-fCallPutSign * kStar), fCallPutSign,
                            refPriceStar1, refPriceStar2, kStar, volStar1, volStar2, correlation);

                        // The option itself cannot be worth less than zero (for long positions).
                        // However, due to that the Bjerksund & Stensland is a type of non-optimal exercise, it is possible to end up with negative PV via
                        // its pricing formula.
                        pv.AssignMax(pv, 0.0);

                        // Discount payment made at settlement date
                        pv.MultiplyBy(fScale * fDiscountRate.Get(tgi.T, fTimeToSettlement));
                    }
                    
                    // Cash settlement at settlement date
                    if (tgi.T == fTimeToSettlement && accumulator != null && !accumulator.Ignore)
                    {
                        accumulator.Accumulate(fFxPayoffRate, tgi.Date, pv);
                    }

                    result.AppendVector(tgi.Date, pv * fFxPayoffRate.Get(tgi.T));
                }

                result.Complete(fT);
            }
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal        = (CalendarSpreadOption)Deal;

            fCallPutSign    = deal.Option_Type  == OptionType.Call  ?   1     : -1;
            fScale          = (deal.Buy_Sell    == BuySell.Buy      ?   +1.0  : -1.0) * deal.Units * deal.Contract_Size;

            // Add to valuation time grid
            fT.AddPayDate(deal.Settlement_Date, requiredResults.CashRequired());
        }

        /// <summary>
        /// Register required price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            var deal = (CalendarSpreadOption)Deal;

            // Register forward price factor - using a reference price.
            var referencePrice = factors.RegisterInterface<IReferencePrice>(deal.Reference_Type);

            // Register volatility price factor based on an explicit user-defined property.
            // Default to Reference Type if Reference Vol Type is not set.
            if (string.IsNullOrEmpty(deal.Reference_Vol_Type))
            {
                factors.Register<ReferenceVol>(deal.Reference_Type);
            }
            else
            {
                factors.Register<ReferenceVol>(deal.Reference_Vol_Type);
            }

            // Register FX rate price factors.
            factors.RegisterInterface<IFxRate>(deal.Currency);
            factors.RegisterInterface<IFxRate>(deal.DealCurrency());
            factors.RegisterInterface<IFxRate>(factors.BaseCcyCode);
            factors.RegisterInterface<IFxRate>(referencePrice.DomesticCurrency());

            // Register correlation price factor.
            factors.Register<ForwardPriceCorrelations>(referencePrice.GetForwardPrice());

            // Register forward price sample price factor for reference prices.
            var sample = factors.Register<ForwardPriceSample>(deal.Sampling_Type);

            if (!string.IsNullOrWhiteSpace(sample.Sampling_Convention))
            {
                sample.Prepare();

                // Validate period 1.
                IEnumerable<ContractPeriod> contractPeriods = deal.GetContractPeriods(deal.Period_Start_1, deal.Period_End_1);
                sample.ValidateRange(contractPeriods, "Set 1 of sample dates", deal, errors);

                // Validate period 2.
                contractPeriods = deal.GetContractPeriods(deal.Period_Start_2, deal.Period_End_2);
                sample.ValidateRange(contractPeriods, "Set 2 of sample dates", deal, errors);
            }

            // Register interest rate price factor to get discount factor.
            factors.RegisterInterface<IInterestRate>(deal.Currency);

            // Register interest rate price factor for discount rate currency.
            if (!string.IsNullOrEmpty(deal.Discount_Rate))
            {
                string discountRateCurrency = factors.RegisterInterface<IInterestRate>(InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency)).GetCurrency();
                if (!string.IsNullOrEmpty(discountRateCurrency) && discountRateCurrency != deal.Currency)
                {
                    errors.Add(ErrorLevel.Error, "Currency and currency of Discount_Rate must be the same");
                }
            }
        }

        /// <summary>
        /// Returns the energy option pricer.
        /// </summary>
        protected virtual EnergySpreadOptionPricer GetEnergyOptionPricer(ForwardPriceSample forwardSample, IReferencePrice referencePrice, ReferenceVol referenceVol, double baseDate, double tExpiry, double periodStart, double periodEnd, double realisedAverage, TDate realisedAverageDate, VectorScopedCache.Scope cache)
        {
            // Price samples
            List<TDate> priceSamples = forwardSample.GetSamplesList(periodStart, periodEnd).ToList();

            return new EnergySpreadOptionPricer(
                    baseDate,
                    tExpiry,
                    priceSamples,
                    forwardSample.Offset,
                    realisedAverage,
                    realisedAverageDate,
                    referencePrice,
                    referenceVol,
                    fFxRate,
                    fPriceFactorFxRate,
                    fFxPayoffRate,
                    cache);
        }

        /// <summary>
        /// Get price factors.
        /// </summary>
        private void PreValue(PriceFactorList factors)
        {
            var deal                = (CalendarSpreadOption)Deal;

            // Get forward price samples.
            fForwardSample          = factors.Get<ForwardPriceSample>(deal.Sampling_Type);

            // Get ReferencePrice price factors.
            fReferencePrice1        = factors.GetInterface<IReferencePrice>(deal.Reference_Type);
            fReferencePrice2        = factors.GetInterface<IReferencePrice>(deal.Reference_Type);

            // Get ReferenceVol price factors.
            // Default to Reference Type if Reference Vol Type is not set.
            if (string.IsNullOrEmpty(deal.Reference_Vol_Type))
            {
                fReferenceVol1      = factors.Get<ReferenceVol>(deal.Reference_Type);
                fReferenceVol2      = factors.Get<ReferenceVol>(deal.Reference_Type);
            }
            else
            {
                fReferenceVol1      = factors.Get<ReferenceVol>(deal.Reference_Vol_Type);
                fReferenceVol2      = factors.Get<ReferenceVol>(deal.Reference_Vol_Type);
            }

            // Get correlation price factor based on the ID of the forward price.
            fCorrelations           = factors.Get<ForwardPriceCorrelations>(fReferencePrice1.GetForwardPrice());

            // Get FX rate price factors.
            fFxRate                 = factors.GetInterface<IFxRate>(deal.Currency);
            fFxPayoffRate           = factors.GetInterface<IFxRate>(deal.DealCurrency());
            fPriceFactorFxRate      = factors.GetInterface<IFxRate>(fReferencePrice1.DomesticCurrency());

            // Get discount rate price factor.
            fDiscountRate           = factors.GetInterface<IInterestRate>(InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency));
        }
    }
}