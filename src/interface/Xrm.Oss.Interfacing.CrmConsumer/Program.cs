﻿using System;
using Castle.Windsor;
using Castle.Windsor.Installer;
using NLog;
using Topshelf;
using Xrm.Oss.Interfacing.Domain.Contracts;

namespace Xrm.Oss.Interfacing.CrmConsumer
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                IWindsorContainer container = null;

                x.Service<IService>(s =>
                {
                    s.BeforeStartingService(sc => sc.RequestAdditionalTime(TimeSpan.FromSeconds(30)));
                    s.BeforeStoppingService(sc => sc.RequestAdditionalTime(TimeSpan.FromSeconds(30)));

                    s.ConstructUsing(() =>
                    {
                        // container should be initialized in here according to Chris Patterson
                        // https://groups.google.com/d/msg/masstransit-discuss/Pz7ttS7niGQ/A-K7MTK8aiUJ
                        container = new WindsorContainer().Install(FromAssembly.This());

                        return container.Resolve<IService>();
                    });

                    s.WhenStarted(service => { service.Start(); });

                    s.WhenStopped(service =>
                    {
                        service.Stop();

                        if (container != null)
                        {
                            container.Dispose();
                        }
                    });

                    s.WhenShutdown(service =>
                    {
                        service.Stop();

                        if (container != null)
                        {
                            container.Dispose();
                        }
                    });
                });

                x.UseNLog();
                x.OnException(ex => _logger.Error(ex));
                x.EnableShutdown();
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.SetServiceName("Xrm-Oss-CrmConsumer");
                x.SetDisplayName("Xrm-Oss-CrmConsumer");
                x.SetDescription("Xrm-Oss-CrmConsumer");
            });
        }
    }
}
