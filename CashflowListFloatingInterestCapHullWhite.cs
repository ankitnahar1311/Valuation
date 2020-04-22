using System;
using System.ComponentModel;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Valuation model for caps, priced using the 1 factor Hull White model.
    /// </summary>
    [Serializable]
    [DisplayName("Hull White Cap Valuation")]
    public class CapHullWhiteValuation : CFListBaseValuation<CFFloatingInterestList>
    {
        /// <summary>
        /// The cashflows that will be used in valuation. This is either the cashflows from the 
        /// original deal or a modified clone of them.
        /// </summary>
        protected CFFloatingInterestList fCashflows;

        private HullWhite1FactorModelParameters fModelParameters;
        private string fModelParametersId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapHullWhiteValuation"/> class.
        /// </summary>
        public CapHullWhiteValuation()
        {
            Model_Parameters = string.Empty;
        }

        /// <summary>
        /// ID of the Hull White model parameters that the valuation model will use.
        /// </summary>
        public string Model_Parameters
        {
            get; set;
        }

        /// <inheritdoc />
        public override void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate)
        {
            fCashflows.CollectCashflows(cashAccumulators, fFxRate, null, baseDate, endDate, fBuySellSign, fForecastRate, fCutoffDate);
        }

        /// <inheritdoc />
        public override Type DealType()
        {
            return typeof(CFFloatingInterestListDeal);
        }

        /// <summary>
        /// Clones the cashflow list and applies missing fixings from the fixings file.  If 
        /// fixings are applied, the clone is stored in place of the original deal.
        /// </summary>
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);

            var baseDate = factors.BaseDate;
            var deal = (CFFloatingInterestListDeal)fDeal;
            fCashflows = deal.Cashflows;

            // Apply any missing rate fixings, performing minimal cloning
            if (fCashflows.HasMissingRates(baseDate))
                fCashflows = CashflowsFixingsHelper.ApplyRateFixings(factors, deal, fCashflows);
        }

        /// <inheritdoc />
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            fModelParameters = factors.Get<HullWhite1FactorModelParameters>(fModelParametersId);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            var deal = (CFFloatingInterestListDeal)Deal;

            if (!IsValidCashflowList(factors, deal))
                Deal.AddToErrors(errors, ErrorLevel.Error, string.Format("{0} is for cashflow lists consisting of vanilla swaplets, caplets and floorlets", this.DisplayName()));

            var rateIDs = SetCurrencyAndGetRateIds();

            fModelParametersId = string.IsNullOrWhiteSpace(Model_Parameters) ? rateIDs.ForecastId1 : Model_Parameters;

            factors.Register<HullWhite1FactorModelParameters>(fModelParametersId);
        }

        /// <summary>
        /// Value a caplet or floorlet under the 1 factor Hull-White model.
        /// </summary>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            int count = fCashflows.Count();

            bool forecastIsDiscount = ReferenceEquals(fForecastRate, fDiscountRate);

            // time of dfStart and dfEnd
            double tDfStart = double.NegativeInfinity;
            double tDfEnd = double.NegativeInfinity;
            
            using (var cache = Vector.CacheLike(pv))
            {
                // Shared between loops
                Vector dfStart = cache.Get();
                Vector dfEnd = cache.Get();

                VectorEngine.For(0, count, LoopDirection.Backwards, i =>
                    {
                        using (var innerCache = Vector.CacheLike(pv))
                        {
                            CFFloatingInterest cashflow = fCashflows[i];

                            if (cashflow.Payment_Date < valueDate || cashflow.Payment_Date <= fCutoffDate)
                                return LoopAction.Break;

                            Vector rate = innerCache.Get();
                            Vector dfPay = innerCache.Get();
                            Vector stdDev = innerCache.GetClear();
                            Vector amount = innerCache.GetClear();

                            GeneralCashflowProperties properties = fCashflows.GetCashflowProperties(i);

                            double tPay = CalcUtils.DaysToYears(cashflow.Payment_Date - baseDate);
                            bool haveDfPay = false;
                            if (forecastIsDiscount && tPay == tDfStart)
                            {
                                dfPay.Assign(dfStart);
                                haveDfPay = true;
                            }

                            using (IntraValuationDiagnosticsHelper.StartCashflow(intraValuationDiagnosticsWriter))
                            using (var volatilitiesAtDateStore = IntraValuationDiagnosticsHelper.CreateVolatilitiesAtDateStore(intraValuationDiagnosticsWriter, pv.Count))
                            {
                                cashflow.AddPropertiesToIntraValuationDiagnostics(intraValuationDiagnosticsWriter);

                                // Standard Libor implies single reset.
                                var reset = cashflow.Resets.Single();

                                if (reset.IsKnown(baseDate))
                                {
                                    rate.Assign(reset.Known_Rate);
                                }
                                else
                                {
                                    double tValue = CalcUtils.DaysToYears(valueDate - baseDate);
                                    double tReset = CalcUtils.DaysToYears(reset.Reset_Date - baseDate);
                                    double tStart = CalcUtils.DaysToYears(reset.Rate_Start_Date - baseDate);
                                    double tEnd = CalcUtils.DaysToYears(reset.Rate_End_Date - baseDate);

                                    // Reset is a historical or forward Libor rate.
                                    InterestRateUtils.LiborRate(rate, fForecastRate, tValue, tReset, tStart, tEnd, reset.Rate_Year_Fraction,
                                                                dfStart, ref tDfStart, dfEnd, ref tDfEnd);

                                    if (tReset > tValue)
                                    {
                                        GetStandardDeviation(stdDev, tValue, tReset, tStart, tEnd);
                                        volatilitiesAtDateStore.Add(valueDate, reset.Reset_Date, stdDev);
                                    }
                                }

                                if (!haveDfPay && forecastIsDiscount && tPay == tDfEnd)
                                {
                                    dfPay.Assign(dfEnd);
                                    haveDfPay = true;
                                }

                                // Add swaplet value
                                amount.AddProduct(properties.Swap_Multiplier, rate);

                                double tau = reset.Rate_Year_Fraction;
                                rate.Assign(1.0 + rate * tau);

                                // Add cap and floor option values.
                                AddOptionValue(amount, OptionType.Call, rate, properties.Cap_Strike, stdDev, tau, properties.Cap_Multiplier);
                                AddOptionValue(amount, OptionType.Put, rate, properties.Floor_Strike, stdDev, tau, properties.Floor_Multiplier);

                                amount.Assign(fBuySellSign * (cashflow.Fixed_Amount + cashflow.Notional * (amount + cashflow.Margin) * cashflow.Accrual_Year_Fraction));

                                IntraValuationDiagnosticsHelper.AddImpliedVolatilities(intraValuationDiagnosticsWriter, volatilitiesAtDateStore);
                                CFFixedList.RoundCashflow(amount, Cashflow_Rounding);
                                CFFixedList.UpdatePvAndCash(cashflow, baseDate, valueDate, haveDfPay ? null : fDiscountRate, null, amount,
                                                            dfPay, pv, cash, intraValuationDiagnosticsWriter);
                            }
                        }

                        return LoopAction.Continue;
                    });
            }
        }

        /// <summary>
        /// Adds the cap or floor option value.
        /// </summary>
        private static void AddOptionValue(Vector amount, OptionType optionType, Vector rate, double strike, Vector stdDev, double tau, double multiplier)
        {
            if (multiplier != 0.0 && tau > 0.0)
            {
                double optionStrike = 1.0 + tau * strike;
                amount.AddProduct(multiplier / tau, PricingFunctions.BlackFunction(optionType, rate, optionStrike, stdDev));
            }
        }

        /// <summary>
        /// Calculates the standard deviation under the 1 factor Hull White model.
        /// </summary>
        private void GetStandardDeviation(Vector stdDev, double t, double tExpiry, double tStart, double tEnd)
        {
            using (var cache = Vector.CacheLike(stdDev))
            {
                Vector b1Vector = cache.Get();
                Vector b2Vector = cache.Get();
                Vector bDiff = cache.Get();

                fModelParameters.GetB(b2Vector, t, tEnd);
                fModelParameters.GetB(b1Vector, t, tStart);

                bDiff.AssignDifference(b2Vector, b1Vector);
                fModelParameters.GetZeta(stdDev, t, tExpiry);
                stdDev.AssignSqrt(stdDev);
                stdDev.MultiplyBy(VectorMath.Abs(bDiff));
            }
        }

        /// <summary>
        /// Returns true if the cashflow list consists of vanilla swaplets, caplets and floorlets.
        /// </summary>
        private bool IsValidCashflowList(PriceFactorList factors, CFFloatingInterestListDeal deal)
        {
            // Get characteristics using cashflow list from the original deal.
            CashflowListCharacteristics characteristics = deal.Cashflows.Analyze(factors.BaseDate);
            bool isCompounding = deal.Cashflows.Compounding_Method != CompoundingMethod.None;

            return !fForecastIsForeign &&
                   characteristics.IsStandardPayoff &&
                   characteristics.IsStandardLibor &&
                   !isCompounding;
        }
    }
}
