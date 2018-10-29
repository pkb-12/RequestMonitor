using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DataModel;
using DataStore.Interface;
using System.Threading;
using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataStore.Implementation
{
    public class InMemoryStore : IItemStore
    {
        //concurrent thread safe dictionary to save items list by sec.
        private ConcurrentDictionary<string, List<ItemDataModel>> itemsCounterByTime;

        // list keep track of dictionary keys. Used to cleanup old keys and items
        private List<string> keyList;

        // total items counter
        private int totalCount;

        //config and logger
        private IConfiguration configuration;
        private ILogger logger;

        private object lockObj = new object();
        private const string DATE_KEY_FORMAT = "yyyy-MM-ddTHH:mm:ss";
        
        public InMemoryStore(IConfiguration config, ILoggerFactory loggerFactory)
        {
            itemsCounterByTime = new ConcurrentDictionary<string, List<ItemDataModel>>();
            totalCount = 0;
            keyList = new List<string>();

            configuration = config;
            logger = loggerFactory.CreateLogger<InMemoryStore>();

            //trigger cleanup task in a seperate thread. This will run continously for the lifetime of the app.
            Task.Run(() =>
            {
                // read clean up settings from config
                CleanUpOldItems(Convert.ToInt32(config["RemoveOldItemsInLastSec"]),
                    Convert.ToInt32(config["MinimumItemsToKeep"]),
                    Convert.ToInt32(config["RunCleanUpEveryInSec"]));
            });
        }

        /// <summary>
        /// Adds items to a List
        /// </summary>
        /// <param name="item">item object</param>
        /// <param name="timeStamp">Timestamp when received in long datetime format</param>
        /// <returns></returns>
        public Task AddItems(ItemDataModel item)
        {
            string timeStamp = DateTime.Now.ToString(DATE_KEY_FORMAT);
            bool newKey = true;

            // thread safe counter increment.
            Interlocked.Increment(ref totalCount);

            //Add items to list after getting lock
            lock (lockObj)
            {
                //Increase the counter for given timestamp key
                itemsCounterByTime.AddOrUpdate(timeStamp, new List<ItemDataModel>() { item },
                    (key, value) => {
                        newKey = false;
                        value.Add(item);
                        return value;
                    });
            }

            //Add key to list for clean up later
            if (newKey)
                keyList.Add(timeStamp);

            return Task.CompletedTask;
        }

        private List<ItemDataModel> GetAllItems()
        {
            List<ItemDataModel> items = new List<ItemDataModel>();
            foreach(string key in keyList)
            {
                items.AddRange(itemsCounterByTime[key]);
            }

            return items;
        }

        /// <summary>
        /// Returns latest saved items in last <param name="seconds"> or 
        /// latest <paramref name="count"/> items, whichever is higher
        /// </summary>
        /// <param name="count">no of items to return</param>
        /// <paramref name="seconds"/> latest resquests in last no of seconds.
        /// <returns>List of items</returns>
        public Task<List<ItemDataModel>> GetLatestItemsByCountOrSeconds(int count, int seconds)
        {
            List<ItemDataModel> itemsByKey = null;
            DateTime now = DateTime.Now;
            List<ItemDataModel> result = new List<ItemDataModel>();
            List<string> processedKeys = new List<string>();

            //return all items if total items are less than count
            if (totalCount <= count)
                result = GetAllItems();
            else
            {
                // get all items till T-1 second
                for (int i = seconds - 1; i > 0; i--)
                {
                    itemsByKey = new List<ItemDataModel>();
                    string key = now.AddSeconds(-i).ToString(DATE_KEY_FORMAT);
                    processedKeys.Add(key);
                    if (itemsCounterByTime.TryGetValue(key, out itemsByKey))
                    {
                        result.AddRange(itemsByKey);
                    }
                }

                //current second items, we need lock as items are getting added in parallel.
                itemsByKey = new List<ItemDataModel>();
                // get the lock to read items in a thread safe way.
                lock(lockObj)
                {
                    processedKeys.Add(now.ToString(DATE_KEY_FORMAT));
                    if (itemsCounterByTime.TryGetValue(now.ToString(DATE_KEY_FORMAT), out itemsByKey))
                    {
                        result.AddRange(itemsByKey);
                    }

                    // if result count is less than count add other latest items that was added earlier than required second limit
                    if (result.Count < count)
                    {
                        for (int i = keyList.Count-1; i >= 0 ; i--)
                        {
                            // dont add already added items
                            if(!processedKeys.Contains(keyList[i]))
                            {
                                var list = itemsCounterByTime[keyList[i]];
                                if (list.Count + result.Count <= count)
                                    result.AddRange(list);
                                else
                                {
                                    // get latest items from the end of the list.
                                    int excludeCount = (list.Count + result.Count) - count;
                                    result.AddRange(list.GetRange(excludeCount, (list.Count - excludeCount)));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return Task.FromResult<List<ItemDataModel>>(result);
        }

        /// <summary>
        /// Will clean up old items to restore memory space.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="count"></param>
        private async void CleanUpOldItems(int seconds, int count, int runEverySec)
        {
            await Task.Delay(runEverySec * 1000);

            while (true)
            {
                DateTime now = DateTime.Now;
                try
                {
                    int totalCountTemp = totalCount;
                    // read every key from begining of the list, keylist items are sorted in ascending order or timestamp.
                    while (keyList.Count > seconds && totalCountTemp > count)
                    {
                        DateTime date = DateTime.ParseExact(keyList[0], DATE_KEY_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                        int itemsToDelete = itemsCounterByTime[keyList[0]].Count;
                        if ((now - date).TotalSeconds > seconds && totalCount - itemsToDelete >= count)
                        {
                            List<ItemDataModel> temp;
                            itemsCounterByTime.Remove(keyList[0], out temp);
                            temp = null;
                            Interlocked.Add(ref totalCount, -itemsToDelete);
                            totalCountTemp = totalCountTemp - itemsToDelete;

                            //warning - because to isoloate this log from other info logs for testing.
                            logger.LogWarning($"cleanup key - {keyList[0]}, items - {itemsToDelete}");
                            keyList.Remove(keyList[0]);
                        }
                        else
                        {
                            // no need to clean up further.
                            break;
                        }
                    }

                    //asynchronously wait for specified seconds and continue;
                    await Task.Delay(runEverySec * 1000);
                }
                catch (Exception ex)
                {
                    //log it, dont throw, we need this cleanup task to keep running
                    logger.LogError(ex.ToString());
                }
            }
        }
    }
}
