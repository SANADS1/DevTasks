//using MapsterMapper;
//using Bogus;
//using DevTasks.Application.DTOs;
//using DevTasks.Application.Interfaces.Repositories;
//using DevTasks.Application.Services;
//using DevTasks.Domain.Entities;
//using FluentAssertions;
//using Moq;
//using Xunit;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Logging;
//using System.Threading;

//namespace DevTasks.UnitTests.Services
//{
//    public class TaskServiceTests
//    {
//        private readonly Mock<ITaskRepository> _repositoryMock;
//        private readonly Mock<IMapper> _mapperMock;
//        private readonly Mock<ILogger<TaskService>> _loggerMock;
//        private readonly Mock<IDistributedCache> _cacheMock;
//        private readonly TaskService _sut; // "system under test"
//        private readonly Faker _faker = new();

//        public TaskServiceTests()
//        {
//            _repositoryMock = new Mock<ITaskRepository>();
//            _mapperMock = new Mock<IMapper>();
//            _loggerMock = new Mock<ILogger<TaskService>>();
//            _cacheMock = new Mock<IDistributedCache>();
//            // Explicitly force a cache miss by default -- Moq's default for byte[]-returning
//            // methods is an empty array, not null, which otherwise causes GetStringAsync to
//            // return "" instead of null and wrongly trigger the cache-hit branch.
//            _cacheMock
//                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync((byte[]?)null);
//            _sut = new TaskService(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object, _cacheMock.Object);
//        }

//        [Fact]
//        public async Task GetTaskByIdAsync_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var requestingUserId = Guid.NewGuid(); // different user
//            var taskId = Guid.NewGuid();

//            var task = new TaskItem
//            {
//                Id = taskId,
//                UserId = ownerId,
//                Title = _faker.Lorem.Sentence()
//            };

//            _repositoryMock
//                .Setup(r => r.GetByIdAsync(taskId))
//                .ReturnsAsync(task);

//            // Act
//            Func<Task> act = () => _sut.GetTaskByIdAsync(taskId, requestingUserId, isAdmin: false);

//            // Assert
//            await act.Should().ThrowAsync<UnauthorizedAccessException>();
//        }

//        [Fact]
//        public async Task GetTaskByIdAsync_WhenUserIsOwner_ReturnsTask()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var taskId = Guid.NewGuid();

//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "My Task" };
//            var expectedDto = new TaskItemDto { Id = taskId, Title = "My Task" };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
//            _mapperMock.Setup(m => m.Map<TaskItemDto>(task)).Returns(expectedDto);

//            // Act
//            var result = await _sut.GetTaskByIdAsync(taskId, ownerId, isAdmin: false);

//            // Assert
//            result.Should().NotBeNull();
//            result!.Id.Should().Be(taskId);
//        }

//        [Fact]
//        public async Task GetTaskByIdAsync_WhenUserIsAdmin_BypassesOwnershipCheck()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var adminId = Guid.NewGuid(); // not the owner
//            var taskId = Guid.NewGuid();

//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Someone else's task" };
//            var expectedDto = new TaskItemDto { Id = taskId, Title = "Someone else's task" };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
//            _mapperMock.Setup(m => m.Map<TaskItemDto>(task)).Returns(expectedDto);

//            // Act
//            var result = await _sut.GetTaskByIdAsync(taskId, adminId, isAdmin: true);

//            // Assert
//            result.Should().NotBeNull();
//        }

//        [Fact]
//        public async Task GetTaskByIdAsync_WhenTaskDoesNotExist_ReturnsNull()
//        {
//            // Arrange
//            _repositoryMock
//                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
//                .ReturnsAsync((TaskItem?)null);

//            // Act
//            var result = await _sut.GetTaskByIdAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false);

//            // Assert
//            result.Should().BeNull();
//        }

//        [Fact]
//        public async Task CreateTaskAsync_SetsUserIdFromParameter()
//        {
//            // Arrange
//            var userId = Guid.NewGuid();
//            var createDto = new CreateTaskDto { Title = _faker.Lorem.Sentence() };
//            var mappedEntity = new TaskItem { Title = createDto.Title };

//            _mapperMock.Setup(m => m.Map<TaskItem>(createDto)).Returns(mappedEntity);
//            _repositoryMock
//                .Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
//                .ReturnsAsync((TaskItem t) => t); // echo back whatever was passed in
//            _mapperMock
//                .Setup(m => m.Map<TaskItemDto>(It.IsAny<TaskItem>()))
//                .Returns((TaskItem t) => new TaskItemDto { Id = t.Id, Title = t.Title });

//            // Act
//            await _sut.CreateTaskAsync(createDto, userId);

//            // Assert
//            _repositoryMock.Verify(r => r.AddAsync(
//                It.Is<TaskItem>(t => t.UserId == userId)), Times.Once);
//        }

//        [Fact]
//        public async Task UpdateTaskAsync_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var requestingUserId = Guid.NewGuid();
//            var taskId = Guid.NewGuid();
//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Original" };
//            var updateDto = new UpdateTaskDto { Title = "Updated", IsCompleted = true };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

//            // Act
//            Func<Task> act = () => _sut.UpdateTaskAsync(taskId, updateDto, requestingUserId, isAdmin: false);

//            // Assert
//            await act.Should().ThrowAsync<UnauthorizedAccessException>();
//            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
//        }

//        [Fact]
//        public async Task UpdateTaskAsync_WhenUserIsOwner_UpdatesAndReturnsTrue()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var taskId = Guid.NewGuid();
//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Original" };
//            var updateDto = new UpdateTaskDto { Title = "Updated", IsCompleted = true };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
//            _repositoryMock.Setup(r => r.UpdateAsync(task)).ReturnsAsync(true);

//            // Act
//            var result = await _sut.UpdateTaskAsync(taskId, updateDto, ownerId, isAdmin: false);

//            // Assert
//            result.Should().BeTrue();
//            _mapperMock.Verify(m => m.Map(updateDto, task), Times.Once);
//        }

//        [Fact]
//        public async Task UpdateTaskAsync_WhenTaskDoesNotExist_ReturnsFalse()
//        {
//            // Arrange
//            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
//            var updateDto = new UpdateTaskDto { Title = "Updated" };

//            // Act
//            var result = await _sut.UpdateTaskAsync(Guid.NewGuid(), updateDto, Guid.NewGuid(), isAdmin: false);

//            // Assert
//            result.Should().BeFalse();
//        }

//        [Fact]
//        public async Task DeleteTaskAsync_WhenUserIsNotOwnerAndNotAdmin_ThrowsUnauthorizedAccessException()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var requestingUserId = Guid.NewGuid();
//            var taskId = Guid.NewGuid();
//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Task" };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

//            // Act
//            Func<Task> act = () => _sut.DeleteTaskAsync(taskId, requestingUserId, isAdmin: false);

//            // Assert
//            await act.Should().ThrowAsync<UnauthorizedAccessException>();
//            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
//        }

//        [Fact]
//        public async Task DeleteTaskAsync_WhenUserIsAdmin_DeletesEvenIfNotOwner()
//        {
//            // Arrange
//            var ownerId = Guid.NewGuid();
//            var adminId = Guid.NewGuid();
//            var taskId = Guid.NewGuid();
//            var task = new TaskItem { Id = taskId, UserId = ownerId, Title = "Task" };

//            _repositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
//            _repositoryMock.Setup(r => r.DeleteAsync(taskId)).ReturnsAsync(true);

//            // Act
//            var result = await _sut.DeleteTaskAsync(taskId, adminId, isAdmin: true);

//            // Assert
//            result.Should().BeTrue();
//            _repositoryMock.Verify(r => r.DeleteAsync(taskId), Times.Once);
//        }
//    }
//}