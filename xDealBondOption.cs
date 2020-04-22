/// <author>
/// Phil Koop, Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for bond options.
/// </summary>
using System;
using System.ComponentModel;
using System.Drawing.Design;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Interface to be implemented by BondOption and BondFutureOption.
    /// </summary>
    public interface IBondForwardOption
    {
        /// <summary>
        /// Get currency of the deal.
        /// </summary>
        string GetCurrency();

        /// <summary>
        /// Time to expiry of the deal given a base date.
        /// </summary>
        double GetTimeToExpiry(double baseDate);

        /// <summary>
        /// Get notional amount of the deal.
        /// </summary>
        double GetNotional();

        /// <summary>
        /// Get settlement date of the deal.
        /// </summary>
        TDate GetSettlementDate();

        /// <summary>
        /// Get the strike price.
        /// </summary>
        double GetStrikePrice();

        /// <summary>
        /// Set the strike price.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        void SetStrikePrice(double strike);

        /// <summary>
        /// Set the expiry of the deal.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        void SetExpiryDate(TDate expiry);

        /// <summary>
        /// Get the discount rate.
        /// </summary>
        string GetDiscountRate();

        /// <summary>
        /// Set the discount rate.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        void SetDiscountRate(string discountRate);

        /// <summary>
        /// End date of the deal.
        /// </summary>
        double EndDate();
    }

    /// <summary>
    /// Deal class for bond options.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Option")]
    public class BondOptionDeal : IRDeal, IBondForwardOption
    {
        public BondOptionDeal()
        {
            Amortisation         = new Amortisation();
            Option_Type          = OptionType.Call;
            Strike_Is_Clean      = YesNo.Yes;
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
        }

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

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

        public double Coupon_Rate
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

        public OptionType Option_Type
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the option volatility.
        /// </summary>
        [NonMandatory]
        public string Yield_Volatility
        {
            get { return fForecastVolatility; }
            set { fForecastVolatility = value; }
        }

        public double Strike_Price
        {
            get; set;
        }

        public YesNo Strike_Is_Clean
        {
            get; set;
        }

        public TDate Expiry_Date
        {
            get; set;
        }

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

        [NonMandatory]
        public string Recovery_Rate
        {
            get;
            set;
        }

        /// <summary>
        /// Returns bond option's dirty strike.
        /// </summary>
        public double DirtyStrike(double strikePrice, DateList payDates, double[] accruals)
        {
            var strike = Percentage.PercentagePoint * strikePrice;

            if (Strike_Is_Clean == YesNo.Yes)
            {
                strike += PricingFunctions.AccruedInterest(Expiry_Date, Issue_Date, payDates, accruals, Percentage.PercentagePoint * Coupon_Rate, 1, null);
            }

            return strike;
        }

        /// <summary>
        /// Returns the yield implied by strike price.
        /// </summary>
        public double GetStrikeYield(ref double strike, double tExpiry, double tMaturity, DateList payDates, double[] accruals)
        {
            strike = DirtyStrike(strike, payDates, accruals);
            return PricingFunctions.BondYieldFromPrice(tExpiry, tMaturity, Percentage.PercentagePoint * Coupon_Rate, Coupon_Interval, strike);
        }

        /// <summary>
        /// Returns a <see cref="DateGenerationResults"/> which contains bond option's payment dates, pricipals and accrual year fractions information.
        /// </summary>
        public DateGenerationResults GetDateGenerationResults()
        {
            var dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresPrincipals = true,
                RequiresYearFractions = true
            };

            var dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = Issue_Date,
                MaturityDate = Bond_Maturity_Date,
                CouponPeriod = Coupon_Interval,
                FirstCouponDate = First_Coupon_Date,
                PenultimateCouponDate = Penultimate_Coupon_Date,
                AccrualCalendar = GetHolidayCalendar(),
                AccrualDayCount = Accrual_Day_Count,
                Principal = Notional,
                Amortisation = Amortisation
            };

            return CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);
        }

        /// <summary>
        /// Set the strike price.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        public void SetStrikePrice(double strike)
        {
            Strike_Price = strike;
        }

        /// <summary>
        /// Set the expiry of the deal.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        public void SetExpiryDate(TDate expiry)
        {
            Expiry_Date = expiry;
        }

        /// <summary>
        /// Set the discount rate.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        public void SetDiscountRate(string discountRate)
        {
            Discount_Rate = discountRate;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Expiry_Date;
        }

        /// <summary>
        /// Get currency of the deal.
        /// </summary>
        public string GetCurrency()
        {
            return Currency;
        }

        /// <summary>
        /// Calculates time to expiry for a given baseDate.
        /// </summary>
        public double GetTimeToExpiry(double baseDate)
        {
            return CalcUtils.DaysToYears(Expiry_Date - baseDate);
        }

        /// <summary>
        /// Get notional amount of the deal.
        /// </summary>
        public double GetNotional()
        {
            return Notional;
        }

        /// <summary>
        /// Get settlement date of the deal.
        /// </summary>
        public TDate GetSettlementDate()
        {
            return Expiry_Date;
        }

        /// <summary>
        /// Get the strike price.
        /// </summary>
        public double GetStrikePrice()
        {
            return Strike_Price;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, First_Coupon_Date, Penultimate_Coupon_Date, true, "issue", "bond maturity");

            if (Coupon_Interval <= 0.0)
                AddToErrors(errors, "Coupon interval must be greater than zero");

            if (Expiry_Date >= Bond_Maturity_Date)
                AddToErrors(errors, "Expiry date must lie before bond maturity date: " + Bond_Maturity_Date.ToString());

            if (Strike_Price <= 0.0)
                AddToErrors(errors, "Strike price must be positive");

            Amortisation.Validate(errors);
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3} {4}", Buy_Sell, fCurrency, Notional, Bond_Maturity_Date, Coupon_Interval);
        }
    }

    /// <summary>
    /// Valuation class for bond options.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Option Valuation")]
    public class BondOptionValuation : IRValuation, ICanUseSurvivalProbability
    {
        [NonSerialized]
        protected DateList fPayDates = null; // payment dates
        [NonSerialized]
        protected double[] fAccruals = null; // accrual periods
        [NonSerialized]
        protected double[] fPrincipals = null;
        [NonSerialized]
        protected double fFinalPrincipal = 0.0;
        [NonSerialized]
        protected double fStrike = 0.0;
        [NonSerialized]
        protected double fStrikeYield = 0.0;
        [NonSerialized]
        protected RecoveryRate fRecoveryRate;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb;
        [NonSerialized]
        protected CreditRating fCreditRating;


        /// <summary>
        /// Property used to set or determine if the valuation model is to take default into account.
        /// </summary>
        public YesNo Respect_Default
        {
            get; set;
        }

        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        public BondOptionValuation()
        {
            Respect_Default = YesNo.No;
            Use_Survival_Probability = YesNo.No;
        }

        /// <summary>
        /// Returns true if deal is strongly path-dependent.
        /// </summary>
        public override bool FullPricing()
        {
            return Respect_Default == YesNo.Yes;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(BondOptionDeal);
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (BondOptionDeal)Deal;

            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, string.Format("For deal valued using {0}, Issuer is missing but Use_Survival_Probability or Respect_Default is set to Yes; valuation of this deal will be treated as if Use_Survival_Probability and Respect_Default are both No.", GetType().Name));
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            BondOptionDeal deal = (BondOptionDeal)Deal;
            InterestVolBase.RegisterInterestYieldVol(factors, deal.Yield_Volatility, fCurrency);

            bool needRating = Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needSurvival = Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needRecovery = needRating;

            if (needRating)
                factors.Register<CreditRating>(deal.Issuer);

            if (needRecovery)
                factors.Register<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate);

            if (needSurvival)
                factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (BondOptionDeal)Deal;

            var tExpiry = deal.GetTimeToExpiry(factors.BaseDate);
            var tMaturity = CalcUtils.DaysToYears(deal.Bond_Maturity_Date - factors.BaseDate);
            var dateGenerationResults = deal.GetDateGenerationResults();
            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;
            fPrincipals = dateGenerationResults.Principals;
            fFinalPrincipal = dateGenerationResults.FinalPrincipal;

            fStrike = deal.Strike_Price;
            fStrikeYield = deal.GetStrikeYield(ref fStrike, tExpiry, tMaturity, fPayDates, fAccruals);
            fT.AddPayDate(deal.Expiry_Date, requiredResults.CashRequired());
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        /// <param name="factors">Price factors.</param>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            BondOptionDeal deal = (BondOptionDeal)Deal;

            bool needRating = Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needSurvival = Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needRecovery = needRating;

            fCreditRating = needRating ? factors.Get<CreditRating>(deal.Issuer) : null;
            fRecoveryRate = needRecovery ? factors.Get<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate) : null;
            fSurvivalProb = needSurvival ? factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability) : null;
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            TimeGridIterator tgi = new TimeGridIterator(fT);
            PVProfiles result = valuationResults.Profile;
            CashAccumulators cashAccumulators = valuationResults.Cash;

            BondOptionDeal deal = (BondOptionDeal)Deal;

            double baseDate  = factors.BaseDate;
            double notional  = deal.Notional;
            double interval  = deal.Coupon_Interval;
            double buySign   = (deal.Buy_Sell == BuySell.Buy) ? +1 : -1;
            double paySign   = (deal.Option_Type == OptionType.Call) ? +1 : -1;
            double coupon    = Percentage.PercentagePoint * deal.Coupon_Rate;
            double tExpiry   = CalcUtils.DaysToYears(deal.Expiry_Date - baseDate);
            double tMaturity = CalcUtils.DaysToYears(deal.Bond_Maturity_Date - baseDate);

            IInterestYieldVol interestYieldVol = InterestVolBase.GetYieldVol(factors, deal.Yield_Volatility, fCurrency);

            if ((deal.Amortisation) != null && (deal.Amortisation.Count > 0))
                notional = deal.Amortisation.GetPrincipal(notional, deal.Expiry_Date);

            bool respectDefault = Respect_Default == YesNo.Yes && fCreditRating != null;

            using (IntraValuationDiagnosticsHelper.StartDeal(fIntraValuationDiagnosticsWriter, Deal))
            {
                using (var pricerCache = Vector.Cache(factors.NumScenarios))
                {
                    Vector defaultTime = null;
                    Vector bondIsAlive = null;
                    Vector historicalRecovery = null;

                    if (respectDefault)
                    {
                        defaultTime = pricerCache.Get();
                        bondIsAlive = pricerCache.Get(1.0);
                        historicalRecovery = pricerCache.GetClear();

                        fCreditRating.DefaultTime(defaultTime);
                    }

                    var defaultedBeforeBaseDate = respectDefault && CreditRating.DefaultedBeforeBaseDate(fCreditRating, baseDate);

                    VectorEngine.For(tgi, () =>
                        {
                            using (IntraValuationDiagnosticsHelper.StartValuation(fIntraValuationDiagnosticsWriter, tgi.Date))
                            {
                                using (var cache = Vector.Cache(factors.NumScenarios))
                                {
                                    Vector optionValue = cache.GetClear();
                                    Vector stdDev = cache.Get();    // Std.Dev of Price
                                    Vector stdDevYield = cache.Get();   //Std.Dev of Yield
                                    Vector price = cache.Get();
                                    Vector yield = cache.Get();
                                    Vector macaulayDuration = cache.Get();
                                    Vector bondValue = cache.Get();
                                    Vector df = cache.Get();
                                    Vector dfr = fRepoIsDiscount ? null : cache.Get();

                                    if (defaultedBeforeBaseDate)
                                    {
                                        result.AppendVector(tgi.Date, optionValue);
                                        return LoopAction.Break;
                                    }

                                    // This BondPrice function returns the value of the bond cashflows after ExpiryDate, including accrual, discounted back to T.date
                                    double accrual, cash;
                                    PricingFunctions.BondPrice(bondValue, out accrual, out cash, baseDate, tgi.Date, deal.Expiry_Date, deal.Issue_Date, deal.Bond_Maturity_Date, notional, coupon, fPayDates, fAccruals, fDiscountRate, deal.Amortisation, fPrincipals, fFinalPrincipal, fSurvivalProb, +1.0);

                                    // Now check scenario by scenario for defaults, overwriting bondValue as necessary
                                    if (respectDefault)
                                    {
                                        AdjustBondValueForDefault(notional, tExpiry, bondValue, bondIsAlive, historicalRecovery, defaultTime, tgi.T, fDiscountRate, fRecoveryRate);
                                    }

                                    // convert price and duration to forward (tExpiry) basis
                                    if (tgi.Date == deal.Expiry_Date)
                                    {
                                        optionValue.Assign(buySign * VectorMath.Max(0.0, paySign * (bondValue - notional * fStrike)));
                                        cashAccumulators.Accumulate(fFxRate, tgi.Date, optionValue);
                                    }
                                    else
                                    {
                                        fDiscountRate.GetValue(df, tgi.T, tExpiry);

                                        if (fRepoIsDiscount)
                                            dfr = df;
                                        else
                                            fRepoRate.GetValue(dfr, tgi.T, tExpiry);

                                        // Need yield and duration to convert yield vol to price vol.
                                        PricingFunctions.BondForwardPriceAndAdjustedMacaulayDuration(price, macaulayDuration, tgi.T, tExpiry, tMaturity, coupon, interval, df, fDiscountRate, fSurvivalProb);
                                        PricingFunctions.BondYieldFromPrice(yield, tExpiry, tMaturity, coupon, interval, price);

                                        // Calculate Modified Duration from Macaulay Duration.
                                        Vector modifiedDuration = cache.GetClear();
                                        PricingFunctions.GetModifiedDuration(modifiedDuration, macaulayDuration, yield, interval);

                                        // Calculate Std.Dev of Yield and Price
                                        interestYieldVol.GetStdDev(stdDevYield, tgi.T, yield, fStrikeYield, tExpiry, tMaturity - tExpiry);
                                        stdDev.Assign(modifiedDuration * stdDevYield);

                                        if (interestYieldVol.GetDistributionType() == ProbabilityDistribution.Lognormal)
                                        {
                                            stdDev.MultiplyBy(yield);
                                        }

                                        price.AssignQuotient(bondValue, df);
                                        PricingFunctions.BlackFunction(optionValue, deal.Option_Type, price, notional * fStrike, stdDev);

                                        optionValue.MultiplyBy(buySign * dfr);

                                        if (fIntraValuationDiagnosticsWriter.Level > IntraValuationDiagnosticsLevel.None)
                                        {
                                            // Add Intra-valuation Diagnostics
                                            using (var volatilitiesAtDateStore = IntraValuationDiagnosticsHelper.CreateVolatilitiesAtDateStore(fIntraValuationDiagnosticsWriter, factors.NumScenarios))
                                            using (var volatilitiesYieldAtDateStore = IntraValuationDiagnosticsHelper.CreateVolatilitiesAtDateStore(fIntraValuationDiagnosticsWriter, factors.NumScenarios))
                                            {
                                                volatilitiesAtDateStore.Add(tgi.Date, tgi.TimeGrid.fEndDate, stdDev);
                                                volatilitiesYieldAtDateStore.Add(tgi.Date, tgi.TimeGrid.fEndDate, stdDevYield);
                                                IntraValuationDiagnosticsHelper.AddBondOptionProperties(fIntraValuationDiagnosticsWriter, price, dfr, bondValue, accrual,
                                                    volatilitiesAtDateStore, volatilitiesYieldAtDateStore);
                                                IntraValuationDiagnosticsHelper.AddCashflowsPV(fIntraValuationDiagnosticsWriter, optionValue);
                                            }
                                        }
                                    }

                                    result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * optionValue);
                                    return LoopAction.Continue;
                                }
                            }
                        });
                }

                result.Complete(fT);
            }
        }

        /// <summary>
        /// This method check scenario by scenario for defaults, overwriting bondValue as necessary
        /// </summary>
        public static void AdjustBondValueForDefault(double notional, double expiryTime, Vector bondValue, Vector bondIsAlive,
                                                Vector historicalRecovery, Vector defaultTime, double currentTime, IInterestRate discountRate, RecoveryRate recoveryRate)
        {
            using (var cache = Vector.CacheLike(bondValue))
            {
                Vector recoveryValue = cache.Get();
                Vector expiryDf      = cache.Get();

                discountRate.GetValue(expiryDf, currentTime, expiryTime);
                recoveryRate.GetValue(recoveryValue, currentTime);

                // If this is the period when the bond defaults fill in the historical recovery amount.
                historicalRecovery.AssignConditional((defaultTime <= currentTime).And(bondIsAlive), recoveryValue * notional, historicalRecovery);

                bondValue.AssignConditional((defaultTime <= currentTime), expiryDf * historicalRecovery, bondValue);

                bondIsAlive.AssignConditional((defaultTime <= currentTime), 0.0, bondIsAlive);
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

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }
    }
}
