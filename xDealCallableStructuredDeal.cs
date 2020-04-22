/// <author>
/// Alastair Wilkins
/// </author>
/// <owner>
/// Alastair Wilkins
/// </owner>
/// <summary>
/// Deal and valuation classes for callable structured deal.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    using ExerciseList = List<InterestRateOptionPricer.ExerciseItem>;

    /// <summary>
    /// Callable structured deal class.
    /// Uses DealList property of Deal base class.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Callable Structured Deal")]
    public class CallableStructuredDeal : IRDeal
    {
        /// <summary>
        /// CallableStructuredDeal constructor creating deal list. 
        /// </summary>
        public CallableStructuredDeal()
        {
            Description          = Property.DisplayName(typeof(CallableStructuredDeal));
            Exercise_Dates       = new ExercisePropertiesList();
            fItems               = new DealList();
            Issuer               = string.Empty;
            Survival_Probability = string.Empty;
            Option_Type          = OptionType.Call;
            Settlement_Style     = SettlementType2.Embedded;
            Recovery_Rate        = string.Empty;
        }

        [NonMandatory]
        public string Forecast_Rate
        {
            get { return fForecast; } set { fForecast = value; }
        }

        [NonMandatory]
        public string Forecast_Rate_Cap_Volatility
        {
            get { return fForecastVolatility; }            set { fForecastVolatility = value; }
        }

        [NonMandatory]
        public string Forecast_Rate_Swaption_Volatility
        {
            get { return fForecast2Volatility; }
            set { fForecast2Volatility = value; }
        }

        [NonMandatory]
        public string Description
        {
            get; set;
        }

        public BuySell Buy_Sell
        {
            get; set;
        }

        public double Principal
        {
            get; set;
        }

        public OptionType Option_Type
        {
            get; set;
        }

        public SettlementType2 Settlement_Style
        {
            get; set;
        }

        public ExercisePropertiesList Exercise_Dates
        {
            get; set;
        }

        [NonMandatory]
        public string Issuer
        {
            get; set;
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
            get; set;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            // Currently, cannot return the last exercise date fExercises[fExercises.Count - 1].Exercise_Date
            // when fPayoffType == PayoffType.Call || fPayoffType == PayoffType.Put
            // because Value (and AggregrateValue) would not be called after the last exercise date,
            // and then the DealProfiles for the component deals are not created.
            return fItems.EndDate();
        }

        /// <summary>
        /// Deal description constructed from main deal properties.
        /// </summary>
        public override string Summary()
        {
            return Description;
        }

        public override string GetIssuer()
        {
            return Issuer;
        }

        /// <summary>
        /// Gets the survival probability price factor ID.
        /// </summary>
        public override string GetSurvivalProbability()
        {
            return Survival_Probability;
        }

        public override string GetRecoveryRate()
        {
            return Recovery_Rate;
        }

        /// <summary>
        /// Validate the deal properties and component deals.
        /// </summary>
        protected override void DoValidate(ICalendarData calendar, IStaticDataObjects staticData, ErrorList errors)
        {
            Exercise_Dates.Validate(errors, EndDate());
            fItems.Validate(calendar, staticData, errors);
        }
    }

    /// <summary>
    /// Base valuation class for callable structured deals.
    /// </summary>
    [Serializable]
    public abstract class CallableStructuredDealValuation : IRValuation, ICanUseSurvivalProbability
    {
        [NonSerialized]
        protected IInterestRateVol fInterestRateVol = null;
        [NonSerialized]
        protected IInterestYieldVol fInterestYieldVol = null;
        [NonSerialized]
        protected ISurvivalProb fSurvivalProb = null;
        [NonSerialized]
        protected ExerciseList fExercises = null;
        [NonSerialized]
        protected CreditRating fCreditRating = null;
        [NonSerialized]
        protected RecoveryRate fRecoveryRate = null;

        protected CallableStructuredDealValuation()
        {
            Use_Survival_Probability = YesNo.No;
            Respect_Default = YesNo.No;
        }

        public YesNo Use_Survival_Probability
        {
            get; set;
        }

        /// <summary>
        /// Property used to set or determine if the valuation model is to take default into account.
        /// </summary>
        public YesNo Respect_Default
        {
            get; set;
        }

        /// <summary>
        /// Prepare component deals for valuation.
        /// </summary>
        public static void PreValueDeals(ValuationList models, PriceFactorList factors)
        {
            foreach (Valuation model in models)
            {
                if (model.Deal.fIgnore)
                    continue;

                if (model.IsContainer())
                {
                    PreValueDeals(model.fItems, factors);
                    continue;
                }

                var irValuation = model as IRValuation;
                if (irValuation != null && model.Deal.EndDate() >= factors.BaseDate)
                    irValuation.PreValue(factors);
            }
        }

        /// <summary>
        /// Type of deals that this class can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(CallableStructuredDeal);
        }

        /// <summary>
        /// Returns true if deal is strongly path-dependent.
        /// </summary>
        public override bool FullPricing()
        {
            return Respect_Default == YesNo.Yes;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            var deal = (CallableStructuredDeal)Deal;
            
            if ((Use_Survival_Probability == YesNo.Yes || Respect_Default == YesNo.Yes) && string.IsNullOrWhiteSpace(deal.Issuer))
                deal.AddToErrors(errors, ErrorLevel.Warning, string.Format("For deal valued using {0}, Issuer is missing but Use_Survival_Probability or Respect_Default is set to Yes; valuation of this deal will be treated as if Use_Survival_Probability and Respect_Default are both No.", GetType().Name));
        }

        /// <summary>
        /// Register price factors used in valuation.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            if (!string.IsNullOrEmpty(fForecastCurrency) && fForecastCurrency != fCurrency)
                errors.Add(ErrorLevel.Error, "Settlement currency (Currency) and currency of Forecast_Rate must be the same");

            SetModelParameters(fItems);
            ValidateModels(fItems, errors);

            fItems.RegisterFactors(factors, errors);

            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;

            bool needRating   = Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needSurvival = Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);

            if (needRating)
            {
                factors.Register<CreditRating>(deal.Issuer);
                factors.Register<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }

            if (needSurvival)
                factors.RegisterInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <inheritdoc />
        public override void HeadNodeInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.HeadNodeInitialize(factors, baseTimes, resultsRequired);
            fItems.HeadNodeInitialize(factors, baseTimes, resultsRequired);
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults ResultsRequired)
        {
            base.PreCloneInitialize(factors, baseTimes, ResultsRequired);

            fItems.PreCloneInitialize(factors, baseTimes, ResultsRequired);

            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;

            // Create a list of sorted exercise items with exercise date >= baseDate and exercise date <= deal's end date.
            fExercises = CreateExerciseList(factors.BaseDate, deal.EndDate(), deal.Exercise_Dates, deal.Principal);

            AddDatesToValuationGrid(factors.BaseDate, ResultsRequired.CashRequired());
        }

        /// <summary>
        /// Prepare for valuation.
        /// </summary>
        public override void PreValue(PriceFactorList factors)
        {
            base.PreValue(factors);

            SetModelParameters(fItems);

            PreValueDeals(fItems, factors);

            CallableStructuredDeal deal = (CallableStructuredDeal)Deal;

            // Set volatility price factors if they have been registered by model or underlying deals
            InterestVol.TryGet<IInterestRateVol>(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency, out fInterestRateVol);
            InterestVol.TryGet<IInterestYieldVol>(factors, deal.Forecast_Rate_Swaption_Volatility, fForecastCurrency, out fInterestYieldVol);

            bool needRating = Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);
            bool needSurvival = Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);

            if (needRating)
            {
                fCreditRating = factors.Get<CreditRating>(deal.Issuer);
                fRecoveryRate = factors.Get<RecoveryRate>(InterestRateUtils.GetRateId(deal.Recovery_Rate, deal.Issuer));
            }

            if (needSurvival)
                fSurvivalProb = factors.GetInterface<ISurvivalProb>(string.IsNullOrEmpty(deal.Survival_Probability) ? deal.Issuer : deal.Survival_Probability);
        }

        /// <summary>
        /// Calculate valuation profiles.
        /// </summary>
        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors);

            TimeGridIterator tgi             = new TimeGridIterator(fT);
            PVProfiles       result          = valuationResults.Profile;
            CashAccumulators cashAccumulator = valuationResults.Cash;
            double           baseDate        = factors.BaseDate;

            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;

            int buySellSign = deal.Buy_Sell    == BuySell.Buy ? +1 : -1;
            int callPutSign = deal.Option_Type == OptionType.Call ? 1 : -1;

            InterestRateOptionPricer optionPricer = CreateOptionPricer(factors);

            CalcUtils.CreateDealProfilesIfRequired(valuationResults, fItems, factors);

            bool needRating = Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(deal.Issuer);

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                Vector exercised      = cache.GetClear(); // vector taking value 0 or 1 indicating exercise before tgi.date
                Vector exercisedToday = cache.Get();      // vector taking value 0 or 1 indicating exercise at tgi.date

                Vector optionPv     = cache.Get();
                Vector pv           = cache.Get();
                Vector cash         = cache.Get();
                Vector settlementDateAtExercise = cache.GetClear();
                Vector defaultDate = needRating ? cache.Get(CalcUtils.DateTimeMaxValueAsDouble) : null;

                var defaultedBeforeBaseDate = needRating &&
                                              CreditRating.DefaultedBeforeBaseDate(fCreditRating, baseDate);

                while (tgi.Next())
                {
                    if (defaultedBeforeBaseDate)
                    {
                        pv.Clear();
                        result.AppendVector(tgi.Date, pv);
                        break;
                    }

                    if (needRating)
                        UpdateDefaultDate(fCreditRating, tgi.Date, tgi.T, defaultDate);

                    double val;
                    bool allExercised = exercised.AllElementsTheSame(out val) && val == 1.0;

                    if (deal.Settlement_Style == SettlementType2.Physical)
                    {
                        // Calculate value of option (option value is zero after last exercise date)
                        if (!allExercised)
                            optionPricer.Value(baseDate, tgi.Date, optionPv, exercised, exercisedToday, settlementDateAtExercise, defaultDate);

                        // Calculate value of underlying cashflows after settlementDateAtExercise
                        pv.Clear();
                        cash.Clear();
                        InterestRateOptionPricer.ValueDeals(fItems, pv, cash, baseDate, tgi.Date, settlementDateAtExercise, defaultDate, fDiscountRate, fForecastRate, fRepoRate, fInterestRateVol, fInterestYieldVol, fSurvivalProb, fRecoveryRate);
                        pv.MultiplyBy(callPutSign);
                        cash.MultiplyBy(callPutSign);

                        if (!allExercised)
                        {
                            // If exercised today the cashflow is the value of the option minus the value of the physically settled part
                            // Else if already exercised, cash is the unnderlying cash.
                            // Else (before exercise) there is no cash.
                            cash.AssignConditional(exercisedToday, optionPv - pv, exercised * cash);

                            // If already exercised, pv is the unnderlying pv.
                            // Else (before exercise or exercised today), pv is the option pv.
                            pv.AssignConditional(exercised, pv, optionPv);
                            pv.AssignConditional(exercisedToday, optionPv, pv);
                        }
                    }
                    else
                    {
                        if (allExercised)
                        {
                            // Already exercised on all scenarios
                            result.AppendZeroVector(tgi.Date);
                            continue;
                        }

                        if (deal.Settlement_Style == SettlementType2.Cash)
                        {
                            // Calculate value of option
                            optionPricer.Value(baseDate, tgi.Date, pv, exercised, exercisedToday, settlementDateAtExercise, defaultDate);

                            // If exercised today then option pv is settled today, otherwise there is no cash
                            cash.AssignProduct(pv, exercisedToday);
                        }
                        else // Embedded option (callable or puttable)
                        {
                            // Calculate underlying value
                            pv.Clear();
                            cash.Clear();
                            InterestRateOptionPricer.ValueDeals(fItems, pv, cash, baseDate, tgi.Date, null, defaultDate, fDiscountRate, fForecastRate, fRepoRate, fInterestRateVol, fInterestYieldVol, fSurvivalProb, fRecoveryRate);

                            // Calculate value of option
                            optionPricer.Value(baseDate, tgi.Date, optionPv, exercised, exercisedToday, settlementDateAtExercise, defaultDate);

                            // Add or subtract value of embedded option
                            pv.AddProduct(-callPutSign, optionPv);

                            // Option payoff is Max(callPutSign * (underlyingPv - accruedInterest - discountedFee), 0)
                            // Callable/puttable payoff on exercise is
                            // underlyingPv - callPutSign * (callPutSign * (underlyingPv - accruedInterest - discountedFee))
                            // = accruedInterest + discountedFee

                            // Set pv and cash to zero if already exercised.
                            // If exercised today then the pv is settled today.
                            pv.AssignConditional(exercised, exercisedToday * pv, pv);
                            cash.AssignConditional(exercised, exercisedToday * pv, cash);
                        }
                    }

                    pv.MultiplyBy(buySellSign);
                    cash.MultiplyBy(buySellSign);
                    result.AppendVector(tgi.Date, fFxRate.Get(tgi.T) * pv);
                    cashAccumulator.Accumulate(fFxRate, tgi.Date, cash);
                }
            }

            result.Complete(fT);
        }

        /// <inheritdoc />
        protected override void GetDefaultTime(Vector defaultTime, PriceFactorList factors)
        {
            if (fCreditRating != null)
            {
                fCreditRating.DefaultTime(defaultTime);
                return;
            }

            base.GetDefaultTime(defaultTime, factors);
        }

        /// <summary>
        /// Create option pricer and other preparation.
        /// </summary>
        protected abstract InterestRateOptionPricer CreateOptionPricer(PriceFactorList factors);

        /// <summary>
        /// Create a list of sorted exercise items with exercise date >= baseDate and exercise date <= deal's end date.
        /// </summary>
        internal static ExerciseList CreateExerciseList(double baseDate, double endDate, ExercisePropertiesList exercisePropertiesList, double principal)
        {
            return (from exerciseProperties in exercisePropertiesList
                let exerciseDate = exerciseProperties.Exercise_Date
                where !(exerciseDate < baseDate || exerciseDate > endDate)
                select new InterestRateOptionPricer.ExerciseItem
                {
                    ExerciseDate = exerciseDate,
                    SettlementDate = exerciseProperties.Settlement_Date,
                    ExerciseFee = principal * exerciseProperties.Exercise_Fee
                }).ToList();
        }

        /// <summary>
        /// Add dates to valuation grid.
        /// </summary>
        private void AddDatesToValuationGrid(double baseDate, bool cashRequired)
        {
            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;

            if (deal.Settlement_Style != SettlementType2.Cash)
            {
                // For callable and puttable and physical option, add dates from component deals
                Curve profile = new Curve();
                fItems.GetValuationDates(profile, baseDate);
                foreach (float date in profile.X)
                    fT.Add(date);
            }

            // Add exercise and settlement dates
            foreach (InterestRateOptionPricer.ExerciseItem item in fExercises)
            {
                fT.Add(item.ExerciseDate, true);
                if (item.SettlementDate > item.ExerciseDate)
                {
                    fT.AddPayDate(item.SettlementDate, cashRequired);
                }
            }
        }

        /// <summary>
        /// Set valuation model parameters on component deals.
        /// </summary>
        private void SetModelParameters(ValuationList models)
        {
            YesNo useSurvivalProbability = Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(fDeal.GetIssuer()) ? YesNo.Yes : YesNo.No;

            foreach (Valuation model in models)
            {
                if (model.Deal.fIgnore)
                    continue;

                if (model.IsContainer())
                {
                    SetModelParameters(model.fItems);
                    continue;
                }
                
                var canUseSurvivalProbabilityModel = model as ICanUseSurvivalProbability;
                if (canUseSurvivalProbabilityModel != null)
                    canUseSurvivalProbabilityModel.Use_Survival_Probability = useSurvivalProbability;
            }
        }

        /// <summary>
        /// Validate the valuation models of the component deals.
        /// </summary>
        private void ValidateModels(ValuationList models, ErrorList errors)
        {
            const string Messsage = "{0} of underlying deals must be the same as {0} of {1}";

            // Get properties of Callable Structured Deal
            string discount = InterestRateUtils.GetRateId(fDeal.GetDiscountRate(), fDeal.Currency);
            string forecast = InterestRateUtils.GetRateId(fDeal.GetForecastRate(), discount);
            string issuer   = fDeal.GetIssuer();
            string recovery = InterestRateUtils.GetRateId(fDeal.GetRecoveryRate(), issuer);

            foreach (Valuation model in models)
            {
                if (model.Deal.fIgnore)
                {
                    continue;
                }
                else if (model.IsContainer())
                {
                    ValidateModels(model.fItems, errors);
                }
                else if (model.Deal is IRDealBase && model is ISingleDateValuation)
                {
                    IRDealBase underlyingDeal = (IRDealBase)model.Deal;

                    // Currency of underlying must match CSD
                    if (underlyingDeal.Currency != fDeal.Currency)
                        errors.Add(ErrorLevel.Error, string.Format(Messsage, "Currency", fDeal.GetType().Name));

                    // Discount_Rate and Forecast_Rate of underlying must either be left blank or match CSD
                    string underlyingDiscount = underlyingDeal.GetDiscountRate();
                    if (!string.IsNullOrEmpty(underlyingDiscount) && underlyingDiscount != discount)
                        errors.Add(ErrorLevel.Error, string.Format(Messsage, "Discount_Rate", fDeal.GetType().Name));

                    string underlyingForecast = underlyingDeal.GetForecastRate();
                    if (!string.IsNullOrEmpty(underlyingForecast) && underlyingForecast != forecast)
                        errors.Add(ErrorLevel.Error, string.Format(Messsage, "Forecast_Rate", fDeal.GetType().Name));

                    if (Use_Survival_Probability == YesNo.Yes && !string.IsNullOrEmpty(issuer))
                    {
                        // Issue and Recovery_Rate of underlying must either be left blank or match CSD
                        string underlyingIssuer = underlyingDeal.GetIssuer();
                        if (!string.IsNullOrEmpty(underlyingIssuer) && underlyingIssuer != issuer)
                            errors.Add(ErrorLevel.Error, string.Format(Messsage, "Issuer", fDeal.GetType().Name));
                    }

                    if (Respect_Default == YesNo.Yes && !string.IsNullOrEmpty(issuer))
                    {
                        string underlyingIssuer = underlyingDeal.GetIssuer();
                        string underlyingRecovery = InterestRateUtils.GetRateId(underlyingDeal.GetRecoveryRate(), underlyingIssuer);
                        if (!string.IsNullOrEmpty(underlyingRecovery) && underlyingRecovery != recovery)
                            errors.Add(ErrorLevel.Error, string.Format(Messsage, "Recovery_Rate", fDeal.GetType().Name));
                    }
                }
                else
                {
                    // Underlying deal type must be IRDealBase and model must support ISingleDateValuation
                    errors.Add(ErrorLevel.Error, string.Format("{0} cannot be used in {1}", model.Deal.GetType().Name, fDeal.GetType().Name));
                }
            }
        }
    }

    /// <summary>
    /// Valuation of callable structured deals using MarketModelTreeOptionPricer.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Callable Structured Deal Market Model Valuation")]
    public class CallableStructuredDealMarketModelValuation : CallableStructuredDealValuation
    {
        public CallableStructuredDealMarketModelValuation()
        {
            Time_Step_Size = MarketModelTreeOptionPricer.DefaultTimeStepSize;
            Max_Time_Steps = MarketModelTreeOptionPricer.DefaultMaxTimeSteps;
        }

        /// <summary>
        /// Model type: Swap_Rate or LIBOR_Rate.
        /// </summary>
        public MarketModelTreeOptionPricer.ModelType Model_Type
        {
            get; set;
        }

        /// <summary>
        /// Size of time step in binomial tree (subject to Max_Time_Steps).
        /// </summary>
        public Period Time_Step_Size
        {
            get; set;
        }

        /// <summary>
        /// Maximum number of time steps in binomial tree.
        /// </summary>
        public int Max_Time_Steps
        {
            get; set;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            if (Time_Step_Size <= 0.0)
                errors.Add(ErrorLevel.Error, string.Format("Time_Step_Size must be greater than zero for {0}.", GetType().Name));

            if (Max_Time_Steps <= 0)
                errors.Add(ErrorLevel.Error, string.Format("Max_Time_Steps must be greater than zero for {0}.", GetType().Name));
        }

        /// <summary>
        /// Register price factors used in valuation.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            CallableStructuredDeal deal = (CallableStructuredDeal)Deal;

            if (Model_Type == MarketModelTreeOptionPricer.ModelType.Libor_Rate)
            {
                InterestVol.Register<IInterestRateVol>(factors, deal.Forecast_Rate_Cap_Volatility, fForecastCurrency);
            }
            else
            {
                InterestVol.Register<IInterestYieldVol>(factors, deal.Forecast_Rate_Swaption_Volatility, fForecastCurrency);
            }
        }

        /// <summary>
        /// Create option pricer and other preparation.
        /// </summary>
        protected override InterestRateOptionPricer CreateOptionPricer(PriceFactorList factors)
        {
            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;
            return new MarketModelTreeOptionPricer(factors.BaseDate, deal.Option_Type, fExercises, fItems, fDiscountRate, fForecastRate, fInterestRateVol, fInterestYieldVol, fSurvivalProb, factors.NumScenarios, Model_Type, Time_Step_Size, Max_Time_Steps);
        }
    }

    /// <summary>
    /// Valuation of callable structured deals using LinearGaussMarkovOptionPricer.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Callable Structured Deal LGM Model Valuation")]
    public class CallableStructuredDealLGMModelValuation : CallableStructuredDealValuation
    {
        private string fModelParametersId;

        public CallableStructuredDealLGMModelValuation()
        {
            Driver_Space_Points = 25;
            Standard_Deviations = 5;
            Integration_Points  = 10;
        }

        public int Driver_Space_Points
        {
            get; set;
        }

        public int Standard_Deviations
        {
            get; set;
        }

        public int Integration_Points
        {
            get; set;
        }

        /// <summary>
        /// ID of the linear Gauss Markov factor that the valuation model will use.
        /// </summary>
        public string Model_Parameters
        {
            get; set;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            if (Driver_Space_Points <= 0)
                errors.Add(ErrorLevel.Error, string.Format("Driver_Space_Points must be greater than zero for {0}.", GetType().Name));

            if (Standard_Deviations <= 0)
                errors.Add(ErrorLevel.Error, string.Format("Standard_Deviations must be greater than zero for {0}.", GetType().Name));

            if (Integration_Points <= 2)
                errors.Add(ErrorLevel.Error, string.Format("Integration_Points must be greater than two for {0}.", GetType().Name));
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            fModelParametersId = string.IsNullOrWhiteSpace(Model_Parameters) ? fForecastCurrency : Model_Parameters;
            factors.Register<LinearGaussMarkovFactor>(fModelParametersId);
        }

        /// <summary>
        /// Create option pricer and other preparation.
        /// </summary>
        protected override InterestRateOptionPricer CreateOptionPricer(PriceFactorList factors)
        {
            CallableStructuredDeal deal = (CallableStructuredDeal)fDeal;
            LinearGaussMarkovFactor lgmPriceFactor = factors.Get<LinearGaussMarkovFactor>(fModelParametersId);
            return new LinearGaussMarkovOptionPricer(deal.Option_Type, fExercises, fItems, fDiscountRate, fForecastRate, fInterestRateVol, fInterestYieldVol, fSurvivalProb, factors.NumScenarios, lgmPriceFactor, Driver_Space_Points, Standard_Deviations, Integration_Points);
        }
    }

    /// <summary>
    /// Serializable and displayable trio of exercise date, settlement date and exercise fee.
    /// </summary>
    [Serializable]
    public class ExerciseProperties : IComparable<ExerciseProperties>, IStringConverter
    {
        public TDate Exercise_Date
        {
            get; set;
        }

        public TDate Settlement_Date
        {
            get; set;
        }

        public Percentage Exercise_Fee
        {
            get; set;
        }

        /// <summary>
        /// Trio are ordered by Exercise_Date.
        /// </summary>
        public int CompareTo(ExerciseProperties other)
        {
            return Exercise_Date.CompareTo(other.Exercise_Date);
        }

        public void FromString(string value)
        {
            Property.SetPropPacked(this, value);
        }

        public override string ToString()
        {
            return Property.GetPropPacked(this);
        }
    }

    /// <summary>
    /// Displayable list of exercise properties.
    /// </summary>
    [Serializable]
    public class ExercisePropertiesList : DisplayableList<ExerciseProperties>
    {
        /// <summary>
        /// Construct from string and sort.
        /// </summary>
        public override void FromString(string value)
        {
            base.FromString(value);
            Sort();
        }

        /// <summary>
        /// Validate settlement dates.
        /// If not sorted then add a warning to errors list and sort.
        /// </summary>
        public void Validate(ErrorList errors, double endDate)
        {
            foreach (ExerciseProperties exercise in this)
            {
                if (exercise.Settlement_Date < exercise.Exercise_Date)
                    errors.Add(ErrorLevel.Error, string.Format("Settlement date must be on or after exercise date {0}", exercise.Exercise_Date));

                if (endDate < exercise.Exercise_Date)
                    errors.Add(ErrorLevel.Warning, string.Format("Exercise dates cannot be after the end date {0} of the underlying deals. Exercise date {1} will be ignored.", new TDate(endDate), exercise.Exercise_Date));
            }

            if (!CalcUtils.IsListSorted<ExerciseProperties>(this))
            {

                errors.Add(ErrorLevel.Warning, "Exercise dates were not in ascending order of date and have been sorted");
                Sort();
            }
        }
    }
}