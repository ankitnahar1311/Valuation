// -----------------------------------------------------------------------------
// Name         xDealCreditBase
// Author:      Phil Koop, SunGard
// Project:     CORE Analytics
// Description: Base class definition for credit deals, using equity model to
// determine default probabilities.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Parameters for credit and credit index deal valuation models.
    /// </summary>
    public struct CreditValuationParameters 
    {
        public IInterestRate    DF;
        public IFxRate          X;
        public ISurvivalProb    SP;
        public RecoveryRate[]   RR;
        public CreditRating[]   CR;
        public Vector[]         DefaultTime;
        public double[]         Weights;

        public readonly bool[] NamesDefaultedBeforeBaseDate;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CreditValuationParameters(DealCreditBase deal, CreditBaseValuation valuation, PriceFactorList factors, VectorScopedCache.Scope cache)
        {
            DF = DiscountRate.Get(factors, InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency));
            X = factors.GetInterface<IFxRate>(deal.Currency);
            SP = factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Name : deal.Survival_Probability);
            RR = null;
            CR = null;
            DefaultTime = null;
            Weights = null;
            NamesDefaultedBeforeBaseDate = null;

            if (valuation.Respect_Default == YesNo.Yes)
            {
                List<string> names = new List<string>();
                if (deal.ProtectionReferenceType() == DealCreditBase.ReferenceType.Single_Name)
                {
                    names.Add(deal.Name);
                    Weights = new double[] { 1.0 };
                }
                else
                {
                    IndexCDSPool indexCds = factors.Get<IndexCDSPool>(deal.Name);
                    Weights = new double[indexCds.Names.Count];
                    for (int i = 0; i < indexCds.Names.Count; ++i)
                    {
                        names.Add(indexCds.Names[i].Name);
                        Weights[i] = indexCds.Names[i].Weight;
                    }
                }

                if (valuation.RequiresRecoveryOnDefault())
                {
                    RR = new RecoveryRate[names.Count];
                    for (int i = 0; i < names.Count; ++i)
                        RR[i] = factors.Get<RecoveryRate>(string.IsNullOrEmpty(deal.Recovery_Rate) ? names[i] : deal.Recovery_Rate);
                }

                CR = new CreditRating[names.Count];
                NamesDefaultedBeforeBaseDate = new bool[names.Count];
                DefaultTime = new Vector[names.Count];
                for (int i = 0; i < names.Count; ++i)
                {
                    DefaultTime[i] = cache.Get();
                    CR[i] = factors.Get<CreditRating>(names[i]);
                    NamesDefaultedBeforeBaseDate[i] = CreditRating.DefaultedBeforeBaseDate(CR[i], factors.BaseDate);
                    CR[i].DefaultTime(DefaultTime[i]);
                }
            }
        }

        /// <summary>
        /// Return true iff default can be supported by these parameters.
        /// </summary>
        public bool RespectDefault()
        {
            return CR != null;
        }

        /// <summary>
        /// Return true iff all name(s) are in default before base date.
        /// </summary>
        public bool DefaultedBeforeBaseDate()
        {
            return NamesDefaultedBeforeBaseDate.Aggregate(true, (current, nameDefaultedBeforeBaseDate) => current && nameDefaultedBeforeBaseDate);
        }
    }

    [Serializable]
    public abstract class DealCreditBase : Deal
    {
        protected string fName = string.Empty;
        protected string fCurrency = string.Empty;
        protected string fRecovery = string.Empty;
        protected string fDiscountRate = string.Empty;

        // Reference type: Index or single name
        protected ReferenceType fReferenceType;
        
        /// <summary>
        /// Type of reference. Single Name of Index
        /// </summary>
        /// <remarks>public, so can be accessed by callers of the class</remarks>
        public enum ReferenceType
        {
            Single_Name,
            Index
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DealCreditBase"/> class.
        /// </summary>
        protected DealCreditBase()
        {
            Survival_Probability = string.Empty;
        }

        public string Name
        {
            get { return fName;         } set { fName         = value; }
        }

        /// <summary>
        /// Gets or sets the survival probability price factor ID.
        /// </summary>
        [NonMandatory]
        public string Survival_Probability
        {
            get;
            set;
        }

        [NonMandatory]
        public string Recovery_Rate
        {
            get { return fRecovery;     } set { fRecovery     = value; }
        }

        public string Currency
        {
            get { return fCurrency;     } set { fCurrency     = value; }
        }

        [NonMandatory]
        public string Discount_Rate
        {
            get { return fDiscountRate; } set { fDiscountRate = value; }
        }

        /// <summary>
        /// Public getter for the protected Reference type property
        /// </summary>
        public ReferenceType ProtectionReferenceType()
        {
            return fReferenceType;
        }

        /// <summary>
        /// Returns the ID of the deal's valuation currency, which is a simple multiplier in the valuation function
        /// </summary>
        public override string DealCurrency()
        {
            return fCurrency;
        }

        /// <summary>
        /// text string summary of the deal parameters
        /// </summary>
        /// <returns></returns>
        public override string Summary()
        {
            return string.Format("{0} {1}", fName, fCurrency);
        }
    }

    [Serializable]
    public abstract class CreditBaseValuation : Valuation
    {
        protected DealCreditBase fCreditBaseDeal;
        protected YesNo          fRespectDefault = YesNo.No;

        /// <summary>
        /// Gets or sets the Respect_Default valuation model parameter (Yes or No).
        /// </summary>
        public YesNo Respect_Default
        {
            get { return fRespectDefault; } set { fRespectDefault = value; }
        }

        public override Deal Deal
        {
            get { return this.fCreditBaseDeal; }
            set { this.fCreditBaseDeal = (DealCreditBase)value; }
        }

        /// <summary>
        /// Valuation becomes path-dependent if valuation model respects default.
        /// </summary>
        public override bool FullPricing()
        {
            return Respect_Default == YesNo.Yes;
        }

        // -----------------------------------------------------------------------------
        // Description: Register price factors
        // -----------------------------------------------------------------------------
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            factors.Register<DiscountRate>(InterestRateUtils.GetRateId(fCreditBaseDeal.Discount_Rate, fCreditBaseDeal.Currency));
            factors.RegisterInterface<IFxRate>(fCreditBaseDeal.Currency);
            factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(fCreditBaseDeal.Survival_Probability) ? fCreditBaseDeal.Name : fCreditBaseDeal.Survival_Probability);

            if (Respect_Default == YesNo.Yes)
            {
                if (fCreditBaseDeal.ProtectionReferenceType() == DealCreditBase.ReferenceType.Single_Name)
                {
                    factors.Register<CreditRating>(fCreditBaseDeal.Name);
                    if (RequiresRecoveryOnDefault())
                        factors.Register<RecoveryRate>(string.IsNullOrEmpty(fCreditBaseDeal.Recovery_Rate) ? fCreditBaseDeal.Name : fCreditBaseDeal.Recovery_Rate);
                }
                else
                {
                    factors.Register<IndexCDSPool>(fCreditBaseDeal.Name);
                }
            }
        }

        /// <summary>
        /// Return true if a recovery rate is needed when default is respected.
        /// </summary>
        public virtual bool RequiresRecoveryOnDefault()
        {
            return true;
        }

        /// <inheritdoc />
        protected override void GetDefaultTime(Vector defaultTime, PriceFactorList factors)
        {
            if (fCreditBaseDeal != null && fRespectDefault == YesNo.Yes)
            {
                using (var cache = Vector.CacheLike(defaultTime))
                {
                    var parameters = new CreditValuationParameters(fCreditBaseDeal, this, factors, cache);
                    defaultTime.Assign(parameters.DefaultTime[0]);
                    return;
                }
            }

            base.GetDefaultTime(defaultTime, factors);
        }
    }
}