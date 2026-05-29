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
        private const string ParametersKey = "nop.externalauth.parameters";
        private const string ErrorsKey = "nop.externalauth.errors";

        private static ISession GetSession()
        {
            var httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
            return httpContextAccessor.HttpContext?.Session;
        }

        public static void StoreParametersForRoundTrip(OpenAuthenticationParameters parameters)
        {
            var session = GetSession();
            if (session != null)
                session.SetString(ParametersKey, Newtonsoft.Json.JsonConvert.SerializeObject(parameters));
        }

        public static OpenAuthenticationParameters RetrieveParametersFromRoundTrip(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            var json = session.GetString(ParametersKey);
            if (string.IsNullOrEmpty(json))
                return null;

            if (removeOnRetrieval)
                RemoveParameters();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAuthenticationParameters>(json);
        }

        public static void RemoveParameters()
        {
            var session = GetSession();
            session?.Remove(ParametersKey);
        }

        public static void AddErrorsToDisplay(string error)
        {
            var session = GetSession();
            if (session == null)
                return;

            var json = session.GetString(ErrorsKey);
            var errors = string.IsNullOrEmpty(json) 
                ? new List<string>() 
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
            errors.Add(error);
            session.SetString(ErrorsKey, Newtonsoft.Json.JsonConvert.SerializeObject(errors));
        }

        public static IList<string> RetrieveErrorsToDisplay(bool removeOnRetrieval)
        {
            var session = GetSession();
            if (session == null)
                return null;

            var json = session.GetString(ErrorsKey);
            if (string.IsNullOrEmpty(json))
                return null;

            var errors = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
            if (removeOnRetrieval)
                session.Remove(ErrorsKey);

            return errors;
        }
    }
}
