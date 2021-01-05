﻿using Nop.Plugin.Api.JSON.Serializers;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Topics;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Stores;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Orders;
using Nop.Core;
using Nop.Plugin.Api.DTOs.Topics;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.Models.TopicsParameters;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.Factories;
using Nop.Core.Domain.Topics;

namespace Nop.Plugin.Api.Controllers
{
    public class TopicsController : BaseApiController
    {
        private readonly ITopicService _topicService;
        private readonly IStoreContext _storeContext;
        private readonly IDTOHelper _dtoHelper;
        private readonly IFactory<Topic> _factory;

        public TopicsController(
            IJsonFieldsSerializer jsonFieldsSerializer,
            IAclService aclService,
            ITopicService topicService,
            ICustomerService customerService,
            IStoreMappingService storeMappingService,
            IStoreService storeService,
            IDiscountService discountService,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            IDTOHelper dtoHelper,
            IPictureService pictureService,
            IFactory<Topic> factory

            ) : base(jsonFieldsSerializer, aclService, customerService, storeMappingService,
                   storeService, discountService, customerActivityService, localizationService, pictureService)
        {
            _topicService = topicService;
            _storeContext = storeContext;
            _dtoHelper = dtoHelper;
            _factory = factory;
        }

        /// <summary>
        ///     Receive a list of all Topics
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/topics")]
        [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult GetTopics(TopicsParametersModel parameters)
        {
            var storeId = _storeContext.CurrentStore.Id;

            var topics = _topicService.GetAllTopics(storeId);

            IList<TopicDto> topicsAsDtos = topics.Select(x => _dtoHelper.PrepareTopicDTO(x)).ToList();

            var topicsRootObject = new TopicsRootObject
            {
                Topics = topicsAsDtos
            };

            var json = JsonFieldsSerializer.Serialize(topicsRootObject, parameters.Fields);

            return new RawJsonActionResult(json);
        }

        /// <summary>
        ///     Retrieve topic by spcified id
        /// </summary>
        /// ///
        /// <param name="id">Id of the topic</param>
        /// <param name="fields">Fields from the topic you want your json to contain</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/topics/{id}")]
        [ProducesResponseType(typeof(TopicsRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult GetOrderById(int id, string fields = "")
        {
            if (id <= 0)
            {
                return Error(HttpStatusCode.BadRequest, "id", "invalid id");
            }

            var topic = _topicService.GetTopicById(id);

            if (topic == null)
            {
                return Error(HttpStatusCode.NotFound, "topic", "not found");
            }

            var topicsRootObject = new TopicsRootObject();

            var topicDto = _dtoHelper.PrepareTopicDTO(topic);
            topicsRootObject.Topics.Add(topicDto);

            var json = JsonFieldsSerializer.Serialize(topicsRootObject, fields);

            return new RawJsonActionResult(json);
        }

        [HttpPost]
        [Route("/api/topics")]
        [ProducesResponseType(typeof(TopicsRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        public IActionResult CreateTopic(
            [ModelBinder(typeof(JsonModelBinder<TopicDto>))]
            Delta<TopicDto> topicDelta)
        {
            // Here we display the errors if the validation has failed at some point.
            if (!ModelState.IsValid)
            {
                return Error();
            }

            var newTopic = _factory.Initialize();
            newTopic.Title = topicDelta.Dto.Title;
            newTopic.Body = topicDelta.Dto.Body;
            newTopic.SystemName = topicDelta.Dto.SystemName;

            _topicService.InsertTopic(newTopic);

            var topicsRootObject = new TopicsRootObject();

            var topicDto = _dtoHelper.PrepareTopicDTO(newTopic);

            topicsRootObject.Topics.Add(topicDto);

            var json = JsonFieldsSerializer.Serialize(topicsRootObject, string.Empty);

            return new RawJsonActionResult(json);
        }


        [HttpDelete]
        [Route("/api/topics/{id}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult DeleteTopic(int id)
        {
            if (id <= 0)
            {
                return Error(HttpStatusCode.BadRequest, "id", "invalid id");
            }

            var topicToDelete = _topicService.GetTopicById(id);

            if (topicToDelete == null)
            {
                return Error(HttpStatusCode.NotFound, "topic", "not found");
            }

            _topicService.DeleteTopic(topicToDelete);

            //activity log
            CustomerActivityService.InsertActivity("DeleteTopic", LocalizationService.GetResource("ActivityLog.DeleteTopic"), topicToDelete);

            return Json(new { status = "ok" });
        }

        [HttpPut]
        [Route("/api/topics/{id}")]
        [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        public IActionResult UpdateTopic(
            [ModelBinder(typeof(JsonModelBinder<TopicDto>))]
            Delta<TopicDto> topicDelta)
        {
            // Here we display the errors if the validation has failed at some point.
            if (!ModelState.IsValid)
            {
                return Error();
            }

            var currentTopic = _topicService.GetTopicById(topicDelta.Dto.Id);

            if (currentTopic == null)
            {
                return Error(HttpStatusCode.NotFound, "topic", "not found");
            }

            topicDelta.Merge(currentTopic);

            _topicService.UpdateTopic(currentTopic);

            var topicsRootObject = new TopicsRootObject();

            var topicDto = _dtoHelper.PrepareTopicDTO(currentTopic);

            topicsRootObject.Topics.Add(topicDto);

            var json = JsonFieldsSerializer.Serialize(topicsRootObject, string.Empty);

            return new RawJsonActionResult(json);
        }
    }
}
