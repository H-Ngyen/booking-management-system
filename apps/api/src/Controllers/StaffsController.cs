using API.Common;
using API.DTOs;
using API.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// api/v1/staffs
public class StaffsController(IStaffsService staffsService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<StaffDto>>> GetAllMatching([FromQuery] GetAllMatchStaffRequest request)
    {
        return await staffsService.GetAllMatch(request);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateNewStaffRequest request)
    {
        int id = await staffsService.Create(request);
        return Created("",id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(UpdateStaffRequest request, int id)
    {
        request.Id = id;
        await staffsService.Update(request);
        return NoContent();
    }

    [HttpGet("{staffId}/schedules")]
    public async Task<ActionResult<IEnumerable<WorkScheduleDto>>> GetSchedules(int staffId, [FromQuery] GetSchedulesRequest request)
    {
        IEnumerable<WorkScheduleDto> response = await staffsService.GetSchedules(staffId, request);
        return Ok(response);
    }

    [HttpPost("{staffId}/schedules")]
    public async Task<ActionResult<WorkScheduleDto>> CreateSchedule(int staffId, CreateScheduleRequest request)
    {
        WorkScheduleDto schedule = await staffsService.CreateSchedule(staffId, request);
        return Created("", schedule);
    }

    [HttpDelete("{staffId}/schedules/{scheduleId}")]
    public async Task<ActionResult> DeleteSchedule(int staffId, int scheduleId)
    {
        await staffsService.DeleteSchedule(staffId, scheduleId);
        return NoContent();
    }
}
