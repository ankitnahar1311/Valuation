using System;
using System.ComponentModel;
using System.Drawing.Design;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// IDI Option Deal.
    /// </summary>
    /// <remarks> Indice de Depositos Interfinanceiros European option. </remarks>
    [Serializable]
    [DisplayName("IDI Option")]
    public class IDIOptionDeal : BaseStandardOption
    {
        private const int CalendarIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDIOptionDeal"/> class.
        /// </summary>
        public IDIOptionDeal()
        {
            IDI_Name = string.Empty;
        }
   
        /// <summary>
        /// Gets or sets the name of the IDI index, for example IDI2003.
        /// </summary>
        public string IDI_Name
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the holiday calendars.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Index_Calendars
        {
            get { return GetCalendarNames(CalendarIndex); }
            set { SetCalendarNames(CalendarIndex, value); }
        }

        /// <summary>
        /// Gets the deal helper.
        /// </summary>
        public override BaseDealHelper GetDealHelper()
        {
            return new SingleInterestRateCompoundIndexDealHelper(IDI_Name, Currency);
        }

        /// <summary>
        /// Get the calendar used to determine the period until xxx.
        /// </summary>
        public IHolidayCalendar Calendar()
        {
            return GetHolidayCalendar(CalendarIndex);
        }

        /// <summary>
        /// Validates the specified errors.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (String.IsNullOrEmpty(Index_Calendars))
                AddToErrors(errors, "At least one calendar must be specified");
        }
    }

    /// <summary>
    /// IDI Option Valuation Class.
    /// </summary>
    [Serializable]
    [DisplayName("IDI Option Valuation")]
    public class IDIOptionValuation : BaseBarrierOptionValuation
    {
        [NonSerialized]
        protected EuropeanOption fEuroPricer = null;
        [NonSerialized]
        protected double fStrike;
        [NonSerialized]
        protected double fBaseDate;

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(IDIOptionDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            var deal = (IDIOptionDeal)Deal;
            fStrike = deal.StrikeForValuation();
            fBaseDate = factors.BaseDate;
        }

        /// <summary>
        /// Full pricing is required because of the strong path dependency.
        /// </summary>
        public override bool FullPricing()
        {
            return false;
        }

        /// <summary>
        /// Returns the monitoring period, which is the period between each aging date. 
        /// </summary>
        protected override double GetMonitoringPeriod()
        {
            return 0.0;
        }

        /// <summary>
        /// Create European option pricer prior to valuation.
        /// </summary>
        protected override void CreateOption(double baseDate, VectorScopedCache.Scope cache)
        {
            var deal = (IDIOptionDeal)Deal;
            base.CreateOption(baseDate, cache);

            fEuroPricer = new EuropeanOption(deal.Option_Type, fStrike, deal.Expiry_Date, deal.Expiry_Date, deal.Expiry_Date, baseDate, cache, DayCount.BUS_252, deal.Calendar());
        }

        /// <summary>
        /// Age the option by updating its status to reflect the market state on a given day.
        /// </summary>
        protected override void Age(CashAccumulators cashAccumulators, double time, double date)
        {
        }

        /// <summary>
        /// Spot Valuation function.
        /// </summary>
        protected override void ValueFn(Vector pv, Vector cash, double t, double date, double tResidual, Vector spot, Vector r, Vector b, Vector v)
        {
            fEuroPricer.Value(pv, cash, date, spot, r, b, v);
        }

        /// <summary>
        /// Gets the discount rate used in the option valuation formula.
        /// </summary>
        protected override void GetDiscountRate(Vector r, IInterestRate discountRate, double t, double tPay)
        {
            Double date = fBaseDate + CalcUtils.YearsToDays(t);
            Double payDate = fBaseDate + CalcUtils.YearsToDays(tPay);

            r.Assign(VectorMath.Log(discountRate.Get(t, tPay)) / CalcUtils.DayCountFraction(payDate, date, DayCount.BUS_252, ((IDIOptionDeal)Deal).Calendar()));
        }

        /// <summary>
        /// Get carry rate from forward factor.
        /// </summary>
        protected override void GetCarryRate(Vector b, Vector forwardFactor, double t, double tPay)
        {
            Double date = fBaseDate + CalcUtils.YearsToDays(t);
            Double payDate = fBaseDate + CalcUtils.YearsToDays(tPay);

            b.Assign(VectorMath.Log(forwardFactor) / CalcUtils.DayCountFraction(date, payDate, DayCount.BUS_252, ((IDIOptionDeal)Deal).Calendar()));
        }
    }
}
