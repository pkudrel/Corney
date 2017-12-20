

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Corney.Core.Common.Infrastructure;
using MediatR;
using MediatR.Pipeline;
using Module = Autofac.Module;

namespace Corney.Core.Common.App.Config
{
    /// <summary>
    /// Mediator module
    /// </summary>
    /// <remarks>
    /// Links:
    /// https://lostechies.com/jimmybogard/2014/09/09/tackling-cross-cutting-concerns-with-a-mediator-pipeline/
    /// https://gist.github.com/NotMyself/07ea9ac7884d8d29e147
    /// https://gist.github.com/NotMyself/579f94e1aad6a022ddb9
    /// </remarks>
    public class MediatorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var bly = AssemblyCollector.Instance.GetAssemblies();

            //  builder.RegisterSource(new ContravariantRegistrationSource());
            builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly).AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(bly)
                .Where(t =>
                    t.GetInterfaces().Any(i => i.IsClosedTypeOf(typeof(IRequestHandler<,>))
                                               || i.IsClosedTypeOf(typeof(IRequestHandler<,>))
                                               || i.IsClosedTypeOf(typeof(INotificationHandler<>))
                    )
                )
                .AsImplementedInterfaces();
            ;

            builder.RegisterGeneric(typeof(RequestPreProcessorBehavior<,>)).AsImplementedInterfaces();
            builder.RegisterGeneric(typeof(RequestPostProcessorBehavior<,>)).AsImplementedInterfaces();


            builder.Register<SingleInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t =>
                {
                    object o;
                    return c.TryResolve(t, out o) ? o : null;
                };
            });
            builder.Register<MultiInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t => (IEnumerable<object>)c.Resolve(typeof(IEnumerable<>).MakeGenericType(t));
            });
        }
    }
}