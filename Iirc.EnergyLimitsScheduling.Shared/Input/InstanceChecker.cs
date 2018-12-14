// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceChecker.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    public class InstanceChecker
    {
        private Instance instance;

        private InstanceChecker.InstanceStatus status;

        public InstanceChecker.InstanceStatus Check(Instance instance)
        {
            this.instance = instance;

            var ok =
                this.HorizonIsDivisibleByMeteringIntervalLength();

            if (ok)
            {
                this.status = InstanceStatus.Ok;
            }

            return this.status;
        }

        private bool HorizonIsDivisibleByMeteringIntervalLength()
        {
            if ((this.instance.Horizon % this.instance.LengthMeteringInterval) != 0)
            {
                this.status = InstanceStatus.HorizonNotDivisibleByMeteringIntervalLength;
                return false;
            }

            return true;
        }

        public enum InstanceStatus
        {
            Ok = 0,
            HorizonNotDivisibleByMeteringIntervalLength = 1
        }
    }
}