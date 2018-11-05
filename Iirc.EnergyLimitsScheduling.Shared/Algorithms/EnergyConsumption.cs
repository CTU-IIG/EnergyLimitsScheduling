namespace Iirc.EnergyLimitsScheduling.Shared.Algorithms
{
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.Utils.Math;

    public class EnergyConsumption
    {
        public static double[] ComputeConsumptionInMeteringIntervals(Instance instance, StartTimes startTimes)
        {
            var consumedEnergyInMeteringIntervals = new double[instance.NumMeteringIntervals];

            foreach (var meteringIntervalIndex in instance.MeteringIntervals())
            {
                double totalConsumedEnergy = 0.0;
                foreach (var operation in instance.AllOperations())
                {
                    var startTime = startTimes[operation];
                    var completionTime = startTime + operation.ProcessingTime;

                    totalConsumedEnergy += operation.PowerConsumption * Intervals.OverlapLength(
                        startTime,
                        completionTime,
                        instance.MeteringIntervalStart(meteringIntervalIndex),
                        instance.MeteringIntervalEnd(meteringIntervalIndex));
                }

                consumedEnergyInMeteringIntervals[meteringIntervalIndex] = totalConsumedEnergy;
            }

            return consumedEnergyInMeteringIntervals;
        }

        public static bool AreEnergyLimitsSatisfied(Instance instance, StartTimes startTimes)
        {
            return AreEnergyLimitsSatisfied(instance, ComputeConsumptionInMeteringIntervals(instance, startTimes));
        }

        public static bool AreEnergyLimitsSatisfied(Instance instance, double[] consumedEnergyInMeteringIntervals)
        {
            foreach (var meteringIntervalIndex in instance.MeteringIntervals())
            {
                if (NumericComparer.Default.Greater(
                    consumedEnergyInMeteringIntervals[meteringIntervalIndex],
                    instance.EnergyLimit))
                {
                    return false;
                }
            }

            return true;
        }
    }
}