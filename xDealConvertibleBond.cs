/// <author>
/// Nathalie Rouille
/// </author>
/// <owner>
/// Nathalie Rouille
/// </owner>
/// <summary>
/// Defines the convertible bond deal and its valuation. 
/// Create object to represent conversion, call and put clauses with any 
/// combination of Bermudean or American exercise style.
/// </summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// Specify the scheme used to discretised the PDE.
    /// </summary>
    public enum SchemeType
    {
        Theta_Scheme, SOR, Rannacher, Explicit_Euler
    }

    /// <summary>
    /// Monitor the convertible bond status on the valuation scenario.
    /// </summary>
    public enum ConvertibleExercised
    {
        Not_Exercised, Converted, Early_Redeemed
    }

    /// <summary>
    /// This interface define a Bermudean or American style exercise for a deal.
    /// </summary>
    /// <remarks>
    /// A Bermudean exercise condition has an exercise period equal to 0 and
    /// an American exercise is defined by the start date and its exercise period.
    /// </remarks>
    public interface ICondition : IComparable
    {
        TDate Date
        {
            get; set;
        }

        Period Exercise_Window
        {
            get; set;
        }

        /// <summary>
        /// Returns the end of exercise condition.
        /// </summary>
        TDate GetEndDate();

        /// <summary>
        /// Set a Bermudean exercise condition (exercise period is set to 0).
        /// </summary>
        void SetCondition(TDate date);

        /// <summary>
        /// Set the begining and exercise period of a exercise condition.
        /// </summary>
        void SetCondition(TDate date, Period period);

        /// <summary>
        /// Returns true is the intersection of the condition period is not empty.
        /// </summary>
        bool IsOverlapping(ICondition condition);

        /// <summary>
        /// Convert a string to an exercise condition.
        /// </summary>
        /// <param name="value">starting date of the exercise condition.</param>
        /// <param name="options">array of fields for the exercise condition.</param>
        void ParseCondition(string value, String[] options);

        /// <summary>
        /// Convert an exercise condition to a string.
        /// </summary>
        String AppendProperties();
    }

    /// <summary>
    /// This class implements the ICondition interface.
    /// </summary>
    [Serializable]
    public class TimeCondition : ICondition
    {
        protected TDate fDate = 0;
        protected Period fExercisePeriod = new Period();
        protected TDate fEndPeriod = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TimeCondition()
        {
            fDate           = new TDate();
            fExercisePeriod = new Period();
            fEndPeriod      = 0;
        }

        /// <summary>
        /// Contructor for a Bermudean exercise condition.
        /// </summary>
        public TimeCondition(TDate date)
        {
            SetCondition(date);
        }

        /// <summary>
        /// Construct an exercise condition begining at date and finishing at endDate.
        /// </summary>
        public TimeCondition(TDate date, TDate endDate)
        {
            SetCondition(date, endDate);
        }

        /// <summary>
        /// Contruct an exercise condition begining at date for a specified period.
        /// </summary>
        public TimeCondition(TDate date, Period period)
        {
            SetCondition(date, period);
        }

        public TDate Date
        {
            get { return fDate;           } set { fDate = value; AdjustEndPeriod();           }
        }

        public Period Exercise_Window
        {
            get { return fExercisePeriod; } set { fExercisePeriod = value; AdjustEndPeriod(); }
        }

        /// <summary>
        /// Returns the end date.
        /// </summary>
        public TDate GetEndDate()
        {
            return fEndPeriod;
        }

        /// <summary>
        /// Set the end date.
        /// </summary>
        public void AdjustEndPeriod()
        {
            fEndPeriod = DateAdjuster.Add(fDate, Period.ValueToTerm(fExercisePeriod));
        }

        /// <summary>
        /// Compare by date.
        /// </summary>
        public int CompareTo(Object other)
        {
            ICondition otherCond = (ICondition)other;
            return (int)(Date - otherCond.Date);
        }

        /// <summary>
        /// Set a Bermudean exercise condition (exercise period is set to 0).
        /// </summary>
        public void SetCondition(TDate date)
        {
            fDate           = date;
            fEndPeriod      = date;
            fExercisePeriod = new Period(0);
        }

        /// <summary>
        /// Set the begining and exercise period of a exercise condition.
        /// </summary>
        public void SetCondition(TDate date, Period period)
        {
            fDate = date;
            fExercisePeriod = period;
            AdjustEndPeriod();
        }

        /// <summary>
        /// Set an exercise condition from date to endDate.
        /// </summary>
        public void SetCondition(TDate date, TDate endDate)
        {
            if (date > endDate)
                return;

            fDate = date;
            fEndPeriod = endDate;
            double v = endDate - date;
            fExercisePeriod = new Period(v.ToString() + "d");
        }

        /// <summary>
        /// Returns true is the intersection of the condition period is not empty.
        /// </summary>
        /// <remarks>
        /// 2 Bermudean exercise on the same date is considered overlapping but 
        /// an American exercise ending on the same date as the other condition
        /// is not overlapping.
        /// </remarks>
        public virtual bool IsOverlapping(ICondition other)
        {
            return (Date == other.Date ||
                    (Date < other.Date && fEndPeriod > other.Date) ||
                    (Date > other.Date && Date < other.GetEndDate() ));
        }

        /// <summary>
        /// Convert a string to an exercise condition.
        /// </summary>
        /// <param name="value">starting date of the exercise condition.</param>
        /// <param name="options">array of fields for the exercise condition.</param>
        public virtual void ParseCondition(string value, String[] options)
        {
            if (options.Length == 1)
            {
                Period period = new Period();
                period.FromString(options[0]);
                SetCondition(TDate.ParseInvariant(value), period);
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid number of arguments : {0}, expected 1", options.Length));
            }
        }

        /// <summary>
        /// Convert an exercise condition to a string.
        /// </summary>
        public virtual String AppendProperties()
        {
            return string.Format("{0}={1}\\", Date, Exercise_Window);
        }
    }

    /// <summary>
    /// This class represents a conversion condition, it consists of a time condition plus a optional up and in barrier.
    /// </summary>
    /// <remarks>
    /// A barrier equals to 0 means the conversion is not subject to any barrier condition.
    /// </remarks>
    [Serializable]
    public class ConversionCondition : TimeCondition, ICondition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConversionCondition()
            : base()
        {
            Barrier = 0;
        }

        /// <summary>
        /// Construct a conversion condition.
        /// </summary>
        public ConversionCondition(TDate date, Period period, double barrier)
        {
            SetCondition(date, period, barrier);
        }

        public double Barrier
        {
            get; set;
        }

        /// <summary>
        /// Set a conversion condition.
        /// </summary>
        public void SetCondition(TDate date, Period period, double barrier)
        {
            base.SetCondition(date, period);
            Barrier = barrier;
        }

        /// <summary>
        /// Convert a string to an exercise condition.
        /// </summary>
        /// <param name="value">starting date of the exercise condition.</param>
        /// <param name="options">array of fields for the exercise condition.</param>
        public override void ParseCondition(string value, String[] options)
        {
            if (options.Length == 2)
            {
                Period period = new Period();
                period.FromString(options[0]);
                SetCondition(TDate.ParseInvariant(value), period, Convert.ToDouble(options[1], CultureInfo.InvariantCulture));
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid number of arguments : {0}, expected 2", options.Length));
            }
        }

        /// <summary>
        /// Convert an exercise condition to a string.
        /// </summary>
        public override String AppendProperties()
        {
            return string.Format("{0}={1},{2}\\", Date, Exercise_Window, Barrier);
        }
    }

    /// <summary>
    /// This class represents a callable or puttable condition, it consist of a conversion condition plus a strike price.
    /// </summary>
    [Serializable]
    public class RedemptionCondition : ConversionCondition, ICondition
    {
        // ENHANCEMENT : parisian condition can be added here
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RedemptionCondition()
            : base()
        {
            Strike_Amount = 0;
        }

        /// <summary>
        /// Construct a redemption condition.
        /// </summary>
        public RedemptionCondition(TDate date, Period period, double barrier, double strike)
        {
            SetCondition(date, period, barrier, strike);
        }

        public double Strike_Amount
        {
            get; set;
        }

        /// <summary>
        /// Set a redemption condition.
        /// </summary>
        public void SetCondition(TDate date, Period period, double barrier, double strike)
        {
            base.SetCondition(date, period, barrier);
            Strike_Amount = strike;
        }

        /// <summary>
        /// Convert a string to an exercise condition.
        /// </summary>
        /// <param name="value">starting date of the exercise condition.</param>
        /// <param name="options">array of fields for the exercise condition.</param>
        public override void ParseCondition(string value, String[] options)
        {
            if (options.Length == 3)
            {
                Period period = new Period();
                period.FromString(options[0]);
                SetCondition(TDate.ParseInvariant(value), period, Convert.ToDouble(options[1], CultureInfo.InvariantCulture), Convert.ToDouble(options[2], CultureInfo.InvariantCulture));
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid number of arguments : {0}, expected 3", options.Length));
            }
        }

        /// <summary>
        /// Convert an exercise condition to a string.
        /// </summary>
        public override String AppendProperties()
        {
            return string.Format("{0}={1},{2},{3}\\", Date, Exercise_Window, Barrier, Strike_Amount);
        }
    }

    /// <summary>
    /// This class defines a list of element implementing the interface ICondition.
    /// </summary>
    [Serializable]
    public class ConditionList<T> : DisplayableList<T>, IStringConverter
        where T : ICondition, new()
    {
        /// <summary>
        ///  Return the last element of the list.
        /// </summary>
        public T GetLastElement()
        {
            if (Count == 0)
                return default(T);
            else
                return this[this.Count - 1];
        }

        /// <summary>
        /// Return exercise condition for the specified time, null otherwise.
        /// </summary>
        public T GetExerciseCondition(double time)
        {
            int index = FindIndex(
                delegate(T cond)
                {
                    return (cond.GetEndDate() >= time && cond.Date <= time);
                });

            // No exercise condition for this period
            if (index < 0)
                return default(T);

            // Correction : end of a period can coincide with the begining of the next one
            // in that case, use the next one
            if (index + 1 < Count && this[index + 1].Date == time)
            {
                ++index;
            }

            return this[index];
        }

        /// <summary>
        /// Return exercise conditions occuring on or after the specified time, null otherwise.
        /// </summary>
        public ConditionList<T> GetExerciseConditionAfterTime(double time)
        {
            ConditionList<T> list = new ConditionList<T>();
            foreach (T cond in this)
            {
                if (cond.GetEndDate() >= time)
                    list.Add(cond);
            }

            return list;
        }

        /// <summary>
        /// Add the list of dates without duplicates that are on or after t and sort.
        /// </summary>
        public void MergeDates(double t, IEnumerable<double> dates)
        {
            List<double> listStartEnd = RetrieveAllDates();
            foreach (double date in dates)
            {
                if (date >= t)
                {
                    int index = listStartEnd.IndexOf(date);
                    if (index < 0)
                    {
                        T element = new T();
                        element.SetCondition(date);
                        Add(element);
                    }
                }
            }
            Sort();
        }

        /// <summary>
        /// Retrieve all begin date and end without duplicates.
        /// </summary>
        public List<double> RetrieveAllDates()
        {
            List<double> result = new List<double>();
            if (Count > 0)
            {
                foreach (T condition in this)
                {
                    if (result.IndexOf(condition.Date) < 0)
                    {
                        result.Add(condition.Date);
                    }
                    if (condition.Exercise_Window > 0 && result.IndexOf(condition.GetEndDate()) < 0)
                    {
                        result.Add(condition.GetEndDate());
                    }
                }
            }
            result.Sort();
            return result;
        }

        /// <summary>
        /// Return all conditions in the same period as target.
        /// </summary>
        public ConditionList<T> GetConditionInSamePeriod(T target)
        {
            ConditionList<T> result = new ConditionList<T>();
            foreach (T condition in this)
            {
                if (condition.IsOverlapping(target))
                    result.Add(condition);
            }

            return result;
        }

        /// <summary>
        /// Validate the exercise condition list.
        /// </summary>
        public bool Validate(double issueDate, double maturityDate, ref String message)
        {
            if (Count == 0)
                return true;

            Sort();
            for (int i = 1; i < Count; ++i)
            {
                // List of element should not overlap
                if (this[i].IsOverlapping(this[i - 1]))
                {
                    message += string.Format("\nExercices {0} and {1} correspond to the same period", this[i - 1].AppendProperties(), this[i].AppendProperties());
                    return false;
                }
            }

            if ((this[0].Date < issueDate || this.GetLastElement().GetEndDate() > maturityDate))
            {
                message += String.Format("\nExercice dates must lie between {0} and {1}", issueDate, maturityDate);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert a string into a condition list.
        /// </summary>
        public override void FromString(string value)
        {
            Clear();

            int len = value.Length;
            int i0 = 0;
            int i = 0;
            while (i < len)
            {
                // Scan along till we find a \ delimiting the end of an entry or the end of the string
                while ((i < len) && (value[i] != '\\'))
                {
                    i++;
                }

                // Parse the current section of string into a rate entry
                string entryStr = value.Substring(i0, i - i0);
                int pEq = entryStr.IndexOf('=');
                if (pEq > 0)
                {
                    T t = new T();
                    String[] options = entryStr.Substring(pEq + 1).Split(',');
                    t.ParseCondition(entryStr.Substring(0, pEq), options);
                    Add(t);
                }

                // Update the string indexes
                i++;
                i0 = i;
            }

            Sort();
        }

        /// <summary>
        /// Convert a condition list into a string.
        /// </summary>
        public override string ToString()
        {
            String res = String.Empty;
            foreach (T t in this)
            {
                res += t.AppendProperties();
            }
            return res;
        }
    }

    /// <summary>
    /// This class specializes the condition list to a conversion condition list.
    /// </summary>
    [Serializable]
    public class ConversionList : ConditionList<ConversionCondition>
    {
    }

    /// <summary>
    /// This class specializes the condition list to a redemption condition list.
    /// </summary>
    [Serializable]
    public class RedemptionList : ConditionList<RedemptionCondition>
    {
    }

    /// <summary>
    /// Base class for convertible bond deal.
    /// </summary>
    [Serializable]
    public abstract class ConvertibleBase : DealCreditBase
    {
        [NonMandatory]
        [LocationStringsFormAttribute]
        [Editor(typeof(ModalEditor), typeof(UITypeEditor))]
        public string Calendars
        {
            get { return GetCalendarNames(0); }
            set { SetCalendarNames(0, value); }
        }

        public BuySell Buy_Sell
        {
            get; set;
        }

        public TDate Issue_Date
        {
            get; set;
        }

        public TDate Bond_Maturity_Date
        {
            get; set;
        }

        public double Bond_Notional
        {
            get; set;
        }

        public Period Coupon_Interval
        {
            get; set;
        }

        public DayCount Accrual_Day_Count
        {
            get; set;
        }

        [NonMandatory]
        public Amortisation Amortisation
        {
            get; set;
        }

        public Percentage Coupon_Rate
        {
            get; set;
        }

        public double Redemption_Amount
        {
            get; set;
        }

        public string Equity
        {
            get; set;
        }

        public string Equity_Volatility
        {
            get; set;
        }

        public double Conversion_Ratio
        {
            get; set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected ConvertibleBase()
        {
            Amortisation      = new Amortisation();
            Equity            = string.Empty;
            Equity_Volatility = string.Empty;
        }

        /// <summary>
        /// Deal end date.
        /// </summary>
        public override double EndDate()
        {
            return Bond_Maturity_Date;
        }

        /// <summary>
        /// Validate deal properties.
        /// </summary>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            if (Bond_Notional < CalcUtils.MinAssetPrice)
                AddToErrors(errors, string.Format("Bond Notional must be at least {0}", CalcUtils.MinAssetPrice));

            CalcUtils.ValidateDates(errors, Issue_Date, Bond_Maturity_Date, true, "issue", "bond maturity");

            Amortisation.Validate(errors);
        }

        /// <summary>
        /// Return a list of key dates and periods for the deal.
        /// </summary>
        public virtual ConditionList<TimeCondition> GetKeyDate(double t)
        {
            ConditionList<TimeCondition> result = new ConditionList<TimeCondition>();

            result.Add(new TimeCondition(Issue_Date));
            result.Add(new TimeCondition(Bond_Maturity_Date));

            return result;
        }

        /// <summary>
        /// Return a list of key levels for the deal.
        /// </summary>
        public virtual List<double> GetKeyLevel(double t)
        {
            return new List<double>();
        }

        /// <summary>
        /// Gets the equity deal helper.
        /// </summary>
        public SingleEquityDealHelper GetEquityDealHelper()
        {
            return new SingleEquityDealHelper(Equity, Equity_Volatility, Currency);
        }
    }

    /// <summary>
    /// This class defines a convertible that supports any combination of 
    /// Bermudean/American style exercise for call, put and conversion clauses.
    /// </summary>
    [Serializable]
    public class ConvertibleBond : ConvertibleBase
    {
        protected ConversionList fConversionExercise = new ConversionList();
        protected RedemptionList fCallExercise = new RedemptionList();
        protected RedemptionList fPutExercise = new RedemptionList();

        public ConversionList Conversion_Exercise
        {
            get { return fConversionExercise; } set { fConversionExercise = value; }
        }

        public RedemptionList Call_Exercise
        {
            get { return fCallExercise;       } set { fCallExercise = value;       }
        }

        public RedemptionList Put_Exercise
        {
            get { return fPutExercise;        } set { fPutExercise = value;        }
        }

        /// <summary>
        /// Return a list of key dates and periods for the deal.
        /// </summary>
        public override ConditionList<TimeCondition> GetKeyDate(double t)
        {
            ConditionList<TimeCondition> unionList = new ConditionList<TimeCondition>();
            unionList.AddRange(fConversionExercise.Cast<TimeCondition>());
            unionList.AddRange(fCallExercise.Cast<TimeCondition>());
            unionList.AddRange(fPutExercise.Cast<TimeCondition>());

            unionList = unionList.GetExerciseConditionAfterTime(t);

            unionList.AddRange(base.GetKeyDate(t));
            unionList.Sort();

            // Merge the time condition with an exercise period from conversion, call and put exercise
            List<TimeCondition> unionPeriod = new List<TimeCondition>();
            foreach (TimeCondition timeCondition in unionList)
            {
                if (timeCondition.Exercise_Window > 0)
                {
                    int index = unionPeriod.FindIndex(
                        delegate(TimeCondition other)
                        {
                            return other.Date == timeCondition.Date;
                        });

                    if (index >= 0 && unionPeriod[index].Exercise_Window < timeCondition.Exercise_Window)
                    {
                        unionPeriod[index].Exercise_Window = timeCondition.Exercise_Window;
                    }
                    else if (index < 0)
                    {
                        // Floor the element if the begining of the period is before t
                        double begin = Math.Max(t, timeCondition.Date);
                        unionPeriod.Add(new TimeCondition(begin, timeCondition.GetEndDate()));
                    }
                }
            }

            ConditionList<TimeCondition> aggregatedTime = new ConditionList<TimeCondition>();

            int i = 0;
            while (i < unionPeriod.Count)
            {
                TDate begin = Math.Max(unionPeriod[i].Date, t);
                TDate end = unionPeriod[i].GetEndDate();

                int j = i + 1;
                TDate endDate = end;
                while (j < unionPeriod.Count && unionPeriod[j].Date <= endDate)
                {
                    // If the next time condition is overlapping the current time condition, then merge by extending the current time condition
                    endDate = Math.Max(endDate, unionPeriod[j].GetEndDate());

                    ++j;
                }
                aggregatedTime.Add(new TimeCondition(begin, endDate));
                i = j;
            }

            // Add the individual date (ie no exercise period)
            List<double> dateList = unionList.RetrieveAllDates();
            aggregatedTime.MergeDates(t, dateList);

            return aggregatedTime;
        }

        /// <summary>
        /// Return a list of key levels for the deal.
        /// </summary>
        public override List<double> GetKeyLevel(double t)
        {
            ConditionList<RedemptionCondition> unionList = new ConditionList<RedemptionCondition>();
            unionList.AddRange(fCallExercise);
            unionList.AddRange(fPutExercise);

            unionList = unionList.GetExerciseConditionAfterTime(t);

            ConditionList<ConversionCondition> conversionList = new ConditionList<ConversionCondition>();
            conversionList.AddRange(fConversionExercise);
            conversionList = conversionList.GetExerciseConditionAfterTime(t);

            var query = (from condition in unionList
                         select condition.Barrier).Union
                         (from condition in unionList
                          select condition.Strike_Amount).Union
                              (from condition in conversionList
                               select condition.Barrier).Union
                                    (base.GetKeyLevel(t));

            List<double> aggregatedLevels = query.ToList();

            // Remove level equals to zero because it is a convention for "no barrier"
            aggregatedLevels.Remove(0.0);
            aggregatedLevels.Sort();

            return aggregatedLevels;
        }

        /// <summary>
        /// Validates the deal parameters.
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="errors"></param>
        /// <param name="Errors">Error list</param>
        public override void Validate(ICalendarData calendar, ErrorList errors)
        {
            base.Validate(calendar, errors);

            // Check the exercise conditions are valid
            // Check exercise date and conversion date are prior to maturity and after issue date
            // Sort lists
            String errorMessage = String.Empty;
            fConversionExercise.Validate(Issue_Date, Bond_Maturity_Date, ref errorMessage);
            fCallExercise.Validate(Issue_Date, Bond_Maturity_Date, ref errorMessage);
            fPutExercise.Validate(Issue_Date, Bond_Maturity_Date, ref errorMessage);

            if (errorMessage.Length != 0)
                AddToErrors(errors, errorMessage);

            // Check call and put strike are consistent (Kp <= Kc)
            List<RedemptionCondition> conditionInSameDateRange = new List<RedemptionCondition>();
            foreach (RedemptionCondition putElement in fPutExercise)
            {
                // find call exercise for the same period as the current put exercise
                conditionInSameDateRange = fCallExercise.GetConditionInSamePeriod(putElement);

                foreach (RedemptionCondition callElement in conditionInSameDateRange)
                {
                    if (callElement.Strike_Amount < putElement.Strike_Amount)
                        AddToErrors(errors, String.Format("Put strike for period [{0},{1}] is greater than call strike", putElement.Date, putElement.GetEndDate()));
                }
            }
        }
    }

    /// <summary>
    /// This class defines a european convertible bond with optional 
    /// or mandatory conversion at maturity.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Convertible Bond with European Conversion")]
    public class EuropeanConvertible : ConvertibleBase
    {
        public YesNo Is_Mandatory_Conversion
        {
            get; set;
        }
    }

    /// <summary>
    /// Base class for convertible bond valuation.
    /// </summary>
    [Serializable]
    public abstract class ConvertibleBaseValuation : CreditBaseValuation
    {
        [NonSerialized]
        protected DateList fPayDates = null; // payment dates
        [NonSerialized]
        protected double[] fAccruals = null; // accrual periods
        [NonSerialized]
        protected double[] fPrincipals = null;
        [NonSerialized]
        protected double fFinalPrincipal = 0;

        [NonSerialized]
        public double[] fDatePoints = null; // date points mesh
        [NonSerialized]
        public double[] fTimePoints = null; // time points relative to the base date in year fraction
        [NonSerialized]
        public double[] fAccruedCoupon = null;

        protected const String PrefixErrorMessage = "Convertible Bond Valuation : ";

        [NonSerialized]
        protected int fAmericanTimeStep = 0;
        [NonSerialized]
        protected int fTimeStep = 0; // period in days
        [NonSerialized]
        protected double fDecayFactorHazardRate = 0;
        [NonSerialized]
        protected double fLimitHazardRate = 0;
        [NonSerialized]
        protected IInterestRate DF = null;
        [NonSerialized]
        protected IFxRate X = null;
        [NonSerialized]
        protected ISurvivalProb SP = null;
        [NonSerialized]
        protected RecoveryRate RR = null;
        [NonSerialized]
        protected CreditRating CR = null;
        [NonSerialized]
        protected IAssetPrice fEquityPrice = null;
        [NonSerialized]
        protected ISpotProcessVol fEquityVol = null;
        [NonSerialized]
        protected double fJumpLevel = 0;
        [NonSerialized]
        protected PDESolver fSolver = null;
        [NonSerialized]
        protected double[] fCouponAmount = null;

        /// <summary>
        /// Default construtor, set properties to default values.
        /// </summary>
        protected ConvertibleBaseValuation()
        {
            Space_Step_Size = 0.1;          // expressed in ln already
            Time_Step       = new Period("1M");
        }

        public SchemeType Numerical_Scheme_Type
        {
            get; set;
        }

        public double Space_Step_Size
        {
            get; set;
        }

        public Period Time_Step
        {
            get; set;
        }

        /// <summary>
        /// Returns whether the deal requires full path dependent valuation
        /// or whether quadratic grid pricing is an adequate estimate of
        /// its value. (used in hybrid Monte Carlo)
        /// </summary>
        public override bool FullPricing()
        {
            return true;
        }

        /// <summary>
        /// Prepare for valuation anything that is not dependent upon the scenario.
        /// </summary>
        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            base.PreCloneInitialize(factors, baseTimes, resultsRequired);

            ConvertibleBase deal = (ConvertibleBase)Deal;

            DateGenerationRequest request = new DateGenerationRequest
            {
                RequiresPayDates = true,
                RequiresYearFractions = true,
                RequiresPrincipals = true
            };

            DateGenerationParams dateGenerationParams = new DateGenerationParams
            {
                EffectiveDate = deal.Issue_Date,
                MaturityDate = deal.Bond_Maturity_Date,
                CouponPeriod = deal.Coupon_Interval,
                AccrualCalendar = deal.GetHolidayCalendar(),
                AccrualDayCount = deal.Accrual_Day_Count,
                Principal = deal.Bond_Notional, 
                Amortisation = deal.Amortisation
            };

            DateGenerationResults dateGenerationResults = CashflowGeneration.GenerateCashflowDateAndValueLists(request, dateGenerationParams);

            fPayDates = dateGenerationResults.PayDates;
            fAccruals = dateGenerationResults.AccrualYearFractions;
            fPrincipals = dateGenerationResults.Principals;
            fFinalPrincipal = dateGenerationResults.FinalPrincipal;

            // Initialize the coupon amout for each payment date
            fCouponAmount = new double[fPayDates.Count];
            double principal = deal.Bond_Notional;

            for (int i = 0; i < fPayDates.Count; ++i)
            {
                // Find the current principal
                if ((deal.Amortisation) != null && (deal.Amortisation.Count > 0))
                {
                    principal = deal.Amortisation.GetPrincipal(deal.Bond_Notional, fPayDates[i]);
                }

                fCouponAmount[i] = deal.Coupon_Rate * fAccruals[i] * principal;
            }

            fTimeStep = Period.ValueToTerm(Time_Step).ToDays();
            if (fAmericanTimeStep <= 0)
            {
                fAmericanTimeStep = fTimeStep;
            }

            // Initilize the time mesh
            GenerateTimeGrid(factors.BaseDate);

            // Add the time mesh to the valuation time grid
            foreach (double date in fDatePoints)
            {
                fT.Add(date, true);
            }

            // Cache hazard rate parameters to compute the hazard rate as a function of the stock price
            IssuerHazardRateParameters hazardRateParameters = factors.Get<IssuerHazardRateParameters>(deal.Name);

            fDecayFactorHazardRate = hazardRateParameters.Decay_Factor_Hazard_Rate;
            fLimitHazardRate       = hazardRateParameters.Limit_Hazard_Rate;
        }

        /// <summary>
        /// Cache price factors.
        /// </summary>
        public virtual void PreValue(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            ConvertibleBase deal = (ConvertibleBase)fCreditBaseDeal;

            DF = DiscountRate.Get(factors, InterestRateUtils.GetRateId(deal.Discount_Rate, fCreditBaseDeal.Currency));
            X  = factors.GetInterface<IFxRate>(fCreditBaseDeal.Currency);
            SP = factors.GetInterface<ISurvivalProb>(fCreditBaseDeal.Name);
            if (Respect_Default == YesNo.Yes)
            {
                RR = factors.Get<RecoveryRate>(string.IsNullOrEmpty(fCreditBaseDeal.Recovery_Rate) ? fCreditBaseDeal.Name : fCreditBaseDeal.Recovery_Rate);
                CR = factors.Get<CreditRating>(fCreditBaseDeal.Name);
            }
            else
            {
                RR = null;
                CR = null;
            }

            deal.GetEquityDealHelper().PreValueAsset(out fEquityPrice, out fEquityVol, factors);

            // validation of the deal parameters that cannot be done before retrieving the factor
            var equityPrice = (IEquityPrice)fEquityPrice;
            if (equityPrice.GetIssuer() != deal.Name)
            {
                throw new AnalyticsException(String.Format(PrefixErrorMessage + "Equity issuer {0} and bond issuer {1} must be the same", equityPrice.GetIssuer(), deal.Name.ToString()));
            }

            fJumpLevel = equityPrice.GetJumpLevel();

            // Create the solver and space mesh objects
            fSolver = (PDESolver)Activator.CreateInstance(SolverType());
        }

        /// <summary>
        /// Register price factors.
        /// </summary>
        public override void RegisterFactors(PriceFactorList factors, ErrorList errors)
        {
            base.RegisterFactors(factors, errors);

            ConvertibleBase deal = (ConvertibleBase)fCreditBaseDeal;

            // Register the equity and its vol
            deal.GetEquityDealHelper().RegisterFactors(factors, errors);

            // Register hazard rate parameters specific to this issuer
            factors.Register<IssuerHazardRateParameters>(deal.Name);
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            if (Space_Step_Size < 0)
                throw new AnalyticsException(PrefixErrorMessage + "Model parameter space step should be a positive integer");

            // Stability condition of the explicit euler scheme
            if (Numerical_Scheme_Type == SchemeType.Explicit_Euler)
            {
                if (Time_Step / (Space_Step_Size * Space_Step_Size) > 0.5)
                    throw new AnalyticsException(PrefixErrorMessage + "Explicit Scheme is conditionally stable, set model parameters such as dt/(dx)^2 <= 1/2");
            }
        }

        public override void Value(ValuationResults valuationResults, PriceFactorList factors, BaseTimeGrid baseTimes)
        {
            PreValue(factors, baseTimes, valuationResults.ResultsRequired());

            TimeGridIterator tgi = new TimeGridIterator(fT);

            var result = valuationResults.Profile;
            var cash = valuationResults.Cash;

            using (var cache = Vector.Cache(factors.NumScenarios))
            {
                // used to flag different states of the bond. Keeping them separate is much
                // more convenient than combining them.
                var notExercised  = cache.Get(1);
                var earlyRedeemed = cache.GetClear();
                var converted = cache.GetClear();

                double baseDate = factors.BaseDate;

                var deal = (ConvertibleBase)fCreditBaseDeal;

                double conversionRatio = deal.Conversion_Ratio;
                double sign = (deal.Buy_Sell == BuySell.Buy) ? +1 : -1;
                double principal = deal.Bond_Notional;

                // these are just storage to use within the loop
                var underlyingSpot = cache.Get();

                var pv = cache.Get();

                int scenariosLeft = factors.NumScenarios;
                bool allScenariosExercised = false;
                var defaultedBeforeBaseDate = CreditRating.DefaultedBeforeBaseDate(CR, baseDate);

                // Calculation loop
                while (tgi.Next())
                {
                    pv.Clear();
                    if (defaultedBeforeBaseDate)
                    {
                        result.AppendVector(tgi.Date, pv);
                        break;
                    }

                    // Find the current principal
                    if ((deal.Amortisation) != null && (deal.Amortisation.Count > 0))
                        principal = deal.Amortisation.GetPrincipal(deal.Bond_Notional, tgi.Date);

                    underlyingSpot.Assign(fEquityPrice.Get(tgi.T) / X.Get(tgi.T));

                    // this is the PV if the CB has been converted (simply the PV of the underlying)
                    pv.AssignConditional(converted, conversionRatio * underlyingSpot, pv);

                    // this is the PV if the CB has been early redeemed, just zero
                    pv.AssignConditional(earlyRedeemed, 0, pv);

                    if (allScenariosExercised)
                    {
                        result.AppendVector(tgi.Date, sign * pv * X.Get(tgi.T));
                        continue;
                    }

                    // this assigns the PV if the CB has not been exercised
                    GetPvForNotExercised(tgi.Date, tgi.T, pv, cash, converted, notExercised, earlyRedeemed, conversionRatio,
                        underlyingSpot, principal, sign, factors, ref scenariosLeft);

                    if (scenariosLeft == 0)
                        allScenariosExercised = true;

                    result.AppendVector(tgi.Date, sign * pv * X.Get(tgi.T));
                }
            }

            // After maturity
            result.Complete(fT);
        }

        /// <summary>
        /// Type of deal that the model can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(ConvertibleBase);
        }

        /// <summary>
        /// Initialise the payoff of the convertible at maturity.
        /// Conversion into stock is optional
        /// </summary>
        public virtual ConvertibleExercised Payoff(Mesh mesh)
        {
            ConvertibleBase deal = (ConvertibleBase)Deal;

            // Compute the redemption value at maturity
            double redemptionBond = deal.Redemption_Amount;

            // Eventually add coupons at maturity
            if (deal.Bond_Maturity_Date == fPayDates.Last())
                redemptionBond += fCouponAmount.Last();

            int j = mesh.Space_End_Index;
            double conversionValue = 0;

            while (j >= 0)
            {
                conversionValue = deal.Conversion_Ratio * mesh.ExpSpacePoints[j];
                if (conversionValue < redemptionBond)
                    break;

                fSolver.UNew[j] = conversionValue;
                --j;
            }

            while (j >= 0)
            {
                fSolver.UNew[j] = redemptionBond;
                --j;
            }

            // Set up the convertible status at maturity
            return (fSolver.UNew[mesh.Spot_Index] == conversionValue) ? ConvertibleExercised.Converted : ConvertibleExercised.Early_Redeemed;
        }

        /// <summary>
        /// Compute the free boundary for an european convertible and update its status. 
        /// </summary>
        public virtual ConvertibleExercised FreeBoundary(int dateIndex, Mesh mesh)
        {
            // Eventually add the coupon to the rolled back value
            int couponDateIndex = fPayDates.IndexOf(fDatePoints[dateIndex]);
            if (couponDateIndex >= 0)
            {
                for (int i = mesh.Space_End_Index; i >= 0; --i)
                {
                    fSolver.UNew[i] += fCouponAmount[couponDateIndex];
                }
            }

            // No exercise is permitted for european convertible
            return ConvertibleExercised.Not_Exercised;
        }

        /// <summary>
        /// Return the type of PDE scheme to use.
        /// </summary>
        public virtual Type SolverType()
        {
            switch (Numerical_Scheme_Type)
            {
                case SchemeType.SOR:
                    return typeof(SOR);
                case SchemeType.Theta_Scheme:
                    return typeof(ThetaScheme);
                case SchemeType.Explicit_Euler:
                    return typeof(ExplicitEuler);
                case SchemeType.Rannacher:
                    return typeof(Rannacher);
                default:
                    throw new AnalyticsException(PrefixErrorMessage + "PDE scheme doesn't exist"); // Sanity check
            }
        }

        /// <summary>
        /// Return the valuation of the convertible bond given by the PDE solver.
        /// </summary>
        public double GetPV(Mesh mesh)
        {
            return fSolver.UOld[mesh.Spot_Index];
        }

        /// <summary>
        /// Allocate or reallocate memory for data arrays according to the space mesh size.
        /// </summary>
        public void Initialise(bool reBuild, int size, int endIndex)
        {
            if (reBuild)
            {
                // Initialise the PDE solver
                fSolver.Initialise(size);
            }

            fSolver.SetEndIndex(endIndex);
        }

        /// <summary>
        /// Create the time mesh (relative to the base date of the deal) and date mesh and the accrued coupon amount array.
        /// </summary>
        public void GenerateTimeGrid(double baseDate)
        {
            ConvertibleBase deal = (ConvertibleBase)Deal;
            double maturityDate = deal.Bond_Maturity_Date;

            // If maturity is before base date, no time mesh to create (sanity check)
            if (maturityDate < baseDate)
                return;

            // Retrieve key dates for the deal
            ConditionList<TimeCondition> keyDates = deal.GetKeyDate(baseDate);

            // Add time grid and coupon payment dates
            keyDates.MergeDates(baseDate, fT);
            keyDates.MergeDates(baseDate, fPayDates);

            double lastEnd = baseDate;
            List<double> datePoints = new List<double>();
            List<double> timePoints = new List<double>();
            datePoints.Add(lastEnd);
            timePoints.Add(0);

            for (int i = 0; i < keyDates.Count; ++i)
            {
                double ti = keyDates[i].Date;

                // For period without American exercise, create a time mesh with the coarser time step
                AddIntermediatePoints(datePoints, timePoints, baseDate, lastEnd, ti, fTimeStep);
                lastEnd = Math.Max(lastEnd, ti);

                // For period with American exercise, use a finer grid
                if (keyDates[i].Exercise_Window > 0)
                {
                    lastEnd = keyDates[i].GetEndDate();
                    AddIntermediatePoints(datePoints, timePoints, baseDate, ti, lastEnd, fAmericanTimeStep);
                }
            }

            if (keyDates.Count == 0)
            {
                datePoints.Add(lastEnd);
            }

            AddIntermediatePoints(datePoints, timePoints, baseDate, lastEnd, maturityDate, fTimeStep);

            datePoints.Sort();
            timePoints.Sort();

            fDatePoints = datePoints.ToArray();
            fTimePoints = timePoints.ToArray();

            // Compute the accrued coupon for each time points of the mesh
            fAccruedCoupon = new double[fDatePoints.Count()];
            int nextCouponIndex = 0;
            double previousCouponDate = 0;
            if (fCouponAmount != null && fCouponAmount.Count() > 0)
            {
                for (int i = 0; i < fDatePoints.Count(); ++i)
                {
                    // Search PayDate such as
                    // PayDate[next -1] < date <= PayDate[next]
                    while (nextCouponIndex < fPayDates.Count)
                    {
                        if (fDatePoints[i] <= fPayDates[nextCouponIndex])
                            break;

                        ++nextCouponIndex;
                    }

                    if (nextCouponIndex == fPayDates.Count)
                    {
                        // Strictly after the last payment date
                        fAccruedCoupon[i] = 0;
                    }
                    else
                    {
                        if (nextCouponIndex == 0)
                        {
                            // Before or on the first coupon date
                            previousCouponDate = deal.Issue_Date;
                        }
                        else
                        {
                            // Between 2 payment dates
                            previousCouponDate = fPayDates[nextCouponIndex - 1];
                        }
                        fAccruedCoupon[i] = fCouponAmount[nextCouponIndex] * CalcUtils.DaysToYears(fDatePoints[i] - previousCouponDate) / CalcUtils.DaysToYears(fPayDates[nextCouponIndex] - previousCouponDate);
                    }
                }
            }
        }

        /// <summary>
        /// Return the time of default for the underlying obligor.
        /// </summary>
        /// <remarks>TODO: this should be a vector method and call creditRating.GetValue(double t, Vector vout)</remarks>
        protected static double DefaultTime(CreditRating creditRating)
        {
            if (creditRating != null)
            {
                if (creditRating.GetPriceModel() != null)
                    throw new AnalyticsException("Convertible bond valuation does not support CreditRatings with factor models");
                if (creditRating.Rating == CreditRating.Default)
                    return 0.0;
            }
            return CreditRating.TimeOfDefault.Never;
        }

        /// <summary>
        /// This calculates the PV, convertible status, and cashflows for a single scenario and date.
        /// </summary>
        /// <param name="scenario">The index of the current scenario to value.</param>
        /// <param name="values">The Values container holding mesh and Price Factor values for all the scenarios.</param>
        /// <param name="valueDate">the date of valuation</param>
        /// <param name="recoveryValue">the value of redemption</param>
        /// <param name="conversionRatio">the ratio of bond to underlying when converted</param>
        /// <param name="convertibleStatus">The status of the convertible bond.</param>
        /// <param name="sign">1 if buying and -1 if selling.</param>
        /// <param name="pv">The pv to assign to.</param>
        /// <param name="cash">cashflows to update</param>
        protected void GetPvForNotExercisedAndNotDefaultedScalar(int scenario, ScenarioValues values, double valueDate,
                double recoveryValue, double conversionRatio, out ConvertibleExercised convertibleStatus, double sign,
                out double pv, out double cash)
        {
            fSolver.Reset();
            Mesh mesh = values.Meshs[scenario].Item2;

            // Set the solution array to the payoff at maturity
            convertibleStatus = Payoff(mesh);
            int vi = values.NumValues - 1;
            int ti = vi + values.TimeBeginIndex;
            double dt = values.TimePoints[ti] - values.TimePoints[ti - 1];

            // Prepare the solver data at maturity
            fSolver.PrepareAtMaturity(scenario, mesh, dt, vi, values.ShortRates, values.DividendRates, values.StraightBonds, values.Volatilities,
                                      values.HazardRateSurfaces, recoveryValue, conversionRatio, fJumpLevel);

            for (; vi > 0; --vi)
            {
                ti = vi + values.TimeBeginIndex;
                dt = values.TimePoints[ti] - values.TimePoints[ti - 1];

                // Compute the rolled back value of the convertible at previous time step
                fSolver.SolveForTimeStep(scenario, mesh, dt, vi, values.ShortRates, values.DividendRates, values.StraightBonds, values.Volatilities,
                                            values.HazardRateSurfaces, recoveryValue, conversionRatio, fJumpLevel);

                // Apply the free boundary conditions
                convertibleStatus = FreeBoundary(ti - 1, mesh);

                // Shift solver data and solution for next time step
                fSolver.Shift();
            }

            // Retrieve the dirty price given by the PDE solver at valuation date
            pv = GetPV(mesh);

            // Collect cash in case of early redemption or coupon payments
            int couponIndex = fPayDates.IndexOf(valueDate);

            cash = 0;

            if (convertibleStatus == ConvertibleExercised.Converted)
                return;

            if (convertibleStatus == ConvertibleExercised.Early_Redeemed)
            {
                // Accumulate the strike
                cash = sign * pv;
                return;
            }

            if (couponIndex >= 0)
            {
                // Accumulate coupon at payment date
                cash = sign * fCouponAmount[couponIndex];
            }
        }

        /// <summary>
        /// Discretise the interval ]begin,end] using a specified time step.
        /// </summary>
        protected void AddIntermediatePoints(List<double> datePoints, List<double> timePoints, double baseDate, double begin, double end, double timeStep)
        {
            // Do nothing if begin >= end or if end already exist
            if (begin >= end && datePoints.IndexOf(end) >= 0)
                return;

            // Add points begin (excluded) up to end (included)
            double currentPoint = begin + timeStep;
            while (currentPoint < end)
            {
                datePoints.Add(currentPoint);
                timePoints.Add(CalcUtils.DaysToYears(currentPoint - baseDate));
                currentPoint += timeStep;
            }

            datePoints.Add(end);
            timePoints.Add(CalcUtils.DaysToYears(end - baseDate));
        }

        /// <summary>
        /// Return the index of the date in the date points, -1 if not found.
        /// </summary>
        protected int FindIndex(double date)
        {
            int index = 0;
            while (index < fDatePoints.Count())
            {
                if (fDatePoints[index] == date)
                {
                    return index;
                }
                ++index;
            }

            return -1;
        }

        /// <summary>
        /// Set the bond price using the survival probability derived form the parametric functions of the hazard rate.
        /// </summary>
        protected void BondPrice(ScenarioValues values, VectorScopedCache.Scope cache, Vector powers)
        {
            ConvertibleBase deal = (ConvertibleBase)Deal;

            double tMaturity = values.TimePoints.Last();
            double tValue       = values.TimePoints[values.TimeBeginIndex];
            double valueDate    = fDatePoints[values.TimeBeginIndex];
            double maturityDate = fDatePoints.Last();
            double baseDate      = fDatePoints[0];
            double issueDate    = deal.Issue_Date;
            double tIssue        = CalcUtils.DaysToYears(issueDate - baseDate);

            double principal    = deal.Bond_Notional;

            Vector pv = cache.Get();
            Vector pStart = cache.Get();
            Vector pEnd = cache.Get();

            Amortisation amortisation = deal.Amortisation;
            bool haveAmortisation = (amortisation != null) && (amortisation.Count > 0);

            Vector multiplierMaturity = cache.Get(VectorMath.Exp(-fLimitHazardRate * (1 - powers) * (tMaturity - tValue)));
            Vector multiplierIssue = cache.Get(VectorMath.Exp(-fLimitHazardRate * (1 - powers) * (tIssue - tValue)));
            double amount;

            for (int i = values.TimeBeginIndex; i < values.TimePoints.Length; ++i)
            {
                double settlementDate = fDatePoints[i];
                double tSettle = values.TimePoints[i];
                pv.Clear();
                amount = 0;

                // Value of principal
                if (tValue <= tMaturity && tMaturity > tSettle)
                {
                    amount = ((fPrincipals != null) ? fFinalPrincipal : principal);
                    pv.Add(amount * DF.Get(tValue, tMaturity) * multiplierMaturity * VectorMath.Pow(SP.Get(tValue, tMaturity), powers));
                }

                // Value of issue
                if (tValue <= tIssue && tIssue > tSettle)
                {
                    amount = -principal;
                    pv.Add(amount * DF.Get(tValue, tIssue) * multiplierIssue * VectorMath.Pow(SP.Get(tValue, tIssue), powers));
                }

                // Value of amortisation payments
                if (haveAmortisation)
                {
                    for (int j = amortisation.Count - 1; j >= 0; j--)
                    {
                        // Add PV of amortisation amount if ValueDate <= amortDate <= MaturityDate
                        double amortDate = amortisation[j].Date;
                        if (amortDate > maturityDate)
                        {
                            continue;
                        }
                        if (amortDate < valueDate)
                        {
                            // Can exit loop because amortisation list is ordered
                            break;
                        }

                        amount = amortisation[j].Amount;
                        double tAmort = CalcUtils.DaysToYears(amortDate - baseDate);
                        pv.Add(amount * DF.Get(tValue, tAmort) * VectorMath.Exp(-fLimitHazardRate * (1 - powers) * (tAmort - tValue)) * VectorMath.Pow(SP.Get(tValue, tAmort), powers));
                    }
                }

                // Value of coupons
                int k = fPayDates.Count - 1;

                pEnd.Assign(1.0);

                while (k >= 0 && fPayDates[k] >= valueDate && fPayDates[k] > settlementDate)
                {
                    double startDate = k > 0 ? fPayDates[k - 1] : issueDate;
                    double tStart = CalcUtils.DaysToYears(startDate - baseDate);
                    tStart = Math.Max(tStart, Math.Max(tValue, tSettle));

                    double endDate = fPayDates[k];
                    double tEnd = CalcUtils.DaysToYears(endDate - baseDate);

                    pStart.Assign(VectorMath.Exp(-fLimitHazardRate * (1 - powers) * (tStart - tValue)) * VectorMath.Pow(SP.Get(tValue, tStart), powers));

                    if (k == fPayDates.Count - 1)
                    {
                        pEnd.Assign(VectorMath.Exp(-fLimitHazardRate * (1 - powers) * (tEnd - tValue)) * VectorMath.Pow(SP.Get(tValue, tEnd), powers));
                    }

                    // Value of payment on default
                    double PrincipalAtDefault = (haveAmortisation ? amortisation.GetPrincipal(principal, 0.5 * (tStart + tEnd)) : principal);
                    pv.Add(PrincipalAtDefault * DF.Get(tValue, 0.5 * (tStart + tEnd)) * SP.GetRecoveryRate() * (pStart - pEnd));

                    // Value of coupon
                    amount = ((fPrincipals != null) ? fPrincipals[k] : principal) * deal.Coupon_Rate * fAccruals[k];

                    pv.Add(amount * DF.Get(tValue, tEnd) * pEnd);

                    pEnd.DestructiveAssign(pStart);
                    k--;
                }

                Vector flooredPV = cache.Get(VectorMath.Max(0, pv));
                int valuesIndex = i - values.TimeBeginIndex;
                values.StraightBonds[valuesIndex] = flooredPV;
            }
        }

        /// <summary>
        /// Gets the ConvertibleExercised status for the given scenario and state vectors.
        /// </summary>
        /// <param name="scenario">the scenario whose status will be returned.</param>
        /// <param name="notExercised">notExercised[i] is 0 if scenario i has been exercised, and 0 otherwise.</param>
        /// <param name="converted">converted[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="earlyRedeemed">earlyRedeemed[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        [SuppressMessage("Microsoft.Usage", "CA1801", Justification =
                "Fires in release because the parameter is used in a Debug.Assert.")]
        private static ConvertibleExercised GetScenarioConvertibleStatus(int scenario, Vector converted,
            Vector earlyRedeemed, Vector notExercised)
        {
            if (converted[scenario] == 1)
                return ConvertibleExercised.Converted;

            if (earlyRedeemed[scenario] == 1)
                return ConvertibleExercised.Early_Redeemed;

            Debug.Assert(notExercised[scenario] == 1);

            return ConvertibleExercised.Not_Exercised;
        }

        /// <summary>
        /// Sets the state vectors to reflect the given status for the given scenario.
        /// </summary>
        /// <param name="status">the status of the scenario to set</param>
        /// <param name="scenario">the scenario whose status will be returned.</param>
        /// <param name="notExercised">notExercised[i] is 0 if scenario i has been exercised, and 0 otherwise.</param>
        /// <param name="converted">converted[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="earlyRedeemed">earlyRedeemed[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        private static void SetScenarioConvertibleStatus(int scenario, ConvertibleExercised status, Vector converted,
            Vector earlyRedeemed, Vector notExercised)
        {
            switch (status)
            {
                case ConvertibleExercised.Converted:
                    converted[scenario] = 1;
                    notExercised[scenario] = 0;
                    break;
                case ConvertibleExercised.Early_Redeemed:
                    earlyRedeemed[scenario] = 1;
                    notExercised[scenario] = 0;
                    break;
                default:
                    // don't change anything otherwise
                    break;
            }
        }

        /// <summary>
        /// Called when we must exercise the convertible in some way (for example if a default has occured).
        /// This function decides whether or not to convert or redeem, for those scenarios that haven't been
        /// exercised.
        /// </summary>
        /// <param name="valueDate">valuation date.</param>
        /// <param name="pv">The pv to assign to.</param>
        /// <param name="notExercised">notExercised[i] is 0 if scenario i has been exercised, and 0 otherwise.</param>
        /// <param name="converted">converted[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="earlyRedeemed">earlyRedeemed[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="cash">cashflows to update</param>
        /// <param name="recoveryValue">the value of redemption</param>
        /// <param name="conversionRatio">the ratio of bond to underlying when converted</param>
        /// <param name="underlyingSpot">the spot of the underlying</param>
        /// <param name="sign">1 if buying and -1 if selling.</param>
        private void EitherConvertOrRedeemIfNotExercised(double valueDate, Vector pv, Vector notExercised, Vector converted,
            Vector earlyRedeemed, CashAccumulators cash, Vector recoveryValue, double conversionRatio,
            Vector underlyingSpot, double sign)
        {
            // convert if recoveryValue less than value of equity
            using (var cache = Vector.CacheLike(pv))
            {
                var valueOfEquity = cache.Get(conversionRatio * underlyingSpot);
                var shouldConvert = cache.Get(cache.Get(recoveryValue) < valueOfEquity);

                // convert where appropriate
                var newlyConverted = cache.Get(notExercised.And(shouldConvert));
                pv.AssignConditional(newlyConverted, valueOfEquity, pv);
                converted.AssignConditional(newlyConverted, 1, converted);

                // redeem where appropriate
                var newlyRedeemed = cache.Get(notExercised.And(!shouldConvert));
                pv.AssignConditional(newlyRedeemed, recoveryValue, pv);
                earlyRedeemed.AssignConditional(newlyRedeemed, 1, earlyRedeemed);
                cash.Accumulate(X, valueDate, sign * pv * newlyRedeemed);

                // everything is now exercised in one way or another
                notExercised.Clear();
            }
        }

        /// <summary>
        /// Called to output PV and update Cash for the scenarios which are not yet exercised and not defaulted.
        /// </summary>
        /// <param name="valueDate">valuation date.</param>
        /// <param name="factors">The market data price factors</param>
        /// <param name="underlyingVol">The volatility of the underlying</param>
        /// <param name="pv">The pv to assign to.</param>
        /// <param name="notExercised">notExercised[i] is 0 if scenario i has been exercised, and 0 otherwise.</param>
        /// <param name="converted">converted[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="earlyRedeemed">earlyRedeemed[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="cash">cashflows to update</param>
        /// <param name="recoveryValue">the value of redemption</param>
        /// <param name="conversionRatio">the ratio of bond to underlying when converted</param>
        /// <param name="underlyingSpot">the spot of the underlying</param>
        /// <param name="sign">1 if buying and -1 if selling.</param>
        /// <param name="scenariosLeft">The number of scenarios remaining un-exercised.</param>
        private void GetPvForNotExercisedAndNotDefaulted(double valueDate, PriceFactorList factors,
            Vector underlyingSpot, Vector underlyingVol, double recoveryValue, double conversionRatio,
            Vector converted, Vector earlyRedeemed, Vector notExercised, double sign, Vector pv,
            CashAccumulators cash, ref int scenariosLeft)
        {
            using (var cache = Vector.CacheLike(pv))
            {
                var preCash = cache.GetClear();
                ScenarioValues values = GetDeterministicValuesForSolver(cache, valueDate, factors, underlyingSpot, underlyingVol, converted, earlyRedeemed, notExercised);

                // iterate the market data over the scenarios so that we can scalar price this bit
                // until mesh and solver are vectorised
                foreach (var scenario in factors.Scenarios())
                {
                    // rebuild solver, Mesh.
                    Tuple<bool, Mesh> scenarioMesh;
                    if (!values.Meshs.TryGetValue(scenario, out scenarioMesh))
                        continue;

                    double scenarioPv;
                    double scenarioCash;

                    var scenarioUnderlyingSpot = underlyingSpot[scenario];
                    var scenarioUnderlyingVol = underlyingVol[scenario];

                    var scenarioStatus = GetScenarioConvertibleStatus(scenario, converted, earlyRedeemed, notExercised);

                    // Initiliaze or reinitialize the solver.
                    Initialise(scenarioMesh.Item1, scenarioMesh.Item2.SpacePoints.Length, scenarioMesh.Item2.Space_End_Index);

                    GetPvForNotExercisedAndNotDefaultedScalar(scenario, values, valueDate, recoveryValue,
                                                              conversionRatio, out scenarioStatus,
                                                              sign, out scenarioPv, out scenarioCash);

                    if (scenarioStatus != ConvertibleExercised.Not_Exercised)
                        scenariosLeft--;

                    pv[scenario] = scenarioPv;
                    preCash[scenario] = scenarioCash;

                    SetScenarioConvertibleStatus(scenario, scenarioStatus, converted, earlyRedeemed, notExercised);
                }

                pv.PostLoop();
                preCash.PostLoop();

                cash.Accumulate(X, valueDate, preCash);
            }
        }

        /// <summary>
        /// Called to output PV and update Cash for the scenarios which are not yet exercised.
        /// </summary>
        /// <param name="valueDate">valuation date.</param>
        /// <param name="t">Time from baseDate to valueDate.</param>
        /// <param name="pv">The pv to assign to.</param>
        /// <param name="cash">cashflows to update</param>
        /// <param name="converted">converted[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="notExercised">notExercised[i] is 0 if scenario i has been exercised, and 0 otherwise.</param>
        /// <param name="earlyRedeemed">earlyRedeemed[i] is 1 if scenario i has been converted, and 0 otherwise.</param>
        /// <param name="conversionRatio">the ratio of bond to underlying when converted</param>
        /// <param name="underlyingSpot">the spot of the underlying</param>
        /// <param name="principal">the principal of the bond</param>
        /// <param name="sign">1 if buying and -1 if selling.</param>
        /// <param name="factors">The market data price factors</param>
        /// <param name="scenariosLeft">The number of scenarios remaining un-exercised.</param>
        private void GetPvForNotExercised(double valueDate, double t, Vector pv, CashAccumulators cash, Vector converted,
                Vector notExercised, Vector earlyRedeemed, double conversionRatio, Vector underlyingSpot, double principal,
                double sign, PriceFactorList factors, ref int scenariosLeft)
        {
            double tMaturityAbsolute = fTimePoints.Last();

            using (var cache = Vector.CacheLike(pv))
            {
                var underlyingVol = cache.Get(fEquityVol.Get(t, underlyingSpot, underlyingSpot, tMaturityAbsolute));
                var recoveryValue = Respect_Default == YesNo.Yes ? cache.Get(principal * RR.Get(t)) : null;

                // TODO: default time is scenario dependent
                if (DefaultTime(CR) <= t)
                {
                    // Defaulted between last valuation date and current valuation date
                    // Holder has the right to converted or recover fraction of the bond
                    EitherConvertOrRedeemIfNotExercised(valueDate, pv, notExercised, converted, earlyRedeemed, cash,
                        recoveryValue, conversionRatio, underlyingSpot, sign);
                    scenariosLeft = 0;
                }

                GetPvForNotExercisedAndNotDefaulted(valueDate, factors, underlyingSpot, underlyingVol,
                    principal * SP.GetRecoveryRate(), conversionRatio, converted, earlyRedeemed, notExercised, sign, pv, cash, ref scenariosLeft);
            }
        }

        /// <summary>
        /// Get deterministic market values for the solver at the specified value date index.
        /// </summary>
        private ScenarioValues GetDeterministicValuesForSolver(VectorScopedCache.Scope cache, double valueDate, PriceFactorList factors, 
            Vector underlyingSpot, Vector underlyingVol, Vector converted, Vector earlyRedeemed, Vector notExercised)
        {
            ScenarioValues values = new ScenarioValues(fTimePoints, FindIndex(valueDate));

            int maxSpaceEndIndex = 0;
            foreach (var scenario in factors.Scenarios())
            {
                var scenarioStatus = GetScenarioConvertibleStatus(scenario, converted, earlyRedeemed, notExercised);

                // if exercised then skip
                if (scenarioStatus != ConvertibleExercised.Not_Exercised)
                    continue;

                Mesh scenarioMesh = new Mesh(Space_Step_Size);
                bool reBuild = scenarioMesh.SetGrid(valueDate, (ConvertibleBase)fCreditBaseDeal, underlyingSpot[scenario], underlyingVol[scenario]);
                values.Meshs.Add(scenario, Tuple.Create(reBuild, scenarioMesh));
                maxSpaceEndIndex = Math.Max(maxSpaceEndIndex, scenarioMesh.Space_End_Index + 1); // +1 space_end_index is (int)(2 * half the number of steps), so we could be one short
            }

            Vector moneyness = cache.Get();
            Vector strikePrice = cache.Get(1.0);
            double tValue = values.TimePoints[values.TimeBeginIndex];
            double tMaturity = values.TimePoints.Last();
            Vector powers = cache.GetClear();
            double epsilon = 0.1;

            for (int vi = 0; vi < values.NumValues; ++vi)
            {
                double t = values.TimePoints[vi+values.TimeBeginIndex];
                values.Volatilities[vi] = new Vector[maxSpaceEndIndex];
                values.HazardRateSurfaces[vi] = new Vector[maxSpaceEndIndex];

                Vector excessHazardRate = cache.Get(VectorMath.Max(((VectorMath.Log(SP.Get(tValue, t) / SP.Get(tValue, t + epsilon)) / epsilon) - fLimitHazardRate), 0));

                Vector dividendRate = cache.Get();
                fEquityPrice.Yield(dividendRate, tValue, t);
                values.DividendRates[vi] = dividendRate;

                Vector shortRate = cache.Get((VectorMath.Log(DF.Get(tValue, t) / DF.Get(tValue, t + epsilon))) / epsilon);
                values.ShortRates[vi] = shortRate;

                for (int spacePointIndex = 0; spacePointIndex < maxSpaceEndIndex; ++spacePointIndex)
                {
                    moneyness.Clear();
                    for (int scenario = 0; scenario < factors.NumScenarios; ++scenario)
                    {
                        Tuple<bool, Mesh> scenarioMesh;
                        if (!values.Meshs.TryGetValue(scenario, out scenarioMesh) || spacePointIndex > scenarioMesh.Item2.Space_End_Index)
                            continue;

                        double pointDiff = scenarioMesh.Item2.SpacePoints[spacePointIndex] - scenarioMesh.Item2.GetX0();
                        moneyness[scenario] = Math.Exp(pointDiff);

                        if (spacePointIndex == 0)
                            powers[scenario] = Math.Exp(-fDecayFactorHazardRate * pointDiff);
                    }

                    Vector vol = cache.Get(fEquityVol.Get(tValue, moneyness, strikePrice, tMaturity + tValue - t));
                    values.Volatilities[vi][spacePointIndex] = vol;

                    Vector hazardRateSurface = cache.Get(fLimitHazardRate + excessHazardRate * VectorMath.Pow(moneyness, -fDecayFactorHazardRate));
                    values.HazardRateSurfaces[vi][spacePointIndex] = hazardRateSurface;
                }
            }

            BondPrice(values, cache, powers);
            return values;
        }

        protected sealed class ScenarioValues
        {
            /// <summary>
            /// Constructs a values storage object.
            /// </summary>
            /// <param name="timePoints">The timepoints for the calculation.</param>
            /// <param name="timeBeginIndex">The offset into the timePoints to store values for.</param>
            public ScenarioValues(double[] timePoints, int timeBeginIndex)
            {
                TimePoints = timePoints;
                NumValues = TimePoints.Length - timeBeginIndex;
                Meshs = new SortedList<int, Tuple<bool, Mesh>>();
                Volatilities = new Vector[NumValues][];
                HazardRateSurfaces = new Vector[NumValues][];
                DividendRates = new Vector[NumValues];
                ShortRates = new Vector[NumValues];
                StraightBonds = new Vector[NumValues];
                TimeBeginIndex = timeBeginIndex;
            }

            /// <summary>
            /// The time points for the calculation.
            /// </summary>
            /// <remarks>The values are stored for a subset of these time points, from at TimePoints[TimeBeginIndex] to the end.</remarks>
            public double[] TimePoints
            {
                get; private set;
            }

            /// <summary>
            /// An offset to the start of the TimePoints for which values are stored.
            /// </summary>
            public int TimeBeginIndex
            {
                get; private set;
            }

            /// <summary>
            /// The number of values stored; TimePoints.Length - TimeBeginIndex
            /// </summary>
            public int NumValues
            {
                get; private set;
            }

            /// <summary>
            /// Meshes, and wheter or not this mesh requires a rebuild of the solver. One per sceario -> rebuild,Mesh
            /// </summary>
            public SortedList<int, Tuple<bool, Mesh>> Meshs
            {
                get; private set;
            }

            /// <summary>
            /// Volatility scenario vector, one per timestep, spacePoint
            /// </summary>
            public Vector[][] Volatilities
            {
                get; private set;
            }

            /// <summary>
            /// Hazard Rates scenario vector, one per timestep, spacePoint
            /// </summary>
            public Vector[][] HazardRateSurfaces
            {
                get; private set;
            }

            /// <summary>
            /// Dividend rates scenario vector, one per timestep
            /// </summary>
            public Vector[] DividendRates
            {
                get; private set;
            }

            /// <summary>
            /// Short rates scenario vector, one per timestep
            /// </summary>
            public Vector[] ShortRates
            {
                get; private set;
            }

            /// <summary>
            /// Bond PV scenario vector, one per timestep
            /// </summary>
            public Vector[] StraightBonds
            {
                get; private set;
            }
        }
    }

    /// <summary>
    /// This class defines the valuation of a convertible that supports any combination 
    /// of Bermudean/American style exercise for call, put and conversion clauses.
    /// </summary>
    [Serializable]
    public class ConvertibleBondValuation : ConvertibleBaseValuation
    {
        /// <summary>
        /// Default constructor, set properties to default values.
        /// </summary>
        public ConvertibleBondValuation()
        {
            American_Time_Step = new Period("10d");
        }

        public Period American_Time_Step
        {
            get; set;
        }

        public override void PreCloneInitialize(PriceFactorList factors, BaseTimeGrid baseTimes, RequiredResults resultsRequired)
        {
            fAmericanTimeStep = Period.ValueToTerm(American_Time_Step).ToDays();

            base.PreCloneInitialize(factors, baseTimes, resultsRequired);
        }

        /// <summary>
        /// Compute the free boundary ie Max( Min( RolledBackValue, CallStrike), PutStrike, ConversionValue).
        /// </summary>
        public override ConvertibleExercised FreeBoundary(int dateIndex, Mesh mesh)
        {
            ConvertibleBond deal = (ConvertibleBond)Deal;
            double date = fDatePoints[dateIndex];
            int endIndex = mesh.Space_End_Index;

            base.FreeBoundary(dateIndex, mesh);

            // Save convertible value at spot level before applying the free boundary
            double valueAtSpotLevel = fSolver.UNew[mesh.Spot_Index];

            double barrier = 0;

            RedemptionCondition callCondition = deal.Call_Exercise.GetExerciseCondition(date);
            double callStrike = 0;
            if (callCondition != null)
            {
                // Mind that barrier equals to 0 means no barrier condition
                barrier = callCondition.Barrier;
                callStrike = callCondition.Strike_Amount + fAccruedCoupon[dateIndex];
                for (int i = endIndex; i >= 0; --i)
                {
                    // Issuer can call the convertible at callStrike only if the underlying equity is above the barrier (Up and In)
                    if (mesh.ExpSpacePoints[i] < barrier)
                    {
                        break;
                    }

                    fSolver.UNew[i] = Math.Min(fSolver.UNew[i], callStrike);
                }
            }

            RedemptionCondition putCondition = deal.Put_Exercise.GetExerciseCondition(date);
            double putStrike = 0;
            if (putCondition != null)
            {
                barrier = putCondition.Barrier;
                putStrike = putCondition.Strike_Amount + fAccruedCoupon[dateIndex];
                for (int i = endIndex; i >= 0; --i)
                {
                    // Holder can put the convertible at putStrike only if the underlying equity is above the barrier (Up and In)
                    if (mesh.ExpSpacePoints[i] < barrier)
                    {
                        break;
                    }

                    fSolver.UNew[i] = Math.Max(fSolver.UNew[i], putStrike);
                }
            }

            ConversionCondition conversionCondition = deal.Conversion_Exercise.GetExerciseCondition(date);
            double conversionRatio = deal.Conversion_Ratio;
            if (conversionCondition != null)
            {
                // Holder can convert the convertible into conversionRatio units of stock only if the underlying equity is above the barrier (Up and In)
                barrier = conversionCondition.Barrier;
                for (int i = endIndex; i >= 0; --i)
                {
                    if (mesh.ExpSpacePoints[i] < barrier)
                    {
                        break;
                    }

                    fSolver.UNew[i] = Math.Max(fSolver.UNew[i], conversionRatio * mesh.ExpSpacePoints[i]);
                }
            }

            // Set convertible status at spot level
            double valueAfterConstraints = fSolver.UNew[mesh.Spot_Index];

            if (valueAfterConstraints == callStrike || valueAfterConstraints == putStrike)
                return ConvertibleExercised.Early_Redeemed;

            if (valueAfterConstraints != valueAtSpotLevel)
                return ConvertibleExercised.Converted;

            return ConvertibleExercised.Not_Exercised;
        }

        /// <summary>
        /// Validate model parameters.
        /// </summary>
        public override void Validate(ErrorList errors)
        {
            base.Validate(errors);

            if (Time_Step < American_Time_Step)
                throw new AnalyticsException(PrefixErrorMessage + "Model parameter American Time Step should be smaller than Time Step");
        }

        /// <summary>
        /// Type of deal that the model can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(ConvertibleBond);
        }
    }

    /// <summary>
    /// This class defines the valuation of a european convertible 
    /// bond with optional or mandatory conversion at maturity.
    /// </summary>
    [Serializable]
    public class EuropeanConvertibleValuation : ConvertibleBaseValuation
    {
        /// <summary>
        /// Initialise the payoff of the convertible at maturity
        /// Conversion into stock is optional or mandatory.
        /// </summary>
        public override ConvertibleExercised Payoff(Mesh mesh)
        {
            EuropeanConvertible deal = (EuropeanConvertible)Deal;

            if (deal.Is_Mandatory_Conversion == YesNo.Yes)
            {
                // Conversion is mandatory at maturity
                for (int j = 0; j <= mesh.Space_End_Index; ++j)
                {
                    fSolver.UNew[j] = deal.Conversion_Ratio * mesh.ExpSpacePoints[j];
                }

                // Set up the status
                return ConvertibleExercised.Converted;
            }

            // Conversion is optional at maturity, so use the base function
            return base.Payoff(mesh);
        }

        /// <summary>
        /// Type of deal that the model can value.
        /// </summary>
        public override Type DealType()
        {
            return typeof(EuropeanConvertible);
        }
    }
}
