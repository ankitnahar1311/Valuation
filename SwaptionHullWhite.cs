using System;
using System.ComponentModel;
using System.Diagnostics;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Valuation model for swaption, priced using the 1 factor Hull White model.
    /// </summary>
    [Serializable]
    [DisplayName("Hull White Swaption Valuation")]
    [ContainerValuationImplementsOwnValuation]
    public class SwaptionHullWhiteValuation : Valuation
    {
        [NonSerialized] private IFxRate fFxRate;
        [NonSerialized] private HullWhite1FactorModelParameters fModelParameters;
        [NonSerialized] private IInterestRate fDiscountRate;
        [NonSerialized] private IInterestRate fForecastRate;
        [NonSerialized] private Lazy<GaussHermiteNormalQuadrature> fQuadrature;
        
        private DateList fDates;
        private CFFixedInterestList fFixedCashflowList;
        private CFFloatingInterestList fFloatCashflowList;
        private string fModelParametersId;
        private double[] fFixedCouponWeight;
        private double[] fFixedCouponRate;
        private double[] fFloatingCouponWeight;
        private SwaptionDeal fSwaptionDeal;

        [NonSerialized]
        private Vector fSwapRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaptionHullWhiteValuation"/> class.
        /// </summary>
        public SwaptionHullWhiteValuation()
        {
            Model_Parameters = string.Empty;
        }

        /// <summary>
        /// ID of the Hull White model parameters that the valuation model will use.
        /// </summary>
        public string Model_Parameters
        {
            get; set;
        }

        /// <inheritdoc />
        public override Deal Deal
        {
            get { return fSwaptionDeal; } 
            set { fSwaptionDeal = (SwaptionDeal)value; }
        }

        /// <inheritdoc />
        public override Type DealType()
        {
            return typeof(SwaptionDeal);
        }

        /// <inheritdoc />
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.HeadNodeInitialize(factors, baseTimes, requiredResults);

            var deal = (SwaptionBaseDeal)Deal;

            // Call HeadNodeInitialize on leg models to create time grid on leg models.
            fItems.HeadNodeInitialize(factors, baseTimes, requiredResults);

            SwaptionBaseValuation.SetCashflowLists(null, deal, ref fFixedCashflowList, ref fFloatCashflowList);

            Debug.Assert(fFloatCashflowList != null && fFixedCashflowList != null, "One or both of the underlying swap legs are null.");

            // Calculate fixed interest cashflows
            fFixedCashflowList.CalculateInterest(factors.BaseDate);

            // Add to valuation time grid
            bool cashRequired = requiredResults.CashRequired();
            fT.Add(deal.Option_Expiry_Date, true);
            if (deal.Settlement_Style == SettlementType.Physical)
            {
                fT.AddPayDates(fFixedCashflowList, cashRequired);
                fT.AddPayDates(fFloatCashflowList, cashRequired);
            }
            else
            {
                fT.AddPayDate(deal.Settlement_Date, cashRequired);
            }
        }

        /// <inheritdoc />
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults requiredResults)
        {
            base.PreCloneInitialize(factors, baseTimes, requiredResults);

            fItems.PreCloneInitialize(factors, baseTimes, requiredResults);

            int fixedCount = fFixedCashflowList.Count();
            int floatCount = fFloatCashflowList.Count();

            fDates = new DateList();

            // Get a list of all the relevant dates for the deal.
            for (int i = 0; i < fixedCount; i++)
            {
                var fixedDate = fFixedCashflowList[i].Payment_Date;
                fDates.Add(fixedDate);
            }

            for (int i = 0; i < floatCount; i++)
            {
                var payDate = fFloatCashflowList[i].Payment_Date;
                var startDate = fFloatCashflowList[i].Resets[0].Rate_Start_Date;

                if (!fDates.Contains(payDate))
                    fDates.Add(payDate);

                if (!fDates.Contains(startDate))
                    fDates.Add(startDate);
            }

            int count = fDates.Count;

            fFixedCouponWeight = new double[count];
            fFixedCouponRate = new double[count];

            // Calucate the static parts of the coefficients
            foreach (var cf in fFixedCashflowList)
            {
                int idx = fDates.IndexOf(cf.Payment_Date);
                fFixedCouponWeight[idx] += cf.Accrual_Year_Fraction * cf.Notional;
                fFixedCouponRate[idx] = cf.Rate;
            }

            fFloatingCouponWeight = new double[count];

            foreach (var cf in fFloatCashflowList)
            {
                int idx = fDates.IndexOf(cf.Payment_Date);
                double rateYearFraction = cf.Resets[0].Rate_Year_Fraction;
                double yearFractionRatio = (rateYearFraction < CalcUtils.TINY) ? 1.0 : cf.Accrual_Year_Fraction / rateYearFraction;
                fFloatingCouponWeight[idx] += cf.Notional * yearFractionRatio;                
            }
        }

        /// <inheritdoc />
        public void PreValue(PriceFactorList factors)
        {
            var deal = (SwaptionDeal)Deal;

            var discountId = InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency);
            var forecastId = InterestRateUtils.GetRateId(deal.Forecast_Rate, discountId);

            fModelParameters = factors.Get<HullWhite1FactorModelParameters>(fModelParametersId);
            
            fFxRate = factors.GetInterface<IFxRate>(deal.Currency);
            fDiscountRate = DiscountRate.Get(factors, discountId);
            fForecastRate = factors.GetInterface<IInterestRate>(forecastId);

            fQuadrature = new Lazy<GaussHermiteNormalQuadrature>(() => new GaussHermiteNormalQuadrature(30));
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            var deal = (SwaptionDeal)Deal;

            // Get underlying cashflow lists
            SwaptionBaseValuation.SetCashflowLists(errors, deal, ref fFixedCashflowList, ref fFloatCashflowList);

            if (!IsVanillaSwaption())
                Deal.AddToErrors(errors, ErrorLevel.Error, "The Hull White swaption valuation model is for vanilla swaptions only.");

            // Register deal currency
            factors.RegisterInterface<IFxRate>(deal.Currency);

            // Register discount rate
            var discountId = InterestRateUtils.GetRateId(deal.Discount_Rate, deal.Currency);
            string discountRateCurrency = DiscountRate.Register(factors, discountId).GetCurrency();
            if (!string.IsNullOrEmpty(discountRateCurrency) && discountRateCurrency != deal.Currency)
                errors.Add(ErrorLevel.Error, "Settlement currency (Currency) and currency of Discount_Rate must be the same");

            // Register forecast rate
            var forecastId = InterestRateUtils.GetRateId(deal.Forecast_Rate, discountId);
            var forecastRateCurrency = factors.RegisterInterface<IInterestRate>(forecastId).GetCurrency();
            if (forecastRateCurrency != deal.Currency)
                errors.Add(ErrorLevel.Error, "Settlement currency (Currency) and currency of Forecast_Rate must be the same");

            // Register the HW model parameters
            fModelParametersId = string.IsNullOrWhiteSpace(Model_Parameters) ? forecastId : Model_Parameters;

            factors.Register<HullWhite1FactorModelParameters>(fModelParametersId);
            
            // Check that floating cashflow list is standard enough to be valued by ValueSwap
            if (fFloatCashflowList == null || fFixedCashflowList == null)
            {
                errors.Add(ErrorLevel.Error, "Deal must contain exactly one floating and one fixed leg.");
            }
            else
            {
                var characteristics = fFloatCashflowList.Analyze(factors.BaseDate);
                if (!characteristics.HasSwaplet || characteristics.HasOptionlet ||
                    !characteristics.IsStandardPayoff || characteristics.HasCms || !characteristics.IsStandardLibor ||
                    fFloatCashflowList.Compounding_Method != CompoundingMethod.None || fFixedCashflowList.Compounding == YesNo.Yes)
                {
                    errors.Add(ErrorLevel.Error, "Underlying swap has non-standard floating cashflows.");
                }
            }
        }

        /// <summary>
        /// Sets the vector of swap rates used during rebootstrapping.
        /// </summary>
        /// <remarks>
        /// If the swap rate vector is null at valuation time, it is read from the underlying cashflow list.
        /// </remarks>
        public void SetSwapRate(Vector swaprate)
        {
            fSwapRate = swaprate;
        }

        /// <summary>
        /// Calculate vector valuation profile and vector realised cash profile.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            CalcUtils.CreateDealProfilesIfRequired(valuationResults, fItems, factors);

            double paySign = fSwaptionDeal.Payer_Receiver == PayerReceiver.Payer ? +1 : -1;
            double buySign = fSwaptionDeal.Buy_Sell       == BuySell.Buy         ? +1 : -1;
            
            bool isCashSettled       = fSwaptionDeal.Settlement_Style == SettlementType.Cash;
            bool isPhysicallySettled = fSwaptionDeal.Settlement_Style == SettlementType.Physical;
            bool cashRequired        = !valuationResults.Cash.Ignore;

            TimeGridIterator tgi = new TimeGridIterator(fT);

            PVProfiles result = valuationResults.Profile;

            using (IntraValuationDiagnosticsHelper.StartDeal(fIntraValuationDiagnosticsWriter, Deal))
            {
                using (var outerCache = Vector.Cache(factors.NumScenarios))
                {
                    Vector pv = outerCache.Get();
                    Vector exerciseWeight = outerCache.GetClear();
                    Vector cash = cashRequired ? outerCache.GetClear() : null;
                    
                    // For a cash settled swaption, Settlement amount to be paid on Settlement Date.
                    Vector settlementCash = isCashSettled ? outerCache.GetClear() : null;

                    VectorEngine.For(tgi, () =>
                        {
                            // Work out the PV
                            if (tgi.Date < fSwaptionDeal.Option_Expiry_Date)
                            {
                                ValueBeforeExpiry(pv, factors, isCashSettled, tgi);
                            }
                            else
                            {
                                ValueOnOrAfterExpiry(pv, exerciseWeight, settlementCash, cash, factors, isCashSettled, isPhysicallySettled, cashRequired, tgi, paySign);
                            }

                            result.AppendVector(tgi.Date, buySign * pv * fFxRate.Get(tgi.T));

                            if (cashRequired)
                                valuationResults.Cash.Accumulate(fFxRate, tgi.Date, buySign * cash);
                        });
                }

                result.Complete(fT);
            }
        }

        private static void CreateVectorArrays(Vector[] df, VectorScopedCache.Scope cache, Vector[] coupon, Vector[] coefficient, Vector[] stdDev)
        {
            int count = df.Length;
            VectorEngine.For(0, count, i =>
                {
                    df[i] = cache.GetClear();
                    coupon[i] = cache.GetClear();
                    coefficient[i] = cache.GetClear();
                    stdDev[i] = cache.GetClear();
                    return LoopAction.Continue;
                });
        }

        /// <summary>
        /// Calculates the PV using analytic formula if at least one scenario needs it.
        /// </summary>
        private void CalculateAnalyticPV(Vector analyticPv, Vector isUnique, Vector[] stdDev, Vector yStar, Vector[] coefficient, Vector[] coupon, Vector dfTExpiry, Vector[] df)
        {
            if (isUnique.MaxElement() == 1.0)
            {
                OptionType optionType = fSwaptionDeal.Payer_Receiver == PayerReceiver.Payer ? OptionType.Call : OptionType.Put;
   
                // Value by summing the of the caplets or floorlets
                analyticPv.Clear();

                using (var cache = Vector.CacheLike(yStar))
                {
                    // f_i(y*) plays the role of price and dfTPay[i] the strike.
                    Vector optionletPrice = cache.Get();

                    VectorEngine.For(0, stdDev.Length, i =>
                        {
                            // Performs optionletPrice = dfExpiry * fCoefficient[i] * Exp(-stdDev[i] * yStar) 
                            optionletPrice.Assign(CalcUtils.SafeExpMultiply(-stdDev[i] * yStar, coefficient[i] * dfTExpiry));
                            analyticPv.Add(coupon[i] * PricingFunctions.BlackFunction(optionType, optionletPrice, df[i], stdDev[i]));
                            return LoopAction.Continue;
                        });
                }
            }
        }

        /// <summary>
        /// Calculates the PV using Gauss Hermite quadrature if at least one scenario needs it.
        /// </summary>
        private void CalculateNumericalPV(Vector numericalPv, Vector isUnique, Vector[] stdDev, Vector[] coefficient, Vector[] coupon)
        {
            if (isUnique.MinElement() == 0.0)
            {
                double delta = fSwaptionDeal.Payer_Receiver == PayerReceiver.Payer ? 1.0 : -1.0;
   
                // At least one scenario needs numerical integration
                CalcUtils.GaussianQuadratureIntegral(numericalPv, (vout, y) =>
                    {
                        vout.Clear();

                        VectorEngine.For(0, coupon.Length, i =>
                            {
                                vout.Subtract(CalcUtils.SafeExpMultiply(stdDev[i] * y, delta * coupon[i] * coefficient[i]));
                                return LoopAction.Continue;
                            });

                        vout.Assign(VectorMath.Max(vout, 0.0));
                    }, fQuadrature.Value);
            }
        }

        /// <summary>
        /// Gets the discount and forecast factors for tEnd and tStart avoiding new reads where possible.
        /// </summary>
        private void GetDiscountAndForecastFactors(Vector dfTStart, Vector dfTPay, Vector ffTStart, Vector ffTEnd, double tValue, double tStart, double tEnd, double tPay, double tLastEnd, double tLastPay)
        {
            // Only get Start factors if necessary.
            if (tStart == tLastPay)
                dfTStart.Assign(dfTPay);
            else
                fDiscountRate.GetValue(dfTStart, tValue, tStart);

            if (tStart == tLastEnd)
                ffTStart.Assign(ffTEnd);
            else
                fForecastRate.GetValue(ffTStart, tValue, tStart);

            // Always have to get pay and end factors.
            fDiscountRate.GetValue(dfTPay, tValue, tPay);
            fForecastRate.GetValue(ffTEnd, tValue, tEnd);
        }

        /// <summary>
        /// Fills the arrays of coupon, pay date discount factors, standard deviations and coefficient by running through the cashflows 
        /// </summary>
        private void GetSwapQuantities(Vector[] coupon, Vector[] stdDev, Vector[] coefficient, Vector[] df, double tValue, double tExpiry, double baseDate, Vector dfTExpiry)
        {
            int count = coupon.Length;
            double tLastEnd = double.NegativeInfinity;
            double tLastPay = double.NegativeInfinity;
            int floatIndex = 0;
            CFFloatingInterest cfFloating = fFloatCashflowList[floatIndex];
            TDate floatingStartDate = cfFloating.Resets[0].Rate_Start_Date;

            // We will minimise the number of discount factors we get.
            bool[] haveDF = new bool[count];

            using (var cache = Vector.CacheLike(dfTExpiry))
            {
                Vector bTExpiry = cache.Get();
                Vector rootZeta = cache.Get();
                Vector bT = cache.Get();
                Vector dfTStart = cache.Get();
                Vector dfTPay = cache.Get();
                Vector ffTStart = cache.Get();
                Vector ffTEnd = cache.Get();
                Vector beta = cache.Get();

                fModelParameters.GetB(bTExpiry, tValue, tExpiry);
                fModelParameters.GetZeta(rootZeta, tValue, tExpiry);
                rootZeta.AssignSqrt(rootZeta);

                // Loop over copuons calculating useful quantities.
                VectorEngine.For(0, count, i =>
                    {
                        double tDf = CalcUtils.DaysToYears(fDates[i] - baseDate);

                        // if this date is associated with a floating start date include beta contribution
                        if (fDates[i] == floatingStartDate)
                        {
                            var floatingEndDate = cfFloating.Resets[0].Rate_End_Date;
                            var paymentDate = cfFloating.Payment_Date;

                            double tStart = CalcUtils.DaysToYears(floatingStartDate - baseDate);
                            double tEnd = CalcUtils.DaysToYears(floatingEndDate - baseDate);
                            double tPay = CalcUtils.DaysToYears(paymentDate - baseDate);

                            GetDiscountAndForecastFactors(dfTStart, dfTPay, ffTStart, ffTEnd, tValue, tStart, tEnd, tPay, tLastEnd, tLastPay);

                            beta.Assign(dfTPay * ffTStart / (dfTStart * ffTEnd));

                            double rateYearFraction = cfFloating.Resets[0].Rate_Year_Fraction;
                            double yearFractionRatio = (rateYearFraction < CalcUtils.TINY) ? 1.0 : cfFloating.Accrual_Year_Fraction / rateYearFraction;
                            coupon[i].Assign(-beta * yearFractionRatio * cfFloating.Notional);

                            // Store new discount factors.
                            int payIndex = fDates.IndexOf(paymentDate);
                            df[payIndex].Assign(dfTPay);
                            haveDF[payIndex] = true;

                            if (!haveDF[i])
                            {
                                df[i].Assign(dfTStart);
                                haveDF[i] = true;
                            }

                            // Update for next pass
                            tLastEnd = tEnd;
                            tLastPay = tPay;
                            floatIndex++;

                            if (floatIndex < fFloatCashflowList.Count())
                            {
                                cfFloating = fFloatCashflowList[floatIndex];
                                floatingStartDate = cfFloating.Resets[0].Rate_Start_Date;
                            }
                            else
                            {
                                floatingStartDate = double.PositiveInfinity;
                            }
                        }

                        // Work out coupon, stdDev, coefficient and dfTPay
                        coupon[i].Add(fFloatingCouponWeight[i]);

                        if (fSwapRate != null)
                            coupon[i].Add(fSwapRate * fFixedCouponWeight[i]);
                        else
                            coupon[i].Add(fFixedCouponRate[i] * fFixedCouponWeight[i]);

                        fModelParameters.GetB(bT, tValue, tDf);
                        stdDev[i].Assign((bT - bTExpiry) * rootZeta);

                        // Make sure we get all the discount factors.
                        if (!haveDF[i])
                        {
                            fDiscountRate.GetValue(df[i], tValue, tDf);
                            haveDF[i] = true;
                        }

                        // Performs coefficient[i] = dfTPay[i] / dfTExpiry * Exp(-0.5 * stdDev[i] * stdDev[i])
                        coefficient[i].Assign(CalcUtils.SafeExpMultiply(-0.5 * stdDev[i] * stdDev[i], df[i] / dfTExpiry));

                        return LoopAction.Continue;
                    });
            }
        }

        /// <summary>
        /// Checks that:
        ///     Each floating cashflow has a single reset
        ///     Rate end date after rate start date.
        ///     First rate start must be before first fixed pay date.
        /// </summary>
        private bool IsVanillaSwaption()
        {
            // Make sure that the floating periods are properly behaved.
            if (fFloatCashflowList.Items.Count == 0)
                return false;

            foreach (var cashflow in fFloatCashflowList)
            {
                if (cashflow.Resets.Count != 1)
                    return false;

                var reset = cashflow.Resets[0];

                if (reset.Rate_Start_Date >= reset.Rate_End_Date)
                    return false;
            }

            if (fFixedCashflowList.Items[0].Payment_Date <= fFloatCashflowList[0].Resets[0].Rate_Start_Date)
                return false;
            
            return true;
        }

        /// <summary>
        /// Checks a sufficient condition for yStar to be the unique solution of F(y) = 0.
        /// </summary>
        private void IsSolutionUnique(Vector isYStarUnique, Vector yStar, Vector[] coefficient, Vector[] coupon, Vector[] stdDev)
        {
            using (var cache = Vector.CacheLike(yStar))
            {
                Vector sum = cache.GetClear();
                Vector haveNegative = cache.GetClear();
                Vector positiveValue = cache.Get();

                int count = coefficient.Length;

                // C_0 * f_0 (y*) - this term is negative.
                sum.Assign(CalcUtils.SafeExpMultiply(-stdDev[0] * yStar, coupon[0] * coefficient[0]));

                VectorEngine.For(1, count, LoopDirection.Backwards, i =>
                    {
                        haveNegative.AssignConditional(coupon[i] <= -CalcUtils.TINY, 1.0, haveNegative);

                        positiveValue.Assign(CalcUtils.SafeExpMultiply(-stdDev[i] * yStar, coupon[i] * coefficient[i] * haveNegative));
                        positiveValue.AssignMax(positiveValue, 0.0);
                        sum.Add(positiveValue);

                        return LoopAction.Continue;
                    });

                isYStarUnique.AssignConditional(sum <= 0.0, 1.0, 0.0);
            }
        }
        
        private void SolveForYStar(Vector yStar, Vector[] coefficient, Vector[] coupon, Vector[] stdDev)
        {
            const int MaxIterations = 10;
                
            using (var cache = Vector.CacheLike(yStar))
            {
                Vector y = cache.Get();
                Vector value = cache.Get();
                Vector deriv = cache.Get();
                Vector factor = cache.Get();
                Vector dy = cache.Get();
                Vector product = cache.Get();
                Vector numerator = cache.GetClear();
                Vector demoninator = cache.GetClear();

                int numberOfCoupons = coupon.Length;

                // Solve the equation to first order for the initial guess
                VectorEngine.For(0, numberOfCoupons, i =>
                    {
                        product.Assign(coupon[i] * coefficient[i]);
                        numerator.Add(product);
                        demoninator.Add(product * stdDev[i]);
                        return LoopAction.Continue;
                    });

                y.Assign(numerator / demoninator);

                var dyAbs = cache.Get();

                // Newton-Raphson loop to find yStar
                VectorEngine.For(0, MaxIterations, iteration =>
                    {
                        value.Clear();
                        deriv.Clear();

                        VectorEngine.For(0, numberOfCoupons, i =>
                            {
                                // Performs factor = c[i] * fCoefficient[i] * Exp(-stdDev[i] * y) 
                                factor.Assign(CalcUtils.SafeExpMultiply(-stdDev[i] * y, coupon[i] * coefficient[i]));
                                value.Add(factor);
                                deriv.Subtract(stdDev[i] * factor);
                                return LoopAction.Continue;
                            });

                        // Could get divide by zero
                        dy.Assign(value / deriv);
                        
                        // Terminate if solution reached.
                        dyAbs.AssignAbs(dy);
                        if (dyAbs.MaxElement() == 0)
                            return LoopAction.Break;

                        y.Subtract(dy);

                        return LoopAction.Continue;
                    });

                yStar.Assign(y);
            }
        }

        /// <summary>
        /// Value the swaption before expiry using a Jamshidian style decomposition to value the swaption analytically.
        /// Checks for the validity of the decomposition and falls back on numerical integration when necessary.
        /// </summary>
        private void ValueBeforeExpiry(Vector pv, PriceFactorList factors, bool isCashSettled, TimeGridIterator tgi)
        {
            double baseDate = factors.BaseDate;
            double tExpiry = fSwaptionDeal.GetTimeToExpiry(baseDate);
            double tValue = CalcUtils.DaysToYears(tgi.Date - baseDate);
            int count = fDates.Count;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector dfTExpiry = cache.Get();
                Vector yStar = cache.Get();
                Vector isUnique = cache.Get();
                Vector analyticPv = cache.GetClear();
                Vector numericalPv = cache.GetClear();

                // Theory guide mappings for the vector arrays are:
                //     df          <=> D(0, T_i)
                //     coupons     <=> C_i
                //     stdDev      <=> v_i
                //     coefficient <=> D(0, T_i)/D(0, T) exp (- v_i^2 / 2)
                Vector[] df = new Vector[count];
                Vector[] coupons = new Vector[count];
                Vector[] stdDev = new Vector[count];
                Vector[] coefficient = new Vector[count];
                
                CreateVectorArrays(df, cache, coupons, coefficient, stdDev);

                fDiscountRate.GetValue(dfTExpiry, tValue, tExpiry);

                GetSwapQuantities(coupons, stdDev, coefficient, df, tValue, tExpiry, baseDate, dfTExpiry);

                // Find the value of y which makes the underlying swap value to 0, used for Jamshidian style decomposition
                SolveForYStar(yStar, coefficient, coupons, stdDev);

                // Calculate the scenarios in which the yStar values are guaranteed unique
                IsSolutionUnique(isUnique, yStar, coefficient, coupons, stdDev);

                CalculateAnalyticPV(analyticPv, isUnique, stdDev, yStar, coefficient, coupons, dfTExpiry, df);
                CalculateNumericalPV(numericalPv, isUnique, stdDev, coefficient, coupons);

                pv.AssignConditional(isUnique >= 1.0, analyticPv, numericalPv);

                if (isCashSettled)
                {
                    double tSettlement = CalcUtils.DaysToYears(fSwaptionDeal.Settlement_Date - baseDate);

                    // Factor in time value of money due to settlement delay.
                    pv.MultiplyBy(fDiscountRate.Get(tgi.T, tSettlement) / fDiscountRate.Get(tgi.T, tExpiry));
                }
            }
        }

        /// <summary>
        /// Value the fixed and floating legs of the swap.
        /// </summary>
        private void ValueFixedAndFloatingLegs(TimeGridIterator tgi, Vector fixedPv, Vector fixedCash, double baseDate, Vector floatPv, Vector floatCash)
        {
            using (IntraValuationDiagnosticsHelper.StartCashflowsOnDate(fIntraValuationDiagnosticsWriter, tgi.Date))
            {
                using (IntraValuationDiagnosticsHelper.StartSwaptionCashflows(fIntraValuationDiagnosticsWriter, fFxRate, tgi.T,
                                                                              CashflowType.FixedLeg, fSwaptionDeal.Floating_Margin))
                {
                    // Calculate value of fixed side
                    fixedPv.Clear();
                    fixedCash.Clear();
                    fFixedCashflowList.Value(fixedPv, fixedCash, null, baseDate, tgi.Date, null, fDiscountRate, null, null, null, fIntraValuationDiagnosticsWriter, 0.0);
                    IntraValuationDiagnosticsHelper.AddCashflowsPV(fIntraValuationDiagnosticsWriter, fixedPv);
                }

                using (IntraValuationDiagnosticsHelper.StartSwaptionCashflows(fIntraValuationDiagnosticsWriter, fFxRate, tgi.T,
                                                                              CashflowType.FloatingLeg, fSwaptionDeal.Floating_Margin))
                {
                    // Calculate value of floating side
                    floatPv.Clear();
                    floatCash.Clear();
                    fFloatCashflowList.ValueSwap(floatPv, floatCash, null, baseDate, tgi.Date, null, 0.0, fDiscountRate, fForecastRate, false, fIntraValuationDiagnosticsWriter);
                    IntraValuationDiagnosticsHelper.AddCashflowsPV(fIntraValuationDiagnosticsWriter, floatPv);
                }
            }
        }

        private void ValueOnOrAfterExpiry(Vector pv, Vector exerciseWeight, Vector settlementCash, Vector cash, PriceFactorList factors, bool isCashSettled, bool isPhysicallySettled, bool cashRequired, TimeGridIterator tgi, double paySign)
        {
            double baseDate = factors.BaseDate;
            double tSettlement = isCashSettled ? CalcUtils.DaysToYears(fSwaptionDeal.Settlement_Date - baseDate) : 0.0;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector fixedPv = cache.Get();
                Vector fixedCash = cache.Get();
                Vector floatPv = cache.Get();
                Vector floatCash = cache.Get();

                ValueFixedAndFloatingLegs(tgi, fixedPv, fixedCash, baseDate, floatPv, floatCash);

                if (tgi.Date == fSwaptionDeal.Option_Expiry_Date)
                {
                    pv.Assign(VectorMath.Max(0.0, paySign * (floatPv - fixedPv)));

                    if (isPhysicallySettled)
                    {
                        exerciseWeight.Assign(pv > 0.0); // 1 if exercised and otherwise 0
                    }
                    else if (isCashSettled)
                    {
                        settlementCash.Assign(pv); // Records cash settlement amount on Option Expiry Date.
                        pv.Assign(settlementCash * fDiscountRate.Get(tgi.T, tSettlement)); // Factor in time value of money due to settlement delay.
                    }
                }
                else 
                {
                    // After expiry
                    if (isPhysicallySettled)
                    {
                        pv.Assign(paySign * (floatPv - fixedPv) * exerciseWeight);

                        if (cashRequired)
                            cash.Assign(paySign * (floatCash - fixedCash) * exerciseWeight);
                    }
                    else if (isCashSettled)
                    {
                        pv.Assign(settlementCash * fDiscountRate.Get(tgi.T, tSettlement));

                        if (tgi.Date == fSwaptionDeal.Settlement_Date && cashRequired)
                            cash.Assign(settlementCash);
                    }
                }
            }
        }
    }
}