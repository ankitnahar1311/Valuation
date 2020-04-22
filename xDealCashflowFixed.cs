using System;
using System.ComponentModel;
using System.Drawing.Design;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Base class for fixed cashflow deals.
    /// </summary>
    [Serializable]
    public abstract class FixedCashflowBaseDeal : IRDeal
    {
        /// <summary>
        /// Gets or sets the list of holiday calendar names.
        /// </summary>
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); }
            set { SetCalendarNames(0, value); }
        }

        /// <summary>
        /// Gets or sets the cashflow payment date.
        /// </summary>
        public TDate Payment_Date
        {
            get;
            set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Payment_Date;
        }
    }

    /// <summary>
    /// Fixed cashlfow amount deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Cashflow Fixed")]
    public class FixedCashflowDeal : FixedCashflowBaseDeal
    {
        /// <summary>
        /// Gets or sets the cashflow amount.
        /// </summary>
        public double Amount
        {
            get;
            set;
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1:N} {2}", fCurrency, Amount, Payment_Date);
        }

        /// <summary>
        /// Try to get the notional of the deal at the given date.
        /// </summary>
        protected override bool DoTryGetNotional(PriceFactorList priceFactors, out double notional)
        {
            notional = Amount * priceFactors.GetInterface<IFxRate>(Currency).BaseCurrencySpotPrice();
            return true;
        }
    }

    /// <summary>
    /// Base valuation class for fixed cashflow deals.
    /// </summary>
    [Serializable]
    public abstract class CashflowFixedBaseValuation : IRValuation, ISingleDateValuation
    {
        [NonSerialized]
        protected double fAmount = 0.0;

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(FixedCashflowBaseDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            FixedCashflowBaseDeal deal = (FixedCashflowBaseDeal)fDeal;

            // Add to valuation time grid
            fT.AddPayDate(deal.Payment_Date);
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

            FixedCashflowBaseDeal deal = (FixedCashflowBaseDeal)fDeal;
            double payDate = deal.Payment_Date;
            double tPay = CalcUtils.DaysToYears(payDate - factors.BaseDate);

            VectorEngine.For(tgi, () =>
            {
                if (tgi.Date == payDate)
                {
                    result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * fAmount);
                }
                else
                {
                    result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * fDiscountRate.Get(tgi.T, tPay) * fAmount);
                }
            });

            if (!cashAccumulators.Ignore && factors.BaseDate <= payDate && payDate <= fT.fHorizon)
                cashAccumulators.Accumulate(fFxRate, payDate, fAmount);

            result.Complete(fT);
        }

        /// <summary>
        /// Single date valuation function.
        /// </summary>
        public void Value(Vector pv, Vector cash, double baseDate, double valueDate, Vector settlementDate, IInterestRate discount,
            IInterestRate forecast, IInterestRate repo, IInterestRateVol interestRateVol, IInterestYieldVol interestYieldVol,
            ISurvivalProb survivalProb, ISACCRResult saccrResult, IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            FixedCashflowBaseDeal deal = (FixedCashflowBaseDeal)fDeal;
            double payDate = deal.Payment_Date;

            if (payDate < valueDate)
                return;

            using (var cache = Vector.CacheLike(pv))
            {
                Vector amount = cache.Get(fAmount);
                if (settlementDate != null)
                    amount.MultiplyBy(settlementDate < payDate);

                if (payDate == valueDate)
                {
                    pv.Assign(amount);
                    if (cash != null)
                        cash.Assign(amount);
                }
                else
                {
                    double t = CalcUtils.DaysToYears(valueDate - baseDate);
                    double tPay = CalcUtils.DaysToYears(payDate - baseDate);
                    if (survivalProb != null)
                        pv.Assign(amount * discount.Get(t, tPay) * survivalProb.Get(t, tPay));
                    else
                        pv.Assign(amount * discount.Get(t, tPay));
                }
            }
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }
    }

    /// <summary>
    /// Valuation class for fixed cashflow deals.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Cashflow Fixed Valuation")]
    public class CashflowFixedValuation : CashflowFixedBaseValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(FixedCashflowDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            FixedCashflowDeal deal = (FixedCashflowDeal)fDeal;
            fAmount = deal.Amount;
        }
    }
}