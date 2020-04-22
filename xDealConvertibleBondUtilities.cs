/// <author>
/// Nathalie Rouille
/// </author>
/// <owner>
/// Nathalie Rouille
/// </owner>
/// <summary>
/// Defines the convertible bond utilities : mesh generator, PDE solver
/// and quasi 2-factor parametric form.
/// The space dimension is logarithmically discretized, the mesh 
/// is centered around the log spot price, extends in both direction 
/// up to at least 5 * vol * Sqrt(maturity) and includes all key 
/// levels for the deal such as call and put prices, barrier prices,...
/// Several finite difference schemes can be selected to solve the
/// PDE on the grid : Explicit Euler, Crank Nicolson or Rannacher 
/// time stepping. A Successive Over Relaxation is also available 
/// for implicit or semi implicit scheme.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;

using SunGard.Adaptiv.Analytics.Framework;

namespace SunGard.Adaptiv.Analytics.Models
{
    /// <summary>
    /// This class is used to logarithmically discretized the space dimension.
    /// </summary>
    public class Mesh
    {
        public double[] ExpSpacePoints = null; // array of underlying value S
        public double[] SpacePoints = null; // array of x = lnS
        public double[] StepSizes = null; // array of dx

        protected double fSpaceStep = 0.1; // space step in ln
        protected bool fReBuild = false; // flag to determine if the memory size needs to be extended

        /// <summary>
        /// Constructor.
        /// </summary>
        public Mesh(double step)
        {
            fSpaceStep      = step;
            Spot_Index      = -1;
            Space_End_Index = 0;
        }

        public int Spot_Index
        {
            get; private set;
        }

        public int Space_End_Index
        {
            get; private set;
        }

        /// <summary>
        /// Return the ln price at value date used to create the mesh.
        /// </summary>
        public double GetX0()
        {
            return SpacePoints[Spot_Index];
        }

        /// <summary>
        /// Create the space mesh at a particular value date for the current underlying spot and volatility
        /// Returns true if the mesh dimension needs to be extended or if it is the first time the mesh is set.
        /// </summary>
        public bool SetGrid(double valueDate, ConvertibleBase convertibleDeal, double underlyingSpot, double underlyingVol)
        {
            fReBuild = false;

            double tMaturity = CalcUtils.DaysToYears(convertibleDeal.Bond_Maturity_Date - valueDate);
            if (tMaturity < 0)
            {
                // After maturity : no mesh need to be build (Sanity check)
                Space_End_Index = -1;
                return fReBuild;
            }

            // Convert the underlying spot to logarithm
            underlyingSpot = (underlyingSpot > 0 ? underlyingSpot : CalcUtils.MinAssetPrice);
            double x0 = Math.Log(underlyingSpot);

            List<double> keyLevels = convertibleDeal.GetKeyLevel(valueDate);

            // Convert underlying key levels to logarithm
            for (int j = 0; j < keyLevels.Count; ++j)
            {
                keyLevels[j] = Math.Log(keyLevels[j]);
            }

            // Add the spot to the key levels
            if (keyLevels.BinarySearch(x0) < 0)
            {
                keyLevels.Add(x0);
                keyLevels.Sort();
            }

            // The mesh is centered around x0 +/- 5 * sigma * sqrt(T)
            // if necessary, extend the grid to include all key levels
            double halfLength = 5 * underlyingVol * Math.Sqrt(tMaturity);
            if (Math.Abs(keyLevels.Last() - x0) > halfLength)
            {
                halfLength = Math.Abs(keyLevels.Last() - x0);
            }
            if (Math.Abs(keyLevels[0] - x0) > halfLength)
            {
                halfLength = Math.Abs(keyLevels[0] - x0);
            }

            int halfNumberOfStep = (int)(Math.Max(halfLength / fSpaceStep, 10));

            if (SpacePoints == null || SpacePoints.Length <= 2 * halfNumberOfStep)
            {
                // First valuation of the deal
                // Create arrays of the approximate maximum size
                int size       = 7 * keyLevels.Count() + 2 * (int)(Math.Max(halfLength / fSpaceStep, 10));
                SpacePoints    = new double[size];
                ExpSpacePoints = new double[size];
                StepSizes      = new double[size];
                fReBuild       = true;
            }

            int k = 1;
            SpacePoints[halfNumberOfStep] = x0;
            ExpSpacePoints[halfNumberOfStep] = underlyingSpot;

            while (k <= halfNumberOfStep)
            {
                SpacePoints[halfNumberOfStep - k] = x0 - k * fSpaceStep;
                SpacePoints[halfNumberOfStep + k] = x0 + k * fSpaceStep;

                ExpSpacePoints[halfNumberOfStep - k] = underlyingSpot * Math.Exp(-k * fSpaceStep);
                ExpSpacePoints[halfNumberOfStep + k] = underlyingSpot * Math.Exp(k * fSpaceStep);

                ++k;
            }

            Space_End_Index = 2 * halfNumberOfStep;

            // Array of step size is constant
            for (int i = 0; i < Space_End_Index; ++i)
            {
                StepSizes[i] = fSpaceStep;
            }

            // Find the spot index in the point array
            SearchSpotIndex(x0);

            if (Space_End_Index < 0)
                throw new AnalyticsException("Failed to build a space mesh for the convertible bond " + convertibleDeal.Name.ToString()); // Sanity check

            return fReBuild;
        }

        /// <summary>
        /// Discretise the space dimension from the last point up to end with a specified step (for x and expStep for S).
        /// </summary>
        protected void AddIntermediatePoints(double end, double step, double expStep)
        {
            double begin = SpacePoints[Space_End_Index];

            // If end isn't greater than the last point created then do nothing
            if (begin >= end)
                return;

            double currentPoint = begin + step;
            ++Space_End_Index;

            // Add points up to end while there is enough memory for the space array
            while (currentPoint < end && Space_End_Index < SpacePoints.Length)
            {
                SpacePoints[Space_End_Index] = currentPoint;
                ExpSpacePoints[Space_End_Index] = ExpSpacePoints[Space_End_Index - 1] * expStep;
                StepSizes[Space_End_Index - 1] = step;

                ++Space_End_Index;
                currentPoint += step;
            }

            if (Space_End_Index >= SpacePoints.Length)
            {
                // Reallocate the space array
                Array.Resize<double>(ref SpacePoints, 2 * Space_End_Index);
                Array.Resize<double>(ref ExpSpacePoints, 2 * Space_End_Index);
                Array.Resize<double>(ref StepSizes, 2 * Space_End_Index);

                fReBuild = true;
                --Space_End_Index;

                // Restart adding points
                AddIntermediatePoints(end, step, expStep);
                return;

            }

            // Rounding correction : avoid adding end and a previous point such as end - SMALL where SMALL = 10-5
            if (Math.Abs(currentPoint - step - end) < CalcUtils.SMALL)
            {
                // delete useless point
                Space_End_Index--;
            }

            // Add the end point
            SpacePoints[Space_End_Index]    = end;
            ExpSpacePoints[Space_End_Index] = Math.Exp(end);
            StepSizes[Space_End_Index - 1]  = end - SpacePoints[Space_End_Index - 1];
        }

        /// <summary>
        /// Set the index of the underlying spot price in the point arrays by dichotomy.
        /// </summary>
        protected void SearchSpotIndex(double x0)
        {
            // No space mesh created
            if (SpacePoints == null)
            {
                throw new AnalyticsException("Mesh does not exist for the convertible bond, the index of the spot cannot be found"); // Sanity check
            }

            // Search the underlying value by dichotomy
            int j;
            int beginInterval = 0, endInterval = Space_End_Index;
            while (beginInterval + 1 < endInterval)
            {
                j = beginInterval + (int)((endInterval - beginInterval) / 2);
                if (x0 == SpacePoints[j])
                {
                    Spot_Index = j;
                    return;
                }
                else if (x0 < SpacePoints[j])
                {
                    endInterval = j;
                }
                else
                {
                    beginInterval = j;
                }
            }

            // Space array has 2 elements only
            Spot_Index = -1;
            if (SpacePoints[beginInterval] == x0)
            {
                Spot_Index = beginInterval;
            }
            else if (SpacePoints[endInterval] == x0)
            {
                Spot_Index = endInterval;
            }
        }
    }

    /// <summary>
    /// Base class for finite difference scheme implementation.
    /// </summary>
    /// <remarks>
    /// The theta-scheme can be written as 
    /// Omega * U^i+1 = Lambda * U^i - theta * B^i - (1 - theta) * B^i+1
    /// where Y is the source term, 
    /// Y = Omega * U^i+1 + theta * B^i + (1 - theta) * B^i+1,
    /// Omega = 1/dt + (1 - theta) A^i+1 and 
    /// Lambda = 1/dt - theta * A^i
    /// A is a tridiagonal matrix and so are Omega and Lambda, they are stored as 3 arrays. 
    /// Convention : name0 is the subdiagonal, name1 the diagonal and name2 the 
    /// superdiagonal in order to keep the indexing system simple, the first 
    /// element of name0 and last of name2 are not used.
    /// </remarks>
    public abstract class PDESolver
    {
        protected double[] fOmega0 = null;
        protected double[] fOmega1 = null;
        protected double[] fOmega2 = null;
        protected double[] fA0 = null;
        protected double[] fA1 = null;
        protected double[] fA2 = null;
        protected double[] fY = null;
        protected double[] fB = null;
        protected int fEndIndex = -1; // index of last element to use

        public double[] UOld
        {
            get; set;
        }

        public double[] UNew
        {
            get; set;
        }

        /// <summary>
        /// Set or refresh the index of the last element used in the mesh array.
        /// </summary>
        public void SetEndIndex(int index)
        {
            fEndIndex = index;
        }

        /// <summary>
        /// Step backward from i + 1 to i and compute the solution array U^i at previous time step.
        /// </summary>
        public abstract void SolveForTimeStep(int scenario, Mesh mesh, double dt, int indexTimeOld, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha);

        /// <summary>
        /// Build matrix at maturity.
        /// </summary>
        public abstract void PrepareAtMaturity(int scenario, Mesh mesh, double dt, int maturityIndex, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha);

        /// <summary>
        /// Initiliase all arrays or reallocate memory when the current size is smaller than required.
        /// </summary>
        public virtual void Initialise(int length)
        {
            UOld = new double[length];
            UNew = new double[length];

            fOmega0 = new double[length];
            fOmega1 = new double[length];
            fOmega2 = new double[length];

            fA0 = new double[length];
            fA1 = new double[length];
            fA2 = new double[length];

            fB = new double[length];

            fY = new double[length];
        }

        /// <summary>
        /// Reset arrays to 0.
        /// </summary>
        public virtual void Reset()
        {
            for (int j = 0; j < UOld.Length; ++j)
            {
                UOld[j] = 0.0;
                UNew[j] = 0.0;

                fOmega0[j] = 0.0;
                fOmega1[j] = 0.0;
                fOmega2[j] = 0.0;

                fA0[j] = 0.0;
                fA1[j] = 0.0;
                fA2[j] = 0.0;

                fB[j] = 0.0;

                fY[j] = 0.0;
            }
        }

        /// <summary>
        /// Shift the solution array to prepare for next time step.
        /// </summary>
        public virtual void Shift()
        {
            for (int j = 0; j <= fEndIndex; ++j)
            {
                UOld[j] = UNew[j];
            }
        }

        /// <summary>
        /// Set solution on the upper and lower boundary.
        /// </summary>
        public void BoundaryCondition(int scenario, Mesh mesh, int indexTime, Vector[] bondPrice, double conversionRatio)
        {
            UNew[0] = bondPrice[indexTime][scenario];
            UNew[fEndIndex] = UNew[fEndIndex - 1] + mesh.StepSizes[fEndIndex - 1] * conversionRatio * mesh.ExpSpacePoints[fEndIndex];
        }

        /// <summary>
        /// Build matrix A and B for the current time step, for the explicit Euler 
        /// scheme A = A^i+1  and B = B^i+1 and for other A = A^i and B = B^i.
        /// </summary>
        protected virtual void PrepareForTimeStep(int scenario, Mesh mesh, double dt, int indexTime, Vector[] shortRates, Vector[] dividendRates, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            double k = mesh.StepSizes[0];
            double h;
            double rate = shortRates[indexTime][scenario];
            double hPlusk = 0;
            double kMinush = 0;
            double hazard;
            double volSquared;
            double oneMinusAlpha = 1 - alpha;
            double conversionTimesAlpha = conversionRatio * alpha;
            double dividend = dividendRates[indexTime][scenario];

            // borders are traited separately
            for (int j = 1; j < fEndIndex; ++j)
            {
                h          = mesh.StepSizes[j];
                hPlusk     = h + k;
                kMinush    = k - h;

                volSquared = CalcUtils.Sqr(volSurfaces[indexTime][j][scenario]);
                hazard = hazardRateSurfaces[indexTime][j][scenario];

                double coefficientFirstDeriv = rate - dividend + hazard * oneMinusAlpha;

                fA0[j] = (-h * coefficientFirstDeriv + volSquared * (1 + h / 2)) / (k * hPlusk);
                fA1[j] = -rate - hazard - kMinush / (k * h) * coefficientFirstDeriv - (2 + kMinush) / (2 * h * k) * volSquared;
                fA2[j] = (k * coefficientFirstDeriv + volSquared * (1 - k / 2)) / (h * hPlusk);

                fB[j]  = hazard * Math.Max(conversionTimesAlpha * mesh.ExpSpacePoints[j], recoveryValue);

                k = h;
            }
        }

        /// <summary>
        /// Compute M = 1/dt * I + coefficient * A.
        /// </summary>
        protected void BuildTridiagonal(double dt, double[] A0, double[] A1, double[] A2, double[] M0, double[] M1, double[] M2, double coefficient)
        {
            double overdt = dt > CalcUtils.TINY ? 1 / dt : 0;
            //for 1 to M
            for (int j = 1; j < fEndIndex; ++j)
            {
                M0[j] = coefficient * A0[j];
                M1[j] = coefficient * A1[j] + overdt;
                M2[j] = coefficient * A2[j];

            }

            //special case M
            M0[fEndIndex] = coefficient * A0[fEndIndex];
            M1[fEndIndex] = coefficient * A1[fEndIndex];

            M0[1] = 0;
            M2[fEndIndex] = 0;
        }

        /// <summary>
        /// Compute Y = Omega * U^i+1.
        /// </summary>
        protected void MultiplyMatrix()
        {
            for (int j = 2; j < fEndIndex; ++j)
            {
                fY[j] = fOmega0[j] * UOld[j - 1] + fOmega1[j] * UOld[j] + fOmega2[j] * UOld[j + 1];
            }

            // special case on the edge of the matrix
            fY[1] = fOmega1[1] * UOld[1] + fOmega2[1] * UOld[2];
            fY[fEndIndex] = fOmega0[fEndIndex] * UOld[fEndIndex - 1] + fOmega1[fEndIndex] * UOld[fEndIndex];
        }

        /// <summary>
        /// Compute Y += coefficient * B.
        /// </summary>
        protected void AddVector(double[] B, double coefficient)
        {
            for (int j = 1; j <= fEndIndex; ++j)
            {
                fY[j] += coefficient * B[j];
            }
        }

        /// <summary>
        /// Compute U^i = coefficient * Y.
        /// </summary>
        protected void MultiplyScalar(double coefficient)
        {
            for (int j = 1; j <= fEndIndex; ++j)
            {
                UNew[j] = coefficient * fY[j];
            }
        }
    }

    /// <summary>
    /// This class implement the Explicit Euler scheme (theta = 0).
    /// </summary>
    public class ExplicitEuler : PDESolver
    {
        // Here A = A^i+1 and B = B^i+1
        /// <summary>
        /// Step backward from i + 1 to i and compute the solution array U^i at previous time step.
        /// </summary>
        public override void SolveForTimeStep(int scenario, Mesh mesh, double dt, int indexTimeOld, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            PrepareForTimeStep(scenario, mesh, dt, indexTimeOld, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            //ALGO
            BuildTridiagonal(dt, fA0, fA1, fA2, fOmega0, fOmega1, fOmega2, 1);
            MultiplyMatrix();
            AddVector(fB, 1);
            MultiplyScalar(dt);

            BoundaryCondition(scenario, mesh, indexTimeOld - 1, bondPrices, conversionRatio);
        }

        /// <summary>
        /// Build matrix at maturity.
        /// </summary>
        public override void PrepareAtMaturity(int scenario, Mesh mesh, double dt, int maturityIndex, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            Shift();
        }

        /// <summary>
        /// Build matrix A and B for the current time step.
        /// </summary>
        protected override void PrepareForTimeStep(int scenario, Mesh mesh, double dt, int indexTime, Vector[] shortRates, Vector[] dividendRates, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            base.PrepareForTimeStep(scenario, mesh, dt, indexTime, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            // special case for edges
            fB[1] += fA0[1] * UOld[0];
            fA0[1] = 0;

            fA0[fEndIndex] = 0;
            fA1[fEndIndex] = 0;
            fB[fEndIndex] = 0;
        }
    }

    /// <summary>
    /// Base class for implementation of implicit scheme.
    /// </summary>
    public class BaseImplicit : ExplicitEuler
    {
        // Here    A = A^i   and    B = B^i
        //      AOld = A^i+1 and BOld = B^i+1
        //     theta = 0.5
        protected double[] fLambda0 = null;
        protected double[] fLambda1 = null;
        protected double[] fLambda2 = null;
        protected double[] fAOld0 = null;
        protected double[] fAOld1 = null;
        protected double[] fAOld2 = null;
        protected double[] fBOld = null;
        protected double fTheta = 0.5;

        /// <summary>
        /// Build matrix at maturity.
        /// </summary>
        public override void PrepareAtMaturity(int scenario, Mesh mesh, double dt, int maturityIndex, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            PrepareForTimeStep(scenario, mesh, dt, maturityIndex, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            Shift();
        }

        /// <summary>
        /// Initiliase all arrays or reallocate memory when the current size is smaller than required.
        /// </summary>
        public override void Initialise(int length)
        {
            base.Initialise(length);

            fLambda0 = new double[length];
            fLambda1 = new double[length];
            fLambda2 = new double[length];

            fAOld0   = new double[length];
            fAOld1   = new double[length];
            fAOld2   = new double[length];

            fBOld    = new double[length];
        }

        /// <summary>
        /// Reset arrays to 0.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            for (int j = 0; j < UOld.Length; ++j)
            {
                fLambda0[j] = 0.0;
                fLambda1[j] = 0.0;
                fLambda2[j] = 0.0;

                fAOld0[j] = 0.0;
                fAOld1[j] = 0.0;
                fAOld2[j] = 0.0;

                fBOld[j] = 0.0;
            }
        }

        /// <summary>
        /// Shift the solution array to prepare for next time step.
        /// </summary>
        public override void Shift()
        {
            base.Shift();

            for (int j = 0; j < fEndIndex; ++j)
            {
                fAOld0[j] = fA0[j];
                fAOld1[j] = fA1[j];
                fAOld2[j] = fA2[j];
                fBOld[j] = fB[j];
            }

            fAOld0[fEndIndex] = 0;
            fAOld1[fEndIndex] = 0;

            fBOld[fEndIndex] = 0;
        }

        /// <summary>
        /// Build matrix A and B for the current time step.
        /// </summary>
        protected override void PrepareForTimeStep(int scenario, Mesh mesh, double dt, int indexTime, Vector[] shortRates, Vector[] dividendRates, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            base.PrepareForTimeStep(scenario, mesh, dt, indexTime, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            // Special case for edges
            fB[1]         += fA0[1] * UNew[0];
            fA0[1]         = 0;

            double element = 1 / mesh.StepSizes[fEndIndex - 1];
            fA0[fEndIndex] = element;
            fA1[fEndIndex] = -element;

            fB[fEndIndex]  = conversionRatio * mesh.ExpSpacePoints[fEndIndex];
        }
    }

    /// <summary>
    /// This class implement the theta scheme for theta = 0.5 known as the Crank Nicolson stepping.
    /// </summary>
    /// <remarks>
    /// The scheme is semi implicit and the system to solve can be written as :
    /// Lambda * U^i = Y
    /// Lambda = L * U and V = U * U^i
    /// </remarks>
    public class ThetaScheme : BaseImplicit
    {
        // Working arrays for LU decomposition
        protected double[] fLU1 = null; // diagonal of L
        protected double[] fLU2 = null; // superdiagonal of U
        protected double[] fV = null; // Intermediate variable

        /// <summary>
        /// Step backward from i + 1 to i and compute the solution array U^i at previous time step.
        /// </summary>
        public override void SolveForTimeStep(int scenario, Mesh mesh, double dt, int indexTimeOld, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            BoundaryCondition(scenario, mesh, indexTimeOld - 1, bondPrices, conversionRatio);
            PrepareForTimeStep(scenario, mesh, dt, indexTimeOld - 1, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            // Build matrix Lambda (time i)
            BuildTridiagonal(dt, fA0, fA1, fA2, fLambda0, fLambda1, fLambda2, -fTheta);
            // Build matrix Omega (time i+1)
            BuildTridiagonal(dt, fAOld0, fAOld1, fAOld2, fOmega0, fOmega1, fOmega2, 1 - fTheta);

            MultiplyMatrix();

            AddVector(fB, fTheta);
            AddVector(fBOld, 1 - fTheta);

            // Solve the system using a LU Decomposition
            LUDecomposition();
        }

        /// <summary>
        /// Initiliase all arrays or reallocate memory when the current size is smaller than required.
        /// </summary>
        public override void Initialise(int length)
        {
            base.Initialise(length);

            fLU1 = new double[length];
            fLU2 = new double[length];
            fV = new double[length];
        }

        /// <summary>
        /// Reset arrays to 0.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            for (int j = 0; j < UOld.Length; ++j)
            {
                fLU1[j] = 0.0;
                fLU2[j] = 0.0;

                fV[j] = 0.0;
            }
        }

        /// <summary>
        /// Solve the linear system with a tridiagonal matrix by LU Decomposition :  Lambda * U^i = Y.
        /// </summary>
        protected void LUDecomposition()
        {
            // Unused matrix element
            fLambda0[1] = 0;
            fLambda2[fEndIndex] = 0;

            // Compute coefficient of L and U
            fLU1[1] = fLambda1[1];
            fLU2[1] = fLambda2[1] / fLambda1[1];
            double element;

            for (int j = 2; j <= fEndIndex; ++j)
            {
                element = fLambda1[j] - fLambda0[j] * fLU2[j - 1];
                fLU1[j] = element;
                fLU2[j] = fLambda2[j] / element;
            }

            // Compute V
            fV[1] = fY[1] / fLU1[1];
            for (int j = 2; j <= fEndIndex; ++j)
            {
                fV[j] = (fY[j] - fLambda0[j] * fV[j - 1]) / fLU1[j];
            }

            // Compute U^i
            UNew[fEndIndex] = fV[fEndIndex];
            for (int j = fEndIndex - 1; j >= 1; --j)
            {
                UNew[j] = fV[j] - fLU2[j] * UNew[j + 1];
            }
        }
    }

    /// <summary>
    /// This class implement the Rannacher time stepping.
    /// </summary>
    public class Rannacher : ThetaScheme
    {
        protected int fCount = 0; // number of time step

        private const int NUMBER_IMPLICIT_STEP = 50;

        /// <summary>
        /// Step backward from i + 1 to i and compute the solution array U^i at previous time step.
        /// </summary>
        public override void SolveForTimeStep(int scenario, Mesh mesh, double dt, int indexTimeOld, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            fCount++;
            if (fCount > NUMBER_IMPLICIT_STEP)
                fTheta = 0.5;

            base.SolveForTimeStep(scenario, mesh, dt, indexTimeOld, shortRates, dividendRates, bondPrices, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);
        }

        /// <summary>
        /// Build matrix at maturity.
        /// </summary>
        public override void PrepareAtMaturity(int scenario, Mesh mesh, double dt, int maturityIndex, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            fCount = 0;
            fTheta = 1;
            base.PrepareAtMaturity(scenario, mesh, dt, maturityIndex, shortRates, dividendRates, bondPrices, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);
        }
    }

    /// <summary>
    /// This class implement the Crank Nicolson time stepping solve with the Successive Over Relaxation method.
    /// </summary>
    /// <remarks>
    /// The SOR method is initialized with the solution of the Explicit Euler scheme.
    /// </remarks>
    public class SOR : BaseImplicit
    {
        protected double fRelaxationParameter = 1.5; // ]0,2[
        protected double fErrorTolerance = CalcUtils.SMALL; // stopping criteria of SOR method

        private const int MAX_ITERATION = 1000;

        /// <summary>
        /// Step backward from i + 1 to i and compute the solution array U^i at previous time step.
        /// </summary>
        public override void SolveForTimeStep(int scenario, Mesh mesh, double dt, int indexTimeOld, Vector[] shortRates, Vector[] dividendRates, Vector[] bondPrices, Vector[][] volSurfaces, Vector[][] hazardRateSurfaces, double recoveryValue, double conversionRatio, double alpha)
        {
            // Initialize the solution array to the solution of the explicit Euler scheme
            base.SolveForTimeStep(scenario, mesh, dt, indexTimeOld, shortRates, dividendRates, bondPrices, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            BoundaryCondition(scenario, mesh, indexTimeOld - 1, bondPrices, conversionRatio);

            PrepareForTimeStep(scenario, mesh, dt, indexTimeOld - 1, shortRates, dividendRates, volSurfaces, hazardRateSurfaces, recoveryValue, conversionRatio, alpha);

            // Build matrix Lambda (time i)
            BuildTridiagonal(dt, fA0, fA1, fA2, fLambda0, fLambda1, fLambda2, -fTheta); //en new
            // Build matrix Omega (time i+1)
            BuildTridiagonal(dt, fAOld0, fAOld1, fAOld2, fOmega0, fOmega1, fOmega2, 1 - fTheta); //en old

            MultiplyMatrix();

            AddVector(fB, fTheta);
            AddVector(fBOld, 1 - fTheta);

            // Solve the system by SOR method
            SORAlgorithm();
        }

        /// <summary>
        /// Solve the linear system with a tridiagonal matrix by SOR method :  Lambda * U^i = Y.
        /// </summary>
        /// <remarks>
        /// Create a sequence of solution arrays converging to the solution of the system. 
        /// To ensure convergence, the relaxation parameter should be chosen in ]0,2[. 
        /// </remarks>
        protected void SORAlgorithm()
        {
            int k = 0;
            double element = 0, error = 0;

            while (k < MAX_ITERATION)
            {
                error = 0;

                // Boundary are excluded because they are already set
                for (int j = 1; j < fEndIndex; ++j)
                {
                    UOld[j] = UNew[j];
                    element  = fRelaxationParameter / fLambda1[j] * (fY[j] - fLambda0[j] * UNew[j - 1] - fLambda2[j] * UOld[j + 1]) + (1 - fRelaxationParameter) * UOld[j];
                    UNew[j] = element;
                    error   += Math.Abs(element - UOld[j]);
                }

                if (error < fErrorTolerance)
                    break;

                ++k;
            }

            if (k >= MAX_ITERATION)
                throw new AnalyticsException("PDE Solver for convertible bond : SOR method does not converge, use a different Scheme type");
        }
    }

    /// <summary>
    /// Factor : IssuerHazardRateParameters 
    /// Define the issuer specific hazard rate parameters 
    /// for the parametric form used in the Convertible Bond valuation
    /// </summary>
    [Serializable]
    public class IssuerHazardRateParameters : PriceFactor
    {
        private double fDecayFactorHazardRate;
        private BasisPoint fLimitHazardRate;

        public double Decay_Factor_Hazard_Rate
        {
            get { return fDecayFactorHazardRate; } set { SetDecayFactorHazardRate(value); }
        }

        public BasisPoint Limit_Hazard_Rate
        {
            get { return fLimitHazardRate; } set { SetLimitHazardRate(value);}
        }

        /// <inheritdoc />
        public override void CopyTo(IBaseFactor to)
        {
            base.CopyTo(to);

            var obj = (IssuerHazardRateParameters)to;

            // Properties
            obj.Decay_Factor_Hazard_Rate = this.Decay_Factor_Hazard_Rate;
            obj.Limit_Hazard_Rate = this.Limit_Hazard_Rate;

            // Fields
        }

        /// <remarks>
        /// This price factor can't have a model, so no point in outputting points.
        /// </remarks>
        public override string[] GetPointIDs()
        {
            return new string[0];
        }

        /// <remarks>
        /// This price factor can't have a model, so no point in outputting points.
        /// </remarks>
        public override Vector[] PointValues(Func<Vector> alloc, double t)
        {
            return new Vector[0];
        }

        /// <summary>
        /// Sets the decay factor if it's non negative
        /// </summary>
        private void SetDecayFactorHazardRate(double value)
        {
            if (value < 0)
                throw new AnalyticsException(String.Format("Decay factor {0} for issuer {1} must be non negative.", Decay_Factor_Hazard_Rate, fID.ToString()));

            fDecayFactorHazardRate = value;
        }

        /// <summary>
        /// Sets the limit hazard rate if it's non negative
        /// </summary>
        private void SetLimitHazardRate(BasisPoint value)
        {
            if (Limit_Hazard_Rate < 0)
                throw new AnalyticsException(String.Format("Limit hazard rate {0} for issuer {1} must be non negative.", Limit_Hazard_Rate, fID.ToString()));

            fLimitHazardRate = new BasisPoint(value);
        }
    }
}