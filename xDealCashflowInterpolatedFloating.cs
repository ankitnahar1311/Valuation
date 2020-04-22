using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// A deal representating a a interest rate swap stub cashflow payment where the floating rate is reset against interpolation of two LIBOR rates.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Cashflow (Interpolated)")]
    public class FloatingInterestCashflowInterpolatedDeal : IRDeal
    {
        protected int fInterpolatedRateRounding = 3;

        private const int AccrualCalendarsIndex = 0;
        private const int RateCalendarsIndex = 1;

        /// <summary>
        /// Constructor for FloatingInterestCashflowInterpolatedDeal.
        /// </summary>
        public FloatingInterestCashflowInterpolatedDeal()
        {
            Rate_1_Fixing = string.Empty;
            Rate_2_Fixing = string.Empty;
            Use_Known_Rate_1 = YesNo.No;
            Use_Known_Rate_2 = YesNo.No;
            Rate_Adjustment_Method = DateAdjustmentMethod.Modified_Following;
            Rate_Sticky_Month_End = YesNo.No;
        }

        /// <summary>
        /// ID of the interest rate price factor for the first reference rate.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate_1
        {
            get { return fForecast; }
            set { fForecast = value; }
        }

        /// <summary>
        /// ID of the interest rate price factor for the second reference rate.
        /// </summary>
        [NonMandatory]
        public string Forecast_Rate_2
        {
            get { return fForecast2; }
            set { fForecast2 = value; }
        }

        /// <summary>
        /// Buy/Sell for the stub position.
        /// </summary>
        public BuySell Buy_Sell { get; set; }

        /// <summary>
        /// Principal of the stub.
        /// </summary>
        public double Principal { get; set; }

        /// <summary>
        /// Payment date of the stub. If missing, the payment date is the accrual end date.
        /// </summary>
        public TDate Payment_Date { get; set; }

        /// <summary>
        /// Start date of the accrual period.
        /// </summary>
        public TDate Accrual_Start_Date { get; set; }

        /// <summary>
        /// End date of the accrual period.
        /// </summary>
        public TDate Accrual_End_Date { get; set; }

        /// <summary>
        /// Specified accrual year fraction calculations.
        /// </summary>
        public double Accrual_Year_Fraction { get; set; }

        /// <summary>
        /// The day count convention for accrual year fraction calculations.
        /// </summary>
        /// <remarks>Not used when Accrual_Year_Fraction is specified.</remarks>
        public DayCount Accrual_Day_Count { get; set; }

        /// <summary>
        /// Observed calendar for accrual period.
        /// </summary>
        /// <remarks>Not used when Accrual_Year_Fraction is specified.</remarks>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Accrual_Calendars
        {
            get { return GetCalendarNames(AccrualCalendarsIndex); }
            set { SetCalendarNames(AccrualCalendarsIndex, value); }
        }

        /// <summary>
        /// The reset date of both referenced rates. The assumption is that the reset dates for both reference rates are the same.
        /// </summary>
        public TDate Reset_Date { get; set; }

        /// <summary>
        /// The start date of both referenced rates. The assumption is that the rate start dates for both reference rates are the same.
        /// </summary>
        public TDate Rate_Start_Date { get; set; }

        /// <summary>
        /// Observed calendar for reset period for both referenced rates.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Rate_Calendars
        {
            get { return GetCalendarNames(RateCalendarsIndex); }
            set { SetCalendarNames(RateCalendarsIndex, value); }
        }

        /// <summary>
        /// Adjustment method used to calculate rate end dates and rate year fractions.
        /// </summary>
        public DateAdjustmentMethod Rate_Adjustment_Method { get; set; }

        /// <summary> Gets or sets the rate sticky month end setting. </summary>
        public YesNo Rate_Sticky_Month_End
        {
            get; set;
        }

        /// <summary>
        /// The day count convention for referenced rate year fraction calculations.
        /// </summary>
        /// <remarks>Not used when both Rate_1_Year_Fraction and Rate_2_Year_Fraction are specified.</remarks>
        public DayCount Rate_Day_Count { get; set; }

        /// <summary>
        /// Floating margin.
        /// </summary>
        public BasisPoint Margin { get; set; }

        /// <summary>
        /// Rounding of the calculated interpolated stub rate when the interpolated rate is expressed in percentages. Rounding only in effect if all reset rates are realized (possibly in simulation). 0 means no rounding.
        /// </summary>
        /// <remarks>
        /// For example, if Interpolated_Rate_Rounding is 5, and the calculated interpolated rate is 5.123456789%, then
        /// the rate used to calculate cashflow amount is 5.12346%.
        /// </remarks>
        public int Interpolated_Rate_Rounding
        {
            get
            {
                return fInterpolatedRateRounding;
            }

            set
            {
                if (0 <= value && value <= CFFloatingInterest.MaximumRounding)
                    fInterpolatedRateRounding = value;
                else
                    throw new AnalyticsException(string.Format("Interpolated_Rate_Rounding must be a non-negative integer less than or equal to {0}, with 0 meaning no rounding.", CFFloatingInterest.MaximumRounding));
            }
        }

        /// <summary>
        /// Tenor of first reference rate.
        /// </summary>
        /// <remarks>Not used when Rate_1_End_Date is specified.</remarks>
        public Term Rate_1_Tenor { get; set; }

        /// <summary>
        /// The end date of the first referenced rate.
        /// </summary>
        [NonMandatory]
        public TDate Rate_1_End_Date { get; set; }

        /// <summary>
        /// Specified year fraction for first reference rate.
        /// </summary>
        public double Rate_1_Year_Fraction { get; set; }

        /// <summary>
        /// Identifier for the rate fixing for the first referenced rate in the Rate fixing file.
        /// </summary>
        [NonMandatory]
        public string Rate_1_Fixing { get; set; }

        /// <summary>
        /// Yes to use the Known_Rate_1.
        /// </summary>
        public YesNo Use_Known_Rate_1 { get; set; }

        /// <summary>
        /// The reset rate of the first referenced rate, if known.
        /// </summary>
        public Percentage Known_Rate_1 { get; set; }

        /// <summary>
        /// Tenor of second reference rate.
        /// </summary>
        /// <remarks>Not used when Rate_2_End_Date is specified.</remarks>
        public Term Rate_2_Tenor { get; set; }

        /// <summary>
        /// The end date of the second referenced rate. 
        /// </summary>
        [NonMandatory]
        public TDate Rate_2_End_Date { get; set; }

        /// <summary>
        /// Specified year fraction for second reference rate.
        /// </summary>
        public double Rate_2_Year_Fraction { get; set; }

        /// <summary>
        /// Identifier for the rate fixing for the second referenced rate in the Rate fixing file.
        /// </summary>
        [NonMandatory]
        public string Rate_2_Fixing { get; set; }

        /// <summary>
        /// Yes to use the Known_Rate_2.
        /// </summary>
        public YesNo Use_Known_Rate_2 { get; set; }

        /// <summary>
        /// The reset rate of the second referenced rate, if known.
        /// </summary>
        public Percentage Known_Rate_2 { get; set; }

        /// <summary>
        /// Comparer for sorting lists of FICI deals.
        /// Sort by position (Buy or Sell) first and then sort by Payment_Date for each position.
        /// </summary>
        public static int Compare(FloatingInterestCashflowInterpolatedDeal x, FloatingInterestCashflowInterpolatedDeal y)
        {
            if (x.Buy_Sell == BuySell.Sell && y.Buy_Sell == BuySell.Buy)
                return -1;

            if (x.Buy_Sell == BuySell.Buy && y.Buy_Sell == BuySell.Sell)
                return 1;

            return x.Payment_Date.CompareTo(y.Payment_Date);
        }

        /// <inheritdoc/>
        public override double EndDate()
        {
            return Payment_Date;
        }

        /// <inheritdoc/>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N}", Buy_Sell, fCurrency, Principal);
        }

        /// <inheritdoc/>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (Principal < 0.0)
                AddToErrors(errors, ErrorLevel.Error, "Principal cannot be negative.");

            CalcUtils.ValidateDates(errors, Accrual_Start_Date, Accrual_End_Date, false, "accrual start", "accrual end");
            CalcUtils.ValidateDates(errors, Reset_Date, Rate_Start_Date, false, "reset", "rate start");
            CalcUtils.ValidateDates(errors, Reset_Date, Payment_Date, false, "reset", "payment");

            if (Rate_1_End_Date > 0.0)
                CalcUtils.ValidateDates(errors, Rate_Start_Date, Rate_1_End_Date, true, "rate start", "first rate end");

            if (Rate_2_End_Date > 0.0)
                CalcUtils.ValidateDates(errors, Rate_Start_Date, Rate_2_End_Date, true, "rate start", "second rate end");

            if (!Rate_1_Tenor.IsPositiveOrZero())
                AddToErrors(errors, ErrorLevel.Error, "First rate tenor must be positive or zero.");

            if (!Rate_2_Tenor.IsPositiveOrZero())
                AddToErrors(errors, ErrorLevel.Error, "Second rate tenor must be positive or zero.");
        }

        /// <summary>
        /// Returns true if first referenced rate is specified.
        /// </summary>
        public bool HasRate1()
        {
            return !Rate_1_Tenor.IsZero() || Rate_1_End_Date > 0.0;
        }

        /// <summary>
        /// Returns true if second referenced rate is specified.
        /// </summary>
        public bool HasRate2()
        {
            return !Rate_2_Tenor.IsZero() || Rate_2_End_Date > 0.0;
        }

        /// <summary>
        /// Returns accrual calendars.
        /// </summary>
        public IHolidayCalendar GetAccrualHolidayCalendars()
        {
            return GetHolidayCalendar(AccrualCalendarsIndex);
        }

        /// <summary>
        /// Returns index calendars.
        /// </summary>
        public IHolidayCalendar GetRateHolidayCalendars()
        {
            return GetHolidayCalendar(RateCalendarsIndex);
        }
    }

    /// <summary>
    /// Valuation model for InterpolatedFloatingInterestCashflowDeal.
    /// </summary>
    [Serializable]
    [DisplayName("Floating Interest Cashflow (Interpolated) Valuation")]
    public class FloatingInterestCashflowInterpolatedValuation : IRValuation, ISettlementOffset
    {
        protected TDate fPaymentDate = 0.0;

        protected double fAccrualYearFraction = 0.0;

        protected double fRate1EndDate = 0.0;

        protected double fRate2EndDate = 0.0;

        protected double fRate1YearFraction = 0.0;

        protected double fRate2YearFraction = 0.0;

        protected double? fKnownResetRate1 = null; // Realized reset rate, either provided by rate fixings file or user-specified in Known_Rate. Null if either this reset rate is not specified or is not necessary.

        protected double? fKnownResetRate2 = null; // Realized reset rate, either provided by rate fixings file or user-specified in Known_Rate. Null if either this reset rate is not specified or is not necessary.

        protected SettlementOffsetHelper fSettlementOffsetHelper = new SettlementOffsetHelper();

        protected double fCutoffDate = 0.0;

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
        public override Type DealType()
        {
            return typeof(FloatingInterestCashflowInterpolatedDeal);
        }

        /// <inheritdoc />
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);
            Prepare(factors.BaseDate, factors.RateFixings);

            fSettlementOffsetHelper.InitialiseHolidayCalendars(factors.CalendarData);
        }

        /// <inheritdoc/>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            fSettlementOffsetHelper.ValidateHolidayCalendars(factors.CalendarData, errors);
        }

        /// <summary>
        /// Calculate the dates and years fractions not specified on the deal and get any known rates.
        /// </summary>
        public void Prepare(double baseDate, RateFixingsProvider rateFixings)
        {
            // Calculates various date properties of the deal and cache them.
            var deal = (FloatingInterestCashflowInterpolatedDeal)Deal;
            var accrualCalendars = deal.GetAccrualHolidayCalendars();
            var rateCalendars = deal.GetRateHolidayCalendars();

            fPaymentDate = deal.Payment_Date;

            if (deal.Accrual_Year_Fraction > 0.0)
                fAccrualYearFraction = deal.Accrual_Year_Fraction;
            else
                fAccrualYearFraction = CalcUtils.DayCountFraction(deal.Accrual_Start_Date, deal.Accrual_End_Date, deal.Accrual_Day_Count, accrualCalendars);

            fKnownResetRate1 = null;
            fKnownResetRate2 = null;

            if (deal.HasRate1())
            {
                if (deal.Rate_1_End_Date > 0.0)
                    fRate1EndDate = deal.Rate_1_End_Date;
                else
                    fRate1EndDate = DateAdjuster.Add(deal.Rate_Start_Date, deal.Rate_1_Tenor, 1, rateCalendars, true, deal.Rate_Adjustment_Method, deal.Rate_Sticky_Month_End == YesNo.Yes).ToOADate();

                if (deal.Rate_1_Year_Fraction > 0.0)
                    fRate1YearFraction = deal.Rate_1_Year_Fraction;
                else
                    fRate1YearFraction = CalcUtils.DayCountFraction(deal.Rate_Start_Date, fRate1EndDate, deal.Rate_Day_Count, rateCalendars, deal.Rate_1_Tenor);

                fKnownResetRate1 = GetKnownResetRate(baseDate, deal.Reset_Date, fPaymentDate, deal.Use_Known_Rate_1, deal.Known_Rate_1, deal.Rate_1_Fixing, rateFixings, deal);
            }

            if (deal.HasRate2())
            {
                if (deal.Rate_2_End_Date > 0.0)
                    fRate2EndDate = deal.Rate_2_End_Date;
                else
                    fRate2EndDate = DateAdjuster.Add(deal.Rate_Start_Date, deal.Rate_2_Tenor, 1, rateCalendars, true, deal.Rate_Adjustment_Method, deal.Rate_Sticky_Month_End == YesNo.Yes).ToOADate();

                if (deal.Rate_2_Year_Fraction > 0.0)
                    fRate2YearFraction = deal.Rate_2_Year_Fraction;
                else
                    fRate2YearFraction = CalcUtils.DayCountFraction(deal.Rate_Start_Date, fRate2EndDate, deal.Rate_Day_Count, rateCalendars, deal.Rate_2_Tenor);

                fKnownResetRate2 = GetKnownResetRate(baseDate, deal.Reset_Date, fPaymentDate, deal.Use_Known_Rate_2, deal.Known_Rate_2, deal.Rate_2_Fixing, rateFixings, deal);
            }
        }

        /// <inheritdoc />
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.PreCloneInitialize(factors, baseTimes, resultsRequired);

            // Add to valuation time grid
            fT.AddPayDate(fPaymentDate, resultsRequired.CashRequired());

            fCutoffDate = fSettlementOffsetHelper.GetCutoffDate(factors.BaseDate);
        }

        /// <inheritdoc />
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);
            CashAccumulators cashResults = valuationResults.Cash;
            PVProfiles pvResults = valuationResults.Profile;
            Value(pvResults, cashResults, factors.BaseDate, fDiscountRate, fForecastRate, fForecastRate2, fFxRate, fT, factors.NumScenarios);
        }

        /// <summary>
        /// Value the deal from given base date, price factors and time grid.
        /// </summary>
        public void Value(PVProfiles pvResults, CashAccumulators cashResults, double baseDate, IInterestRate discountRate, IInterestRate forecastRate1, IInterestRate forecastRate2, IFxRate fxRate, TimeGrid timeGrid, int numScenarios)
        {
            var tgi = new TimeGridIterator(timeGrid);
            var deal = (FloatingInterestCashflowInterpolatedDeal)Deal;

            bool hasRate1 = deal.HasRate1();
            bool hasRate2 = deal.HasRate2();
            double scale = deal.Buy_Sell == BuySell.Buy ? +deal.Principal : -deal.Principal;
            double tPay = CalcUtils.DaysToYears(fPaymentDate - baseDate);
            double tReset = CalcUtils.DaysToYears(deal.Reset_Date - baseDate);
            double tRateStart = CalcUtils.DaysToYears(deal.Rate_Start_Date - baseDate);
            double tRateEnd1 = hasRate1 ? CalcUtils.DaysToYears(fRate1EndDate - baseDate) : 0.0;
            double tRateEnd2 = hasRate2 ? CalcUtils.DaysToYears(fRate2EndDate - baseDate) : 0.0;
            double tRateEnd12 = tRateEnd2 - tRateEnd1; // Time from rate 1 end date to rate 2 end date.
            double tAccrualEnd = CalcUtils.DaysToYears(deal.Accrual_End_Date - baseDate);
            double interpCoefficient = Math.Abs(tRateEnd12) >= CalcUtils.MinTime ? (tAccrualEnd - tRateEnd1) / tRateEnd12 : 0.0; // Coefficient used to calculate interpolated rate.

            VectorEngine.For(tgi, () =>
            {
                using (var cache = Vector.Cache(numScenarios))
                {
                    Vector pv = cache.Get();

                    if (tgi.Date <= fPaymentDate && fPaymentDate > fCutoffDate)
                    {
                        Vector interpRate = cache.GetClear();
                        Vector rate1 = cache.GetClear();
                        Vector rate2 = cache.GetClear();

                        if (hasRate1)
                        {
                            if (fKnownResetRate1.HasValue)
                                rate1.Assign(fKnownResetRate1.Value);
                            else
                                InterestRateUtils.LiborRate(rate1, forecastRate1, tgi.T, tReset, tRateStart, tRateEnd1, fRate1YearFraction);
                        }

                        if (hasRate2)
                        {
                            if (fKnownResetRate2.HasValue)
                                rate2.Assign(fKnownResetRate2.Value);
                            else
                                InterestRateUtils.LiborRate(rate2, forecastRate2, tgi.T, tReset, tRateStart, tRateEnd2, fRate2YearFraction);
                        }

                        if (hasRate1 && hasRate2)
                        {
                            if (Math.Abs(tRateEnd12) >= CalcUtils.MinTime)
                                interpRate.Assign(rate1 + interpCoefficient * (rate2 - rate1));
                            else
                                interpRate.Assign(0.5 * rate1 + 0.5 * rate2);
                        }
                        else
                        {
                            interpRate.Assign(hasRate1 ? rate1 : rate2);
                        }

                        // Round the calculated rate, regardless whether the valuation date is before or after the reset date.
                        CFFloatingInterestList.RoundRateTo(deal.Interpolated_Rate_Rounding, interpRate);

                        pv.Assign(scale * (interpRate + deal.Margin) * fAccrualYearFraction);

                        CFFixedList.RoundCashflow(pv, Cashflow_Rounding);

                        if (tgi.Date < fPaymentDate)
                            pv.MultiplyBy(discountRate.Get(tgi.T, tPay));
                        else if (tgi.Date == fPaymentDate)
                            cashResults.Accumulate(fxRate, fPaymentDate, pv);
                    }
                    else
                    {
                        pv.Clear();
                    }

                    pvResults.AppendVector(tgi.Date, pv * fxRate.Get(tgi.T));
                }
            });

            // After maturity
            pvResults.Complete(timeGrid);
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }

        /// <summary>
        /// Returns the known reset rate if necessary, either via user-specified known rate or rate fixings file. Returns null if either reset rate is not necessary or not found.
        /// </summary>
        private static double? GetKnownResetRate(TDate baseDate, TDate resetDate, TDate paymentDate, YesNo useKnownRate, double knownRate, string rateFixing, RateFixingsProvider rateFixingsProvider, Deal deal)
        {
            double? knownResetRate = null;
            if (paymentDate >= baseDate && resetDate <= baseDate)
            {
                if (useKnownRate == YesNo.Yes)
                {
                    knownResetRate = knownRate;
                }
                else if (!string.IsNullOrWhiteSpace(rateFixing) && rateFixingsProvider != null)
                {
                    // Try get realized reset rate from Rate fixing file.
                    DateTime[] missingDates = { DateTime.FromOADate(resetDate) };
                    var fixings = CashflowsFixingsHelper.GetRateFixings(rateFixing, missingDates, rateFixingsProvider, baseDate, deal);

                    if (fixings != null && fixings.Length != 0 && !double.IsNaN(fixings[0]))
                        knownResetRate = fixings[0];
                }
            }

            return knownResetRate;
        }
    }
}