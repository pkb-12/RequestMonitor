using System;

namespace DataModel
{
    public class ItemDataModel
    {
        public ItemData Item { get; set; }
    }

    public class ItemData
    {
        public int Id { get; set; }
        public string TimeStamp { get; set; }
    }
}
