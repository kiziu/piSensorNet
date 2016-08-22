using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.Web.Attributes;
using piSensorNet.Web.Controllers.Base;
using piSensorNet.Web.Custom;
using piSensorNet.Web.Models;
using piSensorNet.Web.Models.DataTables;

namespace piSensorNet.Web.Controllers.Manage
{
    [Area("Manage")]
    public sealed class ModulesController : BaseController
    {
        public ModulesController([NotNull] Func<PiSensorNetDbContext> contextFactory)
            : base(contextFactory) {}

        [HttpGet]
        public ViewResult Index()
        {
            return View("~/Views/Modules/Index.cshtml");
        }

        [HttpGet]
        [LocalOnly]
        public ActionResult Edit(int id)
        {
            Module entity;

            using (var context = ContextFactory())
            {
                entity = context.Modules
                                .AsNoTracking()
                                .Where(i => i.ID == id)
                                .SingleOrDefault();
            }

            if (entity == null)
                return HttpBadRequest($"Module with ID #{id} does not exist.");

            var model = entity.MapToItem();

            return PartialView("~/Views/Modules/Edit.cshtml", model);
        }

        [HttpPost]
        [LocalOnly]
        public JsonResult Edit([FromBody] ModuleItemModel module)
        {
            if (!ModelState.IsValid)
                return Json(ModelState, false);

            using (var context = ContextFactory().WithChangeTracking())
            {
                var entity = context.Modules
                                    .Where(i => i.ID == module.ID)
                                    .SingleOrDefault();

                if (entity == null)
                    return JsonFailure($"Module with ID #{module.ID} does not exist.");

                entity.FriendlyName = module.FriendlyName.TrimToNull();
                entity.Description = module.Description.TrimToNull();

                context.SaveChanges();

                return Json($"Changes to module @{entity.Address} saved successfully.");
            }
        }

        [HttpPost]
        [LocalOnly]
        public JsonResult List([FromBody] DataTablesQueryParametersJsonModel query)
        {
            using (var context = ContextFactory())
            {
                var items = context.Modules
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