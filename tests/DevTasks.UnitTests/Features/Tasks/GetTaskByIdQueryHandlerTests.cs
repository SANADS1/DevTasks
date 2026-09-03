using DevTasks.Application.DTOs;
using DevTasks.Application.Features.Tasks.Queries;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Domain.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using Xunit;

namespace DevTasks.UnitTests.Features.Tasks
{
    public class GetTaskByIdQueryHandlerTests
    {
        private readonly Mock<ITaskRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<GetTaskByIdQueryHandler>> _loggerMock;
        private readonly GetTaskByIdQueryHandler _sut;

        public GetTaskByIdQueryHandlerTests()
        {
            _repositoryMock = new Mock<ITaskRepository>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<GetTaskByIdQueryHandler>>();

            _cacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            _sut = new GetTaskByIdQueryHandler(_repositoryMock.Object, _mapperMock.Object, _cacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
        {
            var ownerId = Guid.NewGuid();
            var requestingUserId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "X" };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

            Func<Task> act = () => _sut.Handle(new GetTaskByIdQuery(taskId, requestingUserId, false), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_WhenUserIsOwner_ReturnsTask()
        {
            var ownerId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "My Task" };
            var expectedDto = new TaskItemDto { Id = taskId, Title = "My Task" };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
            _mapperMock.Setup(m => m.Map<TaskItemDto>(task)).Returns(expectedDto);

            var result = await _sut.Handle(new GetTaskByIdQuery(taskId, ownerId, false), CancellationToken.None);

            result.Should().NotBeNull();
            result!.Id.Should().Be(taskId);
        }

        [Fact]
        public async Task Handle_WhenUserIsAdmin_BypassesOwnershipCheck()
        {
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Someone else's task" };
            var expectedDto = new TaskItemDto { Id = taskId, Title = "Someone else's task" };

            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
            _mapperMock.Setup(m => m.Map<TaskItemDto>(task)).Returns(expectedDto);

            var result = await _sut.Handle(new GetTaskByIdQuery(taskId, adminId, true), CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WhenTaskDoesNotExist_ReturnsNull()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

            var result = await _sut.Handle(new GetTaskByIdQuery(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

            result.Should().BeNull();
        }
    }
}