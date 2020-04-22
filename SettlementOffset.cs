using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Interface responsible for dealing with settlement offset issues.
    /// </summary>
    public interface ISettlementOffset
    {
        /// <summary>
        /// Yes for respecting settlement offset; No otherwise.
        /// </summary>
        /// <remarks>
        /// If No, Settlement_Offset and Settlement_Offset_Calendars will be ignored.
        /// </remarks>
        YesNo Use_Settlement_Offset { get; set; }

        /// <summary>
        /// Number of business days of settlement offset.
        /// </summary>
        /// <remarks>
        /// This property is only in effect if Use_Settlement_Offset is Yes.
        /// If Settlement_Offset is 2, cashflows to be paid prior to and including 2 business days after the calculation base date will be ignored.
        /// A special case is that if Settlement_Offset is zero, even if calculation base date is a business day, cashflow as of calculation base date will be ignored.
        /// </remarks>
        int Settlement_Offset { get; set; }

        /// <summary> 
        /// Gets or sets the calendars associated with the settlement offset. 
        /// </summary>
        [NonMandatory]
        [LocationStringsForm]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        string Settlement_Offset_Calendars { get; set; }
    }

    /// <summary>
    /// Utility methods responsible for dealing with settlement offset issues.
    /// </summary>
    [Serializable]
    public class SettlementOffsetHelper : ISettlementOffset
    {
        private const int SettlementOffsetCalendarIndex = 0;

        private int fSettlementOffset = 0;

        private ValuationCalendarsSupport fCalendarsSupport = new ValuationCalendarsSupport();

        /// <summary> 
        /// Create an instance of the settlement offset helper object with default values.
        /// </summary>
        public SettlementOffsetHelper()
        {
            Use_Settlement_Offset = YesNo.No;
            fCalendarsSupport.SetCalendarNames(SettlementOffsetCalendarIndex, HolidayCalendar.WeekendType.SatAndSun.ToString());
        }

        /// <inheritdoc/>
        public YesNo Use_Settlement_Offset { get; set; }

        /// <inheritdoc/>
        public int Settlement_Offset
        {
            get
            {
                return fSettlementOffset;
            }
            set
            {
                if (value >= 0)
                    fSettlementOffset = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(Settlement_Offset), "Settlement_Offset cannot be negative.");
            }
        }

        /// <inheritdoc/>
        public string Settlement_Offset_Calendars
        {
            get { return fCalendarsSupport.GetCalendarNames(SettlementOffsetCalendarIndex); }
            set { fCalendarsSupport.SetCalendarNames(SettlementOffsetCalendarIndex, value); }
        }

        /// <summary> 
        /// Initialise the holiday calendars for the ValuationCalendarSupport object.
        /// </summary>
        /// <remarks> 
        /// Method to be called by the valuation in HeadNoteInitialize
        /// </remarks>
        public void InitialiseHolidayCalendars(ICalendarData calendarData)
        {
            fCalendarsSupport.InitialiseHolidayCalendars(calendarData);
        }

        /// <summary> 
        /// Validate the holiday calendars for the ValuationCalendarSupport object.
        /// </summary>
        /// <remarks> 
        /// Method to be called by the valuation in RegisterFactors
        /// </remarks>
        public void ValidateHolidayCalendars(ICalendarData calendarData, ErrorList errors)
        {
            fCalendarsSupport.ValidateHolidayCalendars(calendarData, errors);
        }

        /// <summary>
        /// Returns the cutoff date if supplied. If a cutoff date is not supplied, then return 0.0.
        /// </summary>
        /// <remarks>
        /// If a cutoff date is supplied, the deal is valued as if cashflows on or prior to this cutoff date are simply ignored.
        /// </remarks>
        public double GetCutoffDate(double baseDate)
        {
            if (baseDate <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseDate), "Base date must be positive.");

            if (Use_Settlement_Offset == YesNo.Yes)
                return DateAdjuster.Add(baseDate, Settlement_Offset, fCalendarsSupport.GetHolidayCalendar(SettlementOffsetCalendarIndex));

            return 0.0;
        }
    }
}