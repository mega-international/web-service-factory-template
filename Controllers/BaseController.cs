using Mega.WebServiceTemplate1.Models;
using log4net;
using Mega.Bridge.Filters;
using Mega.Bridge.Models;
using Mega.Bridge.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace Mega.WebServiceTemplate1.Controllers
{
    [HopexAuthenticationFilter]
    public class BaseController : ApiController
    {
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(BaseController));

        protected WebServiceResult CallMacro(string macroId, string data = "", string sessionMode = "MS", string accessMode = "RW", bool closeSession = false)
        {
            // Get UserInfo
            var userInfo = (UserInfo)Request.Properties["UserInfo"];

            // Get values from X-HopexContext
            IEnumerable<string> hopexContextHeader;
            HopexContext hopexContext;
            if (!Request.Headers.TryGetValues("X-HopexContext", out hopexContextHeader) || !HopexServiceHelper.TryGetHopexContext(hopexContextHeader.FirstOrDefault(), out hopexContext))
            {
                const string message = "Parameter \"X-HopexContext\" must be set in the header of your request. Example: HopexContext:{\"EnvironmentId\":\"IdAbs\",\"RepositoryId\":\"IdAbs\",\"ProfileId\":\"IdAbs\",\"DataLanguageId\":\"IdAbs\",,\"GuiLanguageId\":\"IdAbs\"}";
                Logger.Debug(message);
                return new WebServiceResult { ErrorType = "BadRequest", Content = message };
            }

            // Find the Hopex session
            var sspUrl = ConfigurationManager.AppSettings["MegaSiteProvider"];
            var securityKey = ((NameValueCollection)WebConfigurationManager.GetSection("secureAppSettings"))["SecurityKey"];
            var mwasUrl = HopexService.FindSession(sspUrl, securityKey, hopexContext.EnvironmentId, hopexContext.DataLanguageId, hopexContext.GuiLanguageId, hopexContext.ProfileId, userInfo.HopexAuthPerson, true);
            if (mwasUrl == null)
            {
                const string message = "Unable to get MWAS url. Please retry later and check your configuration if it doesn't work.";
                Logger.Debug(message);
                return new WebServiceResult { ErrorType = "BadRequest", Content = message };
            }
            mwasUrl = mwasUrl.ToLower().Replace("hopexmwas", "hopexapimwas");

            // Open the Hopex session
            var hopexService = new HopexService(mwasUrl, securityKey);
            var mwasSettings = InitMwasSettings();
            var mwasSessionConnectionParameters = InitMwasSessionConnectionParameters(sessionMode, accessMode, hopexContext, userInfo);
            if (!hopexService.TryOpenSession(mwasSettings, mwasSessionConnectionParameters, findSession: !closeSession))
            {
                var message = "Unable to open an Hopex session. Please check the values in the HopexContext header and retry.";
                Logger.Debug(message);
                return new WebServiceResult { ErrorType = "BadRequest", Content = message };
            }

            // Call the macro
            var macroResult = hopexService.CallMacro(macroId, data);

            // Close the Hopex session
            if (closeSession)
            {
                hopexService.CloseUpdateSession();
            }

            // Return the macro result
            return new WebServiceResult { ErrorType = "None", Content = macroResult };
        }

        protected IHttpActionResult CallAsyncMacroExecute(string macroId, string data = "", string sessionMode = "MS", string accessMode = "RW")
        {
            // Get UserInfo
            var userInfo = (UserInfo)Request.Properties["UserInfo"];

            // Get values from X-HopexContext
            IEnumerable<string> hopexContextHeader;
            HopexContext hopexContext;
            if (!Request.Headers.TryGetValues("X-HopexContext", out hopexContextHeader) || !HopexServiceHelper.TryGetHopexContext(hopexContextHeader.FirstOrDefault(), out hopexContext))
            {
                const string message = "Parameter \"X-Hopex-Context\" must be set in the header of your request. Example: HopexContext:{\"EnvironmentId\":\"IdAbs\",\"RepositoryId\":\"IdAbs\",\"ProfileId\":\"IdAbs\",\"DataLanguageId\":\"IdAbs\",,\"GuiLanguageId\":\"IdAbs\"}";
                Logger.Debug(message);
                return BadRequest(message);
            }

            // Find the Hopex session
            var sspUrl = ConfigurationManager.AppSettings["MegaSiteProvider"];
            var securityKey = ((NameValueCollection)WebConfigurationManager.GetSection("secureAppSettings"))["SecurityKey"];
            var mwasUrl = HopexService.FindSession(sspUrl, securityKey, hopexContext.EnvironmentId, hopexContext.DataLanguageId, hopexContext.GuiLanguageId, hopexContext.ProfileId, userInfo.HopexAuthPerson, true);
            if (mwasUrl == null)
            {
                const string message = "Unable to get MWAS url. Please retry later and check your configuration if it doesn't work.";
                Logger.Debug(message);
                return BadRequest(message);
            }
            mwasUrl = mwasUrl.ToLower().Replace("hopexmwas", "hopexapimwas");

            // Open the Hopex session
            var hopexService = new HopexService(mwasUrl, securityKey);
            var mwasSettings = InitMwasSettings();
            var mwasSessionConnectionParameters = InitMwasSessionConnectionParameters(sessionMode, accessMode, hopexContext, userInfo);
            if (!hopexService.TryOpenSession(mwasSettings, mwasSessionConnectionParameters, findSession: true))
            {
                var message = "Unable to open an Hopex session. Please check the values in the HopexContext header and retry.";
                Logger.Debug(message);
                return BadRequest(message);
            }

            // Call the execution of the macro in async mode
            var asyncMacroResult = hopexService.CallAsyncMacroExecute(macroId, data);

            // If error occurs
            if (asyncMacroResult.Status != "InProgress")
            {
                // Close the Hopex session
                hopexService.CloseSession();
                // Return the bare result
                return Ok(asyncMacroResult);
            }

            // Return the action id
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var hopexSession = HopexServiceHelper.EncryptHopexSessionInfo(hopexService.MwasUrl, hopexService.HopexSessionToken);
            response.Headers.Add("X-HopexSession", hopexSession);
            response.Content = new StringContent(asyncMacroResult.ActionId);
            return ResponseMessage(response);
        }

        protected IHttpActionResult CallAsyncMacroGetResult(string actionId, bool closeSession = true)
        {
            // Get values from X-HopexSession
            IEnumerable<string> hopexSessionHeader;
            HopexSessionInfo hopexSessionInfo;
            if (!Request.Headers.TryGetValues("X-HopexSession", out hopexSessionHeader) || !HopexServiceHelper.TryGetHopexSessionInfo(hopexSessionHeader.FirstOrDefault(), out hopexSessionInfo))
            {
                var message = "Parameter \"X-HopexSession\" must be set in the header of your request.";
                Logger.Debug(message);
                return BadRequest(message);
            }

            // Get the existing Hopex session 
            var hopexService = HopexServiceHelper.GetMwasService(hopexSessionInfo.MwasUrl, hopexSessionInfo.HopexSessionToken);

            // Call the execution result of the macro in async mode
            var asyncMacroResult = hopexService.CallAsyncMacroGetResult(actionId);

            // Return status if action is not finished
            if (asyncMacroResult.Status == "InProgress")
            {
                return Ok(asyncMacroResult.Status);
            }

            // Close the Hopex session
            if (closeSession)
            {
                hopexService.CloseUpdateSession();
            }

            // Return the macro result
            return Ok(asyncMacroResult.Status == "Terminate" ? asyncMacroResult.Result : JsonConvert.SerializeObject(asyncMacroResult));
        }

        private static MwasSettings InitMwasSettings()
        {
            var mwasSettings = new MwasSettings
            {
                CacheFileRootPath = ConfigurationManager.AppSettings["CacheFileRootPath"],
                MaxMegaSessionCount = ConfigurationManager.AppSettings["MaxMegaSessionCount"],
                OpenSessionTimeout = ConfigurationManager.AppSettings["OpenSessionTimeout"],
                StatMinPanelTime = ConfigurationManager.AppSettings["StatMinPanelTime"],
                MultiThreadLimit = ConfigurationManager.AppSettings["MultiThreadLimit"],
                MinConnectionDuration = ConfigurationManager.AppSettings["MinConnectionDuration"],
                MaxConnectionRetry = ConfigurationManager.AppSettings["MaxConnectionRetry"],
                CacheSerialize = ConfigurationManager.AppSettings["CacheSerialize"],
                CacheFileDiscard = ConfigurationManager.AppSettings["CacheFileDiscard"],
                CheckState = ConfigurationManager.AppSettings["CheckState"],
                LazyLog = ConfigurationManager.AppSettings["LazyLog"],
                DisableCache = ConfigurationManager.AppSettings["DisableCache"],
                AllowAnonymousConnection = ConfigurationManager.AppSettings["AllowAnonymousConnection"],
                LogRequest = ConfigurationManager.AppSettings["LogRequest"]
            };
            return mwasSettings;
        }

        private MwasSessionConnectionParameters InitMwasSessionConnectionParameters(string sessionMode, string accessMode, HopexContext hopexContext, UserInfo userInfo)
        {
            var mwasSessionConnectionParameters = new MwasSessionConnectionParameters
            {
                Environment = hopexContext.EnvironmentId,
                Database = hopexContext.RepositoryId,
                Profile = hopexContext.ProfileId,
                DataLanguage = hopexContext.DataLanguageId,
                GuiLanguage = hopexContext.GuiLanguageId,
                Person = userInfo.HopexAuthPerson,
                Login = userInfo.HopexAuthLogin,
                AuthenticationToken = userInfo.HopexAuthToken,
                SessionMode = sessionMode,
                AccessMode = accessMode
            };
            return mwasSessionConnectionParameters;
        }
    }
}
