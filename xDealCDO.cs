/// <author>
/// Philip Koop.
/// </author>
/// <owner>
/// Andy Hudson.
/// </owner>
/// <summary>
/// Deal class and top-down valuation model for CDO tranches.
/// </summary>
using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Deal class for synthetic single-tranche CDO.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("CDO Tranche")]
    public class DealCDO : Deal
    {
        protected BuySell fBuySell = BuySell.Buy;
        protected double fPrincipal = 0.0;
        protected string fCurrency = string.Empty;
        protected double fEffectiveDate = 0.0;
        protected double fMaturityDate = 0.0;
        protected double fPayFrequency = 0.0; // really period, in years
        protected DayCount fAccrualDayCount = DayCount.ACT_365;
        protected double fUpfrontAmount = 0.0;
        protected double fUpfrontDate = 0.0;
        protected double fSpread = 0.0;
        protected string fIndex = string.Empty;
        protected double fAttachment = 0.0;
        protected double fDetachment = 0.0;
        protected string fDiscountRate = string.Empty;
        protected Percentage fPayoffPercentage = 1.0;   // 0% recovery is a more sensible default than 100%

        /// <summary>
        /// Default constructor; initializes non-mandatory automatic properties.
        /// </summary>
        public DealCDO()
        {
            Payoff_Is_Digital = YesNo.No;
        }

        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); }  set { SetCalendarNames(0, value);    }
        }

        public BuySell Buy_Sell
        {
            get { return fBuySell;       }  set { fBuySell       = value; }
        }

        public double Principal
        {
            get { return fPrincipal;     }  set { fPrincipal     = value; }
        }

        public string Currency
        {
            get { return fCurrency;      }  set { fCurrency      = value; }
        }

        public TDate Effective_Date
        {
            get { return fEffectiveDate; }  set { fEffectiveDate = value; }
        }

        public TDate Maturity_Date
        {
            get { return fMaturityDate;  }  set { fMaturityDate  = value; }
        }

        public Period Pay_Frequency
        {
            get { return fPayFrequency;  }  set { fPayFrequency  = value; }
        }

        public DayCount Accrual_Day_Count
        {
            get { return fAccrualDayCount;} set { fAccrualDayCount = value; }
        }

        public Percentage Upfront_Amount
        {
            get { return fUpfrontAmount; }  set { fUpfrontAmount = value; }
        }

        public TDate Upfront_Date
        {
            get { return fUpfrontDate;   }  set { fUpfrontDate   = value; }
        }

        public BasisPoint Spread
        {
            get { return fSpread;        }  set { fSpread        = value; }
        }

        public string Reference_Index
        {
            get { return fIndex;         }  set { fIndex         = value; }
        }

        public Percentage Attachment
        {
            get { return fAttachment;    }  set { fAttachment    = value; }
        }

        public Percentage Detachment
        {
            get { return fDetachment;    }  set { fDetachment    = value; }
        }

        [NonMandatory]
        public string Discount_Rate
        {
            get { return fDiscountRate; } set { fDiscountRate = value; }
        }

        /// <summary>
        /// If Yes, the loss in the event of default of a basket member is given
        /// by Percentage * Notional instead of (1 - Recovery Rate) * Notional.
        /// </summary>
        [NonMandatory]
        public YesNo Payoff_Is_Digital
        {
            get;
            set;
        }

        /// <summary>
        /// The payoff percentage to use when the payoff is digital.
        /// </summary>
        [NonMandatory]
        public Percentage Digital_Payoff_Percentage
        {
            get
            {
                return fPayoffPercentage;
            }

            set
            {
                // Note that a digital payoff is the complement of a recovery rate and so its bounds are the complements of the recovery rate bounds.
                double maxPayoff = 1.0 - CalcUtils.MinRecoveryRate;
                double minPayoff = 1.0 - CalcUtils.MaxRecoveryRate;
                if (value >= minPayoff && value <= maxPayoff)
                    fPayoffPercentage = value;
                else
                    throw new AnalyticsException("The digital payoff percentage must be between " + ((Percentage)minPayoff).ToString() + " and " + ((Percentage)maxPayoff).ToString() + ".");
            }
        }

        /// <summary>
        /// Validations.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);
           
            CalcUtils.ValidateDates(errors, Effective_Date, Maturity_Date, true);

            // pay frequency (period) must not be greater than entire swap tenor
            double swapTenor = CalcUtils.DaysToYears(fMaturityDate - fEffectiveDate);

            if (fPayFrequency > swapTenor)
                AddToErrors(errors, "Period between payments cannot be greater than period from Effective Date to Maturity Date (pay side)");

            // attachment must be on [0,1), detachment on (0,1], attachment < detachment
            if ((fAttachment < 0) || (fAttachment >= 1))
                AddToErrors(errors, "Attachment point must be less than one and cannot be less than zero");

            if ((fDetachment <= 0) || (fDetachment > 1))
                AddToErrors(errors, "Detachment point must be greater than zero and cannot be greater than one");

            if (fDetachment <= fAttachment)
                AddToErrors(errors, "Attachment point must be less than detachment point");

            if (Upfront_Amount != 0 && Upfront_Date == 0.0)
                AddToErrors(errors, "Upfront date must be specified if there is an upfront payment");

            if (string.IsNullOrEmpty(Reference_Index))
                AddToErrors(errors, "Reference Index must be specified");
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return fMaturityDate;
        }

        /// <summary>
        /// Returns the ID of the deal's valuation currency, which is a
        /// simple multiplier in the valuation function.
        /// </summary>
        public override string DealCurrency()
        {
            return fCurrency;
        }

        /// <summary>
        /// Return a text string summary of the deal parameters.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2} {3:N} {4} {5}", fBuySell, fIndex, fCurrency, fPrincipal, Maturity_Date, Pay_Frequency);
        }
    }

    /// <summary>
    /// Top-down and bottom-up CDO models share a common valuation method that uses the
    /// interface defined by this class.
    /// </summary>
    public abstract class CDOValuationParameters
    {
        protected CDOValuationParameters(DealCDO deal, PriceFactorList factors)
        {
            DF = DiscountRate.Get(factors, InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency));
            X  = factors.GetInterface<IFxRate>(deal.Currency);
        }

        public IInterestRate DF
        {
            get; set;
        }

        public IFxRate X
        {
            get; set;
        }

        /// <summary>
        /// Realised loss and recovered vectors as of time t. Expressed as fraction of total index.
        /// </summary>
        public abstract void RealizedLoss(Vector realizedLoss, Vector realizedRecovery, double t, bool payoffIsDigital, double digitalPayoff);

        /// <summary>
        /// Vector of expected loss as of time t, for equity tranche with horizon T and detachment adjustedDetachment. 
        /// Expressed as fraction of total index. adjustedDetachment is compensated for the vector of realised losses.
        /// </summary>
        /// <param name="expectedLoss">Return the expected loss.</param>
        /// <param name="expectedRecovery">Return the expected recovery.</param>
        /// <param name="realizedLoss">Input realized loss.</param>
        /// <param name="realizedRecovery">Input realized recovery.</param>
        public abstract void ExpectedLossAndRecovery(Vector expectedLoss, Vector expectedRecovery, double t, double T, Vector adjustedDetachment, Vector realizedLoss, Vector realizedRecovery);
    }

    /// <summary>
    /// Parameters for top-down CDO deal valuation model.
    /// </summary>
    public class CDOTopDownValuationParameters : CDOValuationParameters
    {
        public PriceFactorList Factors; // temporary expedient

        /// <summary>
        /// Constructor.
        /// </summary>
        public CDOTopDownValuationParameters(DealCDO deal, PriceFactorList factors)
            : base(deal, factors)
        {
            EL = factors.GetInterface<IExpectedLoss>(deal.Reference_Index);
            RL = factors.GetInterface<IRealizedLoss>(deal.Reference_Index);

            Factors = factors;
        }

        public IExpectedLoss EL
        {
            get; set;
        }

        public IRealizedLoss RL
        {
            get; set;
        }

        /// <summary>
        /// Realised loss and recovery vectors as of time t. Expressed as fraction of total index.
        /// </summary>
        /// <remarks>
        /// The realized loss price factor does not count defaults; it must incorporate the payoff assumptions of the deal.
        /// </remarks>
        public override void RealizedLoss(Vector realizedLoss, Vector realizedRecovery, double t, bool payoffIsDigital, double digitalPayoff)
        {
            realizedRecovery.Clear();
            RL.GetValue(realizedLoss, t);
        }

        /// <summary>
        /// Vector of expected loss as of time t, for equity tranche with horizon T and detachment adjustedDetachment. 
        /// Expressed as fraction of total index. adjustedDetachment is compensated for vector of realised losses.
        /// </summary>
        /// <param name="expectedLoss">Return the expected loss.</param>
        /// <param name="expectedRecovery">Return the expected recovery.</param>
        /// <param name="realizedLoss">Input realized loss.</param>
        /// <param name="realizedRecovery">Input realized recovery.</param>
        public override void ExpectedLossAndRecovery(Vector expectedLoss, Vector expectedRecovery, double t, double T, Vector adjustedDetachment, Vector realizedLoss, Vector realizedRecovery)
        {
            expectedRecovery.Clear();
            EL.GetValue(expectedLoss, t, T, adjustedDetachment);
            expectedLoss.MultiplyBy(VectorMath.Max(0.0, 1.0 - realizedLoss));
        }
    }

    /// <summary>
    /// Top-down valuation model for synthetic single-tranche CDO. 
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("CDO Tranche valuation")]
    public class CDOValuation : Valuation
    {
        [NonSerialized]
        protected DateList AccrualDates = null;
        [NonSerialized]
        protected DateList PayDates = null;
        [NonSerialized]
        protected DateList ResetDates = null;
        [NonSerialized]
        protected double[] Accruals = null;

        protected DealCDO fDeal = null;

        /// <summary>
        /// Get or set deal property.
        /// </summary>
        public override Deal Deal
        {
            get { return this.fDeal; }
            set { this.fDeal = (DealCDO)value; }
        }

        /// <summary>
        /// Type of associated deal.
        /// </summary>
        public override Type DealType()
        {
            return typeof(DealCDO);
        }

        /// <summary>
        /// Full pricing is recommended because the simulation of realized
        /// losses causes some path dependency.
        /// </summary>
        public override bool FullPricing()
        {
            return true;
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
            factors.Register<DiscountRate>(InterestRateUtils.GetRateId(fDeal.Discount_Rate, fDeal.Currency));
            factors.RegisterInterface<IFxRate>(fDeal.Currency);
            factors.RegisterInterface<IExpectedLoss>(fDeal.Reference_Index);
            factors.RegisterInterface<IRealizedLoss>(fDeal.Reference_Index);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.PreCloneInitialize(factors, baseTimes, resultsRequired);

            // Set up date lists
            DateGenerationRequest dateGenerationRequest = new DateGenerationRequest
            {
                RequiresPayDates = true, 
                RequiresAccrualDates = true,
                RequiresYearFractions = true
            };
            
            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = fDeal.Effective_Date,
                MaturityDate = fDeal.Maturity_Date,
                CouponPeriod = fDeal.Pay_Frequency,
                AccrualCalendar = fDeal.GetHolidayCalendar(),
                AccrualDayCount = fDeal.Accrual_Day_Count,
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(dateGenerationRequest, dateGenerationParams);

            AccrualDates = dateGenerationResults.AccrualDates;
            PayDates = dateGenerationResults.PayDates;
            Accruals = dateGenerationResults.AccrualYearFractions;

            bool cashRequired = resultsRequired.CashRequired();

            fT.AddPayDate    (fDeal.Upfront_Date, cashRequired);
            fT.AddPayDates   (PayDates, cashRequired);
        }

        /// <summary>
        /// Valuation method.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            CDOValuationParameters parameters = GetValuationParameters(factors);

            double scale        = (fDeal.Buy_Sell == BuySell.Buy) ? +fDeal.Principal : -fDeal.Principal;
            double trancheSize  = fDeal.Detachment - fDeal.Attachment;
            if (trancheSize < CalcUtils.TINY)
                return;

            double tUpfront     = CalcUtils.DaysToYears(fDeal.Upfront_Date - factors.BaseDate);

            TimeGridIterator tgi         = new TimeGridIterator(fT);
            CashAccumulators accumulator = valuationResults.Cash;
            PVProfiles       result      = valuationResults.Profile;

            using (IntraValuationDiagnosticsHelper.StartDeal(fIntraValuationDiagnosticsWriter, Deal))
            {
                VectorEngine.For(tgi, () =>
                {
                    using (var cache = Vector.Cache(factors.NumScenarios))
                    {
                        Vector npv = cache.Get();
                        Vector expectedWritedownPremiumNotional = cache.Get();
                        Vector expectedLoss = cache.Get();
                        Vector expectedRecovery = cache.Get();
                        Vector discountFactor = cache.Get();
                        Vector realizedIndexLoss = cache.Get();
                        Vector realizedIndexRecovery = cache.Get();
                        Vector adjustedAttachment = cache.Get();
                        Vector adjustedDetachment = cache.Get();
                        Vector trancheRemainder = cache.Get();

                        // Handle upfront payment
                        if (fDeal.Upfront_Date >= tgi.Date)
                            npv.Assign(scale * parameters.DF.Get(tgi.T, tUpfront) * fDeal.Upfront_Amount);
                        else
                            npv.Clear();

                        // reinitialise running variables
                        expectedWritedownPremiumNotional.Clear();
                        expectedLoss.Clear();
                        expectedRecovery.Clear();
                        discountFactor.Assign(parameters.DF.Get(tgi.T, tgi.T));

                        if (accumulator != null && tgi.Date == fDeal.Upfront_Date)
                            accumulator.Accumulate(parameters.X, tgi.Date, fDeal.Upfront_Amount);

                        // Check for realized loss and recovery and adjust the attachment and detachment accordingly
                        parameters.RealizedLoss(realizedIndexLoss, realizedIndexRecovery, tgi.T, fDeal.Payoff_Is_Digital == YesNo.Yes, fDeal.Digital_Payoff_Percentage);

                        adjustedDetachment.Assign(VectorMath.Max(0.0, VectorMath.Min(1.0 - realizedIndexRecovery, fDeal.Detachment) - realizedIndexLoss));
                        adjustedAttachment.Assign(VectorMath.Max(0.0, VectorMath.Min(1.0 - realizedIndexRecovery, fDeal.Attachment) - realizedIndexLoss));

                        trancheRemainder.Assign((adjustedDetachment - adjustedAttachment) / trancheSize);

                        if (adjustedDetachment.MaxElement() > CalcUtils.TINY)
                        {
                            // Diagnostics
                            double sumDefaultAccrual = 0;
                            double sumPVPremium = 0;
                            double sumPVProtection = 0;

                            bool needDiagnostics = tgi.T == 0.0 && fIntraValuationDiagnosticsWriter.Level > IntraValuationDiagnosticsLevel.None;

                            using (needDiagnostics ? IntraValuationDiagnosticsHelper.StartCDO(fIntraValuationDiagnosticsWriter, tgi.Date, fDeal.Principal) : null)
                            {
                                // Value future coupon periods
                                VectorEngine.For(0, PayDates.Count, i =>
                                {
                                    if (PayDates[i] < tgi.Date)
                                        return LoopAction.Continue;

                                    double tPay = CalcUtils.DaysToYears(PayDates[i] - factors.BaseDate);

                                    using (var innerCache = Vector.CacheLike(npv))
                                    {
                                        Vector oldExpectedLoss = innerCache.Get(expectedLoss);
                                        Vector oldDiscountFactor = innerCache.Get(discountFactor);
                                        Vector oldExpectedWritedownPremiumNotional = innerCache.Get(expectedWritedownPremiumNotional);

                                        Vector expectedLossAttachment = innerCache.Get();
                                        Vector expectedLossDetachment = innerCache.Get();
                                        Vector premiumLeg = innerCache.Get();
                                        Vector defaultLeg = innerCache.Get();
                                        Vector accruedInDefault = innerCache.Get();
                                        Vector expectedRecoveryAttachment = innerCache.Get();
                                        Vector expectedRecoveryDetachment = innerCache.Get();
                                        Vector avgDiscountFactor = innerCache.Get();
                                        Vector pv = innerCache.Get();

                                        // Get the expected loss and recovery for the tranche detachment and attachment
                                        parameters.ExpectedLossAndRecovery(expectedLossDetachment, expectedRecoveryDetachment, tgi.T, tPay, adjustedDetachment, realizedIndexLoss, realizedIndexRecovery);
                                        parameters.ExpectedLossAndRecovery(expectedLossAttachment, expectedRecoveryAttachment, tgi.T, tPay, adjustedAttachment, realizedIndexLoss, realizedIndexRecovery);

                                        expectedLoss.Assign((expectedLossDetachment - expectedLossAttachment) / trancheSize);
                                        expectedRecovery.Assign((expectedRecoveryDetachment - expectedRecoveryAttachment) / trancheSize);
                                        expectedWritedownPremiumNotional.Assign(expectedLoss + expectedRecovery);

                                        // Premium leg approximation: Accrued in default pays half the accrued. Remove expected loss and recovery (top down writeoff)
                                        premiumLeg.Assign(fDeal.Spread * (trancheRemainder - expectedWritedownPremiumNotional) * Accruals[i]);
                                        accruedInDefault.Assign(fDeal.Spread * (expectedWritedownPremiumNotional - oldExpectedWritedownPremiumNotional) * 0.5 * Accruals[i]);

                                        // Default leg approximation: account for default with bullet payment at end of period
                                        defaultLeg.Assign(expectedLoss - oldExpectedLoss);

                                        // Convention: bought CDO pays the premium to the buyer
                                        discountFactor.Assign(parameters.DF.Get(tgi.T, tPay));
                                        avgDiscountFactor.Assign(0.5 * (discountFactor + oldDiscountFactor));

                                        pv.Assign(scale * (premiumLeg * discountFactor + (accruedInDefault - defaultLeg) * avgDiscountFactor));
                                        npv.Add(pv);

                                        if (accumulator != null && tgi.T == tPay)
                                            accumulator.Accumulate(parameters.X, tgi.Date, scale * (premiumLeg + accruedInDefault - defaultLeg));

                                        if (needDiagnostics)
                                        {
                                            using (var innerCache1 = Vector.CacheLike(npv))
                                            {
                                                Vector expectedPremium = innerCache1.Get(scale * premiumLeg);
                                                Vector expectedDefaultAccrual = innerCache1.Get(scale * accruedInDefault);
                                                Vector expectedDefaultLoss = innerCache1.Get(scale * defaultLeg);

                                                Vector pvDefaultAccrual = innerCache1.Get(expectedDefaultAccrual * avgDiscountFactor);
                                                Vector pvPremium = innerCache1.Get(expectedPremium * discountFactor);
                                                Vector pvProctection = innerCache1.Get(-expectedDefaultLoss * avgDiscountFactor);

                                                // accumulate sums
                                                if (i >= 0)
                                                {
                                                    sumDefaultAccrual += pvDefaultAccrual[0];
                                                    sumPVPremium += pvPremium[0];
                                                    sumPVProtection += pvProctection[0];
                                                }

                                                using (IntraValuationDiagnosticsHelper.StartCashflow(fIntraValuationDiagnosticsWriter, PayDates[i]))
                                                {
                                                    var remainingPool = cache.Get(1.0 - realizedIndexLoss - realizedIndexRecovery);
                                                    AddIntraValuationDiagnostics(fIntraValuationDiagnosticsWriter, parameters, adjustedAttachment, adjustedDetachment,
                                                        remainingPool, tgi.T, tPay);
                                                    IntraValuationDiagnosticsHelper.AddDetailedCDOCashflow(fIntraValuationDiagnosticsWriter,
                                                        expectedPremium, expectedRecovery, expectedDefaultAccrual, expectedDefaultLoss, discountFactor, pv);
                                                }
                                            }
                                        }
                                    }

                                    return LoopAction.Continue;
                                });

                                if (needDiagnostics)
                                    IntraValuationDiagnosticsHelper.AddSummaryCDOAmounts(fIntraValuationDiagnosticsWriter, npv, sumDefaultAccrual,
                                    sumPVPremium, sumPVProtection);

                            }

                            result.AppendVector(tgi.Date, npv * parameters.X.Get(tgi.T));
                        }
                    }
                });
            }

            // After maturity
            result.Complete(fT);
        }

        /// <summary>
        /// Get the accrual period start date a given date is in. Returns 0.0 if the date is outside of all accrual periods.
        /// </summary>
        /// <param name="date">The date to look for in the accrual date list</param>
        /// <returns>The start date of the accrual period that 'date' is in.</returns>
        /// <remarks>Used by the survival probability bootstrapper.</remarks>
        public TDate GetAccrualStartDate(double date)
        {
            // Malformed fAccrualDates - return N/A
            if (AccrualDates.Count < 2)
                return new TDate(0.0);

            // if date is before first or after last accrual date, return N/A
            if (date < AccrualDates.FirstDate() || date > AccrualDates.LastDate())
                return new TDate(0.0);

            // If date is the final accrual date, return start date of last accrual period
            if (date == AccrualDates.LastDate())
                return AccrualDates[AccrualDates.Count - 2];

            // Otherwise, locate the correct period
            int indexOfCurrentPeriod = AccrualDates.Locate(date);

            // If date == fAccrualDates[n] for some n > 0, Locate will have returned n-1, but we need fAccrualDates[n] in this case.
            // n + 1 is guaranteed to be within bounds due to preceeding conditions.
            if (AccrualDates[indexOfCurrentPeriod + 1] == date)
                return new TDate(AccrualDates[indexOfCurrentPeriod + 1]);

            return new TDate(AccrualDates[indexOfCurrentPeriod]);
        }

        /// <summary>
        /// Create appropriate CDO valuation parameters.
        /// </summary>
        public virtual CDOValuationParameters GetValuationParameters(PriceFactorList factors)
        {
            return new CDOTopDownValuationParameters(fDeal, factors);
        }

        /// <summary>
        /// Output Intra-Valuation Diagnostics
        /// </summary>
        public virtual void AddIntraValuationDiagnostics(IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter, CDOValuationParameters parameters,
            Vector adjustedAttachment, Vector adjustedDetachment, Vector remainingPool, double valueTime, double tPay)
        {
        }
    }
}
