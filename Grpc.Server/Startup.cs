using Gprc.Server.Extensions;
using Grpc.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Grpc.Server
{
    public class Startup
    {
        private static readonly SymmetricSecurityKey _securityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();

            services.AddJwtAuthorization();
            services.AddJwtAuthentication(_securityKey);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UsersService>();

                endpoints.MapGet("/jwt", async context =>
                {
                    var jwt = GenerateJwt();
                    await context.Response.WriteAsync(jwt);
                });

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        private string GenerateJwt()
        {
            var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload("GrpcServer", "GrpcClient", null, DateTime.Now, DateTime.Now.AddSeconds(60));
            var token = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
