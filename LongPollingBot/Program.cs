using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;
using Quartz.Impl;

namespace LongPollingBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<TelegramJob>().Build();

            var trigger = TriggerBuilder.Create()
                            .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
                            .Build();

            scheduler.ScheduleJob(job, trigger);
        }
    }
}
