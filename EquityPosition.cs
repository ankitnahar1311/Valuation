using System;
using System.ComponentModel;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Implementation of a bond position deal.
    /// </summary>
    [Serializable]
    [DisplayName("Equity Position")]
    [IssueDealType(typeof(EquityDeal))]
    public sealed class EquityPosition : GenericPosition
    {
        /// <summary>
        /// Gets or sets whether this is a buy or sell position.
        /// </summary>
        public BuySell Buy_Sell { get; set; }

        /// <summary>
        /// Override any built deal properties.
        /// </summary>
        protected override void OverrideBuiltDealProperties()
        {
            ((EquityDeal)fItems[0]).Buy_Sell = Buy_Sell;
        }
    }
}
