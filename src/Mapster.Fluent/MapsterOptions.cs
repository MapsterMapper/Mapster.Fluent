using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Fluent
{
    /// <summary>
    /// Options for configuring Mapster dependency injection.
    /// </summary>
    public class MapsterOptions
    {
        /// <summary>
        /// Gets or sets the assemblies to scan for IRegister implementations and IMapFrom patterns.
        /// </summary>
        public ICollection<Assembly> AssembliesToScan { get; set; } = new List<Assembly>();

        /// <summary>
        /// Gets or sets whether to use ServiceMapper (with DI support) instead of regular Mapper.
        /// Default is true.
        /// </summary>
        public bool UseServiceMapper { get; set; } = true;

        public Action<ITypeAdapterConfig> ConfigureAction { get; set; }
    }
}
