using DevTasks.Application.DTOs;
using DevTasks.Application.Features.Tasks.Commands;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Domain.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Threading;
using Xunit;

namespace DevTasks.UnitTests.Features.Tasks
{
    public class UpdateTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly UpdateTaskCommandHandler _sut;

        public UpdateTaskCommandHandlerTests()
        {
            _repositoryMock = new Mock<ITaskRepository>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IDistributedCache>();
            _sut = new UpdateTaskCommandHandler(_repositoryMock.Object, _mapperMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
        {
            var ownerId = Guid.NewGuid();
            var requestingUserId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Original" };
            var updateDto = new UpdateTaskDto { Title = "Updated", IsCompleted = true };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

            Func<Task> act = () => _sut.Handle(new UpdateTaskCommand(taskId, updateDto, requestingUserId, false), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserIsOwner_UpdatesAndReturnsTrue()
        {
            var ownerId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Original" };
            var updateDto = new UpdateTaskDto { Title = "Updated", IsCompleted = true };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
            _repositoryMock.Setup(r => r.UpdateAsync(task)).ReturnsAsync(true);

            var result = await _sut.Handle(new UpdateTaskCommand(taskId, updateDto, ownerId, false), CancellationToken.None);

            result.Should().BeTrue();
            _mapperMock.Verify(m => m.Map(updateDto, task), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTaskDoesNotExist_ReturnsFalse()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
            var updateDto = new UpdateTaskDto { Title = "Updated" };

            var result = await _sut.Handle(new UpdateTaskCommand(Guid.NewGuid(), updateDto, Guid.NewGuid(), false), CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}