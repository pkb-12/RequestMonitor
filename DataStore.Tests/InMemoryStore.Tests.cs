using DataStore.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

namespace DataStore.Tests
{
    [TestClass]
    public class InMemoryStoreTests
    {

        Mock<IConfiguration> configMock = new Mock<IConfiguration>();
        Mock<LoggerFactory> mockLogFactory = new Mock<LoggerFactory>();
        Mock<ILogger> mockLogger = new Mock<ILogger>();
        InMemoryStore store;

        [TestInitialize]
        public void Setup()
        {
            configMock.Setup(config => config["RemoveOldItemsInLastSec"]).Returns("2");
            configMock.Setup(config => config["MinimumItemsToKeep"]).Returns("100");
            configMock.Setup(config => config["RunCleanUpEveryInSec"]).Returns("5");

            LoggerFactory logger = new LoggerFactory();
            store = new InMemoryStore(configMock.Object, logger);
        }

        [TestMethod]
        public void TestInsertAndGetWhenItemsAreLessThan100()
        {
            AddItems(50);
            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(50, results.Count);

            //Test again after Clean up
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(50, results.Count);
        }

        [TestMethod]
        public void TestInsertAndImmediateGetWhenItemsAreMoreThan100()
        {
            AddItems(101);
            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(101, results.Count);

            //Test again after Clean up
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);
        }

        [TestMethod]
        public void TestInsertFor2SecAndImmediateGetLast2SecsItems()
        {
            AddItems(101);
            Thread.Sleep(1000);
            AddItems(101);

            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(202, results.Count);

            //Test again after Clean up, it should return 100
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);
        }

        [TestMethod]
        public void TestInsertFor3SecAndImmediateGetLast2SecsItems()
        {
            AddItems(101);
            Thread.Sleep(1000);
            AddItems(101);
            Thread.Sleep(1000);
            AddItems(101);

            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(202, results.Count);

            //Test again after Clean up, it should return 100
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);
        }

        [TestMethod]
        public void TestInsertFor3SecAndDelayGetLast2SecsItems()
        {
            AddItems(101);
            Thread.Sleep(1000);
            AddItems(110);
            Thread.Sleep(1000);
            AddItems(150);
            Thread.Sleep(1000);
            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(150, results.Count);

            //Test again after Clean up, it should return 100
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);
        }

        [TestMethod]
        public void TestGetLastItemsWithCountLessThan100InLast2Sec()
        {
            AddItems(50);
            Thread.Sleep(1000);
            AddItems(30);
            Thread.Sleep(1000);
            AddItems(40);
            Thread.Sleep(1000);
            var results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);

            //Test again after Clean up, it should return 100
            Thread.Sleep(7000);

            results = store.GetLatestItemsByCountOrSeconds(100, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(100, results.Count);
        }

        [TestMethod]
        public void TestCleanUpAfterItemsAddedInLast3Secs()
        {
            AddItems(150);
            Thread.Sleep(1000);
            AddItems(100);

            Thread.Sleep(2000);
            AddItems(70);
            Thread.Sleep(1000);
            AddItems(40);

            //let cleanUp run
            Thread.Sleep(6000);

            //call to get more items- greater than 100 to validate cleanup
            var results = store.GetLatestItemsByCountOrSeconds(200, 2).Result;

            Assert.IsNotNull(results);
            Assert.AreEqual(110, results.Count);
        }


        private void AddItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                store.AddItems(new DataModel.ItemDataModel()
                { Item = new DataModel.ItemData() { Id = 123, TimeStamp = DateTime.Now.ToString("o") } });
            }
        }
    }
}
