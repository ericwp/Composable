﻿using System;

namespace Composable.DependencyInjection
{
    public static class ComponentRegistrationExtensions
    {
        class ComponentPromise<TService> where TService : class
        {
            readonly object _lock = new object();
            bool _unInitialized = true;
            bool _isComposableContainer;
            TService _singletonInstance;
            Lifestyle _lifestyle;
            public TService Resolve(IServiceLocatorKernel kern)
            {
                if(_unInitialized)
                {
                    lock(_lock)
                    {
                        if(_unInitialized)
                        {
                            if(kern is ComposableDependencyInjectionContainer container)
                            {
                                _isComposableContainer = true;
                                var registration = container.GetRegistrationFor<TService>();
                                _lifestyle = registration.Lifestyle;
                                switch(registration.Lifestyle)
                                {
                                    case Lifestyle.Singleton:
                                        _singletonInstance = kern.Resolve<TService>();
                                        break;
                                    case Lifestyle.Scoped:
                                        //performance: Custom method for resolving scoped components.
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }

                            _unInitialized = false;
                        }
                    }
                }

                if(_isComposableContainer)
                {
                    switch(_lifestyle)
                    {
                        case Lifestyle.Singleton:
                            return _singletonInstance;
                        case Lifestyle.Scoped:
                            return kern.Resolve<TService>();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return kern.Resolve<TService>();
            }
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TImplementation> factoryMethod) where TService : class
                                                 where TImplementation : TService
        {
            return @this.CreatedBy(_ => factoryMethod());
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TImplementation> factoryMethod) where TService : class
                                                               where TDependency1 : class
                                                               where TImplementation : TService
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                             where TDependency1 : class
                                                                             where TDependency2 : class
                                                                             where TImplementation : TService
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TImplementation> factoryMethod) where TImplementation : TService
                                                                                           where TService : class
                                                                                           where TDependency1 : class
                                                                                           where TDependency2 : class
                                                                                           where TDependency3 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                         where TService : class
                                                                                                         where TDependency1 : class
                                                                                                         where TDependency2 : class
                                                                                                         where TDependency3 : class
                                                                                                         where TDependency4 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                       where TService : class
                                                                                                                       where TDependency1 : class
                                                                                                                       where TDependency2 : class
                                                                                                                       where TDependency3 : class
                                                                                                                       where TDependency4 : class
                                                                                                                       where TDependency5 : class
        {
            return @this.CreatedBy(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>()));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                     where TService : class
                                                                                                                                     where TDependency1 : class
                                                                                                                                     where TDependency2 : class
                                                                                                                                     where TDependency3 : class
                                                                                                                                     where TDependency4 : class
                                                                                                                                     where TDependency5 : class
                                                                                                                                     where TDependency6 : class
        {
            return @this.CreatedBy(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>()));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                   where TService : class
                                                                                                                                                   where TDependency1 : class
                                                                                                                                                   where TDependency2 : class
                                                                                                                                                   where TDependency3 : class
                                                                                                                                                   where TDependency4 : class
                                                                                                                                                   where TDependency5 : class
                                                                                                                                                   where TDependency6 : class
                                                                                                                                                   where TDependency7 : class
        {
            return @this.CreatedBy(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>()));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                 where TService : class
                                                                                                                                                                 where TDependency1 : class
                                                                                                                                                                 where TDependency2 : class
                                                                                                                                                                 where TDependency3 : class
                                                                                                                                                                 where TDependency4 : class
                                                                                                                                                                 where TDependency5 : class
                                                                                                                                                                 where TDependency6 : class
                                                                                                                                                                 where TDependency7 : class
                                                                                                                                                                 where TDependency8 : class
        {
            return @this.CreatedBy(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>()));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                               where TService : class
                                                                                                                                                                               where TDependency1 : class
                                                                                                                                                                               where TDependency2 : class
                                                                                                                                                                               where TDependency3 : class
                                                                                                                                                                               where TDependency4 : class
                                                                                                                                                                               where TDependency5 : class
                                                                                                                                                                               where TDependency6 : class
                                                                                                                                                                               where TDependency7 : class
                                                                                                                                                                               where TDependency8 : class
                                                                                                                                                                               where TDependency9 : class
        {
            return @this.CreatedBy(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>()));
        }
    }
}
