using System;
using System.Collections.Generic;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Parameters used to generate any missing dates or year fractions in a cashflow list.  
    /// </summary>
    public struct CashflowListDateGenerationParameters
    {
        /// <summary>
        /// Day count convention for accrual year fraction.
        /// </summary>
        public DayCount AccrualDayCount { get; set; }

        /// <summary>
        /// Holiday calendar for calculation of accrual period dates.
        /// </summary>
        public IHolidayCalendar AccrualCalendar { get; set; }

        /// <summary>
        /// Day count convention for rate year fraction.
        /// </summary>
        public DayCount RateDayCount { get; set; }

        /// <summary>
        /// Holiday calendar for calculation of rate start date and rate end date.
        /// </summary>
        public IHolidayCalendar RateCalendar { get; set; }

        /// <summary>
        /// Holiday calendar for calculation of first rate end date.
        /// </summary>
        public IHolidayCalendar Rate1Calendar { get; set; }

        /// <summary>
        /// Holiday calendar for calculation of second rate end date.
        /// </summary>
        public IHolidayCalendar Rate2Calendar { get; set; }

        /// <summary>
        /// Number of business days between reset date and rate start date.
        /// </summary>
        public int RateOffset { get; set; }

        /// <summary>
        /// Adjustment method for calculation of rate end date.
        /// </summary>
        public DateAdjustmentMethod RateAdjustmentMethod { get; set; }

        /// <summary>
        /// Force rate end date to be last business day of month when rate start date is last business day of month.
        /// </summary>
        public YesNo RateStickyMonthEnd { get; set; }

        /// <summary>
        /// Generate rate start date from reset date.
        /// </summary>
        public double CalculateRateStartDate(double resetDate)
        {
            return DateAdjuster.Add(resetDate, RateOffset, RateCalendar);
        }

        /// <summary>
        /// Generate rate end date from rate start date and rate tenor (using RateCalendar).
        /// </summary>
        public double CalculateRateEndDate(double rateStartDate, double rateTenor)
        {
            return CalculateEndDate(rateStartDate, rateTenor, RateCalendar);
        }

        /// <summary>
        /// Generate rate end date from rate start date and rate tenor (using Rate1Calendar).
        /// </summary>
        public double CalculateRate1EndDate(double rateStartDate, double rateTenor)
        {
            return CalculateEndDate(rateStartDate, rateTenor, Rate1Calendar);
        }

        /// <summary>
        /// Generate rate end date from rate start date and rate tenor (using Rate2Calendar).
        /// </summary>
        public double CalculateRate2EndDate(double rateStartDate, double rateTenor)
        {
            return CalculateEndDate(rateStartDate, rateTenor, Rate2Calendar);
        }

        private double CalculateEndDate(double rateStartDate, double rateTenor, IHolidayCalendar rateCalendar)
        {
            if (rateTenor == 0.0)
                return rateStartDate;

            var term = Period.ValueToTerm(rateTenor);
            return DateAdjuster.Add(DateTime.FromOADate(rateStartDate), term, 1, rateCalendar, true, RateAdjustmentMethod, RateStickyMonthEnd == YesNo.Yes).ToOADate();
        }
    }

    /// <summary>
    /// Static class to hold helper methods for cashflow principals.
    /// </summary>
    public static class CashflowListPrincipal
    {
        /// <summary>
        /// Get principal amount of first interest cashflow with payment date > value date.
        /// This excludes cashflows that have no accrual period (which represent fixed payments).
        /// </summary>
        /// <typeparam name="TCashflow">The type of cashflows in the list (must implement IInterestCashflow).</typeparam>
        public static double GetPrincipal<TCashflow>(List<TCashflow> cashflows, double valueDate) where TCashflow : IInterestCashflow
        {
            IInterestCashflow cashflow = cashflows.FirstOrDefault(cf => cf.Accrual_End_Date > cf.Accrual_Start_Date && cf.Payment_Date > valueDate);

            if (cashflow != null)
                return cashflow.Notional;
            else
                return 0.0;
        }
    }

    /// <summary>
    /// Base class for cashflow list deals.
    /// </summary>
    /// <typeparam name="TCashflowList">Cashflow list type.</typeparam>
    [Serializable]
    public abstract class CFListBaseDeal<TCashflowList> : IRDeal
        where TCashflowList : ICFList, new()
    {
        protected TCashflowList fCashflows = new TCashflowList();
        protected string fDescription = string.Empty;
        private IDealReferenceProvider fDealReferenceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CFListBaseDeal"/> class.
        /// </summary>
        protected CFListBaseDeal()
        {
            fDealReferenceProvider = DealReferenceProvider.Empty;
        }

        /// <summary>
        /// Gets or sets the cashflow list position (Buy or Sell).
        /// </summary>
        public BuySell Buy_Sell
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cashflow list.
        /// </summary>
        public TCashflowList Cashflows
        {
            get { return fCashflows; }
            set { Property.Assign(fCashflows, value); }
        }

        /// <summary>
        /// Gets or sets the deal description.
        /// </summary>
        public string Description
        {
            get { return fDescription; }
            set { fDescription = value; }
        }

        /// <summary>
        /// Assign cashflows and take ownership.
        /// </summary>
        public void AssignCashflowsTakeOwnership(TCashflowList cashflows)
        {
            fCashflows = cashflows;
        }

        /// <summary>
        /// Returns true if the cashflow list deal is cash settled. 
        /// </summary>
        public virtual bool IsCashSettled()
        {
            return false;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return fCashflows.GetEndDate();
        }

        /// <summary>
        /// Validate the deal.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);
            fCashflows.Validate(errors);
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            // Return the description if there is one.
            // Otherwise return the currency text returned by IRDeal.Summary()
            return fDescription.Length > 0 ? fDescription : base.Summary();
        }

        /// <summary>
        /// Gets the deal reference provider.
        /// </summary>
        public IDealReferenceProvider GetDealReferenceProvider()
        {
            return fDealReferenceProvider;
        }

        /// <summary>
        /// Sets the deal reference provider.
        /// </summary>
        public void SetDealReferenceProvider(IDealReferenceProvider dealReferenceProvider)
        {
            if (dealReferenceProvider == null)
                throw new ArgumentNullException("dealReferenceProvider");

            fDealReferenceProvider = dealReferenceProvider;
        }
    }

    /// <summary>
    /// Base class for cashflow list deal valuation.
    /// </summary>
    /// <typeparam name="TCashflowList">Cashflow list type.</typeparam>
    [Serializable]
    public abstract class CFListBaseValuation<TCashflowList> : IRValuation, ISettlementOffset
        where TCashflowList : ICFList, new()
    {
        [NonSerialized]
        protected int fBuySellSign = 0;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb = null;
        [NonSerialized]
        protected RecoveryRate fRecoveryRate = null;
        [NonSerialized]
        protected CreditRating fCreditRating = null;
        [NonSerialized]
        protected CFRecoveryList fRecoveryList = null;
        [NonSerialized]
        protected double fCutoffDate = 0.0;

        protected SettlementOffsetHelper fSettlementOffsetHelper = new SettlementOffsetHelper();
        
        /// <inheritdoc/>
        public YesNo Use_Settlement_Offset
        {
            get { return fSettlementOffsetHelper.Use_Settlement_Offset; }
            set { fSettlementOffsetHelper.Use_Settlement_Offset = value; }
        }

        /// <inheritdoc/>
        public int Settlement_Offset
        {
            get { return fSettlementOffsetHelper.Settlement_Offset; }
            set { fSettlementOffsetHelper.Settlement_Offset = value; }
        }

        /// <inheritdoc/>
        public string Settlement_Offset_Calendars
        {
            get { return fSettlementOffsetHelper.Settlement_Offset_Calendars; }
            set { fSettlementOffsetHelper.Settlement_Offset_Calendars = value; }
        }

        /// <summary>
        /// Cashflow rounding convention.
        /// </summary>
        public CashflowRounding Cashflow_Rounding
        {
            get; set;
        }

        /// <inheritdoc />
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);

            CFListBaseDeal<TCashflowList> deal = (CFListBaseDeal<TCashflowList>)fDeal;

            fSettlementOffsetHelper.InitialiseHolidayCalendars(factors.CalendarData);
            deal.Cashflows.SetCashflowRounding(Cashflow_Rounding);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CFListBaseDeal<TCashflowList> deal = (CFListBaseDeal<TCashflowList>)fDeal;

            if (string.IsNullOrEmpty(fDeal.GetIssuer()))
                return;

            if (UseSurvivalProbability())
                factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.GetSurvivalProbability()) ? deal.GetIssuer() : deal.GetSurvivalProbability());

            if (RespectDefault())
            {
                factors.Register<RecoveryRate>(InterestRateUtils.GetRateId(deal.GetRecoveryRate(), deal.GetIssuer()));
                factors.Register<CreditRating>(deal.GetIssuer());
            }

            fSettlementOffsetHelper.ValidateHolidayCalendars(factors.CalendarData, errors);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CFListBaseDeal<TCashflowList> deal = (CFListBaseDeal<TCashflowList>)fDeal;
            fBuySellSign = deal.Buy_Sell == BuySell.Buy ? +1 : -1;

            fCutoffDate = fSettlementOffsetHelper.GetCutoffDate(factors.BaseDate);
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CFListBaseDeal<TCashflowList> deal = (CFListBaseDeal<TCashflowList>)fDeal;

            if (string.IsNullOrEmpty(fDeal.GetIssuer()))
                return;

            if (UseSurvivalProbability())
                fSurvivalProb = factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.GetSurvivalProbability()) ? deal.GetIssuer() : deal.GetSurvivalProbability());

            if (RespectDefault())
            {
                fRecoveryRate = factors.Get<RecoveryRate>(InterestRateUtils.GetRateId(deal.GetRecoveryRate(), deal.GetIssuer()));
                fCreditRating = factors.Get<CreditRating>(deal.GetIssuer());
            }
        }

        /// <summary>
        /// Calculate a valuation profile for the deal for the current scenario.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            var deal = (CFListBaseDeal<TCashflowList>)fDeal;

            double baseDate = factors.BaseDate;

            var tgi = new TimeGridIterator(fT);
            PVProfiles result = valuationResults.Profile;
            CashAccumulators cashAccumulators = valuationResults.Cash;

            double endDate = deal.EndDate();

            using (IntraValuationDiagnosticsHelper.StartDeal(fIntraValuationDiagnosticsWriter, fDeal))
            {
                using (var outerCache = Vector.Cache(factors.NumScenarios))
                {
                    Vector defaultDate = fCreditRating != null ? outerCache.Get(CalcUtils.DateTimeMaxValueAsDouble) : null;
                    var defaultedBeforeBaseDate = CreditRating.DefaultedBeforeBaseDate(fCreditRating, baseDate);
                    bool collectCash = ValueOnCashflowDates();

                    var saccrResult = SACCRResultFactory.Create(valuationResults, deal.GetDealReferenceProvider().DealReference,
                        () => new SACCROptionResult(factors.NumScenarios));

                    VectorEngine.For(tgi, () =>
                        {
                            using (var cache = Vector.Cache(factors.NumScenarios))
                            {
                                Vector pv = cache.GetClear();
                                Vector cash = collectCash ? cache.GetClear() : null;

                                if (!defaultedBeforeBaseDate)
                                {
                                    using (IntraValuationDiagnosticsHelper.StartCashflowsOnDate(fIntraValuationDiagnosticsWriter, tgi.Date))
                                    {
                                        using (IntraValuationDiagnosticsHelper.StartCashflows(fIntraValuationDiagnosticsWriter, fFxRate, tgi.T, deal))
                                        {
                                            Value(pv, cash, baseDate, tgi.Date, saccrResult, fIntraValuationDiagnosticsWriter);
                                            IntraValuationDiagnosticsHelper.AddCashflowsPV(fIntraValuationDiagnosticsWriter, pv);
                                        }
                                    }

                                    if (fCreditRating != null)
                                    {
                                        UpdateDefaultDate(fCreditRating, tgi.Date, tgi.T, defaultDate);
                                        GetDefaultValue(baseDate, tgi.Date, defaultDate, fRecoveryRate, pv, cash);
                                    }
                                }

                                result.AppendVector(tgi.Date, pv * fFxRate.Get(tgi.T));

                                if (!cashAccumulators.Ignore && cash != null)
                                {
                                    // Realise all value as cash on deal end date
                                    if (tgi.Date == endDate)
                                        cash.Assign(pv);

                                    cashAccumulators.Accumulate(fFxRate, tgi.Date, cash);
                                }
                            }
                        });

                    if (!cashAccumulators.Ignore && !collectCash)
                    {
                        CollectCashflows(cashAccumulators, baseDate, fT.fHorizon);

                        // Consolidate and net in order to avoid getting Net incoming and outgoing cashflows with the same payment date, 
                        // e.g. for compounding swaps with both positive and negative rates.
                        cashAccumulators.ConsolidateAndNet(fCurrency, factors);
                    }

                    result.Complete(fT);
                }
            }
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        public abstract void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter);

        /// <summary>
        /// Collect cashflows realised along the scenario path up to endDate.
        /// </summary>
        public abstract void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate);

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Returns true if the valuation model supports the Respect_Default parameter and it is set to Yes.
        /// </summary>
        protected virtual bool UseSurvivalProbability()
        {
            return false;
        }

        /// <summary>
        /// Returns true if the valuation model supports the Use_Survival_Probability parameter and it is set to Yes.
        /// </summary>
        protected virtual bool RespectDefault()
        {
            return false;
        }

        /// <summary>
        /// Returns true when the valuation is required on all cashflow dates.
        /// </summary>
        /// <remarks>
        /// When true, the cashflows are calculated in the loop over time grid points.
        /// When false, the cashflows are calculated by CollectCashflows.
        /// </remarks>
        protected virtual bool ValueOnCashflowDates()
        {
            CFListBaseDeal<TCashflowList> deal = (CFListBaseDeal<TCashflowList>)fDeal;
            return deal.IsCashSettled() || RespectDefault() && !string.IsNullOrEmpty(deal.GetIssuer());
        }
    }
}