using System;
using StructureMap.Configuration.DSL;
using StructureMap.Configuration.DSL.Expressions;
using StructureMap.Graph.Scanning;

namespace StructureMap.Graph
{
    // SAMPLE: IRegistrationConvention
    public interface IRegistrationConvention
    {
        [Obsolete("Going to remove this")]
        void Process(Type type, Registry registry);


        Registry ScanTypes(TypeSet types);
    }
    // ENDSAMPLE

    /// <summary>
    /// Allows built-in registration conventions to be configurable through the assembly scanning DSL
    /// </summary>
    /// <remarks>
    /// Intended for StructureMap internal use only. 
    /// Custom registration convention instances can be directly configured 
    /// before being passed to IAssemblyScanner.With(IRegistrationConvention).
    /// </remarks>
    public abstract class ConfigurableRegistrationConvention : IRegistrationConvention
    {
        protected Action<GenericFamilyExpression> ConfigureFamily = x => { };

        public void SetFamilyConfigurationAction(Action<GenericFamilyExpression> configureFamily)
        {
            ConfigureFamily = configureFamily;
        }

        public abstract void Process(Type type, Registry registry);

        public abstract Registry ScanTypes(TypeSet types);
    }
}