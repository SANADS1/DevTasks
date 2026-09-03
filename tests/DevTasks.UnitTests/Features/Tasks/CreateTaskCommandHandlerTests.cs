using DevTasks.Application.DTOs;
using DevTasks.Application.Features.Tasks.Commands;
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
    public class CreateTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<CreateTaskCommandHandler>> _loggerMock;
        private readonly CreateTaskCommandHandler _sut;

        public CreateTaskCommandHandlerTests()
        {
            _repositoryMock = new Mock<ITaskRepository>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<CreateTaskCommandHandler>>();
            _sut = new CreateTaskCommandHandler(_repositoryMock.Object, _mapperMock.Object, _cacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_SetsUserIdFromCommand()
        {
            var userId = Guid.NewGuid();
            var createDto = new CreateTaskDto { Title = "New task" };
            var mappedEntity = new TaskItem { Title = createDto.Title };

            _mapperMock.Setup(m => m.Map<TaskItem>(createDto)).Returns(mappedEntity);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => t);
            _mapperMock.Setup(m => m.Map<TaskItemDto>(It.IsAny<TaskItem>()))
                       .Returns((TaskItem t) => new TaskItemDto { Id = t.Id, Title = t.Title });

            await _sut.Handle(new CreateTaskCommand(createDto, userId), CancellationToken.None);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t.UserId == userId)), Times.Once);
        }
    }
}