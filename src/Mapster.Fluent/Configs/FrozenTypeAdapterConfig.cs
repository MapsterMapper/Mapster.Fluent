using Mapster.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mapster.Fluent.Configs
{
    public class FrozenTypeAdapterConfig : BaseTypeAdapterConfigDecorator, ITypeAdapterConfig
    {

        private readonly ConcurrentDictionary<TypeTuple, TypeTuple> _frozentypes = new();
        private readonly ITypeAdapterConfig _dummyConfig = new TypeAdapterConfig();

        public bool IsTotalFrozen { get; private set; }

       

        private FrozenTypeAdapterConfig(ITypeAdapterConfig config, bool isTotalFrozen, bool IsGlobal = false ) 
        {
            IsTotalFrozen = isTotalFrozen;
        }

        public FrozenTypeAdapterConfig(bool IsGlobal = false) : base(IsGlobal)
        {
        }

        public FrozenTypeAdapterConfig(ITypeAdapterConfig config, bool IsGlobal = false) : base(config, IsGlobal)
        {
        }

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

        public override void Apply(IEnumerable<IRegister> registers)
        {
            foreach (var item in registers)
            {
                item.Register(this);
            }
        }

        public override void Remove(Type sourceType, Type destinationType)
        {
            if (IsTotalFrozen ||
                _frozentypes.TryGetValue(new TypeTuple(sourceType, destinationType), out _)) ;
            else
                base.Remove(sourceType, destinationType);
        }
    }
}
