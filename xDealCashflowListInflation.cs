/// <author>
/// Phil Koop
/// </author>
/// <owner>
/// Phil Koop
/// </owner>
/// <summary>
/// Deal and valuation class for floating inflation cashflow lists.
/// </summary>
using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Interface for inflation cashflow list deals.
    /// </summary>
    public interface IInflationCashflowListDeal
    {
        BuySell Buy_Sell
        {
            get; set;
        }

        string Index
        {
            get; set;
        }

        string Description
        {
            get; set;
        }

        string Calendars
        {
            get; set;
        }

        string Issuer
        {
            get; set;
        }

        string Recovery_Rate
        {
            get; set;
        }

        string Survival_Probability
        {
            get; set;
        }

        string Repo_Rate
        {
            get; set;
        }

        TDate Settlement_Date
        {
            get; set;
        }

        TDate Investment_Horizon
        {
            get; set;
        }

        IHolidayCalendar GetHolidayCalendar();

        /// <summary>
        /// Determines whether cashflow valuation requires InflationRate price factor.
        /// </summary>
        bool NeedInflationRate();

        /// <summary>
        /// Return the underlying cashflow list as an interface.
        /// </summary>
        IInflationCashflowList GetCashflows();

        /// <summary>
        /// Add the payment dates from this list to the given grid.
        /// </summary>
        void AddPayDates(TimeGrid timeGrid);
    }

    /// <summary>
    /// Base inflation cashflow list deal.
    /// </summary>
    [Serializable]
    public abstract class InflationCashflowListDealBase<CFInflationType> : StandardDeal, IInflationCashflowListDeal
        where CFInflationType : CFComparable<CFInflationType>, ICFInflation, new()
    {
        protected InflationCashflowList<CFInflationType> fCashflows = new InflationCashflowList<CFInflationType>();

        /// <summary>
        /// Constructor.
        /// </summary>
        protected InflationCashflowListDealBase()
        {
            Index                = string.Empty;
            Description          = string.Empty;
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
            Repo_Rate            = string.Empty;

            // Initialize common values for convenience
            Is_Forward_Deal = YesNo.No;
        }

        public InflationCashflowList<CFInflationType> Cashflows
        {
            get { return fCashflows; } set { Property.Assign(fCashflows, value); }
        }

        public BuySell Buy_Sell
        {
            get; set;
        }

        public YesNo Is_Forward_Deal
        {
            get; set;
        }

        public string Index
        {
            get; set;
        }

        [NonMandatory]
        public string Description
        {
            get; set;
        }

        [NonMandatory]
        public string Issuer
        {
            get; set;
        }

        [NonMandatory]
        public string Survival_Probability
        {
            get; set;
        }

        [NonMandatory]
        public string Recovery_Rate
        {
            get; set;
        }

        [NonMandatory]
        public TDate Settlement_Date
        {
            get; set;
        }

        [NonMandatory]
        public string Repo_Rate
        {
            get; set;
        }

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

        [NonMandatory]
        public TDate Investment_Horizon
        {
            get; set;
        }

        /// <summary>
        /// Returns true if the cashflow valuation requires InflationRate price factor
        /// </summary>
        public virtual bool NeedInflationRate()
        {
            return true;
        }

        /// <summary>
        /// Return the underlying cashflow list as an interface.
        /// </summary>
        public IInflationCashflowList GetCashflows()
        {
            return fCashflows;
        }

        /// <summary>
        /// Add the payment dates from this list to the given grid.
        /// </summary>
        public void AddPayDates(TimeGrid timeGrid)
        {
            timeGrid.AddPayDates<CFInflationType>(fCashflows);
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            if (Is_Forward_Deal == YesNo.Yes)
            {
                return Settlement_Date;
            }
            else
            {
                if (Investment_Horizon > 0.0)
                    return Math.Min(fCashflows.GetEndDate(), Investment_Horizon);
                else
                    return fCashflows.GetEndDate();
            }
        }

        /// <summary>
        /// Validate the deal.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);
            fCashflows.Validate(errors);

            if (Is_Forward_Deal == YesNo.Yes && Settlement_Date == 0.0)
                AddToErrors(errors, "Must specify a settlement date for a forward inflation bond cashflow list deal");

            if (NeedInflationRate() && String.IsNullOrEmpty(Index))
                AddToErrors(errors, "Must specify an index for an inflation cashflow list deal");
        }

        /// <summary>
        /// Deal description.
        /// </summary>
        public override string Summary()
        {
            return Description.Length > 0 ? Description : base.Summary();
        }
    }

    /// <summary>
    /// Fixed inflation cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Fixed Inflation Cashflow List")]
    public class FixedInflationCashflowListDeal : InflationCashflowListDealBase<CFFixedInflation>
    {
        /// <summary>
        /// Returns list of properties in the order defined.
        /// </summary>
        /// <returns>Returns the list properties with Index excluded. </returns>
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = base.GetProperties(attributes);
            PropertyDescriptor           index      = properties.Find("Index", false);
            properties.Remove(index);
            return properties;
        }

        /// <summary>
        /// Returns true if the cashflow valuation requires InflationRate price factor
        /// </summary>
        public override bool NeedInflationRate()
        {
            return false;
        }
    }

    /// <summary>
    /// Floating inflation cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Floating Inflation Cashflow List")]
    public class FloatingInflationCashflowListDeal : InflationCashflowListDealBase<CFFloatingInflation>
    {
    }

    /// <summary>
    /// Real yield cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Real Yield Cashflow List")]
    public class YieldInflationCashflowListDeal : InflationCashflowListDealBase<CFYieldInflation>
    {
    }

    /// <summary>
    /// Inflation option cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Inflation Option Cashflow List")]
    public class InflationOptionCashflowListDeal : InflationCashflowListDealBase<CFInflationOption>
    {
        private string fPriceIndexVolatility = string.Empty;

        /// <summary>
        /// Specifies the index volatility for inflation options. 
        /// </summary>
        public string Price_Index_Volatility
        {
            get
            {
                return string.IsNullOrEmpty(fPriceIndexVolatility) ? Index : fPriceIndexVolatility;
            }

            set
            {
                fPriceIndexVolatility = value;
            }
        }
    }

    /// <summary>
    /// Inflation cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Inflation Cashflow List Valuation")]
    abstract public class InflationCashflowListValuation : StandardValuation
    {
        [NonSerialized]
        protected IInflationRate          fInflationRate        = null;
        [NonSerialized]
        protected IPriceIndexVolatility   fIndexVolatility      = null;
        [NonSerialized]
        protected IInterestRate           fRepoRate             = null;
        [NonSerialized]
        protected CreditRating            fCreditRating         = null;
        [NonSerialized]
        protected RecoveryRate            fRecoveryRate         = null;
        [NonSerialized]
        protected ISurvivalProb           fSurvivalProb         = null;
        [NonSerialized]
        protected CFRecoveryInflationList fRecoveryCashflowList = null;
        [NonSerialized]
        protected bool                    fIsDefaultNever       = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected InflationCashflowListValuation()
        {
            Respect_Default          = YesNo.No;
            Use_Survival_Probability = YesNo.No;
        }

        public YesNo Respect_Default
        {
            get; set;
        }

        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(IInflationCashflowListDeal);
        }

        /// <summary>
        /// Vaulation becomes path-dependent if valuation model respects default.
        /// </summary>
        public override bool FullPricing()
        {
            return NeedCreditRating();
        }

        /// <summary>
        /// Returns the ID of the price index volatility price factor; if not required, null.
        /// </summary>
        public virtual string GetPriceIndexVolatility()
        {
            return null;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (IInflationCashflowListDeal)Deal;

            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                Deal.AddToErrors(errors, ErrorLevel.Warning, string.Format("For deal valued using {0}, Issuer is missing but Use_Survival_Probability or Respect_Default is set to Yes; valuation of this deal will be treated as if Use_Survival_Probability and Respect_Default are both No.", GetType().Name));
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            IInflationCashflowListDeal deal = (IInflationCashflowListDeal)Deal;

            if (deal.NeedInflationRate())
                factors.RegisterInterface<IInflationRate>(deal.Index);

            if (NeedCreditRating())
                factors.Register<CreditRating>(deal.Issuer);

            if (NeedRecoveryRate())
                factors.Register<RecoveryRate>(GetRecoveryRateID());

            if (NeedSurvivalProb())
                factors.RegisterInterface<ISurvivalProb>(GetSurvivalProbID());

            if (!string.IsNullOrEmpty(deal.Repo_Rate))
                factors.RegisterInterface<IInterestRate>(deal.Repo_Rate);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            IInflationCashflowListDeal deal = (IInflationCashflowListDeal)Deal;

            // Set up cashflow list
            IInflationRate inflationRate = deal.NeedInflationRate() ? factors.GetInterface<IInflationRate>(deal.Index) : null;
            deal.GetCashflows().PreCloneInitialize(factors.BaseDate, inflationRate, deal.GetHolidayCalendar());

            // Add to valuation time grid
            deal.AddPayDates(fT);

            if (deal.Investment_Horizon > 0.0)
                fT.AddPayDate(deal.Investment_Horizon, requiredResults.CashRequired());

            if (deal.Settlement_Date > 0.0)
                fT.AddPayDate(deal.Settlement_Date, requiredResults.CashRequired());

            // Recovery cashflows are created on the fly to respect customized cashflows
            if (NeedRecoveryCashflows())
            {
                fRecoveryCashflowList = new CFRecoveryInflationList();
                fRecoveryCashflowList.PopulateRecoveryCashflowList(factors.BaseDate, deal.Settlement_Date, deal.GetCashflows());
            }
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        /// <param name="factors">Price factors.</param>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            IInflationCashflowListDeal deal = (IInflationCashflowListDeal)Deal;

            fIsDefaultNever  = !NeedCreditRating();
            fCreditRating    = NeedCreditRating()                      ? factors.Get<CreditRating>(deal.Issuer)                       : null;
            fRecoveryRate    = NeedRecoveryRate()                      ? factors.Get<RecoveryRate>(GetRecoveryRateID())               : null;
            fSurvivalProb    = NeedSurvivalProb()                      ? factors.GetInterface<ISurvivalProb>(GetSurvivalProbID())     : null;
            fInflationRate   = !string.IsNullOrEmpty(deal.Index)       ? factors.GetInterface<IInflationRate>(deal.Index)             : null;
            fRepoRate        = !string.IsNullOrEmpty(deal.Repo_Rate)   ? factors.GetInterface<IInterestRate>(deal.Repo_Rate)          : fDiscountRate;
            fIndexVolatility = deal is InflationOptionCashflowListDeal ? factors.GetInterface<IPriceIndexVolatility>(GetPriceIndexVolatility()) : null;
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            var    result          = valuationResults.Profile;
            var    cashAccumulator = valuationResults.Cash;
            var    accruedResults  = valuationResults.Results<AccruedInterest>();
            var    deal            = (IInflationCashflowListDeal)Deal;
            double sign            = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;
            var    cashflows       = deal.GetCashflows();
            var    calendar        = deal.GetHolidayCalendar();
            double baseDate        = factors.BaseDate;
            var    tgi             = new TimeGridIterator(fT);

            CalculateMetrics(valuationResults, factors, deal);

            using (IntraValuationDiagnosticsHelper.StartDeal(fIntraValuationDiagnosticsWriter, fDeal))
            {
                using (var cache = Vector.Cache(factors.NumScenarios))
                {
                    Vector defaultDate = null;

                    if (!fIsDefaultNever)
                        defaultDate = fCreditRating != null ? cache.Get(CalcUtils.DateTimeMaxValueAsDouble) : null;

                    Vector pv = cache.GetClear();
                    Vector cash = cashAccumulator.Ignore ? null : cache.Get();
                    Vector accruedInterest = cache.GetClear();

                    VectorEngine.For(tgi, () =>
                    {
                        if (!fIsDefaultNever && CreditRating.DefaultedBeforeBaseDate(fCreditRating, baseDate))
                        {
                            result.AppendVector(tgi.Date, pv);
                            return LoopAction.Break;
                        }

                        using (IntraValuationDiagnosticsHelper.StartCashflowsOnDate(fIntraValuationDiagnosticsWriter, tgi.Date))
                        {
                            using (IntraValuationDiagnosticsHelper.StartCashflows(fIntraValuationDiagnosticsWriter, fFxRate, tgi.T, fDeal))
                            {
                                cashflows.Value(pv, cash, baseDate, tgi.Date, deal.Settlement_Date, fInflationRate, fIndexVolatility,
                                    fDiscountRate, fRepoRate, fSurvivalProb, sign, fIntraValuationDiagnosticsWriter);
                                IntraValuationDiagnosticsHelper.AddCashflowsPV(fIntraValuationDiagnosticsWriter, pv);

                                if (fRecoveryCashflowList != null && fRecoveryCashflowList.Items.Count > 0)
                                    fRecoveryCashflowList.Value(pv, cash, baseDate, tgi.Date, deal.Settlement_Date, fDiscountRate, fRepoRate, fInflationRate, fSurvivalProb, sign);

                                // Temporary fix up to avoid calculating default when we know the model doesn't support default
                                if (!fIsDefaultNever)
                                {
                                    UpdateDefaultDate(fCreditRating, tgi.Date, tgi.T, defaultDate);
                                    GetDefaultValue(baseDate, tgi.Date, defaultDate, fInflationRate, fIndexVolatility, fRepoRate, pv, cash);
                                }

                                result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * pv);

                                if (cash != null)
                                    cashAccumulator.Accumulate(fFxRate, tgi.Date, cash);

                                if (accruedResults != null)
                                {
                                    cashflows.CalculateAccrual(accruedInterest, baseDate, tgi.Date, accruedResults.AccrueFromToday, calendar, fInflationRate, fIndexVolatility, sign);
                                    accruedResults.SetValue(tgi.Date, accruedInterest);
                                }
                                else if (fIntraValuationDiagnosticsWriter.Level > IntraValuationDiagnosticsLevel.None)
                                {
                                    cashflows.CalculateAccrual(accruedInterest, baseDate, tgi.Date, false, calendar, fInflationRate, fIndexVolatility, sign);
                                }

                                IntraValuationDiagnosticsHelper.AddCashflowsAccruedInterest(fIntraValuationDiagnosticsWriter, accruedInterest);
                            }
                        }

                        return LoopAction.Continue;
                    });

                    // On investment horizon or a bond forward's Settlement Date, the deal value is liquidated as cash.
                    double endDate = Deal.EndDate();
                    if (cash != null && endDate <= fT.fHorizon)
                    {
                        // If endDate on a payment date, cashflow has already been accummulated (as cash), otherwise is 0.
                        // Value liquidated is the value of the pv remaining after accummulating the cashflow.
                        cash.AssignDifference(pv, cash);
                        cashAccumulator.Accumulate(fFxRate, endDate, cash);
                    }
                }

                result.Complete(fT);
            }
        }

        /// <summary>
        /// Sets defaultDate to valueDate on the first call for which t >= defaultTime.
        /// </summary>
        /// <remarks>Assumes that defaultDate was initialized to a large date value.</remarks>
        protected static void UpdateDefaultDate(CreditRating creditRating, double valueDate, double t, Vector defaultDate)
        {
            using (var cache = Vector.CacheLike(defaultDate))
            {
                // Get time of default
                Vector defaultTime = cache.Get();
                creditRating.DefaultTime(defaultTime);

                defaultDate.AssignConditional(defaultTime > t, defaultDate, VectorMath.Min(defaultDate, valueDate));
            }
        }

        /// <summary>
        /// Modify the pv and cash taking the date of default into account.
        /// </summary>
        protected void GetDefaultValue(double baseDate, double valueDate, Vector defaultDate, IInflationRate inflationRate, IPriceIndexVolatility indexVolatility, IInterestRate repo, Vector pv, Vector cash)
        {
            IInflationCashflowListDeal deal = (IInflationCashflowListDeal)Deal;

            double settlementDate = deal.Settlement_Date;
            double t              = CalcUtils.DaysToYears(valueDate - baseDate);
            double buySellSign    = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;

            if (repo == null)
                repo = fDiscountRate;

            using (var cache = Vector.CacheLike(pv))
            {
                Vector principal = cache.Get();
                GetCurrentExposure(principal, t, valueDate, inflationRate);

                // Approximation: recover only principal, neglecting accrued interest.
                Vector recovery = cache.Get(buySellSign * principal * fRecoveryRate.Get(t));

                if (valueDate <= settlementDate)
                {
                    // Set the pv to (recovery - |settlementAmount|) * df when defaultDate <= valueDate <= settlementDate.
                    // Set cash to (recovery - |settlementAmount|) when defaultDate <= valueDate = settlementDate (cash is always zero before settlementDate).
                    // Note that GetSettlementAmount(...) will return a negative value for a long bond position, indicating an outgoing cashflow.
                    double tSettle          = CalcUtils.DaysToYears(settlementDate - baseDate);
                    Vector settlementAmount = cache.Get();
                    GetSettlementAmount(settlementAmount, valueDate, baseDate, inflationRate, indexVolatility);
                    settlementAmount.MultiplyBy(buySellSign);

                    Vector hasDefaulted = cache.Get(defaultDate <= valueDate);

                    pv.AssignConditional(hasDefaulted, repo.Get(t, tSettle) * (recovery + settlementAmount), pv);

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
        /// Returns the current exposure amount in nominal dollars based on the recovery cashflowlist
        /// </summary>
        protected void GetCurrentExposure(Vector exposure, double t, double valueDate, IInflationRate inflationRate)
        {
            if (fRecoveryCashflowList != null)
            {
                for (int i = 0; i < fRecoveryCashflowList.Count(); i++)
                {
                    if (fRecoveryCashflowList[i].Payment_Date >= valueDate)
                    {
                        var cashflow = fRecoveryCashflowList[i];
                        cashflow.GetExposure(exposure, t, valueDate, inflationRate);
                        return;
                    }
                }
            }

            exposure.Assign(0.0);
        }

        /// <summary>
        /// Returns the payment amount on settlement date based on the cashflowlist.
        /// </summary>
        /// <remarks>
        /// The payment amount on settlement date will be either the dirty price of the bond or the clean price of the bond plus
        /// the accrued interest (two cashflows). Note that for a long position in a bond forward, this value is negative
        /// (indicating outgoing cashflow).
        /// </remarks>
        protected virtual void GetSettlementAmount(Vector amount, double valueDate, double baseDate, IInflationRate inflationRate, IPriceIndexVolatility indexVolatility)
        {
            amount.Clear();
            using (var cache = Vector.CacheLike(amount))
            {
                var    deal          = (IInflationCashflowListDeal)Deal;
                var    cashflows     = deal.GetCashflows();
                Vector settlementPay = cache.Get();

                for (int i = 0; i < cashflows.Count(); ++i)
                {
                    if (cashflows.GetCashflow(i).Payment_Date < deal.Settlement_Date)
                    {
                        continue;
                    }
                    else if (cashflows.GetCashflow(i).Payment_Date == deal.Settlement_Date)
                    {
                        cashflows.GetCashflow(i).ExpectedAmount(settlementPay, CalcUtils.DaysToYears(valueDate - baseDate), inflationRate, indexVolatility,
                            IntraValuationDiagnosticsWriterFactory.GetOrCreate(IntraValuationDiagnosticsLevel.None));
                        amount.Add(settlementPay);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        protected virtual bool DealHasIssuer()
        {
            IInflationCashflowListDeal deal = (IInflationCashflowListDeal)Deal;
            return !string.IsNullOrEmpty(deal.Issuer);
        }

        /// <summary>
        /// True if requiring CreditRating price factor; false otherwise.
        /// </summary>
        protected bool NeedCreditRating()
        {
            return Respect_Default == YesNo.Yes && DealHasIssuer();
        }

        /// <summary>
        /// True if requiring Recovery price factor; false otherwise.
        /// </summary>
        protected bool NeedRecoveryRate()
        {
            return Respect_Default == YesNo.Yes && DealHasIssuer();
        }

        /// <summary>
        /// True if requiring SurvivalProb price factor; false otherwise.
        /// </summary>
        protected bool NeedSurvivalProb()
        {
            return Use_Survival_Probability == YesNo.Yes && DealHasIssuer();
        }

        /// <summary>
        /// True if valuation requires an accompanying recovery cashflow list.
        /// </summary>
        protected virtual bool NeedRecoveryCashflows()
        {
            return NeedSurvivalProb();
        }

        /// <summary>
        /// If RecoveryRate price factor is required, the ID of the price factor; null otherwise.
        /// </summary>
        /// <remarks>
        /// If Recovery_Rate is missing, the ID of the Issuer will be used instead.
        /// </remarks>
        private string GetRecoveryRateID()
        {
            var deal = (IInflationCashflowListDeal)Deal;

            if (NeedRecoveryRate())
                return string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate;

            return null;
        }

        /// <summary>
        /// Returns the ID of the price factor if SurvivalProb price factor is required; null otherwise.
        /// </summary>
        /// <remarks>
        /// If Survival_Probability is missing, the ID of the Issuer will be used instead.
        /// </remarks>
        private string GetSurvivalProbID()
        {
            var deal = (IInflationCashflowListDeal)Deal;

            if (NeedSurvivalProb())
                return string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability;
            
            return null;
        }

        /// <summary>
        /// Calculate valuation metrics requested by the Base Valuation calculation.
        /// </summary>
        private void CalculateMetrics(ValuationResults valuationResults, PriceFactorList factors, IInflationCashflowListDeal deal)
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
                    double buySellSign = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;
                    deal.GetCashflows().CalculateAccrual(accruedInterest, factors.BaseDate, factors.BaseDate, accrueFromToday, deal.GetHolidayCalendar(), fInflationRate, fIndexVolatility, buySellSign);
                    results.SetMetricValue(ValuationMetricConstants.AccruedInterest, new ValuationId(this), accruedInterest[0]);
                }
            }
        }
    }

    /// <summary>
    /// Fixed inflation cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Fixed Inflation Cashflow List Valuation")]
    public class FixedInflationCashflowListValuation : InflationCashflowListValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(FixedInflationCashflowListDeal);
        }
    }

    /// <summary>
    /// Floating inflation cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Floating Inflation Cashflow List Valuation")]
    public class FloatingInflationCashflowListValuation : InflationCashflowListValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(FloatingInflationCashflowListDeal);
        }
    }

    /// <summary>
    /// Real yield inflation cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Real Yield Cashflow List Valuation")]
    public class YieldInflationCashflowListValuation : InflationCashflowListValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(YieldInflationCashflowListDeal);
        }
    }

    /// <summary>
    /// Inflation option cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Inflation Option Cashflow List Valuation")]
    public class InflationOptionCashflowListValuation : InflationCashflowListValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(InflationOptionCashflowListDeal);
        }

        /// <summary>
        /// Returns the ID of the price index volatility price factor; if not required, null.
        /// </summary>
        public override string GetPriceIndexVolatility()
        {
            return ((InflationOptionCashflowListDeal)Deal).Price_Index_Volatility;
        }

        /// <summary>
        /// True if valuation requires an accompanying recovery cashflow list.
        /// </summary>
        protected override bool NeedRecoveryCashflows()
        {
            return false; // It's assumed that bond recovery does not consider value of optionality.
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            // Deal must be of IInflationOptionCashflowListDeal type to subscribe to Price Index Volatility.
            factors.RegisterInterface<IPriceIndexVolatility>(((InflationOptionCashflowListDeal)Deal).Price_Index_Volatility);
        }

    }
}
