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
    public class GetAllTasksQueryHandlerTests
    {
        private readonly Mock<ITaskRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<GetAllTasksQueryHandler>> _loggerMock;
        private readonly GetAllTasksQueryHandler _sut;

        public GetAllTasksQueryHandlerTests()
        {
            _repositoryMock = new Mock<ITaskRepository>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<GetAllTasksQueryHandler>>();

            // Force a cache miss by default -- Moq's default for byte[]-returning methods
            // is an empty array, not null, which would otherwise make GetStringAsync return ""
            // and wrongly trigger the cache-hit branch.
            _cacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            _sut = new GetAllTasksQueryHandler(_repositoryMock.Object, _mapperMock.Object, _cacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenNotAdmin_CallsGetAllByUserIdAsync()
        {
            var userId = Guid.NewGuid();
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Guid.NewGuid(), UserId = userId, Title = "Task A" }
            };
            var dtos = new List<TaskItemDto>
            {
                new TaskItemDto { Id = tasks[0].Id, Title = "Task A" }
            };

            _repositoryMock.Setup(r => r.GetAllByUserIdAsync(userId)).ReturnsAsync(tasks);
            _mapperMock.Setup(m => m.Map<IEnumerable<TaskItemDto>>(tasks)).Returns(dtos);

            var result = await _sut.Handle(new GetAllTasksQuery(userId, IsAdmin: false), CancellationToken.None);

            result.Should().BeEquivalentTo(dtos);
            _repositoryMock.Verify(r => r.GetAllByUserIdAsync(userId), Times.Once);
            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenAdmin_CallsGetAllAsync()
        {
            var adminId = Guid.NewGuid();
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Someone's task" },
                new TaskItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Someone else's task" }
            };
            var dtos = tasks.Select(t => new TaskItemDto { Id = t.Id, Title = t.Title }).ToList();

            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);
            _mapperMock.Setup(m => m.Map<IEnumerable<TaskItemDto>>(tasks)).Returns(dtos);

            var result = await _sut.Handle(new GetAllTasksQuery(adminId, IsAdmin: true), CancellationToken.None);

            result.Should().BeEquivalentTo(dtos);
            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
            _repositoryMock.Verify(r => r.GetAllByUserIdAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}