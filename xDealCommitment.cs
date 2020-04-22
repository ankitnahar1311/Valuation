/// <author>
/// Phil Koop, Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for commitment deals.
/// </summary>
using System;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Commitment deal class.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commitment")]
    public class CommitmentDeal : IRDeal
    {
        public CommitmentDeal()
            : base()
        {
            Amortisation = new Amortisation();
        }

        public double Amount
        {
            get; set;
        }

        public TDate Effective_Date
        {
            get; set;
        }

        public TDate Maturity_Date
        {
            get; set;
        }

        [NonMandatory]
        public Amortisation Amortisation
        {
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Maturity_Date;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            CalcUtils.ValidateDates(errors, Effective_Date, Maturity_Date, false);

            Amortisation.Validate(errors);
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return string.Format("{0} {1:N}", fCurrency, Amount);
        }
    }

    /// <summary>
    /// Valuation class for commitment deals.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Commitment Valuation")]
    public class CommitmentValuation : IRValuation
    {
        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CommitmentDeal);
        }

        /// <summary>
        /// Prepare for valuation anything that will be shared between scenarios.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            CommitmentDeal deal = (CommitmentDeal)Deal;

            // Add to valuation time grid
            fT.Add(deal.Effective_Date - 1.0);
            fT.Add(deal.Effective_Date);

            for (int i = 0; i < deal.Amortisation.Count; ++i)
            {
                fT.AddPayDate(deal.Amortisation[i].Date);
            }
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            TimeGridIterator tgi = new TimeGridIterator(fT);
            PVProfiles result = valuationResults.Profile;
            CommitmentDeal deal = (CommitmentDeal)Deal;

            VectorEngine.For(tgi, () =>
            {
                if (tgi.Date < deal.Effective_Date)
                {
                    result.AppendZeroVector(tgi.Date);
                }
                else
                {
                    double amount = deal.Amortisation.GetPrincipal(deal.Amount, tgi.Date - 1.0);
                    result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * amount);
                }
            });

            result.Complete(fT);
        }

        /// <inheritdoc />
        protected override bool DoIsTracingVectorCompatible()
        {
            return true;
        }
    }
}