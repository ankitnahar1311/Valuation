using System;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Fixed interest cashflow deal class.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Cashflow Fixed Interest")]
    public class FixedInterestCashflowDeal : FixedCashflowBaseDeal
    {
        /// <summary>
        /// Gets or sets the cashflow position (Buy or Sell).
        /// </summary>
        public BuySell Buy_Sell
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cashflow notional principal amount.
        /// </summary>
        public double Notional
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the accrual start date.
        /// </summary>
        public TDate Accrual_Start_Date
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the accrual end date.
        /// </summary>
        public TDate Accrual_End_Date
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the accrual day count convention.
        /// </summary>
        public DayCount Accrual_Day_Count
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the fixed interest rate.
        /// </summary>
        public Percentage Fixed_Rate
        {
            get;
            set;
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1} {2:N} {3} {4}", Buy_Sell, fCurrency, Notional, Payment_Date, Fixed_Rate);
        }

        /// <summary>
        /// Try to get the notional of the deal at the given date.
        /// </summary>
        protected override bool DoTryGetNotional(PriceFactorList priceFactors, out double notional)
        {
            notional = Notional * priceFactors.GetInterface<IFxRate>(Currency).BaseCurrencySpotPrice();
            return true;
        }
    }

    /// <summary>
    /// Valuation class for fixed interest cashflow deals.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Cashflow Fixed Interest Valuation")]
    public class CashflowFixedInterestValuation : CashflowFixedBaseValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(FixedInterestCashflowDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            FixedInterestCashflowDeal deal = (FixedInterestCashflowDeal)fDeal;

            int sign = (deal.Buy_Sell == BuySell.Buy ? +1 : -1);
            double accrualDayCountFraction = CalcUtils.DayCountFraction(deal.Accrual_Start_Date, deal.Accrual_End_Date, deal.Accrual_Day_Count, deal.GetHolidayCalendar());
            fAmount = sign * deal.Notional * accrualDayCountFraction * deal.Fixed_Rate;
        }
    }
}