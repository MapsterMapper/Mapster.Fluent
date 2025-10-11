using Mapster.Fluent.Configs;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Reflection;

namespace Mapster.Fluent.Tests
{
    [TestClass]
    public class MapsterDITests
    {
        // ===== FLUENT CONFIGURATION API TESTS =====

        [TestMethod]
        public void AddMapsterFluent_WithFluentConfiguration_RegistersMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(config =>
            {
                config.NewConfig<TestUser, TestUserDto>()
                    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                    .Ignore(dest => dest.Id);
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
            var user = new TestUser { FirstName = "John", LastName = "Doe", Id = 1 };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("John Doe");
            dto.Id.ShouldBe(0);
        }

        [TestMethod]
        public void AddMapsterFluent_WithMultipleConfigurations_AppliesAll()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(config =>
            {
                config.NewConfig<TestUser, TestUserDto>()
                    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                    .Ignore(dest => dest.Id);
                config.NewConfig<TestProduct, TestProductDto>()
                    .Map(dest => dest.DisplayName, src => src.Name.ToUpper());
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            var user = new TestUser { FirstName = "Jane", LastName = "Smith", Id = 1 };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Jane Smith");
            userDto.Id.ShouldBe(0);

            var product = new TestProduct { Name = "widget" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("WIDGET");
        }

        [TestMethod]
        public void AddMapsterFluent_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.AddMapsterFluent(config => { }));
        }

        [TestMethod]
        public void AddMapsterFluent_WithNullConfigure_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.AddMapsterFluent(null));
        }

        [TestMethod]
        public void AddMapsterFluent_RegistersTypeAdapterConfigAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMapsterFluent(config => { });
            var provider = services.BuildServiceProvider();

            // Act
            ITypeAdapterConfig config1, config2;
            using (var scope1 = provider.CreateScope())
            {
                config1 = scope1.ServiceProvider.GetRequiredService<ITypeAdapterConfig>();
            }
            using (var scope2 = provider.CreateScope())
            {
                config2 = scope2.ServiceProvider.GetRequiredService<ITypeAdapterConfig>();
            }

            // Assert
            config1.ShouldBeSameAs(config2);
        }

        [TestMethod]
        public void AddMapsterFluent_RegistersServiceMapperAsScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMapsterFluent(config => { });
            var provider = services.BuildServiceProvider();

            // Act
            IMapper mapper1, mapper2;
            using (var scope1 = provider.CreateScope())
            {
                mapper1 = scope1.ServiceProvider.GetRequiredService<IMapper>();
            }
            using (var scope2 = provider.CreateScope())
            {
                mapper2 = scope2.ServiceProvider.GetRequiredService<IMapper>();
            }

            // Assert
            mapper1.ShouldNotBeSameAs(mapper2);
            mapper1.ShouldBeOfType<ServiceMapper>();
            mapper2.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void AddMapsterFluent_RegistersMapContextFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMapsterFluent(config => { });

            // Act
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IMapContextFactory>();

            // Assert
            factory.ShouldNotBeNull();
            factory.ShouldBeOfType<DefaultMapContextFactory>();
        }

        [TestMethod]
        public void AddMapsterFluent_WithEmptyConfiguration_StillRegistersMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(config => { });
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        // ===== ASSEMBLY SCANNING TESTS =====

        [TestMethod]
        public void AddMapsterFluent_WithAssemblyScanning_RegistersIRegisterImplementations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config => { }, // No fluent config
                options => options.AssembliesToScan = [Assembly.GetExecutingAssembly()]);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            var user = new TestUser { FirstName = "Assembly", LastName = "Scanned" };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("Assembly Scanned");
        }

        [TestMethod]
        public void AddMapsterFluent_WithMultipleAssemblies_ScansAllAssemblies()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config => { },
                options =>
                {
                    options.AssembliesToScan = [Assembly.GetExecutingAssembly(), typeof(TypeAdapterConfig).Assembly];
                });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
        }

        [TestMethod]
        public void AddMapsterFluent_WithScanningAndConfiguration_AppliesBoth()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config =>
                {
                    config.NewConfig<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => $"Product: {src.Name}");
                },
                options => options.AssembliesToScan = [Assembly.GetExecutingAssembly()]);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert - Test scanned mapping
            var user = new TestUser { FirstName = "Both", LastName = "Applied" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Both Applied");

            // Assert - Test additional configuration
            var product = new TestProduct { Name = "Test" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("Product: Test");
        }

        [TestMethod]
        public void ScanMapster_WithValidAssembly_RegistersIRegisterImplementations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ScanMapster(Assembly.GetExecutingAssembly());

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            var config = provider.GetRequiredService<ITypeAdapterConfig>();

            // Assert
            mapper.ShouldNotBeNull();
            config.ShouldNotBeNull();

            var user = new TestUser { FirstName = "Scanned", LastName = "User" };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("Scanned User");
        }

        [TestMethod]
        public void ScanMapster_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.ScanMapster(Assembly.GetExecutingAssembly()));
        }

        [TestMethod]
        public void ScanMapster_WithNullAssemblies_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                services.ScanMapster(null));
        }

        [TestMethod]
        public void ScanMapster_WithEmptyAssemblies_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                services.ScanMapster());
        }

        // ===== SERVICE MAPPER TESTS =====

        [TestMethod]
        public void AddMapsterFluent_WithUseServiceMapperFalse_RegistersBasicMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config => { },
                options => options.UseServiceMapper = false);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<Mapper>();
        }

        [TestMethod]
        public void AddMapsterFluent_WithUseServiceMapperTrue_RegistersServiceMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config => { },
                options => options.UseServiceMapper = true);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        // ===== ADD MAPSTER WITH CONFIG TESTS =====
        [Ignore("To prevent modification, access to TypeAdapterConfig should not be granted.")]
        [TestMethod]
        public void AddMapsterWithConfig_WithValidConfig_RegistersMapperAndConfig()
        {
            // Arrange
            var services = new ServiceCollection();
            var existingConfig = new TypeAdapterConfig();
            existingConfig.NewConfig<TestUser, TestUserDto>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                .Ignore(dest => dest.Id);

            // Act
            services.AddMapsterWithConfig(existingConfig);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            var config = provider.GetRequiredService<ITypeAdapterConfig>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
            config.ShouldBeSameAs(existingConfig);
            var user = new TestUser { FirstName = "John", LastName = "Doe", Id = 1 };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("John Doe");
            dto.Id.ShouldBe(0);
        }

        [TestMethod]
        public void AddMapsterWithConfig_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;
            var existingConfig = new TypeAdapterConfig();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => services.AddMapsterWithConfig(existingConfig));
        }

        [TestMethod]
        public void AddMapsterWithConfig_WithNullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => services.AddMapsterWithConfig(null));
        }
        [Ignore("To prevent modification, access to TypeAdapterConfig should not be granted.")]
        [TestMethod]
        public void AddMapsterWithConfig_RegistersTypeAdapterConfigAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var existingConfig = new TypeAdapterConfig();
            services.AddMapsterWithConfig(existingConfig);
            var provider = services.BuildServiceProvider();

            // Act
            ITypeAdapterConfig config1, config2;
            using (var scope1 = provider.CreateScope())
            {
                config1 = scope1.ServiceProvider.GetRequiredService<ITypeAdapterConfig>();
            }
            using (var scope2 = provider.CreateScope())
            {
                config2 = scope2.ServiceProvider.GetRequiredService<ITypeAdapterConfig>();
            }

            // Assert
            config1.ShouldBeSameAs(config2);
            config1.ShouldBeSameAs(existingConfig);
        }

        [TestMethod]
        public void AddMapsterWithConfig_RegistersServiceMapperAsScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            var existingConfig = new TypeAdapterConfig();
            services.AddMapsterWithConfig(existingConfig);
            var provider = services.BuildServiceProvider();

            // Act
            IMapper mapper1, mapper2;
            using (var scope1 = provider.CreateScope())
            {
                mapper1 = scope1.ServiceProvider.GetRequiredService<IMapper>();
            }
            using (var scope2 = provider.CreateScope())
            {
                mapper2 = scope2.ServiceProvider.GetRequiredService<IMapper>();
            }

            // Assert
            mapper1.ShouldNotBeSameAs(mapper2);
            mapper1.ShouldBeOfType<ServiceMapper>();
            mapper2.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void AddMapsterWithConfig_RegistersMapContextFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            var existingConfig = new TypeAdapterConfig();

            // Act
            services.AddMapsterWithConfig(existingConfig);
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IMapContextFactory>();

            // Assert
            factory.ShouldNotBeNull();
            factory.ShouldBeOfType<DefaultMapContextFactory>();
        }

        // ===== INTEGRATION TESTS =====

        [TestMethod]
        public void Integration_FluentConfiguration_ServiceMapper_AssemblyScanning_WorkTogether()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config =>
                {
                    config.NewConfig<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => $"Product: {src.Name}");
                    config.NewConfig<TestUser, TestUserDto>()
                        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                        .Ignore(dest => dest.Id);
                },
                options =>
                {
                    options.AssembliesToScan = [Assembly.GetExecutingAssembly()];
                    options.UseServiceMapper = true;
                });

            var provider = services.BuildServiceProvider();

            // Assert
            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            // Test scanned mapping (from IRegister)
            var user = new TestUser { FirstName = "Integration", LastName = "Test" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Integration Test");

            // Test fluent configuration
            var product = new TestProduct { Name = "Widget" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("Product: Widget");

            // Test ServiceMapper type
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void ScanMapster_UsesServiceMapperByDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ScanMapster(Assembly.GetExecutingAssembly());

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void FrozenConfigIsWork()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapsterFluent(
                config =>
                {
                    config.NewConfig<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => "Frozen DisplayName");
                    config.NewConfig<TestUser, TestUserDto>()
                        .Map(dest => dest.FullName, src => "Frozen FullName")
                        .Ignore(dest => dest.Id);

                    config.FrozenTypes<TestProduct, TestProductDto>();
                    config.FrozenTypes<TestUser, TestUserDto>();

                    config.NewConfig<TestUser, TestUserDto>()
                        .Map(dest => dest.FullName, src => $"{src.LastName} {src.FirstName}")
                        .Ignore(dest => dest.Id);

                    config.NewConfig<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => $"Product: {src.Name}");

                },
                options =>
                {
                    options.AssembliesToScan = [Assembly.GetExecutingAssembly()];
                    options.UseServiceMapper = true;
                });

            var provider = services.BuildServiceProvider();

            // Assert
            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            // Test scanned mapping (from IRegister)
            var user = new TestUser { FirstName = "Integration", LastName = "Test" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Frozen FullName");

            // Test fluent configuration
            var product = new TestProduct { Name = "Widget" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("Frozen DisplayName");

            // Test ServiceMapper type
            mapper.ShouldBeOfType<ServiceMapper>();
        }
    }


    // ===== TEST MODELS =====

    public class TestUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Id { get; set; }
    }

    public class TestUserDto
    {
        public string FullName { get; set; }
        public int Id { get; set; }
    }

    public class TestProduct
    {
        public string Name { get; set; }
    }

    public class TestProductDto
    {
        public string DisplayName { get; set; }
    }

    // ===== TEST IREGISTER IMPLEMENTATION =====

    public class TestUserMappingConfig : IRegister
    {
        public void Register(ITypeAdapterConfig config)
        {
            config.NewConfig<TestUser, TestUserDto>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
        }
    }
}
