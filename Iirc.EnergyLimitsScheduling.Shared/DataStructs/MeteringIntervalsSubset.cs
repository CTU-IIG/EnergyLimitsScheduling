namespace Iirc.EnergyLimitsScheduling.Shared.DataStructs
{
    using System.Collections;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class MeteringIntervalsSubset : IEnumerable<int>
    {
        public int FirstMeteringIntervalIndex { get; set; }
        public int LastMeteringIntervalIndex { get; set; }

        private int LengthMeteringInterval { get; set; }

        public int Count
        { 
            get
            {
                return this.LastMeteringIntervalIndex - this.FirstMeteringIntervalIndex + 1;
            }
        }

        public int Horizon
        {
             get
             {
                 return (this.LastMeteringIntervalIndex + 1) * this.LengthMeteringInterval;
             }
        }

        public MeteringIntervalsSubset(int firstMeteringIntervalIndex, int lastMeteringIntervalIndex, int lengthMeteringInterval)
        {
            this.FirstMeteringIntervalIndex = firstMeteringIntervalIndex;
            this.LastMeteringIntervalIndex = lastMeteringIntervalIndex;
            this.LengthMeteringInterval = lengthMeteringInterval;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (
                var meteringIntervalIndex = this.FirstMeteringIntervalIndex;
                meteringIntervalIndex <= this.LastMeteringIntervalIndex;
                meteringIntervalIndex++)
            {
                yield return meteringIntervalIndex;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (
                var meteringIntervalIndex = this.FirstMeteringIntervalIndex;
                meteringIntervalIndex <= this.LastMeteringIntervalIndex;
                meteringIntervalIndex++)
            {
                yield return meteringIntervalIndex;
            }
        }
    }
}