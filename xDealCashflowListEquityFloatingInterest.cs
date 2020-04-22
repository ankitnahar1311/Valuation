using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Equity-linked floating interest cashflow list deal.
    /// </summary>
    [Serializable]
    [DisplayName("Equity-Linked Floating Interest Cashflow List")]
    public class CFEquityFloatingInterestListDeal : CFListBaseDeal<CFEquityFloatingInterestList>
    {
        private const int AccrualHolidayCalendarIndex = 0;
        private const int RateHolidayCalendarIndex = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CFEquityFloatingInterestListDeal"/> class.
        /// </summary>
        public CFEquityFloatingInterestListDeal()
        {
            Equity = string.Empty;
            Equity_Currency = string.Empty;
            Equity_Payoff_Currency = string.Empty;
            Equity_Payoff_Type = PayoffType.Standard;
            Equity_Volatility = string.Empty;
            Discount_Rate_Cap_Volatility = string.Empty;
            Forecast_Rate_Cap_Volatility = string.Empty;
            Accrual_Calendars = string.Empty;
            Rate_Calendars = string.Empty;
            Rate_Adjustment_Method = DateAdjustmentMethod.Modified_Following;
            Rate_Sticky_Month_End = YesNo.Yes;
        }

        /// <summary>
        /// Gets or sets the calendars used for calculating the accrual period.
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
        /// Gets or sets the day count convention used to calculate the accrual period.
        /// </summary>
        public DayCount Accrual_Day_Count { get; set; }

        /// <summary>
        /// Gets or sets the underlying equity.
        /// </summary>
        [NonMandatory]
        public string Equity { get; set; }

        /// <summary>
        /// Gets or sets the volatility of the underlying equity.
        /// </summary>
        [NonMandatory]
        public string Equity_Volatility { get; set; }

        /// <summary>
        /// Gets or sets the currency of the equity.
        /// </summary>
        [NonMandatory]
        public string Equity_Currency { get; set; }

        /// <summary>
        /// Gets or sets the forecast rate.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate
        {
            get { return fForecast; }
            set { fForecast = value; }
        }

        /// <summary>
        /// Gets or sets the cap volatility of the forecast rate.
        /// </summary>
        [NonMandatory]
        public string Discount_Rate_Cap_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the cap volatility of the forecast rate.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate_Cap_Volatility
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the calendars used for determining the underlying interest rate.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Rate_Calendars
        {
            get { return GetCalendarNames(RateHolidayCalendarIndex); }
            set { SetCalendarNames(RateHolidayCalendarIndex, value); }
        }

        /// <summary>
        /// Gets or sets the day count convention used to determine the underlying interest rate.
        /// </summary>
        public DayCount Rate_Day_Count { get; set; }

        /// <summary>
        /// Number of business days between reset date and rate start date.
        /// </summary>
        public int Rate_Offset { get; set; }

        /// <summary>
        /// Adjustment method for rate end date calculations.
        /// </summary>
        public DateAdjustmentMethod Rate_Adjustment_Method { get; set; }

        /// <summary>
        /// Force rate end date to be last business day of month when rate start date is last business day of month.
        /// </summary>
        public YesNo Rate_Sticky_Month_End { get; set; }

        /// <summary>
        /// Gets or sets the payoff currency of the corresponding equity swap leg.
        /// </summary>
        [NonMandatory]
        public string Equity_Payoff_Currency { get; set; }

        /// <summary>
        /// Gets or sets the payoff type of the corresponding equity swap leg.
        /// </summary>
        [NonMandatory]
        public PayoffType Equity_Payoff_Type { get; set; }

        /// <summary>
        /// Gets the holiday calendar used for calculating the accrual period.
        /// </summary>
        public IHolidayCalendar AccrualHolidayCalendar()
        {
            return GetHolidayCalendar(AccrualHolidayCalendarIndex);
        }

        /// <summary>
        /// Gets the holiday calendar used for calculating the underlying interest rate.
        /// </summary>
        public IHolidayCalendar RateHolidayCalendar()
        {
            return GetHolidayCalendar(RateHolidayCalendarIndex);
        }

        /// <summary>
        /// Last date for which a discount factor is required.
        /// </summary>
        /// <remarks>Maximum of all payment and rate end dates.</remarks>
        public override double RateEndDate()
        {
            if (fCashflows.Items.Count > 0)
                return fCashflows.Items.Max(cashflow => Math.Max(cashflow.Payment_Date, cashflow.Rate_End_Date));

            return 0.0;
        }

        /// <summary>
        /// Tenor of LIBOR rate.
        /// </summary>
        /// <remarks>Maximum of rate tenor (LIBOR) over all cashflows.</remarks>
        public override double RatePeriod()
        {
            if (fCashflows.Items.Count > 0)
                return fCashflows.Items.Max(cashflow => cashflow.Rate_Tenor);

            return 0.0;
        }

        /// <summary>
        /// Gets the PayoffType of the deal.
        /// </summary>
        public PayoffType GetEquityPayoffType()
        {
            if (string.IsNullOrEmpty(Equity_Payoff_Currency) || Equity_Currency == Equity_Payoff_Currency)
                return PayoffType.Standard;

            return Equity_Payoff_Type;
        }

        /// <summary>
        /// Determines whether this instance is part of a cross currency swap.
        /// </summary>
        public bool IsCrossCurrency()
        {
            return (Equity_Currency != Currency && GetEquityPayoffType() == PayoffType.Standard) || (!string.IsNullOrEmpty(Equity_Payoff_Currency) && Equity_Payoff_Currency != Currency);
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public SingleEquityForwardDealHelper GetDealHelper()
        {
            return new SingleEquityForwardDealHelper(Equity, Equity_Volatility, Equity_Currency, Equity_Payoff_Currency, Equity_Payoff_Type);
        }

        /// <summary>
        /// Validates the specified errors.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            InitialiseHolidayCalendars(calendar);
            var parameters = new CashflowListDateGenerationParameters()
            {
                AccrualDayCount = Accrual_Day_Count,
                AccrualCalendar = AccrualHolidayCalendar(),
                RateDayCount = Rate_Day_Count,
                RateCalendar = RateHolidayCalendar(),
                RateOffset = Rate_Offset,
                RateAdjustmentMethod = Rate_Adjustment_Method,
                RateStickyMonthEnd = Rate_Sticky_Month_End,
            };

            if (fCashflows.FinishBuild(parameters))
                AddToErrors(errors, ErrorLevel.Info, "Missing cashflow properties have been calculated (Accrual_Year_Fraction, Rate_Start_Date, Rate_End_Date, Rate_Year_Fraction)");

            base.Validate(calendar, errors);

            if (string.IsNullOrWhiteSpace(Equity))
                AddToErrors(errors, "Equity must be specified on the deal.");

            if (string.IsNullOrWhiteSpace(Equity_Currency))
                AddToErrors(errors, "Equity_Currency must be specified on the deal.");

            bool equityPayoffInAssetCurrency = string.IsNullOrEmpty(Equity_Payoff_Currency) || Equity_Payoff_Currency == Equity_Currency;

            if (!equityPayoffInAssetCurrency && Equity_Payoff_Type == PayoffType.Standard)
                AddToErrors(errors, ErrorLevel.Error, "Equity_Payoff_Type cannot be Standard when Equity_Payoff_Currency and Equity_Currency are different");
            else if (equityPayoffInAssetCurrency && Equity_Payoff_Type != PayoffType.Standard)
                AddToErrors(errors, ErrorLevel.Info, "Payoff_Currency and Currency are the same but Payoff_Type is not Standard");

            fCashflows.ValidateQuantoCompo(GetEquityPayoffType(), IsCrossCurrency(), errors);
        }
    }

    /// <summary>
    /// Equity-linked floating interest cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [DisplayName("Equity-Linked Floating Interest Cashflow List Valuation")]
    public class CFEquityFloatingInterestListValuation : CFListBaseValuation<CFEquityFloatingInterestList>
    {
        [NonSerialized]
        private IInterestRateVol fDiscountRateVol;
        [NonSerialized]
        private IInterestRateVol fForecastRateVol;
        [NonSerialized]
        private FXVolHelper fForecastFXVol;
        [NonSerialized]
        private IAssetPrice fEquity;
        [NonSerialized]
        private ISpotProcessVol fEquityVol;
        [NonSerialized]
        private IFxRate fEquityFXRate;
        [NonSerialized]
        private IFxRate fEquityPayoffFXRate;
        [NonSerialized]
        private QuantoCompoCalculator fEquityQuantoCompo;
        [NonSerialized]
        private CorrelationHelper fForecastFXCorrel;
        [NonSerialized]
        private CorrelationHelper fForecastDiscountCorrel;
        [NonSerialized]
        private EquityCashflowListCharacteristics fCharacteristics;
        [NonSerialized]
        private string fEquityCurrency;
        [NonSerialized]
        private string fEquityPayoffCurrency;

        /// <summary>
        /// Gets or sets whether to use a convexity correction.
        /// </summary>
        public YesNo Convexity_Correction
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets whether to use a quanto correction.
        /// </summary>
        public YesNo Quanto_Correction
        {
            get; set;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CFEquityFloatingInterestListDeal);
        }

        /// <summary>
        /// Register price factors used in valuation.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            var deal = (CFEquityFloatingInterestListDeal)Deal;
            deal.GetDealHelper().RegisterFactors(factors, errors);

            fEquityCurrency = deal.Equity_Currency;
            factors.RegisterInterface<IFxRate>(fEquityCurrency);

            fEquityPayoffCurrency = string.IsNullOrEmpty(deal.Equity_Payoff_Currency) ? fEquityCurrency : deal.Equity_Payoff_Currency;

            if (fEquityPayoffCurrency != fEquityCurrency)
                factors.RegisterInterface<IFxRate>(fEquityPayoffCurrency);

            // Get characteristics
            EquityCashflowListCharacteristics characteristics = deal.Cashflows.Analyze(factors.BaseDate);
            bool quanto = fForecastIsForeign && Quanto_Correction == YesNo.Yes;

            if (characteristics.fHasLibor)
            {
                if ((!characteristics.fIsStandardLibor && Convexity_Correction == YesNo.Yes) || quanto)
                {
                    var forecastRateVol = InterestVolBase.RegisterInterestRateVol(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency);
                    InterestVolBase.Validate(deal, forecastRateVol, ProbabilityDistribution.Lognormal, errors);
                }

                if (!characteristics.fIsStandardLibor && Convexity_Correction == YesNo.Yes)
                {
                    var discountRateVol = InterestVolBase.RegisterInterestRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency);
                    InterestVolBase.Validate(deal, discountRateVol, ProbabilityDistribution.Lognormal, errors);
                }
            }

            if (quanto)
            {
                FXVolHelper.Register(factors, fForecastCurrency, fCurrency);
                CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IFxRate), fForecastCurrency, fCurrency);
                CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IInterestRate), fCurrency, null);
            }

            deal.Cashflows.RegisterFactorsValidate(factors.BaseDate, deal.GetEquityPayoffType() == PayoffType.Compo, deal.IsCrossCurrency(), errors);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (CFEquityFloatingInterestListDeal)Deal;

            // Set up cashflow list
            fCharacteristics = deal.Cashflows.Analyze(factors.BaseDate);

            // Add to valuation time grid
            fT.AddPayDates(deal.Cashflows);
        }

        /// <summary>
        /// Prepare for valuation.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);
            var deal = (CFEquityFloatingInterestListDeal)Deal;

            deal.GetDealHelper().PreValueAsset(out fEquity, out fEquityVol, ref fEquityQuantoCompo, factors);

            // Get FX rate price factors
            fEquityCurrency = deal.Equity_Currency;
            fEquityPayoffCurrency = string.IsNullOrEmpty(deal.Equity_Payoff_Currency) ? fEquityCurrency : deal.Equity_Payoff_Currency;
            fEquityFXRate = factors.GetInterface<IFxRate>(fEquityCurrency);
            fEquityPayoffFXRate = fEquityPayoffCurrency != fEquityCurrency ? factors.GetInterface<IFxRate>(fEquityPayoffCurrency) : fEquityFXRate;

            bool quanto = fForecastIsForeign && Quanto_Correction == YesNo.Yes;

            if (fCharacteristics.fHasLibor)
            {
                // volatility surfaces for forecast rate
                if ((!fCharacteristics.fIsStandardLibor && Convexity_Correction == YesNo.Yes) || quanto)
                    fForecastRateVol = InterestVolBase.GetRateVol(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency);

                // volatility surfaces for discount rate
                if (!fCharacteristics.fIsStandardLibor && Convexity_Correction == YesNo.Yes)
                    fDiscountRateVol = InterestVolBase.GetRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency);
            }

            if (quanto)
            {
                fForecastFXVol = FXVolHelper.Get(factors, fForecastCurrency, fCurrency);
                fForecastFXCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(FxRate), fForecastCurrency, fCurrency);
                fForecastDiscountCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(InterestRate), fCurrency, null);
            }
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            var deal = (CFEquityFloatingInterestListDeal)Deal;

            pv.Clear();
            if (cash != null)
                cash.Clear();

            var equityParams = new EquityCashflowParams((IEquityPrice)fEquity, (EquityPriceVol)fEquityVol, fEquityFXRate, fEquityPayoffFXRate, deal.GetEquityPayoffType(), fEquityQuantoCompo, null, null);

            if (!fForecastIsForeign && fCharacteristics.fIsStandardLibor)
            {
                // standard swap leg
                deal.Cashflows.ValueSwap(pv, cash, baseDate, valueDate, fDiscountRate, fForecastRate, equityParams, fFxRate, intraValuationDiagnosticsWriter, fCutoffDate);
            }
            else
            {
                using (var cache = Vector.CacheLike(pv))
                {
                    // Use general equity cashflow list valuation for quanto or convexity corrections.
                    var irParams = new IRCashflowParams(fForecastRate, fDiscountRate, fForecastRateVol, fDiscountRateVol, fForecastFXVol, fForecastFXCorrel, fForecastDiscountCorrel, cache.Get(1.0), ReferenceEquals(fForecastRate, fDiscountRate), fForecastRate.GetCurrency() != fDiscountRate.GetCurrency(), Convexity_Correction == YesNo.Yes, Quanto_Correction == YesNo.Yes);

                    deal.Cashflows.Value(pv, cash, baseDate, valueDate, irParams, equityParams, fFxRate, intraValuationDiagnosticsWriter, fCutoffDate);
                }
            }

            pv.Assign(fBuySellSign * pv);
            if (cash != null)
                cash.AssignProduct(fBuySellSign, cash);
        }

        /// <summary>
        /// Collect cashflows realised along the scenario path up to endDate.
        /// </summary>
        public override void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate)
        {
            var deal = (CFEquityFloatingInterestListDeal)Deal;
            var equityParams = new EquityCashflowParams((IEquityPrice)fEquity, (EquityPriceVol)fEquityVol, fEquityFXRate, fEquityPayoffFXRate, deal.GetEquityPayoffType(), fEquityQuantoCompo, null, null);

            deal.Cashflows.CollectCashflows(cashAccumulators, fFxRate, equityParams, baseDate, endDate, fBuySellSign, fForecastRate, fCutoffDate);
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            base.Value(valuationResults, factors, baseTimes);

            var accruedResults = valuationResults.Results<AccruedInterest>();
            if (accruedResults == null)
                return;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector accruedInterest = cache.Get();
                var deal = (CFEquityFloatingInterestListDeal)Deal;
                var tgi = new TimeGridIterator(fT);

                var equityParams = new EquityCashflowParams((EquityPrice)fEquity, (EquityPriceVol)fEquityVol, fEquityFXRate, fEquityPayoffFXRate, deal.GetEquityPayoffType(), fEquityQuantoCompo, null, null);

                while (tgi.Next())
                {
                    deal.Cashflows.CalculateAccrual(accruedInterest, equityParams, factors.BaseDate, tgi.Date, accruedResults.AccrueFromToday, deal.GetHolidayCalendar(), deal.Accrual_Day_Count, fForecastRate, fFxRate);
                    accruedResults.SetValue(tgi.Date, fBuySellSign * accruedInterest);
                }
            }
        }

        /// <summary>
        /// Returns true if this model can value deals with forecast rate currency different from settlement currency.
        /// </summary>
        protected override bool SupportQuanto()
        {
            return true;
        }
    }
}