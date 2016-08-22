using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.Web.Attributes;
using piSensorNet.Web.Controllers.Base;
using piSensorNet.Web.Custom;
using piSensorNet.Web.Models.DataTables;

namespace piSensorNet.Web.Controllers.Manage
{
    [Area("Manage")]
    public sealed class TriggersController : BaseController
    {
        public TriggersController([NotNull] Func<PiSensorNetDbContext> contextFactory)
            : base(contextFactory) { }

        [HttpGet]
        public ViewResult Index()
        {
            return View("~/Views/Triggers/Index.cshtml");
        }
        
        [HttpPost]
        [LocalOnly]
        public JsonResult List([FromBody] DataTablesQueryParametersJsonModel query)
        {
            using (var context = ContextFactory())
            {
                var items = context.Triggers
                                   .AsNoTracking()
                                   .OrderBy(i => 1);

                foreach (var column in query.ColumnSortingDefinitions)
                    items = items.ThenBy(query.ColumnDefinitions[column.ColumnIndex].PropertyName, column.Direction);

                var entities = items.ToList();

                var models = entities.Map(Mapper.MapToListItem);

                return Json(DataTablesJsonResultModel.Create(
                    query.Echo,
                    models.Count,
                    models.Count,
                    models));
            }
        }
    }
}
