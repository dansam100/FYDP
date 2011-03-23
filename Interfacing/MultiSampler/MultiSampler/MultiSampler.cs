using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NationalInstruments.DAQmx;
using MultiSampler.Readers;

namespace MultiSampler
{
    public class MultiSampler
    {
        private Sampler halleffectSampler;
        private Sampler potSampler;
        private Sampler IMUSampler;
        private Sampler testSampler;
        private Sampler activeHighSampler;

        public MultiSampler()
        {
            this.halleffectSampler = new Sampler(new HallEffectReader("HallEffectSensor", "127.0.0.1", 9192));
            this.activeHighSampler = new Sampler(new ActiveReader("Break", "127.0.0.1", 9193));
            this.potSampler = new Sampler(new PotReader("Potentiometer", "127.0.0.1", 9191));
            //this.IMUSampler = new Sampler(new IMUReader("IMU", "127.0.0.1", 9193, "COM8"));
            this.testSampler = new Sampler(new ReadTester("Test"));
        }


        public void BeginSampling()
        {
<<<<<<< HEAD
            this.halleffectSampler.Start();
            this.potSampler.Start();
            //this.IMUSampler.Start();
=======
            //this.halleffectSampler.Start();
            this.activeHighSampler.Start();
            //this.potSampler.Start();
>>>>>>> 791f173038d7d61cf7b866a2576bcf7629b0365c
            //this.testSampler.Start();
        }


        public void StopSampling()
        {
<<<<<<< HEAD
            this.halleffectSampler.Stop();
            this.potSampler.Stop();
            //this.IMUSampler.Stop();
=======
            //this.halleffectSampler.Stop();
            this.activeHighSampler.Stop();
            //this.potSampler.Stop();
>>>>>>> 791f173038d7d61cf7b866a2576bcf7629b0365c
            //this.testSampler.Stop();
        }
    }
}
