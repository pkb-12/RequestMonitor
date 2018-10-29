using DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataStore.Interface
{
    public interface IItemStore
    {
        Task AddItems(ItemDataModel request);
        
        Task<List<ItemDataModel>> GetLatestItemsByCountOrSeconds(int count, int seconds);
    }
}
