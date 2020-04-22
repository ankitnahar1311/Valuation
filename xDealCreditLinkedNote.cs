// -----------------------------------------------------------------------------
// Name         xDealCreditLinkedNote
// Author:      Phil Koop, SunGard
// Project:     CORE Analytics
// Description: Object definition for Credit Linked Notes.
// -----------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    [Serializable]
    public abstract class DealCreditLinkedNoteBase : IRDeal
    {
        protected BuySell fBuySell;
        protected double fPrice = 0;
        protected double fEffectiveDate = 0;
        protected double fMaturityDate = 0;
        protected double fNotionalAmount = 0;
        protected double fCouponRate = 0; // valid iff fixed
        protected double fCouponSpread = 0; // valid iff floating
        protected double fCouponInterval = 0;
        protected DayCount fAccrualDayCount = DayCount.ACT_365;
        protected double fIndexTenor = 0;
        protected DayCount fIndexDayCount = DayCount.ACT_365;
        protected bool fPrincipalGuarntd = false;
        protected InterestRateType fCouponType;

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); } set { SetCalendarNames(0, value); }
        }

        public BuySell Buy_Sell
        {
            get { return fBuySell; } set { fBuySell = value; }
        }

        public double Price
        {
            get { return fPrice; } set { fPrice = value; }
        }

        public TDate Effective_Date
        {
            get { return fEffectiveDate; } set { fEffectiveDate = value; }
        }

        public TDate Maturity_Date
        {
            get { return fMaturityDate; } set { fMaturityDate = value; }
        }

        public double Notional_Amount
        {
            get { return fNotionalAmount; } set { fNotionalAmount = value; }
        }

        public InterestRateType Coupon_Type
        {
            get { return fCouponType; } set { fCouponType = value; }
        }

        public double Coupon_Rate
        {
            get { return fCouponRate; } set { fCouponRate = value; }
        }

        public double Coupon_Spread
        {
            get { return fCouponSpread; } set { fCouponSpread = value; }
        }

        public Period Coupon_Interval
        {
            get { return fCouponInterval; } set { fCouponInterval = value; }
        }

        public DayCount Accrual_Day_Count
        {
            get { return fAccrualDayCount; } set { fAccrualDayCount = value; }
        }

        public Period Index_Tenor
        {
            get { return fIndexTenor; } set { fIndexTenor = value; }
        }

        public DayCount Index_Day_Count
        {
            get { return fIndexDayCount; } set { fIndexDayCount = value; }
        }

        public bool Principal_Guaranteed
        {
            get { return fPrincipalGuarntd; } set { fPrincipalGuarntd = value; }
        }

        [NonMandatory]
        public string Forecast_Rate
        {
            get { return fForecast; } set { fForecast = value; }
        }

        // ------------------------------------------------------------------------------
        // Description: Returns the ID of the deal's valuation currency, which is a
        //              simple multiplier in the valuation function.
        // ------------------------------------------------------------------------------
        public override string DealCurrency()
        {
            return fCurrency;
        }

        // -----------------------------------------------------------------------------
        // Description: Deal end date
        // -----------------------------------------------------------------------------
        public override double EndDate()
        {
            return Maturity_Date;
        }

        // -----------------------------------------------------------------------------
        // Description: Validations
        // -----------------------------------------------------------------------------
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Effective_Date, Maturity_Date, true);
        }

        // ------------------------------------------------------------------------------
        // Description: Return a text string summary of the deal parameters
        // ------------------------------------------------------------------------------
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3}", fBuySell, fCurrency, fNotionalAmount, Maturity_Date);
        }
    }

    [Serializable]
    public abstract class CreditLinkedNoteBaseValuation : IRValuation
    {
        protected YesNo fRespectDefault = YesNo.No;
        [NonSerialized]
        protected DateList PayDates = null; // payment dates
        [NonSerialized]
        protected DateList ResetDates = null; // reset dates
        [NonSerialized]
        protected double[] Accruals = null; // accrual periods

        public override Deal Deal
        {
            get { return this.fDeal; }
            set { this.fDeal = (DealCreditLinkedNoteBase)value; }
        }

        /// <summary>
        /// Gets or sets the Respect_Default valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Respect_Default
        {
            get { return fRespectDefault; }
            set { fRespectDefault = value; }
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList Factors, BaseTimeGrid BaseTimes, RequiredResults ResultsRequired)
        {
            base.PreCloneInitialize(Factors, BaseTimes, ResultsRequired);

            DealCreditLinkedNoteBase deal = (DealCreditLinkedNoteBase) fDeal;

            DateGenerationRequest dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresResetDates = true,
                RequiresYearFractions = true,
            };

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = deal.Effective_Date,
                MaturityDate = deal.Maturity_Date,
                CouponPeriod = deal.Coupon_Interval,
                AccrualCalendar = deal.GetHolidayCalendar(),
                AccrualDayCount = deal.Accrual_Day_Count,
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);

            PayDates = dateGenerationResults.PayDates;
            Accruals = dateGenerationResults.AccrualYearFractions;
            ResetDates = dateGenerationResults.ResetDates;

            // Add to valuation time grid
            bool cashRequired = ResultsRequired.CashRequired();
            fT.AddPayDate(deal.Effective_Date, cashRequired);
            fT.AddPayDates(PayDates, cashRequired);
        }

        /// <summary>
        /// Vector valuation function.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            TimeGridIterator tgi           = new TimeGridIterator(fT);
            PVProfiles       result        = valuationResults.Profile;
            CashAccumulators accumulator   = valuationResults.Cash;
            DealCreditLinkedNoteBase deal  = (DealCreditLinkedNoteBase) fDeal;

            ISurvivalProb    SP          = GetSurvivalProbability(factors);
            RecoveryRate     RR          = GetRecoveryRate(factors);
            CreditRating     CR          = GetCreditRating(factors);

            double tEffective    = CalcUtils.DaysToYears(deal.Effective_Date - factors.BaseDate);
            double scale         = (deal.Buy_Sell == BuySell.Buy) ? +deal.Notional_Amount : -deal.Notional_Amount;
            double purchasePrice = Percentage.PercentagePoint * deal.Price;
            double couponRate    = (deal.Coupon_Type == InterestRateType.Fixed) ? Percentage.PercentagePoint * deal.Coupon_Rate : 0.0;
            double couponSpread  = (deal.Coupon_Type == InterestRateType.Fixed) ? 0.0 : BasisPoint.BasisPointValue * deal.Coupon_Spread;
            double indexTenor    = (deal.Index_Tenor > 0.0) ? deal.Index_Tenor : deal.Coupon_Interval;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                bool[]     hasDefaulted       = (Respect_Default == YesNo.Yes) ? new bool[factors.NumScenarios] : null;
                Vector     defaultTime        = (Respect_Default == YesNo.Yes) ? cache.Get() : null;
                Vector     historicalRecovery = (Respect_Default == YesNo.Yes) ? cache.GetClear() : null;

                if (hasDefaulted != null && CR != null)
                {
                    DefaultTime(defaultTime, CR);
                }

                Vector npv      = cache.Get();
                Vector cash     = cache.Get();
                Vector pStart   = cache.Get();
                Vector pEnd     = cache.Get();
                Vector amount   = cache.Get();
                Vector recovery = cache.Get();
                Vector dfLast   = cache.Get();
                Vector df       = cache.Get();

                cash.Clear();
                var defaultedBeforeTheBaseDate = Respect_Default == YesNo.Yes &&
                                                 CreditRating.DefaultedBeforeBaseDate(CR, factors.BaseDate);
                while (tgi.Next())
                {
                    if (defaultedBeforeTheBaseDate)
                    {
                        npv.Clear();
                        result.AppendVector(tgi.Date, npv);
                        break;
                    }

                    if (!deal.Principal_Guaranteed && Respect_Default == YesNo.Yes)
                        RealizedRecoveryRate(recovery, RR, tgi.T);

                    // Assume defaults are rare and start by valuing under all scenarios without realized defaults

                    // Value of principal repayment
                    SurvivalProbability(pEnd, factors, SP, tgi.T, fT.fLast);
                    fDiscountRate.GetValue(dfLast, tgi.T, fT.fLast);
                    if (deal.Principal_Guaranteed)
                        npv.Assign(dfLast);
                    else
                        npv.Assign(dfLast * pEnd);
                    if (accumulator != null && tgi.T == fT.fLast)
                        cash.Assign(npv);

                    // Value of coupons
                    for (int i = PayDates.Count - 1; i >= 0 && tgi.Date <= PayDates[i]; --i)
                    {
                        double tPay      = CalcUtils.DaysToYears(PayDates[i] - factors.BaseDate);
                        double tReset    = CalcUtils.DaysToYears(ResetDates[i] - factors.BaseDate);
                        double tPrevious = Math.Max(tgi.T, tReset);
                        SurvivalProbability(pStart, factors, SP, tgi.T, tPrevious);

                        if (deal.Coupon_Type == InterestRateType.Floating)
                        {
                            // Forecast a coupon, add the spread
                            InterestRateUtils.LiborRate(amount, fForecastRate, tgi.T, tReset, tReset, tReset + indexTenor, deal.Index_Day_Count);
                            amount.Add(couponSpread);
                            amount.MultiplyBy(Accruals[i]);
                        }
                        else
                        {
                            // Fixed coupon
                            amount.Assign(couponRate * Accruals[i]);
                        }

                        // The value of the coupon if no default
                        npv.Add(amount * fDiscountRate.Get(tgi.T, tPay) * pEnd);
                        if (accumulator != null && tgi.T == tPay)
                            cash.Assign(amount);

                        // The recovery value on default - assume guaranteed principal paid at end, recovery paid immediately
                        if (!deal.Principal_Guaranteed)
                            npv.Add(fDiscountRate.Get(tgi.T, 0.5 * (tPay + tPrevious)) * (pStart - pEnd) * PricingRecoveryRate(SP));

                        pEnd.DestructiveAssign(pStart);
                    }

                    // Now check for realized default scenario by scenario, overwriting NPV and cash as appropriate
                    if (Respect_Default == YesNo.Yes && defaultTime != null)
                    {
                        if (tgi.T < tEffective)
                            fDiscountRate.GetValue(df, tgi.T, tEffective);

                        for (int i = 0; i < npv.Count; ++i)
                        {
                            if (defaultTime[i] > tgi.T)
                                continue;

                            if (deal.Principal_Guaranteed)
                            {
                                npv[i] = dfLast[i];   // full principal paid at maturity
                            }
                            else
                            {
                                if (!hasDefaulted[i])
                                    historicalRecovery[i] = recovery[i];   // record the historical recovery rate

                                if (tgi.T < tEffective)
                                    npv[i] = df[i] * historicalRecovery[i];   // The discounted recovery value of the principal will be paid out on the effective date
                                else if (tgi.T == tEffective || !hasDefaulted[i])
                                    npv[i] = historicalRecovery[i];           // The full recovery amount is paid out
                                else
                                    npv[i] = 0.0;                             // default is in the past but we are after effective date; settlement has already occurred.
                            }

                            hasDefaulted[i] = true;
                        }
                    }

                    // Value of purchase price
                    if (tgi.T < tEffective)
                    {
                        npv.Add(-purchasePrice * fDiscountRate.Get(tgi.T, tEffective));
                    }
                    else if (tgi.T == tEffective)
                    {
                        npv.Add(-purchasePrice);
                        if (accumulator != null)
                            cash.Add(-purchasePrice);
                    }

                    result.AppendVector(tgi.Date, scale * npv * fFxRate.Get(tgi.T));
                    if (accumulator != null)
                        accumulator.Accumulate(fFxRate, tgi.Date, scale * cash);
                }

                // After maturity
                result.Complete(fT);
            }
        }

        /// <inheritdoc />
        protected override void GetDefaultTime(Vector defaultTime, PriceFactorList factors)
        {
            CreditRating cr = GetCreditRating(factors);
            if (cr != null)
            {
                cr.DefaultTime(defaultTime);
                return;
            }

            base.GetDefaultTime(defaultTime, factors);
        }

        /// <summary>
        /// Fetch the survival probability price factor, or null if not applicable.
        /// </summary>
        protected virtual ISurvivalProb GetSurvivalProbability(PriceFactorList factors)
        {
            return null;
        }

        /// <summary>
        /// Fetch the recovery rate price factor, or null if not applicable.
        /// </summary>
        protected virtual RecoveryRate GetRecoveryRate(PriceFactorList factors)
        {
            return null;
        }

        /// <summary>
        /// Fetch the credit rating price factor, or null if not applicable.
        /// </summary>
        protected virtual CreditRating GetCreditRating(PriceFactorList factors)
        {
            return null;
        }

        protected abstract void SurvivalProbability(Vector vout, PriceFactorList factors, ISurvivalProb SP, double t1, double t2);

        /// <summary>
        /// Returns the recovery rate to be used for realized default.
        /// </summary>
        protected abstract void RealizedRecoveryRate(Vector vout, RecoveryRate RR, double t);

        /// <summary>
        /// Returns the recovery rate to be used for pricing.
        /// </summary>
        protected abstract double PricingRecoveryRate(ISurvivalProb sp);

        /// <summary>
        /// Return the time of default for the underlying obligor. Allows derived valuation models to
        /// support realized default.
        /// </summary>
        protected void DefaultTime(Vector vout, CreditRating creditRating)
        {
            if (creditRating == null)
                vout.Assign(CreditRating.TimeOfDefault.Never);
            else
                creditRating.DefaultTime(vout);
        }
    }

    [Serializable]
    [System.ComponentModel.DisplayName("Credit Linked Note")]
    public class DealCreditLinkedNote : DealCreditLinkedNoteBase
    {
        protected string fName = string.Empty;
        protected string fRecovery = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DealCreditLinkedNote"/> class.
        /// </summary>
        public DealCreditLinkedNote()
        {
            Survival_Probability = string.Empty;
        }

        public string Name
        {
            get { return fName;     } set { fName     = value; }
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
            get { return fRecovery; } set { fRecovery = value; }
        }
    }

    [Serializable]
    [DisplayName("Credit Linked Note Valuation")]
    public class CreditLinkedNoteValuation : CreditLinkedNoteBaseValuation
    {
        public override Type DealType()
        {
            return typeof(DealCreditLinkedNote);
        }

        /// <summary>
        /// Vaulation becomes path-dependent if valuation model respects default.
        /// </summary>
        public override bool FullPricing()
        {
            return Respect_Default == YesNo.Yes;
        }

        // -----------------------------------------------------------------------------
        // Description: Register price factors
        // -----------------------------------------------------------------------------
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            DealCreditLinkedNote deal = (DealCreditLinkedNote)Deal;

            base.RegisterFactors(factors, errors);

            factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Name : deal.Survival_Probability);

            if (Respect_Default == YesNo.Yes)
            {
                factors.Register<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Name : deal.Recovery_Rate);
                factors.Register<CreditRating>(deal.Name);
            }
        }

        /// <summary>
        /// Fetch the survival probability price factor, or null if not applicable.
        /// </summary>
        protected override ISurvivalProb GetSurvivalProbability(PriceFactorList factors)
        {
            var deal = (DealCreditLinkedNote)Deal;
            return factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Name : deal.Survival_Probability);
        }

        /// <summary>
        /// Fetch the recovery rate price factor, or null if not applicable.
        /// </summary>
        protected override RecoveryRate GetRecoveryRate(PriceFactorList factors)
        {
            if (Respect_Default == YesNo.Yes)
            {
                var deal = (DealCreditLinkedNote)Deal;
                return factors.Get<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? deal.Name : deal.Recovery_Rate);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Fetch the credit rating price factor, or null if not applicable.
        /// </summary>
        protected override CreditRating GetCreditRating(PriceFactorList factors)
        {
            return (Respect_Default == YesNo.Yes) ? factors.Get<CreditRating>(((DealCreditLinkedNote)Deal).Name) : null;
        }

        protected override void SurvivalProbability(Vector vout, PriceFactorList factors, ISurvivalProb SP, double t1, double t2)
        {
            SP.GetValue(vout, t1, t2);
        }

        /// <summary>
        /// Returns the recovery rate to be used for realized default.
        /// </summary>
        protected override void RealizedRecoveryRate(Vector vout, RecoveryRate RR, double t)
        {
            RR.GetValue(vout, t);
        }

        /// <summary>
        /// Returns the recovery rate to be used for pricing.
        /// </summary>
        protected override double PricingRecoveryRate(ISurvivalProb sp)
        {
            return sp.GetRecoveryRate();
        }
    }
}
