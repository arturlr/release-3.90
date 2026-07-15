//Contributor:  Nicholas Mayne

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Core.Infrastructure;

namespace Nop.Services.Authentication.External
{
    /// <summary>
    /// External authorizer helper
    /// </summary>
    public static partial class ExternalAuthorizerHelper
    {
        private static ISession GetSession()
        {
            var httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
            return httpContextAccessor.HttpContext?.Session;
        }

        public static void StoreParametersForRoundTrip(OpenAuthenticationParameters parameters)
        {
            var session = GetSession();
            if (session == null)
                return;
            session.Set("nop.externalauth.parameters", SerializeToBytes(parameters));
        }

        public static OpenAuthenticationParameters RetrieveParametersFromRoundTrip(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            byte[] data;
            if (!session.TryGetValue("nop.externalauth.parameters", out data))
                return null;

            var parameters = DeserializeFromBytes<OpenAuthenticationParameters>(data);
            if (parameters != null && removeOnRetrieval)
                RemoveParameters();

            return parameters;
        }

        public static void RemoveParameters()
        {
            var session = GetSession();
            if (session == null)
                return;
            session.Remove("nop.externalauth.parameters");
        }

        public static void AddErrorsToDisplay(string error)
        {
            var session = GetSession();
            if (session == null)
                return;

            var errors = GetErrorsFromSession(session);
            if (errors == null)
            {
                errors = new List<string>();
            }
            errors.Add(error);
            session.Set("nop.externalauth.errors", SerializeToBytes(errors));
        }

        public static IList<string> RetrieveErrorsToDisplay(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            var errors = GetErrorsFromSession(session);
            if (errors != null && removeOnRetrieval)
                session.Remove("nop.externalauth.errors");
            return errors;
        }

        private static IList<string> GetErrorsFromSession(ISession session)
        {
            byte[] data;
            if (!session.TryGetValue("nop.externalauth.errors", out data))
                return null;
            return DeserializeFromBytes<IList<string>>(data);
        }

        private static byte[] SerializeToBytes(object obj)
        {
            if (obj == null)
                return null;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private static T DeserializeFromBytes<T>(byte[] data) where T : class
        {
            if (data == null || data.Length == 0)
                return null;
            var json = System.Text.Encoding.UTF8.GetString(data);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
