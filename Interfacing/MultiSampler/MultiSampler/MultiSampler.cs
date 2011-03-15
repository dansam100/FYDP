﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NationalInstruments.DAQmx;

namespace MultiSampler
{
    public class MultiSampler
    {
        private Sampler halleffectSampler;
        private Sampler potSampler;
        private Sampler testSampler;

        public MultiSampler()
        {
            //this.halleffectSampler = new Sampler(new HallEffectReader("HallEffectSensor"));
            this.potSampler = new Sampler(new PotReader("Potentiometer"));
            //this.testSampler = new Sampler(new ReadTester("Test"));
        }


        public void BeginSampling()
        {
            //this.halleffectSampler.Start();
            this.potSampler.Start();
            //this.testSampler.Start();
        }


        public void StopSampling()
        {
            //this.halleffectSampler.Stop();
            this.potSampler.Stop();
            //this.testSampler.Stop();
        }
    }
}
