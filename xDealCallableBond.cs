/// <author>
/// Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for callable bonds.
/// </summary>

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Collections.Generic;
using System.Linq;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    [Serializable]
    [System.ComponentModel.DisplayName("Callable Bond")]
    public class CallableBond : CallableBondForward
    {
        [NonMandatory]
        public TDate Investment_Horizon
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Investment_Horizon > 0 ? Investment_Horizon : Bond_Maturity_Date;
        }

        /// <summary>
        /// Is cash-settled forward deal.
        /// </summary>
        public override bool IsForward()
        {
            return false;
        }
    }

    [Serializable]
    [System.ComponentModel.DisplayName("Callable Bond Forward")]
    public class CallableBondForward : IRDeal
    {
        [NonMandatory]
        public string Repo_Rate
        {
            get { return fRepo; } set { fRepo = value; }
        }

        public BuySell Buy_Sell
        {
            get; set;
        }

        public TDate Issue_Date
        {
            get; set;
        }

        public TDate Bond_Maturity_Date
        {
            get; set;
        }

        public double Notional
        {
            get; set;
        }

        public Period Coupon_Interval
        {
            get; set;
        }

        public PaymentTiming Payment_Timing
        {
            get; set;
        }

        public double Coupon_Rate
        {
            get; set;
        }

        [NonMandatory]
        public RateList Coupon_Rate_Schedule
        {
            get; set;
        }
        public DayCount Accrual_Day_Count
        {
            get; set;
        }

        [NonMandatory]
        public TDate First_Coupon_Date
        {
            get; set;
        }

        [NonMandatory]
        public TDate Penultimate_Coupon_Date
        {
            get; set;
        }

        [NonMandatory]
        public Amortisation Amortisation
        {
            get; set;
        }

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); }
            set { SetCalendarNames(0, value); }
        }

        public TDate Settlement_Date
        {
            get; set;
        }

        public double Price
        {
            get; set;
        }

        public YesNo Price_Is_Clean
        {
            get; set;
        }

        public YesNo Is_Defaultable
        {
            get; set;
        }

        public OptionType Call_Put
        {
            get; set;
        }

        public TDate First_Call_Date
        {
            get; set;
        }

        public TDate Last_Call_Date
        {
            get; set;
        }

        public RateList Call_Prices
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the call option volatility.
        /// </summary>
        [NonMandatory]
        public string Yield_Volatility
        {
            get { return fForecastVolatility; }
            set { fForecastVolatility = value; }
        }

        [NonMandatory]
        public string Issuer
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the survival probability price factor ID.
        /// </summary>
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

        public CallableBondForward()
        {
            Amortisation         = new Amortisation();
            Payment_Timing       = PaymentTiming.End;
            Coupon_Rate_Schedule = new RateList();
            Is_Defaultable       = YesNo.No;
            Call_Prices          = new RateList();
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
        }

        /// <summary>
        /// Deal end date.
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

            if (Notional < CalcUtils.MinAssetPrice)
                AddToErrors(errors, string.Format("Bond Notional must be at least {0}", CalcUtils.MinAssetPrice));

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, First_Coupon_Date, Penultimate_Coupon_Date, false, "Issue", "bond maturity");

            Coupon_Rate_Schedule.Validate(errors, false, "Fixed rate schedule");
            Amortisation.Validate(errors);

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, true, "Issue", "bond maturity");

            if (Settlement_Date != 0.0)
                CalcUtils.ValidateDates(errors, Settlement_Date, Bond_Maturity_Date, true, "Settlement", "bond maturity");

            CalcUtils.ValidateDates(errors, First_Call_Date, Last_Call_Date, false, "First call", "last call");

            Call_Prices.Validate(errors, true, "Call prices");

            if (IsForward())
            {
                if (Settlement_Date == 0.0)
                    AddToErrors(errors, "Settlement_Date must be specified");

                if (Price == 0.0)
                    AddToErrors(errors, ErrorLevel.Info, "Settlement price (Price) is zero.");
            }
            else
            {
                if (Price != 0.0 && Settlement_Date == 0.0)
                    AddToErrors(errors, ErrorLevel.Warning, "Settlement price (Price) is not zero but Settlement_Date is not specified so Price has been ignored.");
            }
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3} {4} {5} {6}", Buy_Sell, Currency, Notional, Bond_Maturity_Date, Coupon_Interval, Coupon_Rate, Call_Put);
        }

        /// <summary>
        /// Is cash-settled forward deal.
        /// </summary>
        public virtual bool IsForward()
        {
            return true;
        }

        /// <summary>
        /// Outstanding notional in base currency given the Notional in Deal Currency and the Amortisation.
        /// </summary>
        protected override bool DoTryGetNotional(PriceFactorList priceFactors, out double notional)
        {
            return DoTryGetOutstandingNotional(Notional, Amortisation, priceFactors, out notional);
        }
    }

    [Serializable]
    [DisplayName("Callable Bond Valuation")]
    public class CallableBondValuation : IRValuation
    {
        [NonSerialized]
        protected CFFixedInterestList fCashflowList = null;
        [NonSerialized]
        protected CFFixedList fFixedCashflowList = null;
        [NonSerialized]
        protected CFRecoveryList fRecoveryList = null;
        [NonSerialized]
        double fSettlementAmount = 0.0;
        [NonSerialized]
        double fAccrued = 0.0;
        [NonSerialized]
        protected IInterestYieldVol fInterestYieldVol = null;
        [NonSerialized]
        protected SwaptionPricer fSwaptionPricer = null;
        [NonSerialized]
        protected CreditRating fCreditRating = null;
        [NonSerialized]
        protected RecoveryRate fRecoveryRate = null;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb = null;
        [NonSerialized]
        protected bool fNeedsCreditRating = true;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CallableBondValuation()
        {
            Step_Size = MarketModelTreeOptionPricer.DefaultTimeStepSize;
            Max_Nodes = MarketModelTreeOptionPricer.DefaultMaxTimeSteps;
            Respect_Default = YesNo.No;
            Use_Survival_Probability = YesNo.No;
            Early_Exercise_Today = YesNo.Yes;
        }

        /// <summary>
        /// Size of time step in binomial tree (subject to Max_Nodes)
        /// </summary>
        public Period Step_Size
        {
            get; set;
        }

        /// <summary>
        /// Maximum number of time steps in binomial tree.
        /// </summary>
        public int Max_Nodes
        {
            get; set;
        }

        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Property used to set or determine if the valuation model is to take default into account.
        /// </summary>
        public YesNo Respect_Default
        {
            get; set;
        }

        /// <summary>
        /// Determines whether to allow exercise on base date.
        /// </summary>
        public YesNo Early_Exercise_Today
        {
            get; set;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CallableBondForward);
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            if (Step_Size <= 0.0)
                errors.Add(ErrorLevel.Error, string.Format("Step_Size must be greater than zero for {0}.", GetType().Name));

            if (Max_Nodes <= 0)
                errors.Add(ErrorLevel.Error, string.Format("Max_Nodes must be greater than zero for {0}.", GetType().Name));

            var deal = (CallableBondForward)Deal;

            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, string.Format("For deal valued using {0}, Issuer is missing but Use_Survival_Probability or Respect_Default is set to Yes; valuation of this deal will be treated as if Use_Survival_Probability and Respect_Default are both No.", GetType().Name));
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CallableBondForward deal = (CallableBondForward)Deal;
            InterestVolBase.RegisterInterestYieldVol(factors, deal.Yield_Volatility, deal.Currency);

            if (NeedCreditRating())
                factors.Register<CreditRating>(deal.Issuer);

            if (NeedRecovery())
                factors.Register<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate);

            if (NeedSurvivalProb())
                factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <summary>
        /// Vaulation becomes path-dependent if valuation model respects default.
        /// </summary>
        public override bool FullPricing()
        {
            return NeedCreditRating();
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CallableBondForward deal = (CallableBondForward)Deal;

            double firstCallDate = deal.First_Call_Date;
            double lastCallDate = deal.Last_Call_Date;
            double baseDate = factors.BaseDate;
            double issueDate = deal.Issue_Date;
            double settlementDate = deal.Settlement_Date;
            double priceDate = Math.Max(baseDate, settlementDate + 1.0); // bond cashflows before priceDate do not contribute to bond price
            double maturityDate = deal.Bond_Maturity_Date;
            double couponInterval = deal.Coupon_Interval;
            double notional = deal.Notional;
            IHolidayCalendar holidayCalendar = deal.GetHolidayCalendar();

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = issueDate,
                MaturityDate = maturityDate,
                AccrualDayCount = deal.Accrual_Day_Count,
                FirstCouponDate = deal.First_Coupon_Date,
                PenultimateCouponDate = deal.Penultimate_Coupon_Date,
                Amortisation = deal.Amortisation,
                CouponPeriod = couponInterval,
                Principal = notional,
                PrincipalExchange = PrincipalExchange.Start_Maturity,
                AccrualCalendar = holidayCalendar
            };

            CashflowListDetail detail = CashflowGeneration.GenerateCashflowListDetail(dateGenerationParams);

            // Collect reset dates as we loop.
            var resetDates = new DateList(detail.Coupon_Details.Count);
            
            // Create cashflow list
            fCashflowList = new CFFixedInterestList();
            fCashflowList.Compounding = YesNo.No;

            foreach (CouponDetail couponDetail in detail.Coupon_Details)
            {
                if (couponDetail.Payment_Date < priceDate)
                    continue; 
                
                foreach (AccrualDetail accrualDetail in couponDetail.Accrual_Details)
                {
                    resetDates.Add(accrualDetail.Accrual_Start_Date);
                    
                    if (couponDetail.Payment_Date < priceDate)
                        continue; 
                    
                    var cashflow = new CFFixedInterest
                    {
                        Payment_Date = couponDetail.Payment_Date,
                        Notional = accrualDetail.Notional,
                        Accrual_Start_Date = accrualDetail.Accrual_Start_Date,
                        Accrual_End_Date = accrualDetail.Accrual_End_Date,
                        Accrual_Year_Fraction = accrualDetail.Accrual_Year_Fraction,
                        Rate = deal.Coupon_Rate * Percentage.PercentagePoint,
                        Accrual_Day_Count = deal.Accrual_Day_Count,
                        Discounted = YesNo.No
                    };
                    
                    fCashflowList.Items.Add(cashflow);
                }
            }

            IRBaseDealSkin.ApplyRateSchedule(fCashflowList.Items, deal.Coupon_Rate_Schedule, Percentage.PercentagePoint, holidayCalendar, DateAdjustmentMethod.Modified_Following);

            // Calculate fixed interest cashflows.
            fCashflowList.CalculateInterest(baseDate);

            fFixedCashflowList = PrincipalCashflows(priceDate, issueDate, maturityDate, PrincipalExchange.Start_Maturity, notional, deal.Amortisation, 1.0);

            fSettlementAmount = 0.0;
            fAccrued = 0.0;
            bool payDatesRequired = requiredResults.CashRequired();

            if (settlementDate >= baseDate)
            {
                double settlementPrincipal = CFFixedInterestListValuation.GetPrincipal(fCashflowList, settlementDate);
                fSettlementAmount          = settlementPrincipal * deal.Price * Percentage.PercentagePoint;

                for (int i = 0; i < fCashflowList.Items.Count; ++i)
                {
                    CFFixedInterest cashflow = fCashflowList[i];

                    if (cashflow.Accrual_Start_Date >= settlementDate)
                        break;

                    if (settlementDate < cashflow.Accrual_End_Date)
                        fAccrued += cashflow.Interest() * (settlementDate - cashflow.Accrual_Start_Date) / (cashflow.Accrual_End_Date - cashflow.Accrual_Start_Date);
                }

                if (deal.Price_Is_Clean == YesNo.Yes)
                    fSettlementAmount += fAccrued; // add accrued interest

                fT.AddPayDate(settlementDate, payDatesRequired);
            }

            // Add the floating and fixed cashflow dates to the time grid.
            fT.AddPayDates<CFFixedInterest>(fCashflowList, payDatesRequired);
            fT.AddPayDates<CFFixed>(fFixedCashflowList, payDatesRequired);

            // We only need an option pricer if callable on or after the settlement date.
            fSwaptionPricer = null;
            if (lastCallDate >= settlementDate)
            {
                // Snap call dates to grid of reset dates and
                // ensure that first call date is on or after settlement date
                int iLast    = resetDates.IndexOfNextDate(lastCallDate);
                lastCallDate = resetDates[iLast];
                int iFirst   = resetDates.IndexOfNextDate(firstCallDate);

                while ((iFirst < resetDates.Count - 1) && (resetDates[iFirst] < settlementDate))
                {
                    // move first exercise date forward
                    iFirst++;
                }

                firstCallDate         = resetDates[iFirst];
                int paySign           = deal.Call_Put == OptionType.Put ? +1 : -1;
                RateList exerciseFees = new RateList();

                foreach (Rate price in deal.Call_Prices)
                {
                    Rate fee  = new Rate();
                    fee.Value = paySign * (Percentage.OverPercentagePoint - price.Value);
                    fee.Date  = price.Date;
                    exerciseFees.Add(fee);
                }

                var amortisation = AllocateAmortisationToPaymentDates<CFFixedInterest>(deal.Amortisation, fCashflowList.Items);

                fSwaptionPricer = new SwaptionPricer(issueDate, maturityDate, couponInterval, couponInterval,
                    deal.Accrual_Day_Count, holidayCalendar, DayCount.ACT_365, holidayCalendar, firstCallDate, lastCallDate, baseDate,
                    paySign, paySign, 0.0, null, notional, amortisation, deal.Coupon_Rate, null, deal.Coupon_Rate_Schedule, exerciseFees,
                    null, OptionStyle2.Bermudan, Max_Nodes, Step_Size, fT, true, requiredResults.CashRequired());
            }

            if (NeedSurvivalProb())
            {
                fRecoveryList = new CFRecoveryList();
                fRecoveryList.PopulateRecoveryCashflowList(baseDate, settlementDate, fCashflowList);
            }
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            CallableBondForward deal = (CallableBondForward)Deal;
            fInterestYieldVol        = InterestVolBase.GetYieldVol(factors, deal.Yield_Volatility, fCurrency);

            fNeedsCreditRating = NeedCreditRating();
            fCreditRating   = NeedCreditRating() ? factors.Get<CreditRating>(deal.Issuer)                                                                               : null;
            fRecoveryRate   = NeedRecovery()     ? factors.Get<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate)               : null;
            fSurvivalProb   = NeedSurvivalProb() ? factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability) : null;
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            CallableBondForward deal = (CallableBondForward)Deal;

            double baseDate       = factors.BaseDate;
            double settlementDate = deal.Settlement_Date;
            double tSettle        = CalcUtils.DaysToYears(settlementDate - baseDate);

            TimeGridIterator tgi = new TimeGridIterator(fT);

            PVProfiles result = valuationResults.Profile;
            AccruedInterest accrued = valuationResults.Results<AccruedInterest>();

            var intraValuationDiagnosticsWriter =
                IntraValuationDiagnosticsWriterFactory.GetOrCreate(IntraValuationDiagnosticsLevel.None);

            using (var outerCache = Vector.Cache(factors.NumScenarios))
            {
                // SwapOptionPricerObject is null when there are no valid exercise dates.
                SwaptionPricer.WorkingArrays arrays = fSwaptionPricer != null ? fSwaptionPricer.PreValue(fDiscountRate, outerCache) : null;
                Vector tExercise                    = outerCache.Get(double.PositiveInfinity); // time of exercise
                int numberScenariosExercised        = 0;

                Vector defaultDate = fCreditRating != null ? outerCache.Get(CalcUtils.DateTimeMaxValueAsDouble) : null;
                var defaultedBeforeBaseDate = fNeedsCreditRating &&
                                              CreditRating.DefaultedBeforeBaseDate(fCreditRating, baseDate);

                VectorEngine.For(tgi, () =>
                {
                    using (var cache = Vector.Cache(factors.NumScenarios))
                    {
                        Vector cash = cache.GetClear();
                        Vector pv   = cache.GetClear();

                        if (defaultedBeforeBaseDate || numberScenariosExercised == factors.NumScenarios)
                        {
                            // already defaulted before base date or All scenarios exercised
                            result.AppendVector(tgi.Date, pv);
                            return LoopAction.Break;
                        }
                        else
                        {
                            // Value of the bond cashflows after the settlement.
                            fCashflowList.Value(pv, cash, null, baseDate, tgi.Date, null, fDiscountRate, fSurvivalProb, null,
                                null, intraValuationDiagnosticsWriter, 0.0);

                            // Add the value of the principal and amortization cashflows.
                            fFixedCashflowList.Value(pv, cash, baseDate, tgi.Date, null, fDiscountRate, fSurvivalProb,
                                intraValuationDiagnosticsWriter, 0.0);

                            if (fSurvivalProb != null)
                            {
                                fRecoveryList.Value(pv, baseDate, tgi.Date, fDiscountRate, fSurvivalProb,
                                    intraValuationDiagnosticsWriter);
                            }

                            if (fNeedsCreditRating)
                                UpdateDefaultDate(fCreditRating, tgi.Date, tgi.T, defaultDate);

                            // Add/subtract value of option
                            if (fSwaptionPricer != null)
                            {
                                using (var innerCache = Vector.Cache(factors.NumScenarios))
                                {
                                    Vector optionPv       = innerCache.Get();
                                    Vector exerciseStrike = innerCache.GetClear();    // strike of underlying at exercise
                                    Vector exerciseFee    = innerCache.GetClear();    // fee paid on exercise
                                    fSwaptionPricer.Value(optionPv, tgi.T, fDiscountRate, fInterestYieldVol, fSurvivalProb, arrays, tExercise, 
                                        exerciseStrike, exerciseFee, Early_Exercise_Today == YesNo.Yes, ref numberScenariosExercised);

                                    // Ignore optionality if in default.
                                    if (fNeedsCreditRating)
                                        optionPv.AssignConditional(defaultDate > tgi.Date, optionPv, 0.0);

                                    pv.Add(optionPv);
                                }
                            }

                            if (tgi.Date < settlementDate)
                            {
                                // Forward deal before settlement date
                                if (deal.Is_Defaultable == YesNo.No)
                                    pv.Assign((pv / fDiscountRate.Get(tgi.T, tSettle) - fSettlementAmount) * fRepoRate.Get(tgi.T, tSettle));
                                else
                                    pv.Subtract((fSettlementAmount - fAccrued) * fRepoRate.Get(tgi.T, tSettle) + fAccrued * fDiscountRate.Get(tgi.T, tSettle));   // discount accrued with bond rate; accrued interest must cancel
                            }
                            else if (tgi.Date == settlementDate)
                            {
                                // Forward deal at settlement date
                                pv.Subtract(fSettlementAmount);
                                cash.Subtract(fSettlementAmount);
                            }

                            if (deal.IsForward())
                            {
                                // Cash settled forward
                                if (tgi.Date == settlementDate)
                                    cash.Assign(pv);
                                else
                                    cash.Clear();
                            }
                            else if (tgi.Date >= settlementDate)
                            {
                                using (var innerCache = Vector.Cache(factors.NumScenarios))
                                {
                                    Vector afterExercise  = innerCache.Get(tExercise < tgi.T);
                                    Vector beforeExercise = innerCache.Get(tExercise > tgi.T);

                                    Vector exercisedToday = innerCache.GetClear();
                                    exercisedToday.Assign(afterExercise.Or(beforeExercise));
                                    exercisedToday.Assign(!exercisedToday);

                                    double callAmount = deal.Notional * Percentage.PercentagePoint * deal.Call_Prices.GetRate(tgi.Date);

                                    // Before exercise: pv is bondPV + optionPv and cash is bondCash.
                                    // On exercise: pv and cash are bondCash + callAmount.
                                    // After exercise: pv and cash are zero.
                                    cash.AssignConditional(exercisedToday, cash + callAmount, beforeExercise * cash);
                                    pv.AssignConditional(exercisedToday, cash, beforeExercise * pv);
                                }
                            }

                            // Apply leg sign to results
                            int buySellSign = deal.Buy_Sell == BuySell.Buy ? +1 : -1;
                            ApplySign(pv, cash, buySellSign);

                            if (fNeedsCreditRating)
                            {
                                Vector beforeExercise      = cache.Get(tExercise > tgi.T);
                                Vector modifiedDefaultDate = cache.Get();

                                // If default occurs after the call option has been exercise, default is irrelevant.
                                // If default occurs on the same date that the call option is exercised, the assumption
                                // is that the bond has been paid back in full, otherwise it wouldn''t be considered exercised.
                                modifiedDefaultDate.AssignConditional(beforeExercise, defaultDate, double.PositiveInfinity);
                                GetDefaultValue(baseDate, tgi.Date, modifiedDefaultDate, fRecoveryRate, pv, cash);
                            }

                            valuationResults.Cash.Accumulate(fFxRate, tgi.Date, cash);
                            result.AppendVector(tgi.Date, pv * fFxRate.Get(tgi.T));

                            if (accrued != null)
                                accrued.SetValue(tgi.Date, fCashflowList.CalculateAccrual(tgi.Date, accrued.AccrueFromToday, fDeal.GetHolidayCalendar()) * buySellSign);
                        }
                    }

                    return LoopAction.Continue;
                });
            }

            result.Complete(fT);
        }

        /// <summary>
        /// Modify the pv and cash taking the date of default into account.
        /// </summary>
        /// <remarks>
        /// The default value considers the buy/sell sign or whether it's a forward deal, but it doesn't consider whether the call option has been exercised or not.
        /// This is partly to respect the method signature of the base class, but partly the call exercise feature can be taken care of by modifying the actual 
        /// underlier default date - if the name is in default but the call option has been exercised, the default has no consequence, and the "effective"
        /// default date can be moved to inifinity (as far as default value is concerned).
        /// </remarks>
        public override void GetDefaultValue(double baseDate, double valueDate, Vector defaultDate, RecoveryRate recoveryRate, Vector pv, Vector cash)
        {
            double principal      = CFFixedInterestListValuation.GetPrincipal(fCashflowList, valueDate);
            var    deal           = (CallableBondForward)Deal;
            double settlementDate = deal.Settlement_Date;
            double t              = CalcUtils.DaysToYears(valueDate - baseDate);
            double buySellSign    = deal.Buy_Sell == BuySell.Buy ? 1.0 : -1.0;

            using (var cache = Vector.CacheLike(pv))
            {
                // Approximation: recover only principal, neglecting accrued interest
                Vector recovery = cache.Get(buySellSign * principal * recoveryRate.Get(t));

                if (valueDate <= settlementDate)
                {
                    // Set the pv to (recovery - settlementAmount) * df when defaultDate <= valueDate <= settlementDate.
                    // Set cash to (recovery - settlementAmount) when defaultDate <= valueDate = settlementDate (cash is always zero before settlementDate).
                    double tSettle          = CalcUtils.DaysToYears(settlementDate - baseDate);
                    double settlementAmount = buySellSign * fSettlementAmount;
                    Vector hasDefaulted     = cache.Get(defaultDate <= valueDate);

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
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
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
        /// Return an amortisation schedule with amortization payments allocated to the nearest payment date in the cashflow list.
        /// </summary>
        private static Amortisation AllocateAmortisationToPaymentDates<TCashflow>(Amortisation sourceAmortisation, List<TCashflow> cashflowList) where TCashflow : CFBase
        {
            if (sourceAmortisation == null || sourceAmortisation.Count == 0)
                return sourceAmortisation;

            var payDates = new DateList(cashflowList.Select(cashflow => (double)cashflow.Payment_Date));

            // Allocate amortisation amounts to nearest payment dates
            var amounts = new double[payDates.Count];
            foreach (var payment in sourceAmortisation)
            {
                int i = payDates.IndexOfClosestDate(payment.Date);
                amounts[i] += payment.Amount;
            }

            var amortisation = new Amortisation();
            for (int i = 0; i < amounts.Length; ++i)
            {
                if (amounts[i] != 0.0)
                    amortisation.Add(new AmountAtDate() { Amount = amounts[i], Date = payDates[i] });
            }

            return amortisation;
        }

        /// <summary>
        /// True if the Issuer field on the deal is specified; false otherwise.
        /// </summary>
        private bool DealHasIssuer()
        {
            CallableBondForward deal = (CallableBondForward)Deal;
            return !string.IsNullOrEmpty(deal.Issuer);
        }

        /// <summary>
        /// True if requiring CreditRating price factor; false otherwise.
        /// </summary>
        private bool NeedCreditRating()
        {
            return Respect_Default == YesNo.Yes && DealHasIssuer();
        }

        /// <summary>
        /// True if requiring Recovery price factor; false otherwise.
        /// </summary>
        private bool NeedRecovery()
        {
            return Respect_Default == YesNo.Yes && DealHasIssuer();
        }

        /// <summary>
        /// True if requiring SurvivalProb price factor; false otherwise.
        /// </summary>
        private bool NeedSurvivalProb()
        {
            return Use_Survival_Probability == YesNo.Yes && DealHasIssuer();
        }
    }
}
