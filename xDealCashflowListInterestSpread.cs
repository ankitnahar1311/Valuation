using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Interest rate spread cashflow list deal.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Spread Cashflow List")]
    public class CFGeneralInterestSpreadListDeal : CFListBaseDeal<CFGeneralInterestSpreadList>
    {
        private const int AccrualHolidayCalendarIndex = 0;
        private const int Rate1HolidayCalendarIndex = 1;
        private const int Rate2HolidayCalendarIndex = 2;

        /// <summary>
        /// Constructor for floating interest spread cashflow list deal.
        /// </summary>
        public CFGeneralInterestSpreadListDeal()
        {
            // cap and swaption vols distinct. base class provides cap vols, these are the distinct swaption vols 
            Discount_Rate_Swaption_Volatility = string.Empty;
            Discount_Rate_Cap_Volatility = string.Empty;
            Forecast_Rate1_Swaption_Volatility = string.Empty;
            Forecast_Rate1_Cap_Volatility = string.Empty;
            Forecast_Rate2_Swaption_Volatility = string.Empty;
            Forecast_Rate2_Cap_Volatility = string.Empty;

            Accrual_Calendars = string.Empty;
            Rate1_Calendars = string.Empty;
            Rate2_Calendars = string.Empty;
            Rate_Adjustment_Method = DateAdjustmentMethod.Modified_Following;
            Rate_Sticky_Month_End = YesNo.Yes;
        }

        /// <summary>
        /// Gets or sets the discount rate cap volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Discount_Rate_Cap_Volatility
        {
            get { return fDiscountVolatility; }
            set { fDiscountVolatility = value; }
        }

        /// <summary>
        /// Gets or sets the discount rate swaption volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Discount_Rate_Swaption_Volatility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the forecast interest rate1.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate1
        {
            get { return fForecast; }
            set { fForecast = value; }
        }

        /// <summary>
        /// Gets or sets the forecast interest rate2.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate2
        {
            get { return fForecast2; }
            set { fForecast2 = value; }
        }

        /// <summary>
        /// Gets or sets the rate1 cap volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate1_Cap_Volatility
        {
            get { return fForecastVolatility; }
            set { fForecastVolatility = value; }
        }

        /// <summary>
        /// Gets or sets the rate1 swaption volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate1_Swaption_Volatility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the rate2 cap volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate2_Cap_Volatility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the rate2 swaption volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate2_Swaption_Volatility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set the list of holiday calendars for interest accrual dates.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Accrual_Calendars
        {
            get { return GetCalendarNames(AccrualHolidayCalendarIndex); }
            set { SetCalendarNames(AccrualHolidayCalendarIndex, value); }
        }

        /// <summary>
        /// Gets or set the list of holiday calendars for the rate1 dates.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Rate1_Calendars
        {
            get { return GetCalendarNames(Rate1HolidayCalendarIndex); }
            set { SetCalendarNames(Rate1HolidayCalendarIndex, value); }
        }

        /// <summary>
        /// Gets or set the list of holiday calendars for the rate2 dates.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Rate2_Calendars
        {
            get { return GetCalendarNames(Rate2HolidayCalendarIndex); }
            set { SetCalendarNames(Rate2HolidayCalendarIndex, value); }
        }

        /// <summary>
        /// Number of business days between reset date and rate start date.
        /// </summary>
        [NonMandatory]
        public int Rate_Offset
        {
            get;
            set;
        }

        /// <summary>
        /// Adjustment method for rate end date calculations.
        /// </summary>
        [NonMandatory]
        public DateAdjustmentMethod Rate_Adjustment_Method
        {
            get;
            set;
        }

        /// <summary>
        /// Force rate end date to be last business day of month when rate start date is last business day of month.
        /// </summary>
        public YesNo Rate_Sticky_Month_End
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the accrual holiday calendar.
        /// </summary>
        public IHolidayCalendar AccrualHolidayCalendar()
        {
            return GetHolidayCalendar(AccrualHolidayCalendarIndex);
        }

        /// <summary>
        /// Gets the rate1 holiday calendar.
        /// </summary>
        public IHolidayCalendar Rate1HolidayCalendar()
        {
            return GetHolidayCalendar(Rate1HolidayCalendarIndex);
        }

        /// <summary>
        /// Gets the rate2 holiday calendar.
        /// </summary>
        public IHolidayCalendar Rate2HolidayCalendar()
        {
            return GetHolidayCalendar(Rate2HolidayCalendarIndex);
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            InitialiseHolidayCalendars(calendar);
            var parameters = new CashflowListDateGenerationParameters()
            {
                AccrualCalendar = AccrualHolidayCalendar(),
                RateCalendar = Rate1HolidayCalendar(),
                Rate1Calendar = Rate1HolidayCalendar(),
                Rate2Calendar = Rate2HolidayCalendar(),
                RateOffset = Rate_Offset,
                RateAdjustmentMethod = Rate_Adjustment_Method,
                RateStickyMonthEnd = Rate_Sticky_Month_End,
            };

            if (fCashflows.FinishBuild(parameters))
                AddToErrors(errors, ErrorLevel.Info, "Missing cashflow properties have been calculated (Accrual_Year_Fraction, Rate_Start_Date, Rate1_End_Date, Rate1_Year_Fraction, Rate2_End_Date, Rate2_Year_Fraction)");

            base.Validate(calendar, errors);
        }
    }

    /// <summary>
    /// Valuation class for floating interest spread cashflow list deal.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Spread Cashflow List Valuation")]
    public class CFGeneralInterestSpreadListValuation : CFListBaseValuation<CFGeneralInterestSpreadList>
    {
        [NonSerialized]
        protected IInterestRateVol fDiscountRateVol = null;
        [NonSerialized]
        protected IInterestYieldVol fDiscountYieldVol = null;
        [NonSerialized]
        protected IInterestRateVol fForecast1RateVol = null;
        [NonSerialized]
        protected IInterestYieldVol fForecast1YieldVol = null;
        [NonSerialized]
        protected IInterestRateVol fForecast2RateVol = null;
        [NonSerialized]
        protected IInterestYieldVol fForecast2YieldVol = null;
        [NonSerialized]
        protected FXVolHelper fFx1Vol = null;
        [NonSerialized]
        protected FXVolHelper fFx2Vol = null;
        [NonSerialized]
        protected CorrelationHelper fForecast1Fx1Correl = null;
        [NonSerialized]
        protected CorrelationHelper fForecast2Fx2Correl = null;
        [NonSerialized]
        protected CorrelationHelper fForecast1DiscountCorrel = null;
        [NonSerialized]
        protected CorrelationHelper fForecast2DiscountCorrel = null;
        [NonSerialized]
        protected CorrelationHelper fForecast1Forecast2Correl = null;
        [NonSerialized]
        protected CMSRateCorrelations fForecast1Forecast2Correls = null;

        protected BasisPoint fConvexityLowRateCutoff = BasisPoint.BasisPointValue;

        /// <summary>
        /// The cashflows that will be used in valuation. This is either the cashflows from the 
        /// original deal or a modified clone of them.
        /// </summary>
        private CFGeneralInterestSpreadList fCashflows;

        /// <summary>
        /// Gets or sets the Convexity_Correction valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Convexity_Correction
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Quanto_Correction valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Quanto_Correction
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Convexity_Low_Rate_Limit_Cutoff valuation model parameter.
        /// </summary>
        public BasisPoint Convexity_Low_Rate_Cutoff
        {
            get
            {
                return fConvexityLowRateCutoff;
            }
            set
            {
                if (value < CalcUtils.TINY || value > 1.0)
                    throw new ArgumentOutOfRangeException("value", "Convexity low rate cutoff cannot be less than 0.000001 bp or more than 10000 bp");

                fConvexityLowRateCutoff = value;
            }
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CFGeneralInterestSpreadListDeal);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CFGeneralInterestSpreadListDeal deal = (CFGeneralInterestSpreadListDeal)Deal;

            // Collect registered volatility price factors to check they have the same distribution type
            var volPriceFactors = new List<IInterestVol>();

            // Get spread flow characteristics
            SpreadCashflowListCharacteristics spreadCashflowCharacteristics = deal.Cashflows.ValuationPriceFactorDependencies(factors.BaseDate, fCurrency, fForecastCurrency, fForecast2Currency);

            // register volatility surfaces for forecast rate1
            if (spreadCashflowCharacteristics.NeedForecast1YieldVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestYieldVol(factors, deal.Forecast_Rate1_Swaption_Volatility, fForecastCurrency));

            if (spreadCashflowCharacteristics.NeedForecast1RateVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestRateVol(factors, deal.Forecast_Rate1_Cap_Volatility, fForecastCurrency));

            // register volatility surfaces for forecast rate2
            if (spreadCashflowCharacteristics.NeedForecast2YieldVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestYieldVol(factors, deal.Forecast_Rate2_Swaption_Volatility, fForecast2Currency));

            if (spreadCashflowCharacteristics.NeedForecast2RateVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestRateVol(factors, deal.Forecast_Rate2_Cap_Volatility, fForecast2Currency));

            // vol surfaces for discount rate
            if (spreadCashflowCharacteristics.NeedDiscountYieldVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestYieldVol(factors, deal.Discount_Rate_Swaption_Volatility, fCurrency));

            if (spreadCashflowCharacteristics.NeedDiscountRateVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency));

            bool convexity = spreadCashflowCharacteristics.NeedDiscountYieldVol || spreadCashflowCharacteristics.NeedDiscountRateVol;

            if (fForecastCurrency != fCurrency)
            {
                if (Quanto_Correction == YesNo.Yes)
                {
                    // fx vol, fx/ir correl and forecast/discount correl
                    FXVolHelper.Register(factors, fForecastCurrency, fCurrency);
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IFxRate), fForecastCurrency, fCurrency);
                }

                if (convexity)
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fCurrency, null, typeof(IInterestRate), fForecastCurrency, null);
            }

            if (fForecast2Currency != fCurrency)
            {
                if (Quanto_Correction == YesNo.Yes)
                {
                    // fx vol, fx/ir correl and forecast/discount correl
                    FXVolHelper.Register(factors, fForecast2Currency, fCurrency);
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fForecast2Currency, null, typeof(IFxRate), fForecast2Currency, fCurrency);
                }

                if (convexity)
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fCurrency, null, typeof(IInterestRate), fForecast2Currency, null);
            }

            if (spreadCashflowCharacteristics.NeedForecast1Forecast2Correlation)
            {
                if (fForecastCurrency == fForecast2Currency)
                {
                    // correl between forecast rates in same currency
                    factors.Register<CMSRateCorrelations>(fForecastCurrency);
                }
                else
                {
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IInterestRate), fForecast2Currency, null);
                }
            }

            if (volPriceFactors.Select(pf => pf.GetDistributionType()).Distinct().Count() > 1)
                Deal.AddToErrors(errors, "Volatility price factors must have the same distribution type.");

            ValidateUnnecessaryVolatilities(deal, spreadCashflowCharacteristics, errors);
        }

        /// <summary>
        /// Clones the cashflow list and applies missing fixings from the fixings file. If 
        /// fixings are applied, the clone is stored in place of the original deal.
        /// </summary>
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);

            var baseDate = factors.BaseDate;
            var deal = (CFGeneralInterestSpreadListDeal)fDeal;
            fCashflows = deal.Cashflows;

            // Apply any missing rate fixings.
            if (fCashflows.HasMissingRates(baseDate))
                fCashflows = CashflowsFixingsHelper.ApplyRateFixings(factors, deal, fCashflows);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);
            
            // Set up cashflow list
            fCashflows.SetUseConvexityCorrection(Convexity_Correction == YesNo.Yes);
            fCashflows.SetUseQuantoCorrection(Quanto_Correction == YesNo.Yes);
            fCashflows.SetConvexityLowRateCutoff(Convexity_Low_Rate_Cutoff);

            // Add to valuation time grid
            fT.AddPayDates(fCashflows);
        }

        /// <summary>
        /// Prepare for valuation.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CFGeneralInterestSpreadListDeal deal = (CFGeneralInterestSpreadListDeal)Deal;

            // Get spread flow characteristics
            SpreadCashflowListCharacteristics spreadCashflowCharacteristics = fCashflows.ValuationPriceFactorDependencies(factors.BaseDate, fCurrency, fForecastCurrency, fForecast2Currency);

            // vols for first forecast rate
            if (spreadCashflowCharacteristics.NeedForecast1YieldVol)
                fForecast1YieldVol = InterestVolBase.GetYieldVol(factors, deal.Forecast_Rate1_Swaption_Volatility, fForecastCurrency);

            if (spreadCashflowCharacteristics.NeedForecast1RateVol)
                fForecast1RateVol = InterestVolBase.GetRateVol(factors, deal.Forecast_Rate1_Cap_Volatility, fForecastCurrency);

            // vols for second forecast rate
            if (spreadCashflowCharacteristics.NeedForecast2YieldVol)
                fForecast2YieldVol = InterestVolBase.GetYieldVol(factors, deal.Forecast_Rate2_Swaption_Volatility, fForecast2Currency);

            if (spreadCashflowCharacteristics.NeedForecast2RateVol)
                fForecast2RateVol = InterestVolBase.GetRateVol(factors, deal.Forecast_Rate2_Cap_Volatility, fForecast2Currency);

            // vols for discount rate
            if (spreadCashflowCharacteristics.NeedDiscountYieldVol)
                fDiscountYieldVol = InterestVolBase.GetYieldVol(factors, deal.Discount_Rate_Swaption_Volatility, fCurrency);

            if (spreadCashflowCharacteristics.NeedDiscountRateVol)
                fDiscountRateVol = InterestVolBase.GetRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency);

            bool convexity = spreadCashflowCharacteristics.NeedDiscountYieldVol || spreadCashflowCharacteristics.NeedDiscountRateVol;

            if (fForecastCurrency != fCurrency)
            {
                if (Quanto_Correction == YesNo.Yes)
                {
                    // fx vol, fx/ir correl and forecast/discount correl
                    fFx1Vol = FXVolHelper.Get(factors, fForecastCurrency, fCurrency);
                    fForecast1Fx1Correl = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(FxRate), fForecastCurrency, fCurrency);
                }

                if (convexity)
                    fForecast1DiscountCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fCurrency, null, typeof(InterestRate), fForecastCurrency, null);
            }

            if (fForecast2Currency != fCurrency)
            {
                if (Quanto_Correction == YesNo.Yes)
                {
                    // fx vol, fx/ir correl and forecast/discount correl
                    fFx2Vol = FXVolHelper.Get(factors, fForecast2Currency, fCurrency);
                    fForecast2Fx2Correl = CorrelationHelper.Get(factors, typeof(InterestRate), fForecast2Currency, null, typeof(FxRate), fForecast2Currency, fCurrency);
                }

                if (convexity)
                    fForecast2DiscountCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fCurrency, null, typeof(InterestRate), fForecast2Currency, null);
            }

            if (spreadCashflowCharacteristics.NeedForecast1Forecast2Correlation)
            {
                if (fForecastCurrency == fForecast2Currency)
                {
                    // correl between forecast rates in same currency
                    fForecast1Forecast2Correls = factors.Get<CMSRateCorrelations>(fForecastCurrency);
                }
                else
                {
                    fForecast1Forecast2Correl = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(InterestRate), fForecast2Currency, null);
                }
            }
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        /// <param name="baseDate">Base date of the calculation.</param>
        /// <param name="valueDate">Valuation date.</param>
        /// <param name="intraValuationDiagnosticsWriter">Cashflow writer.</param>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            // pv -> fBuySellSign * (fBuySellSign * pv + cashflowListPv) = pv + fBuySellSign * cashflowListPv
            ApplySign(pv, cash, fBuySellSign);
            fCashflows.Value(pv, cash, baseDate, valueDate, fDiscountRate, fForecastRate, fForecastRate2, fDiscountRateVol,
                fDiscountYieldVol, fForecast1RateVol, fForecast1YieldVol, fForecast2RateVol, fForecast2YieldVol, fFx1Vol, fFx2Vol,
                fForecast1Fx1Correl, fForecast2Fx2Correl, fForecast1DiscountCorrel, fForecast2DiscountCorrel,
                fForecast1Forecast2Correl, fForecast1Forecast2Correls, fCutoffDate);

            ApplySign(pv, cash, fBuySellSign);
        }

        /// <summary>
        /// Collect cashflows realised along the scenario path up to endDate.
        /// </summary>
        public override void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate)
        {
            fCashflows.CollectCashflows(cashAccumulators, fFxRate, baseDate, endDate, fBuySellSign, fForecastRate, fForecastRate2, fCutoffDate);
        }

        /// <summary>
        /// Returns true if this model can value deals with forecast rate currency different from settlement currency.
        /// </summary>
        protected override bool SupportQuanto()
        {
            return true;
        }

        /// <summary>
        /// Warn of unnecessary volatility surface definitions.
        /// </summary>
        /// <remarks>
        /// Test differently if the cap and swaption volatility definitions are the same or
        /// distinct because they may have been set by a single property or two distinct ones.
        /// </remarks>
        private static void ValidateUnnecessaryVolatilities(CFGeneralInterestSpreadListDeal deal, SpreadCashflowListCharacteristics characteristics, ErrorList errors)
        {
            if (deal.Discount_Rate_Cap_Volatility == deal.Discount_Rate_Swaption_Volatility)
            {
                if (!characteristics.NeedDiscountRateVol && !characteristics.NeedDiscountYieldVol && !string.IsNullOrEmpty(deal.Discount_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Cap_Volatility)} and {nameof(deal.Discount_Rate_Swaption_Volatility)} {deal.Discount_Rate_Cap_Volatility}");
            }
            else
            {
                if (!characteristics.NeedDiscountRateVol && !string.IsNullOrEmpty(deal.Discount_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Cap_Volatility)} {deal.Discount_Rate_Cap_Volatility}");

                if (!characteristics.NeedDiscountYieldVol && !string.IsNullOrEmpty(deal.Discount_Rate_Swaption_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Swaption_Volatility)} {deal.Discount_Rate_Swaption_Volatility}");
            }

            if (deal.Forecast_Rate1_Cap_Volatility == deal.Forecast_Rate1_Swaption_Volatility)
            {
                if (!characteristics.NeedForecast1RateVol && !characteristics.NeedForecast1YieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate1_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate1_Cap_Volatility)} and {nameof(deal.Forecast_Rate1_Swaption_Volatility)} {deal.Forecast_Rate1_Cap_Volatility}.");
            }
            else
            {
                if (!characteristics.NeedForecast1RateVol && !string.IsNullOrEmpty(deal.Forecast_Rate1_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate1_Cap_Volatility)} {deal.Forecast_Rate1_Cap_Volatility}.");

                if (!characteristics.NeedForecast1YieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate1_Swaption_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate1_Swaption_Volatility)} {deal.Forecast_Rate1_Swaption_Volatility}.");
            }

            if (deal.Forecast_Rate2_Cap_Volatility == deal.Forecast_Rate2_Swaption_Volatility)
            {
                if (!characteristics.NeedForecast2RateVol && !characteristics.NeedForecast2YieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate2_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate2_Cap_Volatility)} and {nameof(deal.Forecast_Rate2_Swaption_Volatility)} {deal.Forecast_Rate2_Cap_Volatility}.");
            }
            else
            {
                if (!characteristics.NeedForecast2RateVol && !string.IsNullOrEmpty(deal.Forecast_Rate2_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate2_Cap_Volatility)} {deal.Forecast_Rate2_Cap_Volatility}.");

                if (!characteristics.NeedForecast2YieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate2_Swaption_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate2_Swaption_Volatility)} {deal.Forecast_Rate2_Swaption_Volatility}.");
            }
        }
    }
}