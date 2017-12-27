using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MicroElements.FileStorage.Abstractions;
using MicroElements.FileStorage.KeyGenerators;
using MicroElements.FileStorage.Serializers;
using MicroElements.FileStorage.StorageEngine;
using MicroElements.FileStorage.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MicroElements.FileStorage.Tests
{
    public class FileStorageTests
    {
        [Fact]
        public void key_getter_should_get_id()
        {
            var person = new Person { Id = "persons/1", FirstName = "Bill", LastName = "Gates" };
            var idFunc = new DefaultKeyAccessor<Person>().GetIdFunc();
            var key = idFunc(person);
            key.Should().Be("persons/1");
        }

        [Fact]
        public void key_setter_should_set_id()
        {
            var person = new Person { Id = null, FirstName = "Bill", LastName = "Gates" };
            var idFunc = new DefaultKeyAccessor<Person>().SetIdFunc();
            idFunc(person, "persons/1");
            person.Id.Should().Be("persons/1");
        }

        [Fact]
        public async Task load_single_file_collection()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/SingleFileCollection");
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                StorageEngine = new FileStorageEngine(basePath),
                Collections = new[]
                {
                    new CollectionConfiguration
                    {
                        Name = "Persons",
                        DocumentType = typeof(Person),
                        SourceFile = "Persons.json",
                        Format = "json",
                        Version = "1.0"
                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            dataStore.GetCollection<Person>().Should().BeSameAs(dataStore.GetCollection<Person>());

            var collection = dataStore.GetCollection<Person>();
            collection.Should().NotBeNull();
            collection.Count.Should().Be(2);

            CheckPersons(collection);
        }

        private static void CheckPersons(IDocumentCollection<Person> collection)
        {
            var person = collection.Find(_ => _.Id == "2").First();
            person.Should().NotBeNull();
            person.FirstName.Should().Be("Steve");
            person.LastName.Should().Be("Ballmer");

            person = collection.Get("1");
            person.Should().NotBeNull();
            person.FirstName.Should().Be("Bill");
            person.LastName.Should().Be("Gates");
        }

        [Fact]
        public async Task load_multi_file_collection()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/MultiFileCollection");
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                StorageEngine = new FileStorageEngine(basePath),
                Collections = new[]
                {
                    new CollectionConfiguration
                    {
                        Name = "Persons",
                        DocumentType = typeof(Person),
                        SourceFile = "persons",
                        Format = "json",
                        Version = "1.0"
                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            var collection = dataStore.GetCollection<Person>();
            collection.Should().NotBeNull();
            collection.Count.Should().Be(2);
        }

        [Fact]
        public async Task load_csv_collection()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/WithConvert");
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                StorageEngine = new FileStorageEngine(basePath),
                Collections = new[]
                {
                    new CollectionConfiguration
                    {
                        DocumentType = typeof(Person),
                        SourceFile = "persons.csv",
                        Format = "csv",
                        Serializer = new SimpleCsvSerializer()//todo: ��������� � ���� Format ��� � ����������
                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            var collection = dataStore.GetCollection<Person>();
            collection.Should().NotBeNull();
            collection.Count.Should().Be(2);

            CheckPersons(collection);
        }

        [Fact]
        public async Task create_collection_and_save()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/create_collection_and_save");
            var file = Path.Combine(basePath, "persons.json");
            if (File.Exists(file))
                File.Delete(file);

            Directory.CreateDirectory(basePath);
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                StorageEngine = new FileStorageEngine(basePath),//todo: DI
                Collections = new[]
                {
                    new CollectionConfiguration
                    {
                        Name = "Persons",
                        DocumentType = typeof(Person),
                        SourceFile = "persons.json",
                        Format = "json",
                        Version = "1.0",
                        Serializer = new JsonSerializer(),
                        OneFilePerCollection = true
                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            var collection = dataStore.GetCollection<Person>();
            collection.Should().NotBeNull();
            collection.Drop();
            //collection.Count.Should().Be(0);

            collection.Add(new Person { FirstName = "Bill", LastName = "Gates" });
            collection.Count.Should().Be(1);

            var person = (Person)collection.GetAll().First();
            person.Id.Should().NotBeNullOrEmpty("Id must be generated");

            dataStore.Save();
        }

        [Fact]
        public async Task key_generation()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/key_generation");
            var file = Path.Combine(basePath, "persons.json");
            if (File.Exists(file))
                File.Delete(file);

            Directory.CreateDirectory(basePath);
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                StorageEngine = new FileStorageEngine(basePath),
                Collections = new[]
                {
                    new CollectionConfigurationTyped<Person>
                    {
                        DocumentType = typeof(Person),
                        SourceFile = "persons.json",
                        KeyGenerator = new IdentityKeyGenerator<Person>()
                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            var collection = dataStore.GetCollection<Person>();
            collection.Should().NotBeNull();
            collection.Count.Should().Be(0);

            var person = new Person { FirstName = "Bill", LastName = "Gates" };
            collection.Add(person);
            person.Id.Should().Be("person/1");

            person = new Person { FirstName = "Steve", LastName = "Ballmer" };
            collection.Add(person);
            person.Id.Should().Be("person/2");


            dataStore.Save();
        }

        [Fact]
        public async Task not_standard_id_collection()
        {
            var basePath = Path.GetFullPath("TestData/DataStore/not_standard_id_collection");
            var file = Path.Combine(basePath, "currencies.json");
            if (File.Exists(file))
                File.Delete(file);

            Directory.CreateDirectory(basePath);
            var storeConfiguration = new DataStoreConfiguration
            {
                BasePath = basePath,
                //StorageEngine = new FileStorageEngine(basePath),
                Collections = new[]
                {
                    new CollectionConfigurationTyped<Currency>
                    {
                        DocumentType = typeof(Currency),
                        SourceFile = "currencies.json",
                        KeyGetter = new DefaultKeyAccessor<Currency>(nameof(Currency.Code)),

                    },
                }
            };
            var dataStore = new DataStore(storeConfiguration);

            await dataStore.Initialize();

            var collection = dataStore.GetCollection<Currency>();
            collection.Should().NotBeNull();


            collection.Add(new Currency() { Code = "USD", Name = "Dollar" });
            collection.Add(new Currency() { Code = "EUR", Name = "Euro" });
            collection.Count.Should().Be(2);

            dataStore.Save();
        }

        [Fact]
        public void builder_tests()
        {
            var services = new ServiceCollection();
            new FileStorageModule().ConfigureServices(services);

            services.AddSingleton(new CollectionConfiguration() { Name = "col1" });
            services.AddSingleton(new CollectionConfiguration() { Name = "col2" });
            var serviceProvider = services.BuildServiceProvider(true);
            var collectionConfigurations = serviceProvider.GetService<IEnumerable<CollectionConfiguration>>();
            var documentCollection = serviceProvider.GetRequiredService<IDocumentCollection<Person>>();
        }

        //todo: create collection (market data cache)
        //todo: create snapshot
        //todo: load from other format
        //todo: key work: full key, short, generation, max, guid
        //todo: metrics
        //todo: fullInMemory | lazyLoad
        //todo: readonly
    }



}
