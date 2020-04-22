using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Basel III netting collateral set deal class.
    /// </summary>
    [Serializable]
    [DisplayName("Basel Netting Collateral Set")]
    public class DealBaselNettingCollateralSet : ContainerDealBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DealBaselNettingCollateralSet"/> class.
        /// </summary>
        public DealBaselNettingCollateralSet()
        {
            Collateralised = YesNo.No;
        }

        /// <summary>
        /// Indicates whether the netting set is collaterised.
        /// </summary>
        public YesNo Collateralised { get; set; }

        /// <summary>
        /// Holding period for collateral agreement.
        /// </summary>
        public int Holding_Period { get; set; }

        /// <summary>
        /// Collateral threshold.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Collateral balance.
        /// </summary>
        public double Balance { get; set; }
    }

    /// <summary>
    /// Basel III netting collateral set valuation class.
    /// </summary>
    [Serializable]
    [DisplayName("Basel Netting Collateral Set Valuation")]
    public class BaselNettingCollateralSetValuation : ContainerValuationBase
    {
        protected DealBaselNettingCollateralSet fDeal;

        private double fNetGrossRatioCorrelation = 0.6;

        /// <summary>
        /// Gets or sets the type of deal that this model can value.
        /// </summary>
        public override Deal Deal
        {
            get { return fDeal; }
            set { fDeal = (DealBaselNettingCollateralSet)value; }
        }

        /// <summary>
        /// Correlation factor determining the amount of reduction available due to netting.
        /// </summary>
        public double Net_To_Gross_Ratio_Fraction
        {
            get
            {
                return fNetGrossRatioCorrelation;
            }

            set
            {
                if (value >= 0.0 && value <= 1.0)
                    fNetGrossRatioCorrelation = value;
                else
                    throw new ArgumentOutOfRangeException("Net_To_Gross_Ratio_Fraction", String.Format(CultureInfo.InvariantCulture, "Attempt to set to {0}. Must be in the range [0,1]", value));
            }
        }

        /// <summary>
        /// Does this container require incremental storage to work with IMC?
        /// </summary>
        public override bool RequiresIncrementalStorage
        {
            get { return true; }
        }

        /// <summary>
        /// Return the type of the deal supported by this model.
        /// </summary>
        public override Type DealType()
        {
            return typeof(DealBaselNettingCollateralSet);
        }

        /// <summary>
        /// Add required results.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.PreCloneInitialize(factors, baseTimes, resultsRequired);

            if (resultsRequired.Result<AddOnProfiles>() == null)
            {
                var addOnProfiles = new AddOnProfiles(resultsRequired.Result<PVProfiles>().VectorSize);
                resultsRequired.Add(addOnProfiles);
            }

            if (resultsRequired.Result<PositiveMtmProfiles>() == null)
            {
                var mtmPositiveProfiles = new PositiveMtmProfiles(resultsRequired.Result<PVProfiles>().VectorSize);
                resultsRequired.Add(mtmPositiveProfiles);
            }
        }

        /// <summary>
        /// Aggregate the valuation profile onto a set of result curves to support result partitioning.
        /// </summary>
        protected override void ProcessResults(ValuationResults valResults, DealPartitionAssociations assoc, PriceFactorList factors, BaseTimeGrid baseTimes, ValuationOptions options, int partition)
        {
            var pvProfiles = valResults.Results<PVProfiles>();
            var addOnProfiles = valResults.Results<AddOnProfiles>();
            var positiveMtmProfiles = valResults.Results<PositiveMtmProfiles>();

            Debug.Assert(addOnProfiles != null, "No Add-On profiles. Cannot proceed with valuation.");
            Debug.Assert(positiveMtmProfiles != null, "No Positive mtM profiles. Cannot proceed with valuation.");

            fT = Deal.ValuationGrid(factors, baseTimes, Deal.EndDate());
            var tgi = new TimeGridIterator(fT);

            var nettedExposure = new PVProfiles(factors.NumScenarios);
            var collateralExposure = new PVProfiles(factors.NumScenarios);

            var addOnsProfile = new PVProfiles(factors.NumScenarios);
            var mtmTermProfile = new PVProfiles(factors.NumScenarios);

            DealBaselNettingCollateralSet nettingSetDeal = Deal as DealBaselNettingCollateralSet;

            bool collateralised = nettingSetDeal.Collateralised == YesNo.Yes;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector sumMtm = cache.Get();
                Vector sumPositiveMtm = cache.Get();
                Vector addOns = cache.Get();
                Vector netGrossRatio = cache.Get();
                Vector value = cache.Get();
                Vector term1 = cache.Get();
                Vector term2 = cache.Get();

                // Collateral related vectors.
                Vector mtmTermStart = cache.Get();
                Vector addOnHp = cache.Get();

                // Loop to get the netting set exposure.
                while (tgi.Next())
                {
                    sumMtm.Clear();
                    sumPositiveMtm.Clear();
                    addOns.Clear();
                    value.Clear();

                    double date = tgi.Date;

                    // For MtM Plus Add-On deals PV profiles represents the sum of the MtM profile and Add-On profile.
                    // Subtract the Add-On profile to recover the MtM profile before flooring.
                    sumMtm.Assign(VectorMath.Max(pvProfiles[date] - addOnProfiles[date], 0.0));

                    addOns.Assign(addOnProfiles[date]);
                    sumPositiveMtm.Assign(positiveMtmProfiles[date]);

                    netGrossRatio.AssignConditional(sumPositiveMtm > 0, sumMtm / sumPositiveMtm, 0.0);

                    netGrossRatio.MultiplyBy(this.fNetGrossRatioCorrelation);
                    netGrossRatio.Add(1 - this.fNetGrossRatioCorrelation);

                    term2.AssignProduct(addOns, netGrossRatio);

                    term1.Assign(VectorMath.Max(sumMtm, 0.0));

                    value.AssignSum(term1, term2);

                    nettedExposure.AppendVector(date, value);

                    if (collateralised)
                    {
                        mtmTermProfile.AppendVector(date, term1);
                        addOnsProfile.AppendVector(date, term2);
                    }
                }

                nettedExposure.Complete(this.fT);

                var exposureResults = valResults.Results<Exposure>();
                if (exposureResults != null)
                    exposureResults.Assign(nettedExposure);

                // Collateral cases.
                if (collateralised)
                {
                    mtmTermProfile.Complete(this.fT);
                    addOnsProfile.Complete(this.fT);

                    double date = factors.BaseDate;

                    mtmTermProfile.GetValue(mtmTermStart, date);
                    addOnsProfile.GetValue(addOnHp, date + nettingSetDeal.Holding_Period);

                    // Assume we have post haircut collateral.
                    double collateral = nettingSetDeal.Balance;
                    double threshold = nettingSetDeal.Threshold;

                    tgi.Reset();

                    // Loop to get the netting set exposure.
                    while (tgi.Next())
                    {
                        bool inHoldingPeriod = tgi.T < CalcUtils.DaysToYears(nettingSetDeal.Holding_Period);

                        CollateralBasel3(mtmTermStart, collateral, addOnHp, threshold, inHoldingPeriod, tgi.Date,
                                         nettedExposure,
                                         collateralExposure);
                    }

                    collateralExposure.Complete(this.fT);

                    if (exposureResults != null)
                        exposureResults.Assign(collateralExposure);
                }
            }

            if (options.PartitionCollateralMode != PartitionCollateralMode.Suppress_Collateral_And_Flooring || partition < options.NumTotalPartitions)
                valResults.FloorResult(assoc.AggregationMode, options);

            CollateralPlugIn.CollateralBalancesContainer coProfiles = valResults.Results<CollateralPlugIn.CollateralBalancesContainer>();

            // Store collateral information according to diagnostic collection rules.
            if (coProfiles != null)
                coProfiles.StoreInformation(this);  
        }

        /// <summary>
        /// Calculates the exposure profile using the Basel III methodology.
        /// </summary>
        private static void CollateralBasel3(Vector mtmPlus, double collateral, Vector addOnHp, double threshold,
                                             bool inHoldingPeriod, double date,
                                             PVProfiles nettedExposure, PVProfiles collateralExposure)
        {
            using (var cache = Vector.Cache(mtmPlus.Count))
            {
                Vector payoff = cache.GetClear();

                Vector mtmPlusLessThanThreshold = cache.GetClear();
                Vector mtmPlusMinusCollGreaterThanThreshold = cache.GetClear();

                Vector caseCLogic = cache.GetClear();
                Vector caseDAndELogic = cache.GetClear();
                Vector caseFLogic = cache.GetClear();
                Vector sumOfCases = cache.Get();

                if (inHoldingPeriod)
                {
                    payoff.Assign(VectorMath.Max(0.0, nettedExposure[date] - collateral));
                }
                else
                {
                    mtmPlusLessThanThreshold.AssignConditional(mtmPlus < threshold, 1.0, 0.0);
                    mtmPlusMinusCollGreaterThanThreshold.AssignConditional(mtmPlus >= threshold + collateral, 1.0, 0.0);

                    caseCLogic.Assign(mtmPlusLessThanThreshold);
                    caseDAndELogic.AssignConditional(mtmPlusMinusCollGreaterThanThreshold * (1.0 - mtmPlusLessThanThreshold), 1.0, 0.0);
                    caseFLogic.AssignConditional((1.0 - mtmPlusLessThanThreshold) * (1.0 - mtmPlusLessThanThreshold), 1.0, 0.0);

                    sumOfCases.Assign(caseCLogic + caseDAndELogic + caseFLogic);

                    double values;

                    if (sumOfCases.AllElementsTheSame(out values) && values == 1.0)
                    {
                        payoff.AssignConditional(caseCLogic, nettedExposure[date], payoff);
                        payoff.AssignConditional(caseDAndELogic, VectorMath.Min(threshold + addOnHp, nettedExposure[date] - collateral), payoff);
                        payoff.AssignConditional(caseFLogic, VectorMath.Max(0.0, VectorMath.Min(threshold + addOnHp, nettedExposure[date])), payoff);
                    }
                    else
                    {
                        payoff.Assign(-1.0);
                    }
                }

                collateralExposure.AppendVector(date, payoff);
            }
        }
    }
}
