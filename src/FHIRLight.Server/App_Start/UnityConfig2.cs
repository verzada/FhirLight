using System.Web.Http;
using System.Web.Mvc;
using FHIRLight.Library.Interface;
using FHIRLight.Library.Spark.Engine.Core;
using FHIRLight.Library.Spark.Engine.FhirResponseFactory;
using FHIRLight.Library.Spark.Engine.Interfaces;
using FHIRLight.Library.Spark.Engine.Model;
using FHIRLight.Library.Spark.Engine.Service;
using FHIRLight.Library.Spark.Engine.Service.FhirServiceExtensions;
using Hl7.Fhir.Model;
using Microsoft.Practices.Unity;
using Spark.Engine.Core;
using Spark.Filters;
using Unity.WebApi;
using IFhirResponseFactory = Spark.Engine.FhirResponseFactory.IFhirResponseFactory;

namespace FHIRLight.Server
{
    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config)
        {
            var container = GetUnityContainer();

            // e.g. container.RegisterType<ITestService, TestService>();
            IControllerFactory unityControllerFactory = new UnityControllerFactory(container);
            ControllerBuilder.Current.SetControllerFactory(unityControllerFactory);
            
            config.DependencyResolver = new UnityDependencyResolver(container);            
        }

        public static UnityContainer GetUnityContainer()
        {
            var container = new UnityContainer();

#if DEBUG
            container.AddNewExtension<UnityLogExtension>();
#endif
            //container.RegisterType<HomeController, HomeController>(new PerResolveLifetimeManager(),
            //    new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());
            container.RegisterType<ILocalhost, Localhost>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.Endpoint));
        
            container.RegisterType<ITransfer, Transfer>(new ContainerControlledLifetimeManager());

            //container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            //container.RegisterType<IFhirIndex, MongoFhirIndex>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirResponseFactoryOld, FhirResponseFactoryOld>();
            container.RegisterType<IFhirResponseFactory, FhirResponseFactory>();
            container.RegisterType<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            container.RegisterType<IFhirResponseInterceptor, ConditionalHeaderFhirResponseInterceptor>("ConditionalHeaderFhirResponseInterceptor");
            container.RegisterType<IFhirModel, FhirModel>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(SparkModelInfo.SparkSearchParameters));
            container.RegisterType<FhirPropertyIndex>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(container.Resolve<IFhirModel>()));

            container.RegisterType<CompressionHandler>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(Settings.MaximumDecompressedBodySizeInBytes));

            container.RegisterType<IFhirService, FhirService>(new ContainerControlledLifetimeManager());

            container.RegisterType<IServiceListener, SearchService>("searchListener");
            container.RegisterType<IFhirServiceExtension, SearchService>("search");
            container.RegisterType<ISearchService, SearchService>();
            container.RegisterType<IFhirServiceExtension, TransactionService>("transaction");
            container.RegisterType<IFhirServiceExtension, PagingService>("paging");
            container.RegisterType<IFhirServiceExtension, ResourceStorageService>("storage");
            container.RegisterType<IFhirServiceExtension, ConformanceService>("conformance");
            container.RegisterType<ICompositeServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            return container;
        }
    }
}