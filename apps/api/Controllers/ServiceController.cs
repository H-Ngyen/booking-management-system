using API.Common;
using API.DTOs;
using API.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// api/v1/service
public class ServiceController(IServicesService servicesService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetAllMatching([FromQuery] GetAllMatchServiceRequest request)
    {
        return await servicesService.GetAllMatch(request);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateNewServiceRequest request)
    {
        int id = await servicesService.Create(request);
        return Created("",id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(UpdateServiceRequest request, int id)
    {
        request.Id = id;
        await servicesService.Update(request);
        return NoContent();
    }
}