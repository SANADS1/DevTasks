using DevTasks.Application.Features.Tasks.Commands;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using Xunit;

namespace DevTasks.UnitTests.Features.Tasks
{
    public class DeleteTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _repositoryMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<DeleteTaskCommandHandler>> _loggerMock;
        private readonly DeleteTaskCommandHandler _sut;

        public DeleteTaskCommandHandlerTests()
        {
            _repositoryMock = new Mock<ITaskRepository>();
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<DeleteTaskCommandHandler>>();
            _sut = new DeleteTaskCommandHandler(_repositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
        {
            var ownerId = Guid.NewGuid();
            var requestingUserId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Task" };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

            Func<Task> act = () => _sut.Handle(new DeleteTaskCommand(taskId, requestingUserId, false), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserIsAdmin_DeletesEvenIfNotOwner()
        {
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Task" };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
            _repositoryMock.Setup(r => r.DeleteAsync(taskId)).ReturnsAsync(true);

            var result = await _sut.Handle(new DeleteTaskCommand(taskId, adminId, true), CancellationToken.None);

            result.Should().BeTrue();
            _repositoryMock.Verify(r => r.DeleteAsync(taskId), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTaskDoesNotExist_ReturnsFalse()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

            var result = await _sut.Handle(new DeleteTaskCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}