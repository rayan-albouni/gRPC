using Bogus;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grpc.Server.Services
{
    // Here "User" in "User.UserBase" refers to the "User" service in the proto file
    public class UsersService : User.UserBase
    {
        private readonly ILogger<UsersService> _logger;
        private static readonly List<UserInGroupViewModel> _users = new Faker<UserInGroupViewModel>()
            .RuleFor(x => x.Id, x => x.IndexFaker + 1)
            .RuleFor(x => x.MembershipId, x => x.Random.Int(1, 100))
            .RuleFor(x => x.FirstName, x => x.Person.FirstName)
            .RuleFor(x => x.Surname, x => x.Person.LastName)
            .RuleFor(x => x.EmailAddress, x => x.Person.Email)
            .RuleFor(x => x.Role, x => x.PickRandom<UserInGroupViewModel.Types.Role>())
            .Generate(10);

        public UsersService(ILogger<UsersService> logger)
        {
            _logger = logger;
        }

        public override Task<UsersInGroupListViewModel> GetUsersInGroupList(
            GetUsersInGroupViewModel request,
            ServerCallContext context)
        {
            var usersInGroupList = new UsersInGroupListViewModel
            {
                Users = { _users },
            };

            return Task.FromResult(usersInGroupList);
        }

        public override async Task GetUsersInGroupStream(
            GetUsersInGroupViewModel request,
            IServerStreamWriter<UserInGroupViewModel> responseStream,
            ServerCallContext context)
        {
            foreach (var user in _users)
            {
                await Task.Delay(100);
                await responseStream.WriteAsync(user);
            }
        }

        [Authorize]
        public override Task<UserViewModel> GetUser(
            GetUserByIdViewModel request,
            ServerCallContext context)
        {
            var userId = request.UserId;
            _logger.LogInformation($"Getting user with ID {userId}");

            if (userId < 1)
            {
                var status = new Status(StatusCode.InvalidArgument, "User ID cannot be less than 1");
                throw new RpcException(status);
            }

            var user = _users.Where(x => x.Id == userId).FirstOrDefault();

            if (user == null)
            {
                var status = new Status(StatusCode.NotFound, $"User with ID {userId} could not be found");
                throw new RpcException(status);
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                Surname = user.Surname,
                EmailAddress = user.EmailAddress,
            };

            return Task.FromResult(userViewModel);
        }
    }
}
