using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Floating interest cashflow list deal.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Cashflow List")]
    public class CFFloatingInterestListDeal : CFListBaseDeal<CFFloatingInterestList>
    {
        private const int AccrualHolidayCalendarIndex = 0;
        private const int RateHolidayCalendarIndex = 1;

        /// <summary>
        /// Constructor for floating interest cashflow list deal.
        /// </summary>
        public CFFloatingInterestListDeal()
        {
            Issuer = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate = string.Empty;
            Settlement_Style = SettlementType.Physical;
            Is_Defaultable = YesNo.No;

            // cap and swaption vols distinct. base class provides cap vols, these are the distinct swaption vols 
            Forecast_Rate_Swaption_Volatility = string.Empty;
            Discount_Rate_Swaption_Volatility = string.Empty;

            Accrual_Calendars = string.Empty;
            Rate_Calendars = string.Empty;
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
        /// Gets or sets the forecast interest rate.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate
        {
            get { return fForecast; }
            set { fForecast = value; }
        }

        /// <summary>
        /// Gets or sets the forecast rate cap volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate_Cap_Volatility
        {
            get { return fForecastVolatility; }
            set { fForecastVolatility = value; }
        }

        /// <summary>
        /// Gets or sets the forecast rate swaption volatility price factor.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate_Swaption_Volatility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the issuer.
        /// </summary>
        [NonMandatory]
        public string Issuer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the survival probability price factor ID.
        /// </summary>
        [NonMandatory]
        public string Survival_Probability
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the recovery rate.
        /// </summary>
        [NonMandatory]
        public string Recovery_Rate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the repo interest rate.
        /// </summary>
        [NonMandatory]
        public string Repo_Rate
        {
            get { return fRepo; }
            set { fRepo = value; }
        }

        /// <summary>
        /// Gets or sets the settlement date.
        /// </summary>
        [NonMandatory]
        public TDate Settlement_Date
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settlement amount.
        /// </summary>
        [NonMandatory]
        public double Settlement_Amount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settlement type (Cash or Physical).
        /// </summary>
        public SettlementType Settlement_Style
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Settlement_Amount_Is_Clean property.
        /// </summary>
        public YesNo Settlement_Amount_Is_Clean
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Is_Defaultable property.
        /// </summary>
        public YesNo Is_Defaultable
        {
            get;
            set;
        }

        /// <summary>
        /// Valuation profile is truncated at this date.
        /// </summary>
        [NonMandatory]
        public TDate Investment_Horizon
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set the list of holiday calendars for interest accrual dates.
        /// </summary>
        [NonMandatory]
        [LocationStringsForm]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Accrual_Calendars
        {
            get { return GetCalendarNames(AccrualHolidayCalendarIndex); }
            set { SetCalendarNames(AccrualHolidayCalendarIndex, value); }
        }

        /// <summary>
        /// Gets or set the list of holiday calendars for the floating interest rate dates.
        /// </summary>
        [NonMandatory]
        [LocationStringsForm]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Rate_Calendars
        {
            get { return GetCalendarNames(RateHolidayCalendarIndex); }
            set { SetCalendarNames(RateHolidayCalendarIndex, value); }
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
        /// Gets the rate holiday calendar.
        /// </summary>
        public IHolidayCalendar RateHolidayCalendar()
        {
            return GetHolidayCalendar(RateHolidayCalendarIndex);
        }

        /// <summary>
        /// Gets the issuer.
        /// </summary>
        public override string GetIssuer()
        {
            return Issuer;
        }

        /// <summary>
        /// Gets the survival probability price factor ID.
        /// </summary>
        public override string GetSurvivalProbability()
        {
            return Survival_Probability;
        }

        /// <summary>
        /// Gets the recovery rate.
        /// </summary>
        public override string GetRecoveryRate()
        {
            return Recovery_Rate;
        }

        /// <summary>
        /// Returns true if cash settled (at settlement date or investment horizon).
        /// </summary>
        public override bool IsCashSettled()
        {
            return Settlement_Style == SettlementType.Cash || Investment_Horizon > 0.0;
        }

        /// <summary>
        /// Gets the repo rate.
        /// </summary>
        public override string GetRepoRate()
        {
            return Settlement_Date == 0.0 ? string.Empty : base.GetRepoRate();
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            InitialiseHolidayCalendars(calendar);
            var parameters = new CashflowListDateGenerationParameters
            {
                AccrualCalendar = AccrualHolidayCalendar(),
                RateCalendar = RateHolidayCalendar(),
                RateOffset = Rate_Offset,
                RateAdjustmentMethod = Rate_Adjustment_Method,
                RateStickyMonthEnd = Rate_Sticky_Month_End
            };

            if (fCashflows.FinishBuild(parameters))
                AddToErrors(errors, ErrorLevel.Info, "Missing cashflow properties have been calculated (Accrual_Year_Fraction, Rate_Start_Date, Rate_End_Date, Rate_Year_Fraction)");

            base.Validate(calendar, errors);

            if (Settlement_Style == SettlementType.Cash && Settlement_Date == 0.0)
                AddToErrors(errors, "Settlement_Date must be specified when Settlement_Style is Cash");

            if (Settlement_Amount != 0.0 && Settlement_Date == 0.0)
                AddToErrors(errors, ErrorLevel.Warning, "Settlement_Amount is not zero but Settlement_Date is not specified so Settlement_Amount has been ignored.");

            if (fCashflows.Items.Any(cashflow => cashflow.Payment_Date <= Settlement_Date))
                AddToErrors(errors, "Cashflows must have Payment_Date after Settlement_Date");

            if (fCashflows.Items.Any(cashflow => cashflow.FX_Reset_Date > 0.0 || cashflow.Known_FX_Rate > 0.0))
            {
                // Do not support forward deals on cashflow lists with FX_Reset_Date or Known_FX_Rate
                if (Settlement_Date > 0.0)
                    AddToErrors(errors, "Cashflow list deal with Settlement_Date cannot have cashflows with FX_Reset_Date or Known_FX_Rate");

                // Do not support cashflow lists with FX_Reset_Date or Known_FX_Rate under Use_Survival_Probability or Respect_Default
                if (!string.IsNullOrEmpty(Issuer))
                    AddToErrors(errors, "Cashflow list deal with Issuer cannot have cashflows with FX_Reset_Date or Known_FX_Rate");
            }
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            double endDate;
            if (Settlement_Style == SettlementType.Cash)
                endDate = Settlement_Date;
            else
                endDate = base.EndDate();

            if (Investment_Horizon > 0.0)
                endDate = Math.Min(endDate, Investment_Horizon);

            return endDate;
        }

        /// <summary>
        /// Last date for which a discount factor is required.
        /// </summary>
        /// <remarks>Maximum of all payment and rate end dates.</remarks>
        public override double RateEndDate()
        {
            if (fCashflows.Items.Count > 0)
                return fCashflows.Items.Max(cashflow => Math.Max(cashflow.Payment_Date, MaxRateEndDate(cashflow.Resets)));

            return 0.0;
        }

        /// <summary>
        /// Tenor of LIBOR rate or fixed-side frequency of CMS rate.
        /// </summary>
        /// <remarks>Maximum of rate tenor (LIBOR) or rate frequency (CMS) over all cashflows.</remarks>
        public override double RatePeriod()
        {
            if (fCashflows.Items.Count > 0)
                return fCashflows.Items.Max<CFFloatingInterest>(cashflow => cashflow.GetMaxRateTenor());

            return 0.0;
        }

        /// <summary>
        /// Maximum of Rate_End_Dates in a list of resets.
        /// </summary>
        private static double MaxRateEndDate(RateResetList resets)
        {
            if (resets != null && resets.Count > 0)
                return resets.Max(reset => reset.Rate_End_Date);

            return 0.0;
        }
    }

    /// <summary>
    /// Floating interest cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Cashflow List Valuation")]
    public class CFFloatingInterestListValuation : CFListBaseValuation<CFFloatingInterestList>, ISingleDateValuation,
        ICanUseSurvivalProbability
    {
        [NonSerialized]
        protected IInterestRateVol fDiscountRateVol;
        [NonSerialized]
        protected IInterestYieldVol fDiscountYieldVol;
        [NonSerialized]
        protected IInterestRateVol fForecastRateVol;
        [NonSerialized]
        protected IInterestYieldVol fForecastYieldVol;
        [NonSerialized]
        protected IFxRate fForecastFxRate;
        [NonSerialized]
        protected FXVolHelper fForecastFxVol;
        [NonSerialized]
        protected CorrelationHelper fForecastFxCorrel;
        [NonSerialized]
        protected CorrelationHelper fForecastDiscountCorrel;
        [NonSerialized]
        protected CashflowListCharacteristics fCharacteristics;
        protected BasisPoint fConvexityLowRateCutoff = 1e-4;

        /// <summary>
        /// The cashflows that will be used in valuation. This is either the cashflows from the 
        /// original deal or a modified clone of them.
        /// </summary>
        protected CFFloatingInterestList fCashflows;

        private int fOisRateRounding;

        /// <summary>
        /// Constructor for floating interest cashflow list deal valuation.
        /// </summary>
        public CFFloatingInterestListValuation()
        {
            Convexity_Correction = YesNo.Yes;
            Quanto_Correction = YesNo.Yes;
            Faster_Averaging_Valuation = YesNo.Yes;
            Use_Survival_Probability = YesNo.No;
            Respect_Default = YesNo.No;
            Convexity_Low_Rate_Cutoff = BasisPoint.BasisPointValue;
            OIS_Cashflow_Group_Size = 1;
            OIS_Rate_Rounding = 0;
        }

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
        /// Gets or sets the Faster_Averaging_Valuation valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Faster_Averaging_Valuation
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Use_Survival_Probability valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Respect_Default valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Respect_Default
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
                    throw new ArgumentOutOfRangeException("value", "Convexity low rate cutoff cannot be less than 0.000001 or more than 10000 bp");

                fConvexityLowRateCutoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the OIS_Cashflow_Group_Size valuation model parameter.
        /// </summary>
        public int OIS_Cashflow_Group_Size
        {
            get; set;
        }

        /// <summary>
        /// OIS rate rounding.
        /// </summary>
        /// <remarks>
        /// Number of decimal places used to round the rate in percentage.
        /// E.g. 5.123456% is rounded to 5.1235% when <see cref="OIS_Rate_Rounding"/> == 4.
        /// No rounding is applied when <see cref="OIS_Rate_Rounding"/> == 0.
        /// </remarks>
        public int OIS_Rate_Rounding
        {
            get
            {
                return fOisRateRounding;
            }
            set
            {
                if (value >= 0 && value <= CFFloatingInterest.MaximumRounding)
                    fOisRateRounding = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(value), string.Format(CultureInfo.InvariantCulture, "{0} cannot be less than 0 or greater than {1}.", nameof(OIS_Rate_Rounding), CFFloatingInterest.MaximumRounding));
            }
        }

        /// <summary>
        /// Get principal amount of first interest cashflow with payment date > value date.
        /// This excludes cashflows that have no accrual period (which represent fixed payments).
        /// </summary>
        public static double GetPrincipal(CFFloatingInterestList cashflows, double valueDate)
        {
            return CashflowListPrincipal.GetPrincipal(cashflows.Items, valueDate);
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CFFloatingInterestListDeal);
        }

        /// <summary>
        /// Clones the cashflow list and applies missing fixings from the fixings file. If 
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

        /// <summary>
        /// Override for the various types of sub cashflow deals, so the common case stays lean
        /// </summary>
        /// <returns>The postfix</returns>
        public override string ModelTracesIdPostfix()
        {
            return fCharacteristics.HasOptionlet ? "Option" : string.Empty;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (CFFloatingInterestListDeal)Deal;

            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Survival_Probability = Yes or Respect_Default = Yes ignored for cashflow list deal with missing Issuer.");

            if (Use_Settlement_Offset == YesNo.Yes && deal.Settlement_Date != 0.0)
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Settlement_Offset = Yes ignored for cashflow list deal with Settlement_Date.");
        }

        /// <summary>
        /// Register price factors used in valuation.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CFFloatingInterestListDeal deal = (CFFloatingInterestListDeal)Deal;

            // Deal validation specific to CFFloatingInterestListValuation
            foreach (var cashflow in deal.Cashflows)
            {
                if (!cashflow.Resets.Any())
                    continue;

                var lastResetDate = cashflow.Resets.Last().Reset_Date;
                if (cashflow.FX_Reset_Date > 0.0 && cashflow.FX_Reset_Date < lastResetDate)
                    errors.Add(ErrorLevel.Warning, string.Format("Quanto adjustments for cashflow paying on {0} are not supported when the FX reset date {1} is before interest rate reset date {2}", cashflow.Payment_Date, cashflow.FX_Reset_Date, lastResetDate));
            }

            // Get characteristics
            CashflowListCharacteristics characteristics = deal.Cashflows.Analyze(factors.BaseDate);

            bool quanto = fForecastIsForeign && characteristics.HasQuanto && Quanto_Correction == YesNo.Yes;
            bool convexity = !characteristics.IsStandardLibor && Convexity_Correction == YesNo.Yes;

            var requirements = new VolatilityRequirements(
                characteristics.HasCms,
                characteristics.HasLibor && (characteristics.HasOptionlet || convexity || quanto),
                characteristics.HasCms && convexity,
                characteristics.HasLibor && convexity);

            // Collect registered volatility price factors to check they have the same distribution type
            var volPriceFactors = new List<IInterestVol>();

            // register forecast rate volatility surfaces
            if (requirements.NeedForecastYieldVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestYieldVol(factors, deal.Forecast_Rate_Swaption_Volatility, fForecastCurrency));

            if (requirements.NeedForecastRateVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestRateVol(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency));

            if (requirements.NeedDiscountYieldVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestYieldVol(factors, deal.Discount_Rate_Swaption_Volatility, fCurrency));

            if (requirements.NeedDiscountRateVol)
                volPriceFactors.Add(InterestVolBase.RegisterInterestRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency));

            if (fForecastIsForeign)
            {
                // Register factor for translation from forecast rate currency to settlement currency for cashflows with FX reset date
                if (characteristics.HasFXReset)
                    factors.RegisterInterface<IFxRate>(fForecastCurrency);

                if (quanto)
                {
                    FXVolHelper.Register(factors, fForecastCurrency, fCurrency);
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IFxRate), fForecastCurrency, fCurrency);
                    CorrelationHelper.Register(factors, typeof(IInterestRate), fForecastCurrency, null, typeof(IInterestRate), fCurrency, null);
                }
            }

            if (volPriceFactors.Select(pf => pf.GetDistributionType()).Distinct().Count() > 1)
                deal.AddToErrors(errors, "Volatility price factors must have the same distribution type.");

            ValidateUnnecessaryVolatilities(deal, requirements, errors);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (CFFloatingInterestListDeal)Deal;

            // Set up cashflow list
            fCashflows.SetUseConvexityCorrection(Convexity_Correction == YesNo.Yes);
            fCashflows.SetUseQuantoCorrection(Quanto_Correction == YesNo.Yes);
            fCashflows.SetFasterAveraging(Faster_Averaging_Valuation == YesNo.Yes);
            fCashflows.SetOISCashflowGroupSize(OIS_Cashflow_Group_Size);
            fCashflows.SetConvexityLowRateCutoff(Convexity_Low_Rate_Cutoff);
            fCashflows.SetOISRateRounding(OIS_Rate_Rounding);

            double baseDate = factors.BaseDate;
            fCharacteristics = fCashflows.Analyze(baseDate);

            if (fCharacteristics.IsOIS)
                fCashflows.InitializeFastOISCalculation(baseDate);

            if (fCharacteristics.IsVanillaSwap)
                fCashflows.CalculateAmounts(baseDate);

            // Add to valuation time grid
            bool payDatesRequired = ValueOnCashflowDates() && requiredResults.CashRequired();
            fT.AddPayDates(fCashflows, payDatesRequired);

            double settlementDate = deal.Settlement_Date;

            if (settlementDate > 0.0)
                fT.AddPayDate(settlementDate, payDatesRequired);

            if (Use_Settlement_Offset == YesNo.Yes && settlementDate != 0.0)
                fCutoffDate = 0.0;

            if (deal.Investment_Horizon > 0.0)
                fT.AddPayDate(deal.Investment_Horizon, payDatesRequired);

            if (Use_Survival_Probability == YesNo.Yes)
            {
                fRecoveryList = new CFRecoveryList();
                fRecoveryList.PopulateRecoveryCashflowList(baseDate, settlementDate, fCashflows);
            }
        }

        /// <summary>
        /// Prepare for valuation.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            var deal = (CFFloatingInterestListDeal)Deal;

            base.PreValue(factors);

            bool quanto = fForecastIsForeign && fCharacteristics.HasQuanto && Quanto_Correction == YesNo.Yes;
            bool convexity = !fCharacteristics.IsStandardLibor && Convexity_Correction == YesNo.Yes;

            // volatility surfaces for forecast rate
            if (fCharacteristics.HasCms)
                fForecastYieldVol = InterestVolBase.GetYieldVol(factors, deal.Forecast_Rate_Swaption_Volatility, fForecastCurrency);

            if (fCharacteristics.HasLibor && (fCharacteristics.HasOptionlet || convexity || quanto))
                fForecastRateVol = InterestVolBase.GetRateVol(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency);

            // volatility surfaces for discount rate
            if (convexity)
            {
                // Discount rate volatility and correlation for convexity correction
                if (fCharacteristics.HasCms)
                    fDiscountYieldVol = InterestVolBase.GetYieldVol(factors, deal.Discount_Rate_Swaption_Volatility, fCurrency);

                if (fCharacteristics.HasLibor)
                    fDiscountRateVol = InterestVolBase.GetRateVol(factors, deal.Discount_Rate_Cap_Volatility, fCurrency);
            }

            if (fForecastIsForeign)
            {
                // Get factor for translation from forecast rate currency to settlement currency for cashflows with FX reset date
                if (fCharacteristics.HasFXReset)
                    fForecastFxRate = factors.GetInterface<IFxRate>(fForecastCurrency);

                if (quanto)
                {
                    fForecastFxVol = FXVolHelper.Get(factors, fForecastCurrency, fCurrency);
                    fForecastFxCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(FxRate), fForecastCurrency, fCurrency);
                    fForecastDiscountCorrel = CorrelationHelper.Get(factors, typeof(InterestRate), fForecastCurrency, null, typeof(InterestRate), fCurrency, null);
                }
            }
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            Value(pv, cash, baseDate, valueDate, null, fDiscountRate, fForecastRate, fRepoRate, fForecastRateVol,
                fForecastYieldVol, fSurvivalProb, saccrResult, intraValuationDiagnosticsWriter);

            // Add accruedInterest to Intra-valuation diagnostics
            if (fIntraValuationDiagnosticsWriter.Level > IntraValuationDiagnosticsLevel.None)
            {
                var deal = (CFFloatingInterestListDeal)Deal;
                using (var cache = Vector.CacheLike(pv))
                {
                    Vector accruedInterest = cache.Get();
                    fCashflows.CalculateAccrual(accruedInterest, baseDate, valueDate, false, deal.AccrualHolidayCalendar(), fForecastRate);
                    IntraValuationDiagnosticsHelper.AddCashflowsAccruedInterest(fIntraValuationDiagnosticsWriter, accruedInterest);
                }
            }
        }

        /// <summary>
        /// Value the deal using the cashflow list.
        /// </summary>
        public void Value(Vector pv, Vector cash, double baseDate, double valueDate, Vector settlementDate, IInterestRate discount,
            IInterestRate forecast, IInterestRate repo, IInterestRateVol interestRateVol, IInterestYieldVol interestYieldVol,
            ISurvivalProb survivalProb, ISACCRResult saccrResult, IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            var deal = (CFFloatingInterestListDeal)Deal;

            pv.Clear();
            if (cash != null)
                cash.Clear();

            bool valued = false;

            if (Use_Survival_Probability == YesNo.Yes && survivalProb != null)
            {
                fRecoveryList.Value(pv, baseDate, valueDate, discount, survivalProb, intraValuationDiagnosticsWriter);
            }
            else if (!fForecastIsForeign && fCharacteristics.IsStandardPayoff && fCharacteristics.IsStandardLibor && fCashflows.Compounding_Method != CompoundingMethod.Exponential)
            {
                if (fCharacteristics.HasSwaplet && !fCharacteristics.HasOptionlet)
                {
                    ValueSwap(pv, cash, baseDate, valueDate, settlementDate, discount, forecast, intraValuationDiagnosticsWriter);
                    valued = true;
                }
                else if (fCharacteristics.HasOptionlet && !fCharacteristics.HasSwaplet && fCashflows.Compounding_Method == CompoundingMethod.None)
                {
                    fCashflows.ValueCap(pv, cash, baseDate, valueDate, settlementDate, discount, forecast,
                        fForecastRateVol, saccrResult, intraValuationDiagnosticsWriter, fCutoffDate);
                    valued = true;
                }
            }

            if (!valued)
            {
                // Use general cashflow list valuation
                if (fCashflows.Averaging_Method == AveragingMethod.Average_Rate)
                    fCashflows.ValueAverageRate(pv, cash, baseDate, valueDate, settlementDate, discount, forecast, fForecastRateVol, fForecastYieldVol, 
                        fFxRate, fForecastFxRate, fForecastFxVol, fForecastFxCorrel, survivalProb, intraValuationDiagnosticsWriter, fCutoffDate);
                else
                    fCashflows.Value(pv, cash, baseDate, valueDate, settlementDate, discount, forecast, fDiscountRateVol, fDiscountYieldVol, interestRateVol, interestYieldVol,
                        fFxRate, fForecastFxRate, fForecastFxVol, fForecastFxCorrel, fForecastDiscountCorrel, survivalProb, intraValuationDiagnosticsWriter, fCutoffDate);
            }

            double dealSettlementDate = deal.Settlement_Date;
            if (valueDate <= dealSettlementDate)
            {
                using (var cache = Vector.CacheLike(pv))
                {
                    Vector accruedInterest = cache.Get();
                    fCashflows.CalculateAccrual(accruedInterest, baseDate, dealSettlementDate, false, deal.AccrualHolidayCalendar(), forecast);
                    Vector settlementAmount = cache.Get(deal.Settlement_Amount);
                    if (deal.Settlement_Amount_Is_Clean == YesNo.Yes)
                        settlementAmount.Add(accruedInterest);

                    if (valueDate < dealSettlementDate)
                    {
                        // Forward deal before settlement date
                        double t = CalcUtils.DaysToYears(valueDate - baseDate);
                        double tSettle = CalcUtils.DaysToYears(dealSettlementDate - baseDate);
                        if (deal.Is_Defaultable == YesNo.No)
                            pv.Assign((pv / discount.Get(t, tSettle) - settlementAmount) * repo.Get(t, tSettle));
                        else
                            pv.Subtract(accruedInterest * discount.Get(t, tSettle) + (settlementAmount - accruedInterest) * repo.Get(t, tSettle));
                    }
                    else if (valueDate == dealSettlementDate)
                    {
                        // Forward deal at settlement date
                        pv.Subtract(settlementAmount);
                        if (cash != null)
                        {
                            if (deal.Settlement_Style == SettlementType.Cash)
                                cash.Assign(pv);
                            else
                                cash.Subtract(settlementAmount);
                        }
                    }
                }
            }

            pv.AssignProduct(fBuySellSign, pv);
            if (cash != null)
                cash.AssignProduct(fBuySellSign, cash);
        }

        /// <summary>
        /// Collect cashflows realised along the scenario path up to endDate.
        /// </summary>
        public override void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate)
        {
            var deal = (CFFloatingInterestListDeal)Deal;

            if (!fForecastIsForeign && fCharacteristics.HasSwaplet && !fCharacteristics.HasOptionlet && fCharacteristics.IsStandardPayoff && fCharacteristics.IsStandardLibor && 
                fCashflows.Compounding_Method != CompoundingMethod.Exponential)
            {
                if (fCharacteristics.IsOIS)
                    fCashflows.CollectCashflowsOIS(cashAccumulators, fFxRate, baseDate, endDate, fBuySellSign, fForecastRate, fCutoffDate);
                else
                    fCashflows.CollectCashflowsSwap(cashAccumulators, fFxRate, baseDate, endDate, fBuySellSign, fForecastRate, fCutoffDate);
            }
            else
            {
                fCashflows.CollectCashflows(cashAccumulators, fFxRate, fForecastFxRate, baseDate, endDate, fBuySellSign, fForecastRate, fCutoffDate);
            }

            double settlementDate = deal.Settlement_Date;
            if (settlementDate >= baseDate && settlementDate <= endDate)
            {
                using (var cache = Vector.Cache(cashAccumulators.NumScenarios))
                {
                    Vector settlementAmount = cache.GetClear();
                    if (deal.Settlement_Amount_Is_Clean == YesNo.Yes)
                        fCashflows.CalculateAccrual(settlementAmount, baseDate, settlementDate, false, deal.AccrualHolidayCalendar(), fForecastRate);

                    settlementAmount.Add(deal.Settlement_Amount);
                    cashAccumulators.Accumulate(fFxRate, settlementDate, -fBuySellSign * settlementAmount);
                }
            }
        }

        /// <summary>
        /// Modify the pv and cash taking the date of default into account.
        /// </summary>
        public override void GetDefaultValue(double baseDate, double valueDate, Vector defaultDate, RecoveryRate recoveryRate, Vector pv, Vector cash)
        {
            var deal = (CFFloatingInterestListDeal)Deal;

            double principal = GetPrincipal(fCashflows, valueDate);
            double settlementDate = deal.Settlement_Date;
            double t = CalcUtils.DaysToYears(valueDate - baseDate);

            using (var cache = Vector.CacheLike(pv))
            {
                // Approximation: recover only principal, neglecting accrued interest
                Vector recovery = cache.Get(fBuySellSign * principal * recoveryRate.Get(t));

                if (valueDate <= settlementDate)
                {
                    // Set the pv to (recovery - settlementAmount) * df when defaultDate <= valueDate <= settlementDate.
                    // Set cash to (recovery - settlementAmount) when defaultDate <= valueDate = settlementDate (cash is always zero before settlementDate).
                    double tSettle = CalcUtils.DaysToYears(settlementDate - baseDate);

                    Vector settlementAmount = cache.GetClear();
                    if (deal.Settlement_Amount_Is_Clean == YesNo.Yes)
                        fCashflows.CalculateAccrual(settlementAmount, baseDate, settlementDate, false, deal.AccrualHolidayCalendar(), fForecastRate);
                    settlementAmount.Add(deal.Settlement_Amount);

                    Vector hasDefaulted = cache.Get(defaultDate <= valueDate);

                    pv.AssignConditional(hasDefaulted, fRepoRate.Get(t, tSettle) * (recovery - fBuySellSign * settlementAmount), pv);

                    if (cash != null && valueDate == settlementDate)
                        cash.AssignConditional(hasDefaulted, pv, cash);
                }
                else
                {
                    // after settlement date
                    recovery.MultiplyBy(defaultDate >= valueDate); // set to zero if already defaulted
                    Vector notDefaulted = cache.Get(defaultDate > valueDate);
                    pv.AssignConditional(notDefaulted, pv, recovery);
                    if (cash != null)
                        cash.AssignConditional(notDefaulted, cash, recovery);
                }
            }
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            base.Value(valuationResults, factors, baseTimes);

            var deal = (CFFloatingInterestListDeal)Deal;

            CalculateMetrics(valuationResults, factors, deal);

            var accruedResults = valuationResults.Results<AccruedInterest>();
            if (accruedResults == null)
                return;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector accruedInterest = cache.Get();
                var tgi = new TimeGridIterator(fT);

                VectorEngine.For(tgi, () =>
                {
                    fCashflows.CalculateAccrual(accruedInterest, factors.BaseDate, tgi.Date, accruedResults.AccrueFromToday, deal.GetHolidayCalendar(), fForecastRate);
                    accruedResults.SetValue(tgi.Date, fBuySellSign * accruedInterest);
                });
            }
        }

        /// <inheritdoc />
        protected override void GetDefaultTime(Vector defaultTime, PriceFactorList factors)
        {
            if (fCreditRating != null)
            {
                fCreditRating.DefaultTime(defaultTime);
                return;
            }

            base.GetDefaultTime(defaultTime, factors);
        }

        /// <summary>
        /// Returns true if this model can value deals with forecast rate currency different from settlement currency.
        /// </summary>
        protected override bool SupportQuanto()
        {
            return true;
        }

        /// <summary>
        /// Returns true if Use_Survival_Probability is set to Yes.
        /// </summary>
        protected override bool UseSurvivalProbability()
        {
            return Use_Survival_Probability == YesNo.Yes;
        }

        /// <summary>
        /// Returns true if Respect_Default is set to Yes.
        /// </summary>
        protected override bool RespectDefault()
        {
            return Respect_Default == YesNo.Yes;
        }

        /// <summary>
        /// Warn of unnecessary volatility surface definitions.
        /// </summary>
        /// <remarks>
        /// Test differently if the cap and swaption volatility definitions are the same or
        /// distinct because they may have been set by a single property or two distinct ones.
        /// </remarks>
        private static void ValidateUnnecessaryVolatilities(CFFloatingInterestListDeal deal, VolatilityRequirements requirements, ErrorList errors)
        {
            if (deal.Discount_Rate_Cap_Volatility == deal.Discount_Rate_Swaption_Volatility)
            {
                if (!requirements.NeedDiscountRateVol && !requirements.NeedDiscountYieldVol && !string.IsNullOrEmpty(deal.Discount_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Cap_Volatility)} and {nameof(deal.Discount_Rate_Swaption_Volatility)} {deal.Discount_Rate_Cap_Volatility}.");
            }
            else
            {
                if (!requirements.NeedDiscountRateVol && !string.IsNullOrEmpty(deal.Discount_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Cap_Volatility)} {deal.Discount_Rate_Cap_Volatility}.");

                if (!requirements.NeedDiscountYieldVol && !string.IsNullOrEmpty(deal.Discount_Rate_Swaption_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Discount_Rate_Swaption_Volatility)} {deal.Discount_Rate_Swaption_Volatility}.");
            }

            if (deal.Forecast_Rate_Cap_Volatility == deal.Forecast_Rate_Swaption_Volatility)
            {
                if (!requirements.NeedForecastRateVol && !requirements.NeedForecastYieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate_Cap_Volatility)} and {nameof(deal.Forecast_Rate_Swaption_Volatility)} {deal.Forecast_Rate_Cap_Volatility}.");
            }
            else
            {
                if (!requirements.NeedForecastRateVol && !string.IsNullOrEmpty(deal.Forecast_Rate_Cap_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate_Cap_Volatility)} {deal.Forecast_Rate_Cap_Volatility}.");

                if (!requirements.NeedForecastYieldVol && !string.IsNullOrEmpty(deal.Forecast_Rate_Swaption_Volatility))
                    deal.AddToErrors(errors, ErrorLevel.Info, $"Unnecessary {nameof(deal.Forecast_Rate_Swaption_Volatility)} {deal.Forecast_Rate_Swaption_Volatility}.");
            }
        }

        /// <summary>
        /// Calculate valuation metrics requested by the Base Valuation calculation.
        /// </summary>
        private void CalculateMetrics(ValuationResults valuationResults, PriceFactorList factors, CFFloatingInterestListDeal deal)
        {
            var results = valuationResults.Results<ValuationMetrics>();
            if (results == null)
                return;

            if (results.IsMetricRequested(ValuationMetricConstants.AccruedInterest))
            {
                using (var cache = Vector.Cache(factors.NumScenarios))
                {
                    double? parameter = results.GetMetricParameter(ValuationMetricParameterConstants.AccrueFromToday);
                    bool accrueFromToday = parameter.HasValue && parameter.Value == 1.0;
                    Vector accruedInterest = cache.GetClear();
                    fCashflows.CalculateAccrual(accruedInterest, factors.BaseDate, factors.BaseDate, accrueFromToday, deal.AccrualHolidayCalendar(), fForecastRate);
                    double buySellSign = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;
                    results.SetMetricValue(ValuationMetricConstants.AccruedInterest, new ValuationId(this), buySellSign * accruedInterest[0]);
                }
            }
        }

        /// <summary>
        /// Value cashflow list with standard payoff, standard LIBOR cashflows without options.
        /// </summary>
        private void ValueSwap(Vector pv, Vector cash, double baseDate, double valueDate, Vector settlementDate,
            IInterestRate discount, IInterestRate forecast, IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            if (fCharacteristics.IsOIS && OIS_Cashflow_Group_Size <= 1 && settlementDate == null)
                fCashflows.ValueOIS(pv, cash, baseDate, valueDate, fCutoffDate, discount, forecast, intraValuationDiagnosticsWriter);
            else
                fCashflows.ValueSwap(pv, cash, null, baseDate, valueDate, settlementDate, fCutoffDate, discount, forecast, fCharacteristics.IsVanillaSwap, intraValuationDiagnosticsWriter);
        }

        private struct VolatilityRequirements
        {
            public VolatilityRequirements(bool needForecastYieldVol, bool needForecastRateVol, bool needDiscountYieldVol, bool needDiscountRateVol)
            {
                NeedForecastYieldVol = needForecastYieldVol;
                NeedForecastRateVol = needForecastRateVol;
                NeedDiscountYieldVol = needDiscountYieldVol;
                NeedDiscountRateVol = needDiscountRateVol;
            }

            public bool NeedForecastYieldVol { get; }

            public bool NeedForecastRateVol { get; }

            public bool NeedDiscountYieldVol { get; }

            public bool NeedDiscountRateVol { get; }
        }
    }
}
