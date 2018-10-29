using DataModel;
using DataStore.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace RequestMonitor.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    public class ItemsController : Controller
    {
        IItemStore _itemStore;
        public ItemsController(IItemStore itemStore)
        {
            _itemStore = itemStore;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _itemStore.GetLatestItemsByCountOrSeconds(100, 2);
            System.Console.WriteLine(result.Count);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]ItemDataModel data)
        {
            if (data == null)
                return StatusCode(400, new { Message = "Invalid Request Data. Json is not valid for expected contract" });

            if (data.Item == null)
                return StatusCode(400, new { Message = "Invalid Request Data. Json is not valid for expected contract" });

            await _itemStore.AddItems(data);
            return StatusCode(201);
        }
    }
} 
