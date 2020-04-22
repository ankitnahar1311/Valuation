/// <author>
/// Andy Hudson.
/// </author>
/// <owner>
/// Andy Hudson.
/// </owner>
/// <summary>
/// Bottom-up valuation model for CDO tranches.
/// </summary>
using System;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Parameters for bottom-up CDO deal valuation model.
    /// </summary>
    public class CDOBottomUpValuationParameters : CDOValuationParameters
    {
        public PriceFactorList Factors; // temporary expedient

        /// <summary>
        /// Constructor.
        /// </summary>
        public CDOBottomUpValuationParameters(DealCDO deal, CDOValuationBottomUp valuation, PriceFactorList factors)
            : base(deal, factors)
        {
            Factors = factors;

            IndexCDO = factors.Get<IndexCDO>(deal.Reference_Index);
            IndexCDO.SetPricer(new CDOPricer(valuation.Number_Of_Loss_Buckets, valuation.Number_Integration_Steps, valuation.Bucket_Type, !CalcUtils.IsTiny(deal.Spread), deal.Payoff_Is_Digital == YesNo.Yes, deal.Digital_Payoff_Percentage));
        }

        public IndexCDO IndexCDO
        {
            get; set;
        }

        /// <summary>
        /// Realised loss and recovery vectors as of time t. Expressed as fraction of total index.
        /// </summary>
        public override void RealizedLoss(Vector realizedLoss, Vector realizedRecovery, double t, bool payoffIsDigital, double digitalPayoff)
        {
            if (payoffIsDigital)
                IndexCDO.RealizedLossAndRecovery(realizedLoss, realizedRecovery, t, digitalPayoff);
            else
                IndexCDO.RealizedLossAndRecovery(realizedLoss, realizedRecovery, t);
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
            IndexCDO.GetValue(expectedLoss, expectedRecovery, t, T, adjustedDetachment, realizedLoss, realizedRecovery);
        }
    }

    /// <summary>
    /// Bottom-up valuation model for synthetic single-tranche CDO. 
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("CDO Tranche bottom-up valuation")]
    public class CDOValuationBottomUp : CDOValuation
    {
        protected int fNumLossBuckets = 20;
        protected int fNumIntegrationSteps = 20;
        protected CDOPricer.BucketType fBucketType = CDOPricer.BucketType.DetachmentConsistent;

        /// <summary>
        /// Number of loss buckets in discretisation
        /// </summary>
        public int Number_Of_Loss_Buckets
        {
            get { return fNumLossBuckets; } 
            set { fNumLossBuckets = value; }
        }

        /// <summary>
        /// Number of points in correlation sample
        /// </summary>
        public int Number_Integration_Steps
        {
            get { return fNumIntegrationSteps; }
            set { fNumIntegrationSteps = value; }
        }

        /// <summary>
        /// Bucket the current detachment or the remaining pool
        /// </summary>
        public CDOPricer.BucketType Bucket_Type
        {
            get { return fBucketType; }
            set { fBucketType = value; }            
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
            return fBucketType == CDOPricer.BucketType.DetachmentConsistent;
        }

        /// <summary>
        /// Register pricefactors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            factors.Register<DiscountRate>(InterestRateUtils.GetRateId(fDeal.Discount_Rate, fDeal.Currency));
            factors.RegisterInterface<IFxRate>(fDeal.Currency);
            factors.Register<IndexCDO>(fDeal.Reference_Index);
        }

        /// <summary>
        /// Create appropriate CDO valuation parameters.
        /// </summary>
        public override CDOValuationParameters GetValuationParameters(PriceFactorList factors)
        {
            return new CDOBottomUpValuationParameters(fDeal, this, factors);
        }

        /// <summary>
        /// Output Intra-Valuation Diagnostics (Base correlation at Attachment and Detachment)
        /// </summary>
        public override void AddIntraValuationDiagnostics(IIntraValuationDiagnosticsWriter intraValuationDiagnosticsWriter,
            CDOValuationParameters parameters, Vector adjustedAttachment, Vector adjustedDetachment, Vector remainingPool, double valueTime, double tPay)
        {
            if (intraValuationDiagnosticsWriter.Level == IntraValuationDiagnosticsLevel.None)
                return;

            using (var cache = Vector.CacheLike(adjustedAttachment))
            {
                var paramsCDO = (CDOBottomUpValuationParameters)parameters;
                Vector rhoAttachment = cache.Get();
                Vector rhoDetachment = cache.Get();

                paramsCDO.IndexCDO.GetBaseCorrelation(rhoAttachment, adjustedAttachment, remainingPool, valueTime, tPay);
                paramsCDO.IndexCDO.GetBaseCorrelation(rhoDetachment, adjustedDetachment, remainingPool, valueTime, tPay);

                IntraValuationDiagnosticsHelper.AddBaseCorrelation(intraValuationDiagnosticsWriter, rhoAttachment, rhoDetachment);
            }
        }
    }
}