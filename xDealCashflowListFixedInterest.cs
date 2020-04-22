using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Fixed interest cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Fixed Interest Cashflow List")]
    public class CFFixedInterestListDeal : CFListBaseDeal<CFFixedInterestList>
    {
        /// <summary>
        /// Constructor for fixed interest cashflow list deal.
        /// </summary>
        public CFFixedInterestListDeal()
        {
            Rate_Currency = string.Empty;
            Issuer = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate = string.Empty;
            Settlement_Style = SettlementType.Physical;
            Is_Defaultable = YesNo.No;
        }

        /// <summary>
        /// Gets or sets the rate currency.
        /// </summary>
        [NonMandatory]
        public string Rate_Currency
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the list of holiday calendar names.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); }
            set { SetCalendarNames(0, value); }
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
            var accrualCalendar = GetHolidayCalendar();
            if (fCashflows.FinishBuild(accrualCalendar))
                AddToErrors(errors, ErrorLevel.Info, "Missing cashflow properties have been calculated (Accrual_Year_Fraction)");

            base.Validate(calendar, errors);

            if (Settlement_Style == SettlementType.Cash && Settlement_Date == 0.0)
                AddToErrors(errors, "Settlement_Date must be specified when Settlement_Style is Cash");

            if (Settlement_Amount != 0.0 && Settlement_Date == 0.0)
                AddToErrors(errors, ErrorLevel.Warning, "Settlement_Amount is not zero but Settlement_Date is not specified so Settlement_Amount has been ignored.");

            if (fCashflows.Items.Any<CFFixedInterest>(cashflow => cashflow.Payment_Date <= Settlement_Date))
                AddToErrors(errors, "Cashflows must have Payment_Date after Settlement_Date");

            if (fCashflows.Items.Any<CFFixedInterest>(cashflow => cashflow.FX_Reset_Date > 0.0 || cashflow.Known_FX_Rate > 0.0))
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
        /// Last date for which a discount factor is required (last payment date).
        /// </summary>
        public override double RateEndDate()
        {
            return fCashflows.GetEndDate();
        }

        /// <summary>
        /// Estimate frequency of fixed side from cashflow list.
        /// </summary>
        /// <remarks>
        /// Maximum of accrual years fractions.
        /// Exclude first and last cashflow when more than two cashflows because they may be stub or long periods.
        /// </remarks>
        public override double RatePeriod()
        {
            int size = fCashflows.Items.Count;
            int first = size > 2 ? 1 : 0;
            int last = size > 2 ? size - 2 : size - 1;

            double ratePeriod = 0.0;

            for (int i = first; i <= last; ++i)
            {
                ratePeriod = Math.Max(ratePeriod, fCashflows[i].Accrual_Year_Fraction);
            }

            return ratePeriod;
        }
    }

    /// <summary>
    /// Fixed interest cashflow list deal valuation.
    /// </summary>
    [Serializable]
    [DisplayName("Fixed Interest Cashflow List Valuation")]
    public class CFFixedInterestListValuation : CFListBaseValuation<CFFixedInterestList>, ISingleDateValuation, ICanUseSurvivalProbability
    {
        [NonSerialized]
        IFxRate fRateFxRate = null;
        [NonSerialized]
        double fSettlementAmount = 0.0;
        [NonSerialized]
        double fAccruedInterest = 0.0;

        /// <summary>
        /// Constructor for fixed interest cashflow list deal valuation.
        /// </summary>
        public CFFixedInterestListValuation()
        {
            Use_Survival_Probability = YesNo.No;
            Respect_Default = YesNo.No;
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
        /// Get principal amount of first interest cashflow with payment date > value date.
        /// This excludes cashflows that have no accrual period (which represent fixed payments).
        /// </summary>
        public static double GetPrincipal(CFFixedInterestList cashflows, double valueDate)
        {
            return CashflowListPrincipal.GetPrincipal(cashflows.Items, valueDate);
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CFFixedInterestListDeal);
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (CFFixedInterestListDeal)Deal;

            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Survival_Probability = Yes or Respect_Default = Yes ignored for cashflow list deal with missing Issuer.");

            if (Use_Settlement_Offset == YesNo.Yes && deal.Settlement_Date != 0.0)
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Settlement_Offset = Yes ignored for cashflow list deal with Settlement_Date.");
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)fDeal;

            // Register factor for translation from rate currency to settlement currency for cashflows with FX reset date
            if (!string.IsNullOrEmpty(deal.Rate_Currency) && deal.Rate_Currency != fCurrency)
                factors.RegisterInterface<IFxRate>(deal.Rate_Currency);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)Deal;

            // Set up cashflow list
            deal.Cashflows.CalculateInterest(factors.BaseDate);

            // Add to valuation time grid
            bool payDatesRequired = ValueOnCashflowDates() && requiredResults.CashRequired();
            fT.AddPayDates<CFFixedInterest>(deal.Cashflows, payDatesRequired);

            double baseDate = factors.BaseDate;
            double settlementDate = deal.Settlement_Date;

            if (settlementDate >= baseDate)
            {
                fT.AddPayDate(settlementDate, payDatesRequired);

                var accrualCalendar = deal.GetHolidayCalendar();
                fAccruedInterest = deal.Cashflows.CalculateAccrual(settlementDate, false, accrualCalendar);
                fSettlementAmount = deal.Settlement_Amount;
                if (deal.Settlement_Amount_Is_Clean == YesNo.Yes)
                    fSettlementAmount += fAccruedInterest;
            }

            // Settlement date takes precedence.
            if (Use_Settlement_Offset == YesNo.Yes && settlementDate != 0.0)
                fCutoffDate = 0.0;

            if (deal.Investment_Horizon > 0.0)
                fT.AddPayDate(deal.Investment_Horizon, payDatesRequired);

            if (Use_Survival_Probability == YesNo.Yes)
            {
                fRecoveryList = new CFRecoveryList();
                fRecoveryList.PopulateRecoveryCashflowList(baseDate, settlementDate, deal.Cashflows);
            }
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)fDeal;

            // Get factor for translation from rate currency to settlement currency for cashflows with FX reset date
            if (!string.IsNullOrEmpty(deal.Rate_Currency) && deal.Rate_Currency != fCurrency)
                fRateFxRate = factors.GetInterface<IFxRate>(deal.Rate_Currency);
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            Value(pv, cash, baseDate, valueDate, null, fDiscountRate, null, fRepoRate, null, null, fSurvivalProb, saccrResult,
                intraValuationDiagnosticsWriter);

            // Add accruedInterest to Intra-valuation diagnostics
            if (intraValuationDiagnosticsWriter.Level > IntraValuationDiagnosticsLevel.None)
            {
                CFFixedInterestListDeal deal = (CFFixedInterestListDeal)Deal;
                using (var cache = Vector.CacheLike(pv))
                {
                    Vector accruedInterest = cache.Get(deal.Cashflows.CalculateAccrual(valueDate, false, deal.GetHolidayCalendar()));
                    IntraValuationDiagnosticsHelper.AddCashflowsAccruedInterest(fIntraValuationDiagnosticsWriter, accruedInterest);
                }
            }
        }

        /// <summary>
        /// Value the deal using the cashflow list.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        public void Value(Vector pv, Vector cash, double baseDate, double valueDate, Vector settlementDate, IInterestRate discount,
            IInterestRate forecast, IInterestRate repo, IInterestRateVol interestRateVol, IInterestYieldVol interestYieldVol,
            ISurvivalProb survivalProb, ISACCRResult saccrResult, IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)Deal;
            pv.Clear();
            if (cash != null)
                cash.Clear();

            deal.Cashflows.Value(pv, cash, null, baseDate, valueDate, settlementDate, discount, survivalProb, fFxRate, fRateFxRate, intraValuationDiagnosticsWriter, fCutoffDate);

            using (var cache = Vector.CacheLike(pv))
            {
                Vector sp = cache.Get(1.0);

                double dealSettlementDate = deal.Settlement_Date;
                double t = CalcUtils.DaysToYears(valueDate - baseDate);
                double tSettle = CalcUtils.DaysToYears(dealSettlementDate - baseDate);

                if (Use_Survival_Probability == YesNo.Yes && survivalProb != null)
                {
                    survivalProb.GetValue(sp, t, tSettle);
                    fRecoveryList.Value(pv, baseDate, valueDate, discount, survivalProb, intraValuationDiagnosticsWriter);
                }

                if (valueDate < dealSettlementDate)
                {
                    // Forward deal before settlement date
                    if (deal.Is_Defaultable == YesNo.No)
                        pv.Assign((pv / discount.Get(t, tSettle) - fSettlementAmount) * repo.Get(t, tSettle));
                    else
                        pv.Subtract(fAccruedInterest * discount.Get(t, tSettle) * sp + (fSettlementAmount - fAccruedInterest) * repo.Get(t, tSettle));
                }
                else if (valueDate == dealSettlementDate)
                {
                    // Forward deal at settlement date
                    pv.Subtract(fSettlementAmount);
                    if (cash != null)
                    {
                        if (deal.Settlement_Style == SettlementType.Cash)
                            cash.Assign(pv);
                        else
                            cash.Subtract(fSettlementAmount);
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
            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)Deal;

            deal.Cashflows.CollectCashflows(cashAccumulators, fFxRate, fRateFxRate, baseDate, endDate, fBuySellSign, fCutoffDate);

            double settlementDate = deal.Settlement_Date;
            if (settlementDate >= baseDate && settlementDate <= endDate)
                cashAccumulators.Accumulate(fFxRate, settlementDate, -fBuySellSign * fSettlementAmount);
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            base.Value(valuationResults, factors, baseTimes);

            var deal = (CFFixedInterestListDeal)Deal;

            CalculateMetrics(valuationResults, factors, deal);

            var accruedResults = valuationResults.Results<AccruedInterest>();
            if (accruedResults == null)
                return;

            var tgi = new TimeGridIterator(fT);
            while (tgi.Next())
            {
                double accruedInterest = deal.Cashflows.CalculateAccrual(tgi.Date, accruedResults.AccrueFromToday, fDeal.GetHolidayCalendar());
                accruedResults.SetValue(tgi.Date, fBuySellSign * accruedInterest);
            }
        }

        /// <summary>
        /// Modify the pv and cash taking the date of default into account.
        /// </summary>
        public override void GetDefaultValue(double baseDate, double valueDate, Vector defaultDate, RecoveryRate recoveryRate, Vector pv, Vector cash)
        {
            CFFixedInterestListDeal deal = (CFFixedInterestListDeal)fDeal;

            double principal = GetPrincipal(deal.Cashflows, valueDate);
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
                    double settlementAmount = fBuySellSign * fSettlementAmount;

                    Vector hasDefaulted = cache.Get(defaultDate <= valueDate);

                    pv.AssignConditional(hasDefaulted, fRepoRate.Get(t, tSettle) * (recovery - settlementAmount), pv);

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
        /// Calculate valuation metrics requested by the Base Valuation calculation.
        /// </summary>
        private void CalculateMetrics(ValuationResults valuationResults, PriceFactorList factors, CFFixedInterestListDeal deal)
        {
            var results = valuationResults.Results<ValuationMetrics>();
            if (results == null)
                return;

            if (results.IsMetricRequested(ValuationMetricConstants.Duration))
            {
                using (var cache = Vector.Cache(factors.NumScenarios))
                {
                    Vector duration = cache.GetClear();
                    Vector settlementDate = cache.Get(deal.Settlement_Date);
                    deal.Cashflows.Duration(duration, factors.BaseDate, factors.BaseDate, settlementDate, fDiscountRate, fCutoffDate);
                    results.SetMetricValue(ValuationMetricConstants.Duration, new ValuationId(this), duration[0]);
                }
            }

            if (results.IsMetricRequested(ValuationMetricConstants.AccruedInterest))
            {
                double? parameter = results.GetMetricParameter(ValuationMetricParameterConstants.AccrueFromToday);
                bool accrueFromToday = parameter.HasValue && parameter.Value == 1.0;
                double accruedInterest = deal.Cashflows.CalculateAccrual(factors.BaseDate, accrueFromToday, deal.GetHolidayCalendar());
                double buySellSign = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;
                results.SetMetricValue(ValuationMetricConstants.AccruedInterest, new ValuationId(this), buySellSign * accruedInterest);
            }
        }
    }
}