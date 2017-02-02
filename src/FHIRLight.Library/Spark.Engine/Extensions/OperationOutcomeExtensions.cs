﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FHIRLight.Library.Spark.Engine.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace FHIRLight.Library.Spark.Engine.Extensions
{
    public static class OperationOutcomeExtensions
    {
        static OperationOutcome.IssueSeverity IssueSeverityOf(HttpStatusCode code)
        {
            var range = ((int)code % 100);
            switch(range)
            {
                case 100:
                case 200: return OperationOutcome.IssueSeverity.Information;
                case 300: return OperationOutcome.IssueSeverity.Warning;
                case 400: return OperationOutcome.IssueSeverity.Error;
                case 500: return OperationOutcome.IssueSeverity.Fatal;
                default: return OperationOutcome.IssueSeverity.Information;
            }
        }
        
        private static void SetContentHeaders(HttpResponseMessage response, ResourceFormat format)
        {
            response.Content.Headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(typeof(Resource), format);
        }

        public static OperationOutcome Init(this OperationOutcome outcome)
        {
            if (outcome.Issue == null)
            {
                outcome.Issue = new List<OperationOutcome.IssueComponent>();
            }
            return outcome;
        }

        public static OperationOutcome AddError(this OperationOutcome outcome, Exception exception)
        {
            string message;

            if(exception is SparkException)
                message = exception.Message;
            else
                message = string.Format("{0}: {1}", exception.GetType().Name, exception.Message);
            
           outcome.AddError(message);

            // Don't add a stacktrace if this is an acceptable logical-level error
            if (!(exception is SparkException))
            {
                var stackTrace = new OperationOutcome.IssueComponent();
                stackTrace.Severity = OperationOutcome.IssueSeverity.Information;
                stackTrace.Diagnostics = exception.StackTrace;
                outcome.Issue.Add(stackTrace);
            }
            return outcome;
        }

        public static OperationOutcome AddAllInnerErrors(this OperationOutcome outcome, Exception exception)
        {
            AddError(outcome, exception);
            while (exception.InnerException != null)
            {
                AddError(outcome, exception);
                exception = exception.InnerException;
            }

            return outcome;
        }

        public static OperationOutcome AddError(this OperationOutcome outcome, string message)
        {
            return outcome.AddIssue(OperationOutcome.IssueSeverity.Error, message);
        }

        public static OperationOutcome AddMessage(this OperationOutcome outcome, string message)
        {
            return outcome.AddIssue(OperationOutcome.IssueSeverity.Information, message);
        }

        public static OperationOutcome AddMessage(this OperationOutcome outcome, HttpStatusCode code, string message)
        {
            return outcome.AddIssue(IssueSeverityOf(code), message);
        }

        private static OperationOutcome AddIssue(this OperationOutcome outcome, OperationOutcome.IssueSeverity severity, string message)
        {
            if (outcome.Issue == null) outcome.Init();

            var item = new OperationOutcome.IssueComponent();
            item.Severity = severity;
            item.Diagnostics = message;
            outcome.Issue.Add(item);
            return outcome;
        }

        public static HttpResponseMessage ToHttpResponseMessage(this OperationOutcome outcome, ResourceFormat target, HttpRequestMessage request)
        {
            byte[] data = null;
            if (target == ResourceFormat.Xml)
                data = FhirSerializer.SerializeResourceToXmlBytes(outcome);
            else if (target == ResourceFormat.Json)
                data = FhirSerializer.SerializeResourceToJsonBytes(outcome);

            var response = new HttpResponseMessage {Content = new ByteArrayContent(data)};
            //setResponseHeaders(response, target);
            SetContentHeaders(response, target);

            return response;
        }
    }
}