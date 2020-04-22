/// <author>
/// Eamon Galavan, Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation base classes for futures and futures options.
/// </summary>
using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Deal class for bond futures.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Future")]
    public class BondFuture : Future
    {
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

        [NonMandatory]
        public string Discount_Rate
        {
            get { return fDiscount; } set { fDiscount = value; }
        }

        [NonMandatory]
        public string Repo_Rate
        {
            get { return fRepo; } set { fRepo = value; }
        }

        /// <summary>
        /// Gets or sets the futures issuer.
        /// </summary>
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
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the recovery rate.
        /// </summary>
        [NonMandatory]
        public string Recovery_Rate
        {
            get; set;
        }

        /// <summary>
        /// Constructor method.
        /// </summary>
        public BondFuture()
        {
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
            Repo_Rate            = string.Empty;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (string.IsNullOrWhiteSpace(Contract))
                AddToErrors(errors, ErrorLevel.Error, string.Format("Contract of Bond Future must be specified."));
        }
    }

    /// <summary>
    /// Valuation class for bond futures.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Future Valuation")]
    public class BondFutureValuation : FutureValuation
    {
        [NonSerialized]
        protected DateList fPayDates = null;
        [NonSerialized]
        protected double[] fAccruals = null;
        [NonSerialized]
        protected double fIssueDate = 0.0;
        [NonSerialized]
        protected double fMaturityDate = 0.0;
        [NonSerialized]
        protected double fCouponRate = 0.0;
        [NonSerialized]
        protected double fConversionFactor = 0.0;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb;

        /// <summary>
        /// Property used to set or determine if the valuation model is to take default into account.
        /// </summary>
        public YesNo Respect_Default
        {
            get; set;
        }

        /// <summary>
        /// Property to control whether pricing take credit risk into account.
        /// </summary>
        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public BondFutureValuation() 
            : base()
        {
            Respect_Default          = YesNo.No;
            Use_Survival_Probability = YesNo.No;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(BondFuture);
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (BondFuture)Deal;
            
            if ((Respect_Default == YesNo.Yes || Use_Survival_Probability == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Survival_Probability = Yes or Respect_Default = Yes ignored for deal with missing Issuer.");
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);
            BondFuture deal = (BondFuture)Deal;

            var bfb = (BondFuturesBasis) fFuturesBasis;
            GenerateCTD(bfb.CTD_Issue_Date, bfb.CTD_Maturity_Date, bfb.CTD_Coupon_Interval, bfb.CTD_First_Coupon_Date, bfb.CTD_Penultimate_Coupon_Date, bfb.CTD_Day_Count, Deal.GetHolidayCalendar(), bfb.CTD_Coupon_Rate, bfb.CTD_Conversion_Factor);

            if (NeedRating(Respect_Default, deal.Issuer))
            {
                fCreditRating = factors.Get<CreditRating>(deal.Issuer);
                fRecoveryRate = factors.Get<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }
            else
            {
                fCreditRating = null;
                fRecoveryRate = null;
            }

            if (NeedSurvivalProbability(Use_Survival_Probability, deal.Issuer))
                fSurvivalProb = factors.GetInterface<ISurvivalProb>(InterestRateUtils.GetRateId(deal.Survival_Probability, deal.Issuer));
            else
                fSurvivalProb = null;
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            BondFuture deal = (BondFuture)Deal;

            base.RegisterFactors(factors, errors);

            if (NeedRating(Respect_Default, deal.Issuer))
            {
                factors.Register<CreditRating>(deal.Issuer);

                // register realized recovery rate.
                factors.Register<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }

            if (NeedSurvivalProbability(Use_Survival_Probability, deal.Issuer))
                factors.RegisterInterface<ISurvivalProb>(InterestRateUtils.GetRateId(deal.Survival_Probability, deal.Issuer));
        }

        /// <inheritdoc />
        public override double ScalingFactor()
        {
            return base.ScalingFactor() * Percentage.PercentagePoint;
        }

        /// <inheritdoc />
        protected override void GetDefaultTime(Vector defaultTime, PriceFactorList factors)
        {
            BondFuture deal = (BondFuture)Deal;
            if (NeedRating(Respect_Default, deal.Issuer))
            {
                var cr = factors.Get<CreditRating>(deal.Issuer);
                cr.DefaultTime(defaultTime);
                return;
            }

            base.GetDefaultTime(defaultTime, factors);
        }

        /// <inheritdoc />
        protected override void RegisterFuturesPriceFactor(PriceFactorList factors, ErrorList errors)
        {
            BondFuture deal = (BondFuture)fDeal;

            BondFuturesBasis bfb = factors.Register<BondFuturesBasis>(FutureBase.GetFactorID(deal.Contract, deal.Settlement_Date));

            if (deal.Settlement_Date >= bfb.CTD_Maturity_Date)
                errors.Add(ErrorLevel.Error, "Settlement date must be before cheapest-to-deliver maturity date of the Bond Future Basis price factor.");
        }

        /// <inheritdoc />
        protected override void GetFuturesPriceFactor(PriceFactorList factors)
        {
            BondFuture deal = (BondFuture)fDeal;
            fFuturesBasis = factors.Get<BondFuturesBasis>(FutureBase.GetFactorID(deal.Contract, deal.Settlement_Date));
        }

        /// <summary>
        /// Generate CTD dates and set CTD coupon rate and conversion factor.
        /// </summary>
        protected void GenerateCTD(double issueDate, double maturityDate, double couponInterval, double firstCouponDate, double penultimateCouponDate, DayCount dayCount, IHolidayCalendar calendar, double couponRate, double conversionFactor)
        {
            if (conversionFactor <= 0.0)
                return; // No CTD details or details invalid

            BondFuture deal = (BondFuture)fDeal;

            if (deal.Settlement_Date >= maturityDate)
                throw new AnalyticsException("Settlement date must be before cheapest-to-deliver maturity date.");

            DateGenerationRequest dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresYearFractions = true,
            };

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = issueDate,
                MaturityDate = maturityDate,
                CouponPeriod = couponInterval,
                FirstCouponDate = firstCouponDate,
                PenultimateCouponDate = penultimateCouponDate,
                AccrualCalendar = calendar,
                AccrualDayCount = dayCount
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);

            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;

            fIssueDate        = issueDate;
            fMaturityDate     = maturityDate;
            fCouponRate       = couponRate;
            fConversionFactor = conversionFactor;
        }

        /// <inheritdoc />
        protected override void ForwardPrice(double baseDate, double valueDate, Vector forwardPrice)
        {
            BondFuture deal = (BondFuture)fDeal;

            double t       = CalcUtils.DaysToYears(valueDate - baseDate);
            double tSettle = CalcUtils.DaysToYears(deal.Settlement_Date - baseDate);

            double accrual, cash;
            PricingFunctions.BondPrice(forwardPrice, out accrual, out cash, baseDate, valueDate, deal.Settlement_Date,
                                       fIssueDate, fMaturityDate, 1.0, fCouponRate, fPayDates,
                                       fAccruals, fDiscountRate, null, null, 0.0, fSurvivalProb, 1.0);

            AdjustForDefault(baseDate, valueDate, forwardPrice, deal.Expiry_Date, Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer), fUnderlyingIsAlive, fHistoricalRecovery, fDefaultTime, fDiscountRate, fRecoveryRate);

            // If deal.Repo_Rate is null or empty then fRepoRate will default to the DiscountRate
            forwardPrice.Assign(Percentage.OverPercentagePoint * ((forwardPrice / fRepoRate.Get(t, tSettle) - accrual)) / fConversionFactor);
        }

        /// <summary>
        /// This method Adjusts the bond price vector by taking the default into account give the default time vector.
        /// </summary>
        public static void AdjustForDefault(double baseDate, double valueDate, Vector bondPrice, TDate expiryDate, bool respectDefault, Vector underlyingIsAlive, Vector historicalRecovery, Vector defaultTime, IInterestRate discountRate, RecoveryRate recoveryRate)
        {
            // Adjust the bond price for default: check scenario by scenario for defaults, overwriting bondPrice as necessary
            if (!respectDefault) 
                return;

            double currentTime = CalcUtils.DaysToYears(valueDate - baseDate);
            double tExpiry = CalcUtils.DaysToYears(expiryDate - baseDate);

            BondOptionValuation.AdjustBondValueForDefault(1.0, tExpiry, bondPrice, underlyingIsAlive,
                                                          historicalRecovery, defaultTime, currentTime, discountRate,
                                                          recoveryRate);
        }
    }

    /// <summary>
    /// Deal class for bond futures options.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Future Option")]
    public class BondFutureOption : FutureOption, IBondForwardOption
    {
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

        /// <summary>
        /// Gets or sets the option volatility.
        /// </summary>
        [NonMandatory]
        public string Yield_Volatility
        {
            get { return fForecastVolatility; }
            set { fForecastVolatility = value; }
        }

        /// <summary>
        /// Gets or sets the option issuer.
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
        /// Constructor method.
        /// </summary>
        public BondFutureOption()
        {
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (string.IsNullOrWhiteSpace(Contract))
                AddToErrors(errors, ErrorLevel.Error, string.Format("Contract of Bond Future must be specified."));
        }

        /// <summary>
        /// Returns a <see cref="DateGenerationResults"/> which contains bond futures option's dates and accrual information.
        /// </summary>
        /// <returns></returns>
        public DateGenerationResults GetDateGenerationResults(double issueDate, double maturityDate, double couponInterval, double firstCouponDate, double penultimateCouponDate, DayCount dayCount, IHolidayCalendar calendar)
        {
            var dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresYearFractions = true,
            };

            var dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = issueDate,
                MaturityDate = maturityDate,
                CouponPeriod = couponInterval,
                FirstCouponDate = firstCouponDate,
                PenultimateCouponDate = penultimateCouponDate,
                AccrualCalendar = calendar,
                AccrualDayCount = dayCount
            };

            return CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);
        }

        /// <summary>
        /// Get currency of the deal.
        /// </summary>
        public string GetCurrency()
        {
            return Currency;
        }

        /// <summary>
        /// Get notional amount of the deal.
        /// </summary>
        public double GetNotional()
        {
            return Contract_Size * Units;
        }

        /// <summary>
        /// Get settlement date of the deal.
        /// </summary>
        public TDate GetSettlementDate()
        {
            return Settlement_Date;
        }

        /// <summary>
        /// Get the strike price.
        /// </summary>
        public double GetStrikePrice()
        {
            return Strike;
        }

        /// <summary>
        /// Set the strike price.
        /// </summary>
        /// <remarks>Note that this method is only to be used by bootstrapper. Code is not supposed to change the deal properties programmatically.</remarks>
        public void SetStrikePrice(double strike)
        {
            Strike = strike;
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
    }

    /// <summary>
    /// Valuation class for bond futures options.
    /// </summary>
    [Serializable]
    [DisplayName("Bond Future Option Valuation")]
    public class BondFutureOptionValuation : FutureOptionValuation
    {
        [NonSerialized]
        protected IInterestYieldVol fInterestYieldVol = null;
        [NonSerialized]
        protected double fStrikeYield = 0.0;
        [NonSerialized]
        protected double fAccrual = 0.0;
        [NonSerialized]
        protected DateList fPayDates = null;
        [NonSerialized]
        protected double[] fAccruals = null;
        [NonSerialized]
        protected double fIssueDate = 0.0;
        [NonSerialized]
        protected double fMaturityDate = 0.0;
        [NonSerialized]
        protected double fCouponInterval = 0.0;
        [NonSerialized]
        protected double fCouponRate = 0.0;
        [NonSerialized]
        protected double fConversionFactor = 0.0;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb;

        /// <summary>
        /// Property used to set or determine if the valuation model is to take default into account.
        /// </summary>
        public YesNo Respect_Default
        {
            get; set;
        }

        /// <summary>
        /// Property to control whether pricing take credit risk into account.
        /// </summary>
        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Construc
        /// </summary>
        public BondFutureOptionValuation() 
            : base()
        {
            Respect_Default          = YesNo.No;
            Use_Survival_Probability = YesNo.No;
        }

        /// <summary>
        /// Transform futures price to allow the generic pricing to be applied.
        /// </summary>
        public static double PriceTransform(double price, double conversionFactor, double accrual)
        {
            return Percentage.PercentagePoint * price * conversionFactor + accrual;
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(BondFutureOption);
        }

        /// <summary>
        /// Returns true if deal is strongly path-dependent.
        /// </summary>
        public override bool FullPricing()
        {
            return Respect_Default == YesNo.Yes;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (BondFutureOption)Deal;

            if ((Respect_Default == YesNo.Yes || Use_Survival_Probability == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, "Use_Survival_Probability = Yes or Respect_Default = Yes ignored for deal with missing Issuer.");
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            BondFutureOption deal = (BondFutureOption)Deal;

            base.RegisterFactors(factors, errors);
            InterestVolBase.RegisterInterestYieldVol(factors, deal.Yield_Volatility, fCurrency);

            if (NeedRating(Respect_Default, deal.Issuer))
            {
                factors.Register<CreditRating>(deal.Issuer);

                // register realized recovery rate.
                factors.Register<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }

            if (NeedSurvivalProbability(Use_Survival_Probability, deal.Issuer))
                factors.RegisterInterface<ISurvivalProb>(InterestRateUtils.GetRateId(deal.Survival_Probability, deal.Issuer));
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);
            BondFutureOption deal = (BondFutureOption)Deal;
            fInterestYieldVol = InterestVolBase.GetYieldVol(factors, deal.Yield_Volatility, fCurrency);

            var bfb = (BondFuturesBasis)fFuturesBasis;
            GenerateCTD(factors.BaseDate, bfb.CTD_Issue_Date, bfb.CTD_Maturity_Date, bfb.CTD_Coupon_Interval, bfb.CTD_First_Coupon_Date, bfb.CTD_Penultimate_Coupon_Date, bfb.CTD_Day_Count, Deal.GetHolidayCalendar(), bfb.CTD_Coupon_Rate, bfb.CTD_Conversion_Factor);

            if (NeedRating(Respect_Default, deal.Issuer))
            {
                fCreditRating = factors.Get<CreditRating>(deal.Issuer);
                fRecoveryRate = factors.Get<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }
            else
            {
                fCreditRating = null;
                fRecoveryRate = null;
            }

            if (NeedSurvivalProbability(Use_Survival_Probability, deal.Issuer))
                fSurvivalProb = factors.GetInterface<ISurvivalProb>(InterestRateUtils.GetRateId(deal.Survival_Probability, deal.Issuer));
            else
                fSurvivalProb = null;
        }

        /// <summary>
        /// Transform futures price to allow the generic pricing to be applied.
        /// </summary>
        public override double PriceTransform(double price)
        {
            return PriceTransform(price, fConversionFactor, fAccrual);
        }

        /// <inheritdoc />
        public override double ScalingFactor()
        {
            return base.ScalingFactor() / fConversionFactor;
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <inheritdoc />
        protected override string GetSACCRDealReference()
        {
            return Deal.Reference;
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
        protected override void RegisterFuturesPriceFactor(PriceFactorList factors, ErrorList errors)
        {
            BondFutureOption deal = (BondFutureOption)fDeal;

            BondFuturesBasis bfb  = factors.Register<BondFuturesBasis>(FutureBase.GetFactorID(deal.Contract, deal.Settlement_Date));

            if (deal.Settlement_Date >= bfb.CTD_Maturity_Date)
                errors.Add(ErrorLevel.Error, "Settlement date must be before cheapest-to-deliver maturity date of the Bond Future Basis price factor.");
        }

        /// <inheritdoc />
        protected override void GetFuturesPriceFactor(PriceFactorList factors)
        {
            BondFutureOption deal = (BondFutureOption)fDeal;
            fFuturesBasis         = factors.Get<BondFuturesBasis>(FutureBase.GetFactorID(deal.Contract, deal.Settlement_Date));
        }

        /// <summary>
        /// Generate CTD dates and set CTD coupon rate and conversion factor.
        /// </summary>
        protected void GenerateCTD(double baseDate, double issueDate, double maturityDate, double couponInterval, double firstCouponDate, double penultimateCouponDate, DayCount dayCount, IHolidayCalendar calendar, double couponRate, double conversionFactor)
        {
            if (conversionFactor <= 0.0)
                return; // No CTD details or details invalid

            BondFutureOption deal = (BondFutureOption)fDeal;

            // Validation of settlement date not done for CTD details on price factor
            if (deal.Settlement_Date >= maturityDate)
                throw new AnalyticsException("Settlement date must be before cheapest-to-deliver maturity date.");

            DateGenerationResults dateGenerationResults = deal.GetDateGenerationResults(issueDate, maturityDate, couponInterval, firstCouponDate, penultimateCouponDate, dayCount, calendar);

            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;

            fIssueDate        = issueDate;
            fMaturityDate     = maturityDate;
            fCouponInterval   = couponInterval;
            fCouponRate       = couponRate;
            fConversionFactor = conversionFactor;

            fAccrual = PricingFunctions.AccruedInterest(deal.Settlement_Date, fIssueDate, fPayDates, fAccruals, fCouponRate, 1.0, null);

            double strike    = PriceTransform(deal.Strike);
            double tSettle   = CalcUtils.DaysToYears(deal.Settlement_Date - baseDate);
            double tMaturity = CalcUtils.DaysToYears(fMaturityDate - baseDate);
            fStrikeYield = PricingFunctions.BondYieldFromPrice(tSettle, tMaturity, couponRate, couponInterval, strike);
        }

        /// <summary>
        /// Calculate forward price, discount factor and volatility
        /// </summary>
        protected override void PriceAndVolatility(double baseDate, double valueDate, Vector forwardPrice, Vector discountFactor, Vector volatility)
        {
            BondFutureOption deal = (BondFutureOption)fDeal;

            double t         = CalcUtils.DaysToYears(valueDate - baseDate);
            double tSettle   = CalcUtils.DaysToYears(deal.Settlement_Date - baseDate);
            double tMaturity = CalcUtils.DaysToYears(fMaturityDate - baseDate);

            fDiscountRate.GetValue(discountFactor, t, tSettle);

            if (volatility != null)
            {
                double tExpiry = deal.GetTimeToExpiry(baseDate);
                if (tExpiry > t)
                {
                    // Calculate price volatility
                    using (var cache = Vector.CacheLike(forwardPrice))
                    {
                        Vector macaulayDuration = cache.Get();
                        Vector yield            = cache.Get();
                        Vector yieldStrike      = cache.Get(fStrikeYield);

                        // Calculate forwrad price, yield and adjusted duration using simple bond price functions
                        PricingFunctions.BondForwardPriceAndAdjustedMacaulayDuration(forwardPrice, macaulayDuration, t, tSettle, tMaturity, fCouponRate, fCouponInterval, discountFactor, fDiscountRate, fSurvivalProb);
                        PricingFunctions.BondYieldFromPrice(yield, tSettle, tMaturity, fCouponRate, fCouponInterval, forwardPrice);

                        // Calculate Modified Duration from Macaulay Duration.
                        Vector modifiedDuration = cache.Get();
                        PricingFunctions.GetModifiedDuration(modifiedDuration, macaulayDuration, yield, fCouponInterval);

                        // Get yield volatility
                        fInterestYieldVol.GetValue(volatility, t, yield, yieldStrike, tExpiry, tMaturity - tSettle);

                        // Convert (normal) yield vol to lognormal price vol
                        volatility.MultiplyBy(modifiedDuration);

                        if (fInterestYieldVol.GetDistributionType() == ProbabilityDistribution.Lognormal)
                        {
                            // Convert lognormal yield vol to lognormal price vol.
                            volatility.MultiplyBy(yield);
                        }
                    }
                }
                else
                {
                    volatility.Clear();
                }
            }

            // Recalculate forward price using fAccruals
            double accrual, cash;
            PricingFunctions.BondPrice(forwardPrice, out accrual, out cash, baseDate, valueDate, deal.Settlement_Date, fIssueDate, fMaturityDate, 1.0, fCouponRate, fPayDates, fAccruals, fDiscountRate, null, null, 0.0, fSurvivalProb, 1.0);

            BondFutureValuation.AdjustForDefault(baseDate, valueDate, forwardPrice, deal.Expiry_Date, Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer), fUnderlyingIsAlive, fHistoricalRecovery, fDefaultTime, fDiscountRate, fRecoveryRate);

            forwardPrice.AssignQuotient(forwardPrice, discountFactor);

            if (!fRepoIsDiscount)
                fRepoRate.GetValue(discountFactor, t, tSettle);
        }
    }
}
