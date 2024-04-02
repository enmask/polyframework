using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Timer
    {
        public double TimerValue { get; private set; }
        public double MeasuredTime { get; private set; }

        public Timer()
        {
            Reset();
        }

        public void Update(double elapsedTime)
        {
            TimerValue += elapsedTime;
        }

        public void CaptureTime()
        {
            MeasuredTime = TimerValue;
            Reset();
        }

        private void Reset()
        {
            TimerValue = 0.0;
        }
    }



}
