/// <author>
/// Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for bond repo deals.
/// </summary>
using System;
using System.ComponentModel;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Fixed term bond repo deals.
    /// </summary>
    [Serializable, DisplayName("Bond Repo")]
    public class BondRepo : BondRepoBase
    {
        /// <summary>
        /// Gets or sets the maturity date.
        /// </summary>
        public TDate Maturity_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the repo rate.
        /// </summary>
        public Percentage Repo_Rate
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Maturity_Date;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (Maturity_Date <= Effective_Date)
                AddToErrors(errors, "Effective date must be before maturity date");
        }

        /// <summary>
        /// Gets the next reset date from the current reset date.
        /// </summary>
        /// <remarks>Current reset date is always the effective date and next reset date is therefore always the maturity date.</remarks>
        public override TDate GetNextResetDate(TDate effectiveDate)
        {
            return Maturity_Date;
        }

        /// <summary>
        /// Repo rate on initial loan period.
        /// </summary>
        public override double RepoRate()
        {
            return Repo_Rate;
        }
    }

    /// <summary>
    /// Open term bond repo deals.
    /// </summary>
    [Serializable, DisplayName("Bond Repo Open")]
    public class BondRepoOpen : BondRepoBase
    {
        /// <summary>
        /// Gets or sets the reset frequency.
        /// </summary>
        public Period Reset_Frequency
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the investment horizon.
        /// </summary>
        public Term Investment_Horizon
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the known repo rate of the in-progress repo.
        /// </summary>
        public Percentage Known_Repo_Rate
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the date of the known repo rate.
        /// </summary>
        public TDate Known_Repo_Rate_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the repo rate.
        /// </summary>
        [NonMandatory]
        public string Repo_Rate
        {
            get { return fRepo; } set { fRepo = value; }
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return DateAdjuster.Add(Effective_Date, Investment_Horizon);
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);
            ValidateOpenRepo(errors, Known_Repo_Rate_Date, Reset_Frequency, Investment_Horizon);
        }

        /// <summary>
        /// Gets the next reset date.
        /// </summary>
        /// <remarks>Treat the horizon as truncating the lending.</remarks>
        public override TDate GetNextResetDate(TDate currentResetDate)
        {
            return Reset_Frequency > 0.0 ?  Math.Min(DateAdjuster.Add(currentResetDate, Period.ValueToTerm(Reset_Frequency), GetHolidayCalendar()), EndDate()) : DateAdjuster.Add(Effective_Date, Investment_Horizon);
        }

        /// <summary>
        /// Returns Known_Repo_Rate_Date for validation at RegisterFactors().
        /// </summary>
        public override double RepoRateDate(double baseDate, double currentRepoStartDate, double currentRepoEndDate)
        {
            if (Known_Repo_Rate_Date < Effective_Date)
                return Effective_Date;

            return Known_Repo_Rate_Date;
        }

        /// <summary>
        /// Repo rate on initial loan period.
        /// </summary>
        public override double RepoRate()
        {
            return Known_Repo_Rate;
        }

        /// <summary>
        /// Validates the repo at register factors.
        /// </summary>
        public override void ValidateAtRegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            RegisterFactorsValidateOpenRepo(factors, Known_Repo_Rate_Date, errors);
        }
    }

    /// <summary>
    /// Base class for bond repo deals.
    /// </summary>
    [Serializable]
    public abstract class BondRepoBase : RepoBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BondRepoBase"/> class.
        /// </summary>
        protected BondRepoBase()
        {
            Amortisation         = new Amortisation();
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Recovery_Rate        = string.Empty;
        }

        /// <summary>
        /// Gets or sets the underlying bond issue date.
        /// </summary>
        public TDate Issue_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond maturity date.
        /// </summary>
        public TDate Bond_Maturity_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond notional.
        /// </summary>
        public double Notional
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond amortisation.
        /// </summary>
        [NonMandatory]
        public Amortisation Amortisation
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond coupon interval.
        /// </summary>
        public Period Coupon_Interval
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond coupon rate.
        /// </summary>
        public double Coupon_Rate
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond accrual day count convention.
        /// </summary>
        public DayCount Accrual_Day_Count
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond first coupon date.
        /// </summary>
        [NonMandatory]
        public TDate First_Coupon_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond penultimate coupon date.
        /// </summary>
        [NonMandatory]
        public TDate Penultimate_Coupon_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond issuer.
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
        /// Gets or sets the underlying bond recovery rate.
        /// </summary>
        [NonMandatory]
        public string Recovery_Rate
        {
            get; set;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, First_Coupon_Date, Penultimate_Coupon_Date, true, "issue", "bond maturity");
        }

        /// <summary>
        /// Deal description.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3} {4}", Buy_Sell, Currency, Notional, Bond_Maturity_Date, Coupon_Interval);
        }

        /// <summary>
        /// Gets the underlying bond notional.
        /// </summary>
        public override double GetUnits()
        {
            return Notional;
        }

        /// <summary>
        /// Gets the underlying bond issuer.
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
        /// Gets the underlying bond recovery rate.
        /// </summary>
        public override string GetRecoveryRate()
        {
            return Recovery_Rate;
        }
    }

    /// <summary>
    /// Valuation of bond repo deals.
    /// </summary>
    [Serializable, DisplayName("Bond Repo Valuation")]
    public class BondRepoValuation : RepoBaseValuation
    {
        [NonSerialized]
        protected DateList fPayDates = null;
        [NonSerialized]
        protected double[] fAccruals = null;
        [NonSerialized]
        protected double[] fPrincipals = null;
        [NonSerialized]
        protected double fFinalPrincipal = 0.0;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb = null;
        [NonSerialized]
        protected RecoveryRate fRecoveryRate = null;

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(BondRepoBase);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            var deal = (BondRepoBase)fDeal;

            if (string.IsNullOrEmpty(deal.Issuer))
                return;

            factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);

            factors.Register<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (BondRepoBase)fDeal;

            DateGenerationRequest dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresYearFractions = true,
                RequiresPrincipals = true,
            };

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = deal.Issue_Date,
                MaturityDate = deal.Bond_Maturity_Date,
                CouponPeriod = deal.Coupon_Interval,
                FirstCouponDate = deal.First_Coupon_Date,
                PenultimateCouponDate = deal.Penultimate_Coupon_Date,
                AccrualCalendar = deal.GetHolidayCalendar(),
                AccrualDayCount = deal.Accrual_Day_Count,
                Principal = deal.Notional,
                Amortisation = deal.Amortisation
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);

            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;
            fPrincipals = dateGenerationResults.Principals;
            fFinalPrincipal = dateGenerationResults.FinalPrincipal;
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            var deal = (BondRepoBase)fDeal;

            if (string.IsNullOrEmpty(deal.Issuer))
                return;

            fSurvivalProb = factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);

            fRecoveryRate = factors.Get<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Issuer : deal.Recovery_Rate);
        }

        /// <summary>
        /// Bond price at valuation date.
        /// </summary>
        protected override void SecurityPrice(Vector price, double baseDate, double valueDate, out double cash)
        {
            var deal = (BondRepoBase)fDeal;

            if (deal.Notional == 0.0)
            {
                price.Clear();
                cash = 0.0;
                return;
            }

            double accrual;
            double settlementDate = deal.Effective_Date;
            PricingFunctions.BondPrice(price, out accrual, out cash, baseDate, valueDate, settlementDate, deal.Issue_Date, deal.Bond_Maturity_Date, deal.Notional, Percentage.PercentagePoint * deal.Coupon_Rate, fPayDates, fAccruals, fDiscountRate, deal.Amortisation, fPrincipals, fFinalPrincipal, fSurvivalProb, 1.0);

            cash /= deal.Notional;
            price.MultiplyBy(1.0 / deal.Notional);
        }
    }

    /// <summary>
    /// Fixed term bond lending deals.
    /// </summary>
    [Serializable, DisplayName("Bond Lending")]
    public class BondLending : BondLendingBase
    {
        /// <summary>
        /// Gets or sets the maturity date.
        /// </summary>
        public TDate Maturity_Date
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Maturity_Date;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (Maturity_Date <= Effective_Date)
                AddToErrors(errors, "Effective date must be before maturity date");
        }

        /// <summary>
        /// Days in lending period.
        /// </summary>
        public override int GetDaysInPeriod()
        {
            return (int)(Maturity_Date - Effective_Date);
        }
    }

    /// <summary>
    /// Open term bond lending deals.
    /// </summary>
    [Serializable, DisplayName("Bond Lending Open")]
    public class BondLendingOpen : BondLendingBase
    {
        /// <summary>
        /// Gets or sets the reset frequency.
        /// </summary>
        public Period Reset_Frequency
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the investment horizon.
        /// </summary>
        public Term Investment_Horizon
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return DateAdjuster.Add(Effective_Date, Investment_Horizon);
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (Reset_Frequency <= 0.0)
                AddToErrors(errors, "Reset_Frequency must be greater than zero");

            if (Period.TermToValue(Investment_Horizon) < Reset_Frequency)
                AddToErrors(errors, "Investment_Horizon must be greater than or equal to Reset_Frequency");
        }

        /// <summary>
        /// Days in lending period.
        /// </summary>
        public override int GetDaysInPeriod()
        {
            return Period.ValueToTerm(Reset_Frequency).ToDays();
        }
    }

    /// <summary>
    /// Base class for bond lending deals.
    /// </summary>
    [Serializable]
    public abstract class BondLendingBase : SecurityLendingBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BondLendingBase"/> class.
        /// </summary>
        protected BondLendingBase()
        {
            Amortisation         = new Amortisation();
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
        }

        /// <summary>
        /// Gets or sets the underlying bond issue date.
        /// </summary>
        public TDate Issue_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond maturity date.
        /// </summary>
        public TDate Bond_Maturity_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond notional.
        /// </summary>
        public double Notional
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond amortisation.
        /// </summary>
        [NonMandatory]
        public Amortisation Amortisation
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond coupon interval.
        /// </summary>
        public Period Coupon_Interval
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond coupon rate.
        /// </summary>
        public double Coupon_Rate
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond accrual day count convention.
        /// </summary>
        public DayCount Accrual_Day_Count
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond first coupon date.
        /// </summary>
        [NonMandatory]
        public TDate First_Coupon_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond penultimate coupon date.
        /// </summary>
        [NonMandatory]
        public TDate Penultimate_Coupon_Date
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying bond issuer.
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
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, First_Coupon_Date, Penultimate_Coupon_Date, true, "issue", "bond maturity");
        }

        /// <summary>
        /// Deal description.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3} {4}", Borrower_Lender, Currency, Notional, Bond_Maturity_Date, Coupon_Interval);
        }

        /// <summary>
        ///  Gets the underlying bond notional.
        /// </summary>
        public override double GetUnits()
        {
            return Notional;
        }

        /// <summary>
        /// Gets the underlying bond issuer.
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
    }

    /// <summary>
    /// Valuation of bond lending deals.
    /// </summary>
    [Serializable, DisplayName("Bond Lending Valuation")]
    public class BondLendingValuation : SecurityLendingBaseValuation
    {
        [NonSerialized]
        protected DateList fPayDates = null;
        [NonSerialized]
        protected double[] fAccruals = null;
        [NonSerialized]
        protected double[] fPrincipals = null;
        [NonSerialized]
        protected double fFinalPrincipal = 0.0;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb = null;

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(BondLendingBase);
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            var deal = (BondLendingBase)fDeal;

            if (string.IsNullOrEmpty(deal.Issuer))
                return;

            factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (BondLendingBase)fDeal;

            DateGenerationRequest dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresYearFractions = true,
                RequiresPrincipals = true,
            };

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = deal.Issue_Date,
                MaturityDate = deal.Bond_Maturity_Date,
                CouponPeriod = deal.Coupon_Interval,
                FirstCouponDate = deal.First_Coupon_Date,
                PenultimateCouponDate = deal.Penultimate_Coupon_Date,
                AccrualCalendar = deal.GetHolidayCalendar(),
                AccrualDayCount = deal.Accrual_Day_Count,
                Principal = deal.Notional,
                Amortisation = deal.Amortisation
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);

            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;
            fPrincipals = dateGenerationResults.Principals;
            fFinalPrincipal = dateGenerationResults.FinalPrincipal;
        }

        /// <summary>
        /// Prepare for valuation anything that is dependent upon the scenario.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            var deal = (BondLendingBase)fDeal;

            if (string.IsNullOrEmpty(deal.Issuer))
                return;

            fSurvivalProb = factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <summary>
        /// Bond price at valuation date.
        /// </summary>
        protected override void SecurityPrice(Vector price, double baseDate, double valueDate)
        {
            var deal = (BondLendingBase)fDeal;

            if (deal.Notional == 0.0)
            {
                price.Clear();
                return;
            }

            double accrual;
            double cash;
            PricingFunctions.BondPrice(price, out accrual, out cash, baseDate, valueDate, valueDate, deal.Issue_Date, deal.Bond_Maturity_Date, deal.Notional, Percentage.PercentagePoint * deal.Coupon_Rate, fPayDates, fAccruals, fDiscountRate, deal.Amortisation, fPrincipals, fFinalPrincipal, fSurvivalProb, 1.0);
            price.MultiplyBy(1.0 / deal.Notional);
        }
    }
}