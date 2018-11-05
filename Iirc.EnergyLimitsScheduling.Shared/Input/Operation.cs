using Newtonsoft.Json;

namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    public class Operation
    {
        public int Id { get; }
        public int Index { get; }
        public int JobIndex { get; }
        public int MachineIndex { get; }
        public int ProcessingTime { get; }
        public double PowerConsumption { get; }

        public Operation(int id, int index, int jobIndex, int machineIndex, int processingTime, double powerConsumption)
        {
            this.Id = id;
            this.Index = index;
            this.JobIndex = jobIndex;
            this.MachineIndex = machineIndex;
            this.ProcessingTime = processingTime;
            this.PowerConsumption = powerConsumption;
        }

        protected bool Equals(Operation other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Operation) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}