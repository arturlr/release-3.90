//Contributor:  Nicholas Mayne

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nop.Core.Infrastructure;

namespace Nop.Services.Authentication.External
{
    /// <summary>
    /// External authorizer helper
    /// </summary>
    /// <remarks>
    /// Task 4.2 (design section 5). The legacy implementation resolved
    /// <c>System.Web.HttpSessionStateBase</c> from the container and used its object indexer
    /// to park an <see cref="OpenAuthenticationParameters"/> instance across the redirect to
    /// the external provider and back.
    ///
    /// ASP.NET Core's <see cref="ISession"/> is a byte-array store - it has no object
    /// indexer and does no BinaryFormatter-style serialization. The values are therefore
    /// stored as UTF-8 JSON (<see cref="System.Text.Json"/>).
    ///
    /// <see cref="OpenAuthenticationParameters"/> is abstract, so the concrete type name is
    /// persisted alongside the payload. Rehydration is deliberately CONSTRAINED: the
    /// recorded type must resolve AND must derive from
    /// <see cref="OpenAuthenticationParameters"/>, otherwise the entry is discarded. This
    /// keeps the session value from being usable as an arbitrary-type deserialization
    /// gadget, which a naive <c>$type</c> round-trip would allow.
    ///
    /// RUNTIME DEFERRAL: session state is opt-in in ASP.NET Core.
    /// <c>services.AddSession()</c> + <c>app.UseSession()</c> must be wired up in task
    /// 6.4/7.2 or <see cref="HttpContext.Session"/> throws
    /// <see cref="InvalidOperationException"/>. Every accessor below degrades to a no-op /
    /// null when no session feature is present, so external authentication fails closed
    /// rather than throwing.
    /// </remarks>
    public static partial class ExternalAuthorizerHelper
    {
        private const string ParametersSessionKey = "nop.externalauth.parameters";
        private const string ErrorsSessionKey = "nop.externalauth.errors";

        /// <summary>
        /// Gets the current session, or null when no session is available
        /// </summary>
        private static ISession GetSession()
        {
            var httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
            var httpContext = httpContextAccessor == null ? null : httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            //HttpContext.Session throws when the session feature is not registered
            try
            {
                return httpContext.Session;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Envelope used to carry the concrete parameters type alongside its JSON payload
        /// </summary>
        private class ParametersEnvelope
        {
            public string TypeName { get; set; }
            public string Payload { get; set; }
        }

        public static void StoreParametersForRoundTrip(OpenAuthenticationParameters parameters)
        {
            var session = GetSession();
            if (session == null)
                return;

            if (parameters == null)
            {
                session.Remove(ParametersSessionKey);
                return;
            }

            var concreteType = parameters.GetType();
            var envelope = new ParametersEnvelope
            {
                TypeName = concreteType.AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(parameters, concreteType)
            };
            session.SetString(ParametersSessionKey, JsonSerializer.Serialize(envelope));
        }

        public static OpenAuthenticationParameters RetrieveParametersFromRoundTrip(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            var raw = session.GetString(ParametersSessionKey);
            if (String.IsNullOrEmpty(raw))
                return null;

            if (removeOnRetrieval)
                RemoveParameters();

            ParametersEnvelope envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ParametersEnvelope>(raw);
            }
            catch (JsonException)
            {
                return null;
            }

            if (envelope == null || String.IsNullOrEmpty(envelope.TypeName) || envelope.Payload == null)
                return null;

            var concreteType = Type.GetType(envelope.TypeName, false);

            //only ever rehydrate a subtype of OpenAuthenticationParameters
            if (concreteType == null || !typeof(OpenAuthenticationParameters).IsAssignableFrom(concreteType))
                return null;

            try
            {
                return JsonSerializer.Deserialize(envelope.Payload, concreteType) as OpenAuthenticationParameters;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static void RemoveParameters()
        {
            var session = GetSession();
            if (session == null)
                return;

            session.Remove(ParametersSessionKey);
        }

        public static void AddErrorsToDisplay(string error)
        {
            var session = GetSession();
            if (session == null)
                return;

            var errors = ReadErrors(session) ?? new List<string>();
            errors.Add(error);
            session.SetString(ErrorsSessionKey, JsonSerializer.Serialize(errors));
        }

        public static IList<string> RetrieveErrorsToDisplay(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            var errors = ReadErrors(session);
            if (errors != null && removeOnRetrieval)
                session.Remove(ErrorsSessionKey);
            return errors;
        }

        private static List<string> ReadErrors(ISession session)
        {
            var raw = session.GetString(ErrorsSessionKey);
            if (String.IsNullOrEmpty(raw))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<string>>(raw);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
