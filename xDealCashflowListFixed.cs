using System;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Fixed cashflow list deal.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Fixed Cashflow List")]
    public class CFFixedListDeal : CFListBaseDeal<CFFixedList>
    {
    }

    /// <summary>
    /// Valuation of fixed cashflow list.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Fixed Cashflow List Valuation")]
    public class CFFixedListValuation : CFListBaseValuation<CFFixedList>, ISingleDateValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CFFixedListDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CFFixedListDeal deal = (CFFixedListDeal)Deal;

            // Add to valuation time grid
            fT.AddPayDates<CFFixed>(deal.Cashflows);
        }

        /// <summary>
        /// Value the deal.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        public override void Value(Vector pv, Vector cash, double baseDate, double valueDate, ISACCRResult saccrResult,
            IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            Value(pv, cash, baseDate, valueDate, null, fDiscountRate, null, fRepoRate, null, null, fSurvivalProb, saccrResult,
                intraValuationDiagnosticsWriter);
        }

        /// <summary>
        /// Value the deal using the cashflow list.
        /// </summary>
        /// <param name="pv">Present value to be updated.</param>
        /// <param name="cash">Realised cash to be updated.</param>
        public void Value(Vector pv, Vector cash, double baseDate, double valueDate, Vector settlementDate, IInterestRate discount,
            IInterestRate forecast, IInterestRate repo, IInterestRateVol interestRateVol, IInterestYieldVol interestYieldVol,
            ISurvivalProb survivalProb, ISACCRResult saccrResult, IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter)
        {
            CFFixedListDeal deal = (CFFixedListDeal)Deal;

            pv.Clear();
            if (cash != null)
                cash.Clear();

            deal.Cashflows.Value(pv, cash, baseDate, valueDate, settlementDate, discount, survivalProb, intraValuationDiagnosticsWriter, fCutoffDate);
            ApplySign(pv, cash, fBuySellSign);
        }

        /// <summary>
        /// Collect cashflows realised along the scenario path up to endDate.
        /// </summary>
        public override void CollectCashflows(CashAccumulators cashAccumulators, double baseDate, double endDate)
        {
            CFFixedListDeal deal = (CFFixedListDeal)Deal;
            deal.Cashflows.CollectCashflows(cashAccumulators, fFxRate, baseDate, endDate, fBuySellSign, fCutoffDate);
        }
    }
}