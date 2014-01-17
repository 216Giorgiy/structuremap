using System;
using StructureMap.Building.Interception;

namespace StructureMap.Pipeline
{
    public interface IConfiguredInstance
    {
        string Name { get; set; }
        Type PluggedType { get; }

        DependencyCollection Dependencies { get; }

        void AddInterceptor(IInterceptor interceptor);
        ConstructorInstance Override(ExplicitArguments arguments);

        bool IsUnique();

        bool IsSingleton();

        void SetLifecycleTo<T>() where T : ILifecycle, new();

        void SetLifecycleTo(ILifecycle lifecycle);

        ILifecycle Lifecycle { get; }
    }
}