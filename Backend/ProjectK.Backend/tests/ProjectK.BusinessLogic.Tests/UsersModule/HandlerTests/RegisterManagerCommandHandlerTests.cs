using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers;

namespace ProjectK.BusinessLogic.Tests.UsersModule.HandlerTests
{
    public class RegisterManagerHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDbContextTransaction> _transactionMock;
        private readonly RegisterManagerCommandHandler _handler;

        public RegisterManagerHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionMock = new Mock<IDbContextTransaction>();

            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transactionMock.Object);

            _handler = new RegisterManagerCommandHandler(_mediatorMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenAllStepsSucceed()
        {
            // Arrange
            var command = new RegisterManagerCommand
            {
                Email = "manager@example.com",
                Password = "Password123!",
                FirstName = "John",
                MiddleName = "M",
                LastName = "Manager",
                PhoneNumber = "123456789",
                KurinNumber = 5
            };

            var kurinKey = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var memberKey = Guid.NewGuid();

            var kurinResult = new ServiceResult<KurinResponse>(
                ResultType.Created,
                new KurinResponse { KurinKey = kurinKey, Number = 5 });

            var userResult = new ServiceResult<RegisterUserResponse>(
                ResultType.Success,
                new RegisterUserResponse
                {
                    UserId = userId,
                    Email = command.Email,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Tokens = new JwtResponse
                    {
                        AccessToken = "access-token",
                        RefreshToken = new RefreshToken
                        {
                            Token = "refresh-token",
                            Expires = DateTime.UtcNow.AddDays(7)
                        }
                    }
                });

            var memberResult = new ServiceResult<MemberResponse>(
                ResultType.Created,
                new MemberResponse
                {
                    MemberKey = memberKey,
                    KurinKey = kurinKey,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Email = command.Email,
                    PhoneNumber = command.PhoneNumber
                });

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(kurinResult);
            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userResult);
            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberResult);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(userId, result.Data.UserId);
            Assert.Equal(command.Email, result.Data.Email);
            Assert.Equal(command.FirstName, result.Data.FirstName);
            Assert.Equal(command.LastName, result.Data.LastName);
            Assert.NotNull(result.Data.Tokens);
            Assert.Equal("access-token", result.Data.Tokens.AccessToken);

            // Verify all commands were sent
            _mediatorMock.Verify(x => x.Send(It.Is<UpsertKurin>(cmd => cmd.Number == 5), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.Is<RegisterUserCommand>(cmd =>
                cmd.Email == command.Email &&
                cmd.Role == "Manager" &&
                cmd.KurinKey == kurinKey), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.Is<UpsertMember>(cmd =>
                cmd.KurinKey == kurinKey &&
                cmd.UserKey == userId &&
                cmd.Email == command.Email), It.IsAny<CancellationToken>()), Times.Once);

            // Verify transaction management
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCreateCorrectUpsertKurin()
        {
            // Arrange
            var command = new RegisterManagerCommand
            {
                Email = "test@example.com",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123",
                KurinNumber = 10
            };

            SetupSuccessfulScenario(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(x => x.Send(It.Is<UpsertKurin>(cmd =>
                cmd.Number == 10 &&
                cmd.KurinKey == Guid.Empty), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCreateCorrectRegisterUser()
        {
            // Arrange
            var command = new RegisterManagerCommand
            {
                Email = "manager@test.com",
                Password = "SecurePass123!",
                FirstName = "Manager",
                LastName = "User",
                PhoneNumber = "987654321",
                KurinNumber = 15
            };

            var kurinKey = Guid.NewGuid();
            SetupSuccessfulScenario(command, kurinKey);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(x => x.Send(It.Is<RegisterUserCommand>(cmd =>
                cmd.Email == "manager@test.com" &&
                cmd.Password == "SecurePass123!" &&
                cmd.FirstName == "Manager" &&
                cmd.LastName == "User" &&
                cmd.Role == "Manager" &&
                cmd.KurinKey == kurinKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCreateCorrectUpsertMember()
        {
            // Arrange
            var command = new RegisterManagerCommand
            {
                Email = "member@test.com",
                Password = "password",
                FirstName = "John",
                MiddleName = "Middle",
                LastName = "Doe",
                PhoneNumber = "555-0123",
                KurinNumber = 20
            };

            var kurinKey = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupSuccessfulScenario(command, kurinKey, userId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(x => x.Send(It.Is<UpsertMember>(cmd =>
                cmd.FirstName == "John" &&
                cmd.MiddleName == "Middle" &&
                cmd.LastName == "Doe" &&
                cmd.Email == "member@test.com" &&
                cmd.PhoneNumber == "555-0123" &&
                cmd.KurinKey == kurinKey &&
                cmd.UserKey == userId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_WhenKurinCreationFails()
        {
            // Arrange
            var command = CreateValid();
            var exception = new Exception("Kurin creation failed");

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Same(exception, thrownException);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_WhenUserRegistrationFails()
        {
            // Arrange
            var command = CreateValid();
            var exception = new Exception("User registration failed");

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<KurinResponse>(ResultType.Created, new KurinResponse { KurinKey = Guid.NewGuid() }));
            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Same(exception, thrownException);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_WhenMemberCreationFails()
        {
            // Arrange
            var command = CreateValid();
            var exception = new Exception("Member creation failed");

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<KurinResponse>(ResultType.Created, new KurinResponse { KurinKey = Guid.NewGuid() }));
            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<RegisterUserResponse>(ResultType.Success, CreateValidRegisterUserResponse()));
            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Same(exception, thrownException);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_WhenSaveChangesFails()
        {
            // Arrange
            var command = CreateValid();
            var exception = new Exception("Save changes failed");

            SetupSuccessfulResponses();
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Same(exception, thrownException);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_WhenCommitFails()
        {
            // Arrange
            var command = CreateValid();
            var exception = new Exception("Commit failed");

            SetupSuccessfulResponses();
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Same(exception, thrownException);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldHandleNullMiddleName()
        {
            // Arrange
            var command = new RegisterManagerCommand
            {
                Email = "test@example.com",
                Password = "password",
                FirstName = "Test",
                MiddleName = null,
                LastName = "User",
                PhoneNumber = "123",
                KurinNumber = 1
            };

            SetupSuccessfulScenario(command);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            _mediatorMock.Verify(x => x.Send(It.Is<UpsertMember>(cmd =>
                cmd.MiddleName == null), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldExecutesInCorrectOrder()
        {
            // Arrange
            var command = CreateValid();
            var executionOrder = new List<string>();

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("UpsertKurin"))
                .ReturnsAsync(new ServiceResult<KurinResponse>(ResultType.Created, new KurinResponse { KurinKey = Guid.NewGuid() }));

            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("RegisterUser"))
                .ReturnsAsync(new ServiceResult<RegisterUserResponse>(ResultType.Success, CreateValidRegisterUserResponse()));

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("UpsertMember"))
                .ReturnsAsync(new ServiceResult<MemberResponse>(ResultType.Created, new MemberResponse()));

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("SaveChanges"))
                .ReturnsAsync(1);

            _transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("Commit"))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(new[] { "UpsertKurin", "RegisterUser", "UpsertMember", "SaveChanges", "Commit" }, executionOrder.ToArray());
        }

        [Fact]
        public async Task Handle_ShouldDisposeTransaction()
        {
            // Arrange
            var command = CreateValid();
            SetupSuccessfulScenario(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var handler = new RegisterManagerCommandHandler(_mediatorMock.Object, _unitOfWorkMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        private RegisterManagerCommand CreateValid()
        {
            return new RegisterManagerCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                MiddleName = "Middle",
                LastName = "User",
                PhoneNumber = "123456789",
                KurinNumber = 1
            };
        }

        private RegisterUserResponse CreateValidRegisterUserResponse()
        {
            return new RegisterUserResponse
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Tokens = new JwtResponse
                {
                    AccessToken = "access-token",
                    RefreshToken = new RefreshToken { Token = "refresh-token", Expires = DateTime.UtcNow.AddDays(7) }
                }
            };
        }

        private void SetupSuccessfulResponses()
        {
            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<KurinResponse>(ResultType.Created, new KurinResponse { KurinKey = Guid.NewGuid() }));
            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<RegisterUserResponse>(ResultType.Success, CreateValidRegisterUserResponse()));
            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<MemberResponse>(ResultType.Created, new MemberResponse()));
        }

        private void SetupSuccessfulScenario(RegisterManagerCommand command, Guid? kurinKey = null, Guid? userId = null)
        {
            var actualKurinKey = kurinKey ?? Guid.NewGuid();
            var actualUserId = userId ?? Guid.NewGuid();

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertKurin>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<KurinResponse>(ResultType.Created, new KurinResponse { KurinKey = actualKurinKey, Number = command.KurinNumber }));

            _mediatorMock.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<RegisterUserResponse>(ResultType.Success, new RegisterUserResponse
                {
                    UserId = actualUserId,
                    Email = command.Email,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Tokens = new JwtResponse
                    {
                        AccessToken = "access-token",
                        RefreshToken = new RefreshToken { Token = "refresh-token", Expires = DateTime.UtcNow.AddDays(7) }
                    }
                }));

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<MemberResponse>(ResultType.Created, new MemberResponse
                {
                    MemberKey = Guid.NewGuid(),
                    KurinKey = actualKurinKey,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Email = command.Email
                }));

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }
    }
}