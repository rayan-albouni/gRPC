using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Server;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Grpc.Client
{
    class Program
    {
        private const string _grpcServerAddress = "https://localhost:5001";

        static async Task Main(string[] args)
        {
            using var grpcChannel = GrpcChannel.ForAddress(_grpcServerAddress);
            var userServiceClient = new User.UserClient(grpcChannel);

            GetUsersInGroupList(userServiceClient);
            Console.WriteLine("");
            await GetUsersInGroupStream(userServiceClient);

            Console.WriteLine("");
            Console.WriteLine("Enter the ID of the user to list their details, or type \"exit\" to quit.");

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }

                var userId = int.Parse(input);

                await GetUserById(userId, userServiceClient);
            }

            Console.WriteLine("End");
        }

        private static async Task GetUsersInGroupStream(User.UserClient userServiceClient)
        {
            var getUsersInGroupViewModel = new GetUsersInGroupViewModel
            {
                GroupId = 1,
            };

            using var call = userServiceClient.GetUsersInGroupStream(getUsersInGroupViewModel);
            while (await call.ResponseStream.MoveNext())
            {
                var user = call.ResponseStream.Current;

                Console.WriteLine($"{user.Id} - {user.FirstName} {user.Surname} - {user.EmailAddress} - {user.Role}|{user.MembershipId}");
            }
        }

        private static void GetUsersInGroupList(User.UserClient userServiceClient)
        {
            var getUsersInGroupViewModel = new GetUsersInGroupViewModel
            {
                GroupId = 1,
            };

            var userList = userServiceClient.GetUsersInGroupList(getUsersInGroupViewModel);

            foreach (var user in userList.Users)
            {
                Console.WriteLine($"{user.Id} - {user.FirstName} {user.Surname} - {user.EmailAddress} - {user.Role}|{user.MembershipId}");
            }
        }

        private static async Task GetUserById(int userId, User.UserClient userServiceClient)
        {
            var jwt = await GetJwt();
            var headers = new Metadata
            {
                { "Authorization", $"Bearer {jwt}" }
            };

            var getUserByIdViewModel = new GetUserByIdViewModel
            {
                UserId = userId,
            };

            try
            {
                var user = userServiceClient.GetUser(getUserByIdViewModel, headers);

                Console.WriteLine($"{user.Id} - {user.FirstName} {user.Surname} - {user.EmailAddress}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"{ex.Status.StatusCode} - {ex.Status.Detail}");
            }
        }

        private static async Task<string> GetJwt()
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_grpcServerAddress}/jwt"),
                Method = HttpMethod.Get,
                Version = new Version(2, 0)
            };
            var jwtResponse = await httpClient.SendAsync(request);
            
            jwtResponse.EnsureSuccessStatusCode();

            var jwt = await jwtResponse.Content.ReadAsStringAsync();
            return jwt;
        }
    }
}
