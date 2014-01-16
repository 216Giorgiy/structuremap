using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap.Pipeline;
using StructureMap.TypeRules;
using StructureMap.Util;

namespace StructureMap.Graph
{
    /// <summary>
    ///     Conceptually speaking, a PluginFamily object represents a point of abstraction or variability in
    ///     the system.  A PluginFamily defines a CLR Type that StructureMap can build, and all of the possible
    ///     Plugin�s implementing the CLR Type.
    /// </summary>
    public class PluginFamily : HasLifecycle, IDisposable
    {
        private readonly Cache<string, Instance> _instances = new Cache<string, Instance>(delegate { return null; });
        private readonly Type _pluginType;
        private Lazy<Instance> _defaultInstance;
        private Lazy<Instance> _fallBack = new Lazy<Instance>(() => null);
        private Instance _missingInstance;


        public PluginFamily(Type pluginType)
        {
            _pluginType = pluginType;

            resetDefault();

            Attribute.GetCustomAttributes(_pluginType, typeof (FamilyAttribute), true).OfType<FamilyAttribute>()
                .Each(x => x.Alter(this));
        }

        /// <summary>
        /// The PluginGraph that "owns" this PluginFamily
        /// </summary>
        public PluginGraph Owner { get; internal set; }

        /// <summary>
        /// All the Instances held by this family
        /// </summary>
        public IEnumerable<Instance> Instances
        {
            get { return _instances.GetAll(); }
        }

        /// <summary>
        /// Does this PluginFamily represent an open generic type?
        /// </summary>
        public bool IsGenericTemplate
        {
            get { return _pluginType.IsGenericTypeDefinition || _pluginType.ContainsGenericParameters; }
        }

        /// <summary>
        /// Can be used to create an object for a named Instance that does not exist
        /// </summary>
        public Instance MissingInstance
        {
            get { return _missingInstance; }
            set
            {
                if (value != null)
                {
                    assertInstanceIsValidForThisPluginType(value);
                }

                _missingInstance = value;
            }
        }

        /// <summary>
        ///     The CLR Type that defines the "Plugin" interface for the PluginFamily
        /// </summary>
        public Type PluginType
        {
            get { return _pluginType; }
        }

        void IDisposable.Dispose()
        {
            _instances.Each(x => x.SafeDispose());
        }

        private void resetDefault()
        {
            _defaultInstance = new Lazy<Instance>(determineDefault);
            _fallBack = new Lazy<Instance>(() => null);
        }

        public void AddInstance(Instance instance)
        {
            if (instance == null) throw new ArgumentNullException("instance");

            assertInstanceIsValidForThisPluginType(instance);

            instance.Parent = this;
            _instances[instance.Name] = instance;
        }

        private void assertInstanceIsValidForThisPluginType(Instance instance)
        {
            if (instance.ReturnedType != null &&
                !instance.ReturnedType.CanBeCastTo(_pluginType))
            {
                throw new ArgumentOutOfRangeException(
                    "instance '{0}' with ReturnType {1} cannot be cast to {2}".ToFormat(instance.Description,
                        instance.ReturnedType.GetFullName(), _pluginType.GetFullName()));
            }
        }

        // TODO -- re-evaluate this
        public void SetDefault(Func<Instance> defaultInstance)
        {
            _defaultInstance = new Lazy<Instance>(defaultInstance);
        }

        public void SetDefault(Instance instance)
        {
            AddInstance(instance);
            _defaultInstance = new Lazy<Instance>(() => instance);
        }

        public void SetFallback(Instance instance)
        {
            _fallBack = new Lazy<Instance>(() => instance);
        }

        public Instance GetInstance(string name)
        {
            return _instances[name];
        }

        public Instance GetDefaultInstance()
        {
            return _defaultInstance.Value ?? _fallBack.Value;
        }

        private Instance determineDefault()
        {
            if (_instances.Count == 1)
            {
                return _instances.Single();
            }

            if (_pluginType.IsConcrete() && Policies.CanBeAutoFilled(_pluginType))
            {
                var instance = new ConfiguredInstance(_pluginType);
                AddInstance(instance);

                return instance;
            }

            return null;
        }

        public PluginFamily CreateTemplatedClone(Type[] templateTypes)
        {
            var templatedType = _pluginType.MakeGenericType(templateTypes);
            var templatedFamily = new PluginFamily(templatedType);
            templatedFamily.copyLifecycle(this);

            _instances.GetAll().Select(x => {
                var clone = x.CloseType(templateTypes);
                if (clone == null) return null;

                clone.Name = x.Name;
                return clone;
            }).Where(x => x != null).Each(templatedFamily.AddInstance);

            if (GetDefaultInstance() != null)
            {
                var defaultKey = GetDefaultInstance().Name;
                var @default = templatedFamily.Instances.FirstOrDefault(x => x.Name == defaultKey);
                if (@default != null)
                {
                    templatedFamily.SetDefault(@default);
                }
            }

            //Are there instances that close the templatedtype straight away?
            _instances.GetAll()
                .Where(x => x.ReturnedType.CanBeCastTo(templatedType))
                .Each(templatedFamily.AddInstance);

            return templatedFamily;
        }

        private bool hasType(Type concreteType)
        {
            return _instances.Any(x => x.ReturnedType == concreteType);
        }

        /// <summary>
        /// Add a single concrete type as a new Instance with a derived name.
        /// Is idempotent.
        /// </summary>
        /// <param name="concreteType"></param>
        public void AddType(Type concreteType)
        {
            if (!concreteType.CanBeCastTo(_pluginType)) return;

            if (!hasType(concreteType))
            {
                AddType(concreteType, concreteType.AssemblyQualifiedName);
            }
        }

        /// <summary>
        /// The Policies from the root PluginGraph containing this PluginFamily
        /// or a default set of Policies if none supplied
        /// </summary>
        public Policies Policies
        {
            get
            {
                if (Owner == null || Owner.Root == null) return new Policies();

                return Owner.Root.Policies;
            }
        }

        /// <summary>
        /// Adds a new Instance for the concreteType with a name
        /// </summary>
        /// <param name="concreteType"></param>
        /// <param name="name"></param>
        public void AddType(Type concreteType, string name)
        {
            if (!concreteType.CanBeCastTo(_pluginType)) return;

            if (!hasType(concreteType) && Policies.CanBeAutoFilled(concreteType))
            {
                AddInstance(new ConstructorInstance(concreteType, name));
            }
        }

        /// <summary>
        /// completely removes an Instance from a PluginFamily
        /// </summary>
        /// <param name="instance"></param>
        public void RemoveInstance(Instance instance)
        {
            _instances.Remove(instance.Name);
            instance.Parent = null;
            if (instance == GetDefaultInstance())
            {
                resetDefault();
            }
        }

        /// <summary>
        /// Removes all Instances and resets the default Instance determination
        /// </summary>
        public void RemoveAll()
        {
            _instances.ClearAll();
            resetDefault();
        }
    }
}