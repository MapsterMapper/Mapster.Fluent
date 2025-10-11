using Mapster.Models;
using Mapster.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mapster.Fluent.Configs
{
    public class FrozenTypeAdapterConfig : BaseTypeAdapterConfigDecorator, ITypeAdapterConfig
    {

        private readonly ConcurrentDictionary<TypeTuple, TypeTuple> _frozentypes = new();
        private readonly ITypeAdapterConfig _dummyConfig = new TypeAdapterConfig();

        public bool IsTotalFrozen { get; private set; }

        public FrozenTypeAdapterConfig() : this(new TypeAdapterConfig())
        {
        }

        public FrozenTypeAdapterConfig(ITypeAdapterConfig config) : base(config.Clone())
        {
        }

        private FrozenTypeAdapterConfig(ITypeAdapterConfig config, bool isTotalFrozen) : base(config)
        {
            IsTotalFrozen = isTotalFrozen;
        }

        public override ITypeAdapterConfig GlobalSettings => new FrozenTypeAdapterConfig();

        public override TypeAdapterSetter ForType(Type sourceType, Type destinationType)
        {
            if (IsTotalFrozen ||
                _frozentypes.TryGetValue(new TypeTuple(sourceType, destinationType), out _))
            {
                _dummyConfig.Clear();
                return _dummyConfig.NewConfig(sourceType,destinationType);
            }

            return base.ForType(sourceType, destinationType);
        }

        public override TypeAdapterSetter NewConfig(Type sourceType, Type destinationType)
        {
            if (IsTotalFrozen ||
                _frozentypes.TryGetValue(new TypeTuple(sourceType, destinationType), out _))
            {
                _dummyConfig.Clear();
                return new TypeAdapterSetter(new TypeAdapterSettings(), _dummyConfig);
            }

            return base.NewConfig(sourceType, destinationType);
        }

        public override TypeAdapterSetter<TSource, TDestination> NewConfig<TSource, TDestination>()
        {
            if (IsTotalFrozen ||
                _frozentypes.TryGetValue(new TypeTuple(typeof(TSource), typeof(TDestination)), out _))
            {
                _dummyConfig.Clear();
                return _dummyConfig.NewConfig<TSource, TDestination>(); ;
            }

            return base.NewConfig<TSource, TDestination>();
        }

        public void FrozenTypes(Type sourceType, Type destinationType)
        {
            var types = new TypeTuple(sourceType, destinationType);
            _frozentypes.TryAdd(types, types);
        }

        public void FrozenTypes(TypeTuple types)
        {
            Compile(types.Source, types.Destination);
            _frozentypes.TryAdd(types, types);
        }

        public void FrozenTypes<TSource, TDestination>()
        {
            Compile(typeof(TSource), typeof(TDestination));

            var types = new TypeTuple(typeof(TSource), typeof(TDestination));
            _frozentypes.TryAdd(types, types);
        }

        public void DeepFreeze()
        {
            var keys = RuleMap.Keys.ToList();
            Compile();

            foreach (var item in keys)
            {
                _frozentypes.TryAdd(item, item);
            }

            IsTotalFrozen = true;
        }

        public override void Apply(IEnumerable<IRegister> registers)
        {
            foreach (IRegister register in registers)
            {
                register.Register(this);
            }
        }

       
        public override void Apply(params IRegister[] registers)
        {
            foreach (IRegister register in registers)
            {
                register.Register(this);
            }
        }

        public override void Apply(IEnumerable<Lazy<IRegister>> registers)
        {
            base.Apply(registers);
        }

        public override IList<IRegister> Scan(params Assembly[] assemblies)
        {
            List<IRegister> registers = assemblies.Select(assembly => assembly.GetLoadableTypes()
                .Where(x => typeof(IRegister).GetTypeInfo().IsAssignableFrom(x.GetTypeInfo()) && x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract))
                .SelectMany(registerTypes =>
                    registerTypes.Select(registerType => (IRegister)Activator.CreateInstance(registerType))).ToList();

            Apply(registers);
            return registers;
        }

        public override ITypeAdapterConfig Clone()
        {
            var result = new FrozenTypeAdapterConfig(base.Clone(), IsTotalFrozen);

            if (IsTotalFrozen)
                result.DeepFreeze();

            else if(_frozentypes.Any())
            {
                foreach(var type in _frozentypes)
                {
                    result.FrozenTypes(type.Key);
                }
            }

            return result;
        }

        public override ITypeAdapterConfig Fork(Action<ITypeAdapterConfig> action, [CallerFilePath] string key1 = "", [CallerLineNumber] int key2 = 0)
        {
            return base.Fork(action, key1, key2);
        }
    }
}
