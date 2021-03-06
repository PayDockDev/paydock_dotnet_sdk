﻿using System;
using Paydock_dotnet_sdk.Models;
using Newtonsoft.Json;
using Paydock_dotnet_sdk.Tools;
using Newtonsoft.Json.Linq;

namespace Paydock_dotnet_sdk.Services
{
    /// <summary>
    /// Provides abstraction over the /charges endpoint for the APU
    /// </summary>
    public class Charges : ICharges
    {
        protected IServiceHelper _serviceHelper;
        protected string _overrideConfigSecretKey = null;

        /// <summary>
        /// Service locator style constructor
        /// <param name="overrideConfigSecretKey">Use a custom secret key rather than the value in shared config, defaults to null</param>
        /// </summary>
        public Charges(string overrideConfigSecretKey = null)
        {
            _serviceHelper = new ServiceHelper();
            _overrideConfigSecretKey = overrideConfigSecretKey;
        }

        /// <summary>
        /// Dependency injection constructor to enable testing
        /// <param name="serviceHelper">Service helper class to perform HTTP requests</param>
        /// <param name="overrideConfigSecretKey">Use a custom secret key rather than the value in shared config, defaults to null</param>
        /// </summary>
        public Charges(IServiceHelper serviceHelper, string overrideConfigSecretKey = null)
        {
            _serviceHelper = serviceHelper;
            _overrideConfigSecretKey = overrideConfigSecretKey;
        }

        /// <summary>
        /// Add a charge
        /// </summary>
        /// <param name="request">Charge data</param>
        /// <returns>Charge response</returns>
        [RequiresConfig]
        public ChargeResponse Add(ChargeRequestBase request)
        {
			var requestData = SerializeHelper.Serialize(request);
            var responseJson = _serviceHelper.CallPaydock("charges", HttpMethod.POST, requestData, overrideConfigSecretKey :_overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeResponse>(responseJson);
            response.JsonResponse = responseJson;
            return response;
		}

		/// <summary>
		/// Add an authorise charge
		/// </summary>
		/// <param name="request">Charge data</param>
		/// <returns>Charge response</returns>
		[RequiresConfig]
		public ChargeResponse Authorise(ChargeRequestBase request)
		{
			var requestData = SerializeHelper.Serialize(request);
			var responseJson = _serviceHelper.CallPaydock("charges?capture=false", HttpMethod.POST, requestData, overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeResponse>(responseJson);
			response.JsonResponse = responseJson;
			return response;
		}

		/// <summary>
		/// Capture a previously authorised charge
		/// </summary>
		/// <param name="chargeId">id for the charge to capture</param>
		/// <param name="amount">amount to capture</param>
		/// <returns>Charge response</returns>
		[RequiresConfig]
		public ChargeResponse Capture(string chargeId, decimal? amount = null)
		{
			string requestData =  null;
			if (amount.HasValue)
			{
				requestData = new JObject(new JProperty("amount", amount.Value.ToString())).ToString();
			}
			chargeId = Uri.EscapeUriString(chargeId);
			var responseJson = _serviceHelper.CallPaydock(string.Format("charges/{0}/capture", chargeId), HttpMethod.POST, requestData, overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeResponse>(responseJson);
			response.JsonResponse = responseJson;
			return response;
		}

		/// <summary>
		/// cancel a previously authorised charge
		/// </summary>
		/// <param name="chargeId">id for the charge to capture</param>
		/// <returns>Charge response</returns>
		[RequiresConfig]
		public ChargeResponse CancelAuthorisation(string chargeId)
		{
			chargeId = Uri.EscapeUriString(chargeId);
			var responseJson = _serviceHelper.CallPaydock(string.Format("charges/{0}/capture", chargeId), HttpMethod.DELETE, null, overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeResponse>(responseJson);
			response.JsonResponse = responseJson;
			return response;
		}

		/// <summary>
		/// Retrieve full list of charges, limited to 1000
		/// </summary>
		/// <returns>List of charges</returns>
		[RequiresConfig]
        public ChargeItemsResponse Get()
        {
            var responseJson = _serviceHelper.CallPaydock("charges", HttpMethod.GET, "", overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeItemsResponse>(responseJson);
            response.JsonResponse = responseJson;
            return response;
        }

        /// <summary>
        /// Retrieve a filtered list of charges, limited to 1000
        /// </summary>
        /// <param name="request">filter parameters</param>
        /// <returns>List of charges</returns>
        [RequiresConfig]
        public ChargeItemsResponse Get(ChargeSearchRequest request)
        {
			if (string.IsNullOrEmpty(request.reference)) 
			{
#pragma warning disable 0612
				request.reference = request.transaction_external_id;
#pragma warning restore 0612
			}

			var url = "charges/";
            url = url.AppendParameter("skip", request.skip);
            url = url.AppendParameter("limit", request.limit);
            url = url.AppendParameter("subscription_id", request.subscription_id);
            url = url.AppendParameter("gateway_id", request.gateway_id);
            url = url.AppendParameter("company_id", request.company_id);
            url = url.AppendParameter("created_at.from", request.created_at_from);
            url = url.AppendParameter("created_at.to", request.created_at_to);
            url = url.AppendParameter("search", request.search);
            url = url.AppendParameter("status", request.status);
            url = url.AppendParameter("archived", request.archived);
			url = url.AppendParameter("reference", request.reference);

			var responseJson = _serviceHelper.CallPaydock(url, HttpMethod.GET, "", overrideConfigSecretKey: _overrideConfigSecretKey);

			return SerializeHelper.Deserialize<ChargeItemsResponse>(responseJson);
        }

        /// <summary>
        /// Retrive a single charge
        /// </summary>
        /// <param name="chargeId">id of the charge to retreive</param>
        /// <returns>charge data</returns>
        [RequiresConfig]
        public ChargeItemResponse Get(string chargeId)
        {
            chargeId = Uri.EscapeUriString(chargeId);
            var responseJson = _serviceHelper.CallPaydock("charges/" + chargeId, HttpMethod.GET, "", overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeItemResponse>(responseJson);
            response.JsonResponse = responseJson;
            return response;
        }

        /// <summary>
        /// Refund a transaction
        /// </summary>
        /// <param name="chargeId">id of the charge to refund</param>
        /// <param name="amount">amount to refund, can be used to issue partial refunds</param>
        /// <returns>information on the transaction</returns>
        [RequiresConfig]
        public ChargeRefundResponse Refund(string chargeId, decimal? amount = null)
        {
            chargeId = Uri.EscapeUriString(chargeId);

			string requestData = null;
			if (amount.HasValue)
			{
				requestData = new JObject(new JProperty("amount", amount.Value.ToString())).ToString();
			}

			var json = string.Format("{{\"amount\" : \"{0}\"}}", amount);
            var responseJson = _serviceHelper.CallPaydock(string.Format("charges/{0}/refunds", chargeId), HttpMethod.POST, json, overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeRefundResponse>(responseJson);
            response.JsonResponse = responseJson;
            return response;
        }

        /// <summary>
        /// Archive a transaction
        /// </summary>
        /// <param name="chargeId">id of the charge to archive</param>
        /// <returns>information on the transaction</returns>
        [RequiresConfig]
        public ChargeRefundResponse Archive(string chargeId)
        {
            chargeId = Uri.EscapeUriString(chargeId);
            var responseJson = _serviceHelper.CallPaydock(string.Format("charges/{0}", chargeId), HttpMethod.DELETE, "", overrideConfigSecretKey: _overrideConfigSecretKey);

			var response = SerializeHelper.Deserialize<ChargeRefundResponse>(responseJson);
            response.JsonResponse = responseJson;
            return response;
        }
    }
}
