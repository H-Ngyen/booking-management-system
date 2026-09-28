using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class HealthController : BaseApiController
{
    [HttpGet()] 
    [AllowAnonymous]
    public ActionResult<string> HealthCheck() 
    {
        return Ok("okay!!");
    }
}
