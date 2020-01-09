using Mega.WebServiceTemplate1.Models;
using Newtonsoft.Json;
using System;
using System.Web.Http;

namespace Mega.WebServiceTemplate1.Controllers
{
    [RoutePrefix("api/test")]
    public class DefaultController : BaseController
    {
        private const string SanityCheckMacro = "06CC08C45CAE6E28";

        [HttpPost]
        [Route("sanitycheck")]
        public IHttpActionResult SanityCheck(Person person)
        {
            var data = JsonConvert.SerializeObject(person);
            var result = CallMacro(SanityCheckMacro, data);
            return FormatResult(result);
        }

        [HttpPost]
        [Route("async/sanitycheck")]
        public IHttpActionResult AsyncSanityCheck(Person person)
        {
            var data = JsonConvert.SerializeObject(person);
            return CallAsyncMacroExecute(SanityCheckMacro, data);
        }

        [HttpGet]
        [Route("async/sanitycheck/result/{actionId}")]
        public IHttpActionResult AsyncSanityCheckGetResult(string actionId)
        {
            return CallAsyncMacroGetResult(actionId);
        }

        private IHttpActionResult FormatResult(WebServiceResult result)
        {
            switch (result.ErrorType)
            {
                case "None":
                    return Ok(result.Content);
                case "BadRequest":
                    return BadRequest(result.Content);
                default:
                    return InternalServerError(new Exception($"{result.ErrorType}: {result.Content}"));
            }
        }
    }
}