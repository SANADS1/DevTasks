using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DevTasks.Application.DTOs;
using DevTasks.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using DevTasks.Application.Features.Tasks.Commands;
using DevTasks.Application.Features.Tasks.Queries;

namespace DevTasks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IValidator<CreateTaskDto> _createValidator;
        private readonly IValidator<UpdateTaskDto> _updateValidator;
        private readonly IMediator _mediator;

        public TasksController(
            ITaskService taskService,
            IValidator<CreateTaskDto> createValidator,
            IValidator<UpdateTaskDto> updateValidator,
            IMediator mediator)
        {
            _taskService = taskService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _mediator = mediator;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll()
        {
            var tasks = await _mediator.Send(new GetAllTasksQuery(GetUserId(), IsAdmin()));
            return Ok(tasks);
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll()
        //{
        //    var tasks = await _taskService.GetAllTasksAsync(GetUserId(), IsAdmin());
        //    return Ok(tasks);
        //}

        //[HttpGet("{id}")]
        //public async Task<ActionResult<TaskItemDto>> GetById(Guid id)
        //{
        //    try
        //    {
        //        var task = await _taskService.GetTaskByIdAsync(id, GetUserId(), IsAdmin());
        //        if (task == null) return NotFound();
        //        return Ok(task);
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return Forbid();
        //    }
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItemDto>> GetById(Guid id)
        {
            try
            {
                var task = await _mediator.Send(new GetTaskByIdQuery(id, GetUserId(), IsAdmin()));
                if (task == null) return NotFound();
                return Ok(task);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        //[HttpPost]
        //public async Task<ActionResult<TaskItemDto>> Create(CreateTaskDto createDto)
        //{
        //    var validationResult = await _createValidator.ValidateAsync(createDto);
        //    if (!validationResult.IsValid)
        //        return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

        //    var newTask = await _taskService.CreateTaskAsync(createDto, GetUserId());
        //    return CreatedAtAction(nameof(GetById), new { id = newTask.Id }, newTask);
        //}

        [HttpPost]
        public async Task<ActionResult<TaskItemDto>> Create(CreateTaskDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

            var newTask = await _mediator.Send(new CreateTaskCommand(createDto, GetUserId()));
            return CreatedAtAction(nameof(GetById), new { id = newTask.Id }, newTask);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateTaskDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

            try
            {
                var updated = await _mediator.Send(new UpdateTaskCommand(id, updateDto, GetUserId(), IsAdmin()));
                if (!updated) return NotFound();
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var deleted = await _mediator.Send(new DeleteTaskCommand(id, GetUserId(), IsAdmin()));
                if (!deleted) return NotFound();
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }


        //    [HttpPut("{id}")]
        //    public async Task<IActionResult> Update(Guid id, UpdateTaskDto updateDto)
        //    {
        //        var validationResult = await _updateValidator.ValidateAsync(updateDto);
        //        if (!validationResult.IsValid)
        //            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

        //        try
        //        {
        //            var updated = await _taskService.UpdateTaskAsync(id, updateDto, GetUserId(), IsAdmin());
        //            if (!updated) return NotFound();
        //            return NoContent();
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            return Forbid();
        //        }
        //    }

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    try
        //    {
        //        var deleted = await _taskService.DeleteTaskAsync(id, GetUserId(), IsAdmin());
        //        if (!deleted) return NotFound();
        //        return NoContent();
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return Forbid();
        //    }
        //}
    }
}