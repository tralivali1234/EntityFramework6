// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Types
{
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Configuration.Mapping;
    using System.Data.Entity.ModelConfiguration.Configuration.Properties.Primitive;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.ModelConfiguration.Edm.Services;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using Moq;
    using Xunit;
    using System.Data.Entity.Infrastructure.Annotations;

    public sealed class EntityTypeConfigurationTests
    {
        [Fact]
        public void MapToStoredProcedures_should_create_empty_function_mapping_configuration()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            Assert.Null(entityTypeConfiguration.ModificationStoredProceduresConfiguration);

            entityTypeConfiguration.MapToStoredProcedures();

            Assert.NotNull(entityTypeConfiguration.ModificationStoredProceduresConfiguration);
        }

        [Fact]
        public void Can_pass_function_mapping_configuration_to_map_to_functions()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            Assert.Null(entityTypeConfiguration.ModificationStoredProceduresConfiguration);

            entityTypeConfiguration.MapToStoredProcedures(new ModificationStoredProceduresConfiguration(), true);

            Assert.NotNull(entityTypeConfiguration.ModificationStoredProceduresConfiguration);
        }

        [Fact]
        public void Configure_should_configure_modification_functions()
        {
            var model = new EdmModel(DataSpace.CSpace);

            var entityType = model.AddEntityType("E");
            entityType.GetMetadataProperties().SetClrType(typeof(object));

            model.AddEntitySet("ESet", entityType);

            var modificationFunctionsConfigurationMock = new Mock<ModificationStoredProceduresConfiguration>();

            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            entityTypeConfiguration.MapToStoredProcedures(modificationFunctionsConfigurationMock.Object, true);

            entityType.SetConfiguration(entityTypeConfiguration);

            var databaseMapping
                = new DatabaseMappingGenerator(ProviderRegistry.Sql2008_ProviderInfo, ProviderRegistry.Sql2008_ProviderManifest).Generate(model);

            entityTypeConfiguration.Configure(entityType, databaseMapping, ProviderRegistry.Sql2008_ProviderManifest);

            modificationFunctionsConfigurationMock
                .Verify(
                    m => m.Configure(
                        It.IsAny<EntityTypeModificationFunctionMapping>(), It.IsAny<DbProviderManifest>()),
                    Times.Once());
        }

        [Fact]
        public void Configure_should_set_configuration()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace));

            Assert.Same(entityTypeConfiguration, entityType.GetConfiguration());
        }

        [Fact]
        public void Configure_should_configure_entity_set_name()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var entitySet = model.AddEntitySet("ESet", entityType);

            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object))
                                              {
                                                  EntitySetName = "MySet"
                                              };

            entityTypeConfiguration.Configure(entityType, model);

            Assert.Equal("MySet", entitySet.Name);
            Assert.Same(entityTypeConfiguration, entitySet.GetConfiguration());
        }

        [Fact]
        public void Configure_should_configure_properties()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var property = EdmProperty.CreatePrimitive("P", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property);
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            var mockPropertyConfiguration = new Mock<PrimitivePropertyConfiguration>();
            var mockPropertyInfo = new MockPropertyInfo();
            property.SetClrPropertyInfo(mockPropertyInfo);
            entityTypeConfiguration.Property(new PropertyPath(mockPropertyInfo), () => mockPropertyConfiguration.Object);

            entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace));

            mockPropertyConfiguration.Verify(p => p.Configure(property));
        }

        [Fact]
        public void Configure_should_throw_when_property_not_found()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            var mockPropertyConfiguration = new Mock<PrimitivePropertyConfiguration>();
            entityTypeConfiguration.Property(new PropertyPath(new MockPropertyInfo()), () => mockPropertyConfiguration.Object);

            Assert.Equal(
                Strings.PropertyNotFound(("P"), "E"),
                Assert.Throws<InvalidOperationException>(
                    () => entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace))).Message);
        }

        [Fact]
        public void Can_get_and_set_table_name()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            entityTypeConfiguration.ToTable("Foo");

            Assert.Equal("Foo", entityTypeConfiguration.GetTableName().Name);
        }

        [Fact]
        public void TableName_returns_current_TableName()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            Assert.Equal(null, entityTypeConfiguration.TableName);

            entityTypeConfiguration.ToTable("Foo");
            Assert.Equal("Foo", entityTypeConfiguration.TableName);
        }

        [Fact]
        public void SchemaName_returns_current_SchemaName()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            Assert.Equal(null, entityTypeConfiguration.SchemaName);

            entityTypeConfiguration.ToTable("Foo", "Bar");
            Assert.Equal("Bar", entityTypeConfiguration.SchemaName);
        }

        [Fact]
        public void GetTableName_returns_current_TableName()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            Assert.Equal(null, entityTypeConfiguration.GetTableName());

            entityTypeConfiguration.ToTable("Foo");
            Assert.Equal("Foo", entityTypeConfiguration.GetTableName().Name);
        }

        [Fact]
        public void ToTable_overwrites_existing_name()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));

            entityTypeConfiguration.ToTable("Foo");
            entityTypeConfiguration.ToTable("Bar");

            Assert.Equal("Bar", entityTypeConfiguration.GetTableName().Name);
        }

        [Fact]
        public void Configure_should_configure_and_order_keys_when_keys_and_order_specified()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var property = EdmProperty.CreatePrimitive("P2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property);
            var property1 = EdmProperty.CreatePrimitive("P1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property1);

            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            var mockPropertyInfo2 = new MockPropertyInfo(typeof(int), "P2");
            entityTypeConfiguration.Key(mockPropertyInfo2);
            entityTypeConfiguration.Property(new PropertyPath(mockPropertyInfo2)).ColumnOrder = 1;
            (entityType.GetDeclaredPrimitiveProperties().SingleOrDefault(p => p.Name == "P2")).SetClrPropertyInfo(mockPropertyInfo2);
            var mockPropertyInfo1 = new MockPropertyInfo(typeof(int), "P1");
            entityTypeConfiguration.Key(mockPropertyInfo1);
            entityTypeConfiguration.Property(new PropertyPath(mockPropertyInfo1)).ColumnOrder = 0;
            (entityType.GetDeclaredPrimitiveProperties().SingleOrDefault(p => p.Name == "P1")).SetClrPropertyInfo(mockPropertyInfo1);

            entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace));

            Assert.Equal(2, entityType.KeyProperties.Count);
            Assert.Equal("P1", entityType.KeyProperties.First().Name);
        }

        [Fact]
        public void Configure_should_throw_when_key_properties_and_not_root_type()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace)
                                 {
                                     BaseType = new EntityType("E", "N", DataSpace.CSpace)
                                 };
            var type = typeof(string);

            entityType.BaseType.GetMetadataProperties().SetClrType(type);
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            entityTypeConfiguration.Key(new MockPropertyInfo(typeof(int), "Id"));

            Assert.Equal(
                Strings.KeyRegisteredOnDerivedType(typeof(object), typeof(string)),
                Assert.Throws<InvalidOperationException>(
                    () => entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace))).Message);
        }

        [Fact]
        public void Configure_should_throw_when_key_property_not_found()
        {
            var entityType = new EntityType("E", "N", DataSpace.CSpace);
            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            var mockPropertyInfo = new MockPropertyInfo(typeof(int), "Id");
            entityTypeConfiguration.Key(mockPropertyInfo);

            Assert.Equal(
                Strings.KeyPropertyNotFound(("Id"), "E"),
                Assert.Throws<InvalidOperationException>(
                    () => entityTypeConfiguration.Configure(entityType, new EdmModel(DataSpace.CSpace))).Message);
        }

        [Fact]
        public void AddMappingConfiguration_multiple_mapping_fragments_for_same_table_should_throw()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(new MockType("E1"));
            var entityMappingConfiguration1 =
                new EntityMappingConfiguration
                    {
                        TableName = new DatabaseName("E1Table")
                    };
            entityTypeConfiguration.AddMappingConfiguration(entityMappingConfiguration1);

            Assert.Equal(
                Strings.InvalidTableMapping("E1", "E1Table"), Assert.Throws<InvalidOperationException>(
                    () => entityTypeConfiguration
                              .AddMappingConfiguration(
                                  new EntityMappingConfiguration
                                      {
                                          TableName = new DatabaseName("E1Table")
                                      })).Message);
        }

        [Fact]
        public void AddMappingConfiguration_multiple_mapping_fragments_with_no_table_name_throws()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(new MockType("E1"));
            var entityMappingConfiguration1 =
                new EntityMappingConfiguration();
            entityTypeConfiguration.AddMappingConfiguration(entityMappingConfiguration1);

            Assert.Equal(
                Strings.InvalidTableMapping_NoTableName("E1"), Assert.Throws<InvalidOperationException>(
                    () => entityTypeConfiguration
                              .AddMappingConfiguration(
                                  new EntityMappingConfiguration
                                      {
                                          TableName = new DatabaseName("E1Table")
                                      })).Message);
        }

        [Fact]
        public void AddMappingConfiguration_multiple_mapping_fragments_for_different_tables_allowed()
        {
            var entityTypeConfiguration = new EntityTypeConfiguration(new MockType("E1"));
            var entityMappingConfiguration1 =
                new EntityMappingConfiguration
                    {
                        TableName = new DatabaseName("E1Table")
                    };
            entityTypeConfiguration.AddMappingConfiguration(entityMappingConfiguration1);

            entityTypeConfiguration.AddMappingConfiguration(
                new EntityMappingConfiguration
                    {
                        TableName = new DatabaseName("E1TableExtended")
                    });
        }

        [Fact]
        public void Key_appends_key_members_when_set_by_attributes()
        {
            var config = new EntityTypeConfiguration(typeof(AType1));

            config.Key(typeof(AType1).GetDeclaredProperty("Key1"), null);
            config.Key(typeof(AType1).GetDeclaredProperty("Key2"), null);

            Assert.Equal(2, config.KeyProperties.Count());
            Assert.Equal("Key1", config.KeyProperties.First().Name);
            Assert.Equal("Key2", config.KeyProperties.Last().Name);
        }

        public class AType1
        {
            public int Key1 { get; set; }
            public int Key2 { get; set; }
        }

        [Fact]
        public void Key_appends_key_members_when_not_set_by_attributes()
        {
            var config = new EntityTypeConfiguration(typeof(AType1));

            config.Key(typeof(AType1).GetDeclaredProperty("Key1"));
            config.Key(typeof(AType1).GetDeclaredProperty("Key2"));

            Assert.Equal(2, config.KeyProperties.Count());
            Assert.Equal("Key1", config.KeyProperties.First().Name);
            Assert.Equal("Key2", config.KeyProperties.Last().Name);
        }

        [Fact]
        public void Index_creates_index_configuration()
        {
            var config = new EntityTypeConfiguration(typeof(AType1));

            config.Index(new PropertyPath(new[] { 
                typeof(AType1).GetDeclaredProperty("Key1"),
                typeof(AType1).GetDeclaredProperty("Key2")
            }));

            config.Index(new PropertyPath(new[] { 
                typeof(AType1).GetDeclaredProperty("Key2"),
                typeof(AType1).GetDeclaredProperty("Key1")
            }));

            Assert.Equal(2, config.PropertyIndexes.Count());

            Assert.Equal(2, config.PropertyIndexes.First().Count());
            Assert.Equal("Key1", config.PropertyIndexes.First().First().Name);
            Assert.Equal("Key2", config.PropertyIndexes.First().Last().Name);


            Assert.Equal(2, config.PropertyIndexes.Last().Count());
            Assert.Equal("Key2", config.PropertyIndexes.Last().First().Name);
            Assert.Equal("Key1", config.PropertyIndexes.Last().Last().Name);
        }

        [Fact]
        public void Index_creates_index_configuration_and_doesnt_duplicate()
        {
            var config = new EntityTypeConfiguration(typeof(AType1));

            config.Index(new PropertyPath(new[] { 
                typeof(AType1).GetDeclaredProperty("Key1"),
                typeof(AType1).GetDeclaredProperty("Key2")
            }));

            // Duplicate
            config.Index(new PropertyPath(new[] { 
                typeof(AType1).GetDeclaredProperty("Key1"),
                typeof(AType1).GetDeclaredProperty("Key2")
            }));


            Assert.Equal(1, config.PropertyIndexes.Count());

            Assert.Equal(2, config.PropertyIndexes.First().Count());
            Assert.Equal("Key1", config.PropertyIndexes.First().First().Name);
            Assert.Equal("Key2", config.PropertyIndexes.First().Last().Name);
        }
    }
}
