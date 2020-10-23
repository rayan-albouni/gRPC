# gRPC with .NET Core
[![GitHub license](https://img.shields.io/github/license/mikesuffield/gRPC?color=blue&label=License)](https://github.com/mikesuffield/gRPC/blob/master/LICENSE)

## Contents
- [Introduction](#introduction)
- [Example project](#example-project)
- [Protobuf files](#protobuf-files)
	- [Example](#example)
- [Developing gRPC services with .NET Core](#developing-grpc-services-with-.net-core)
- [gRPC vs HTTP APIs](#grpc-vs-http-apis)
- [When to use gRPC](#when-to-use-grpc)
- [Drawbacks](#drawbacks)
- [Further reading](#further-reading)

## Introduction

gRPC is a Remote Procedure Call system initially developed at Google, and is now an open-source Cloud Native Computing Foundation [incubating project](https://www.cncf.io/blog/2017/03/01/cloud-native-computing-foundation-host-grpc-google/). It represents a method of web communication between services, allowing a server to respond to requests and return requested information to a caller.

On the surface, gRPC may appear similar to a traditional HTTP API, but different they differ underneath:
- gRPC communication is via a binary string (as opposed to JSON or XML you may expect via API) - this is more efficient
- gRPC replies on a known configuration / contract between the client and the server, known as an Interface Definition Language (IDL) - protocol buffer (protobuf) `.proto` files for this

## Example project
The provided example `Grpc` solution contains two projects
- `Grpc.Server` - sits and listens for calls. This is a gRPC Visual Studio project.
- `Grpc.Client` - sends calls to server. This is a console app with the following nuget packages:
    - `Google.Protobuf`
    - `Grpc.Net.Client`
    - `Grpc.Tools`

## Protobuf files
Both the client and server make use of `.proto` files to define the the protocol buffer, which is the contract between the server and the client. Both the client and server need to be using the exact same version of the `.proto` file, so they need to be kept in sync. The `.proto` file is made up of two main parts.
- `service` items
    - Roughly equivalent to a method
    - Defines what you expect to send to the method and what you expect to receive back
    - Each "method" has one input and returns one output
- `message` items
    - Roughly equivalent to a model / view model / DTO
	- Can be nested to create complex models
    - Contains 0 or more properties and the order of those properties
		- Supported message property types include bool, int32, float, double, string, enums and more - see the full list [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/protobuf?view=aspnetcore-3.1).

A list of items can be represented in different ways:
- `repeated` - represents a read-only list of items that are returned at the same time
- `stream` - returns a stream of items to the client as soon as they become available, allowing the client to begin processing initial items before receiving the full list. Learn more about gRPC streaming [here](https://grpc.io/docs/what-is-grpc/core-concepts/#rpc-life-cycle)

### Example
The following `.proto` file is a snippet of the `users.proto` file, used to represent a _UsersService_ in the example project.

```cs
syntax = "proto3";

option csharp_namespace = "Grpc.Server";

service User {
	rpc GetUser (GetUserByIdViewModel) returns (UserViewModel) {}
	rpc GetUsersInGroupList (GetUsersInGroupViewModel) returns (UsersInGroupListViewModel) {}
	rpc GetUsersInGroupStream (GetUsersInGroupViewModel) returns (stream UserInGroupViewModel) {}
}

message GetUsersInGroupViewModel {
	int32 groupId = 1;
}

message UsersInGroupListViewModel {
	repeated UserInGroupViewModel users = 1;
}

message UserInGroupViewModel {
	int32 id = 1;
	...
}

message GetUserByIdViewModel {
	int32 userId = 1;
}

message UserViewModel {
	int32 id = 1;
	string firstName = 2;
	string surname = 3;
	string emailAddress = 4;
}
```

It may help to imagine the service and messages as the following methods and view models in C#.

| Protocol Buffer                                                              | C#                                                                                                         |
|-----------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| `service User {`<br>&nbsp; &nbsp; &nbsp;`rpc GetUser (GetUserByIdViewModel) returns (UserViewModel) {}`<br>`}` | `public interface IUsersService`<br>`{`<br>&nbsp; &nbsp; &nbsp;`public UserViewModel GetUser(GetUserByIdViewModel request);` <br>`}` |
| `message GetUserByIdViewModel {`<br>&nbsp; &nbsp; &nbsp;`int32 userId = 1;` <br>`}` | `public class GetUserByIdViewModel`<br>`{`<br>&nbsp; &nbsp; &nbsp;`public int UserId { get; set; }`<br>`}`  |

## Developing gRPC services with .NET Core
Underlying base classes for the services, methods and models defined in the `.proto` file are automatically generated for you. To ensure this happens, make sure the following properties are set on the `.proto` files in Visual Studio:
- Build Action as "Protobuf compiler"
- gRPC Stub Classes as "Server only" or "Client only" (depending on project)
- Class Access as "Public"
- Compile Protobuf as "Yes"
Note - you may need to rebuild the solution after changing the `.proto` file to force the generation of underlying classes.

You can make use of the auto-generated files as follows:
- The UsersService must extend the `User.UserBase` base class
- Implement methods on the service by overriding methods from the base class - type `public override` and allow IntelliSense to list the available methods for you.
- Models should be automatically available for you to use

Finally, all services must be mapped in `Startup.cs` in the server project.

gRPC services can still leverage ASP.NET Core Authorization and Authentication, including Bearer token authentication and the use of the `Authorize` attribute on services and methods - for more information, see [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-3.1).

See the example project for full implementations of the above.

## gRPC vs HTTP APIs
If you have experience with HTTP APIs, there are lots of features of gRPC requests that you will already be familiar with:
- Headers - accessible via `context.RequestHeaders`
- Status codes - these are roughly equivalent to (but not the same as) HTTP Status Codes, and include common codes such as `NOT_FOUND` and `PERMISSION_DENIED` - a full list of available status codes is available [here](https://github.com/grpc/grpc/blob/master/doc/statuscodes.md)

Features specific to gRPC include
- Trailers - these are like Headers but are received at the end of the call - see [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/client?view=aspnetcore-3.1#access-grpc-trailers)
- Deadlines - allows the caller to set a timeout period when making a call - more information available [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-3.1)
- Support for [bi-directional streaming](https://grpc.io/docs/what-is-grpc/core-concepts/#rpc-life-cycle)

However, traditional HTTP APIs have the following benefits:
- Can be called directly from a browser (gRPC does not support browsers by default, although there are [ways around this](https://docs.microsoft.com/en-us/aspnet/core/grpc/browser?view=aspnetcore-3.1))
- Human readable data (JSON or XML instead of a binary string)

Furthermore, traditional HTTP APIs, particularly when designed with REST in mind, target a resource-oriented approach (e.g. CRUD), where as gRPC, as the name suggests, targets a remote procedure approach.

Further comparisons are available [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/comparison?view=aspnetcore-3.1).

## When to use gRPC
gRPC is a nice tool to have in your developer tool box, but you should know when (and when not) to use it. Although it may be tempting to use the latest, shiniest tools, often a traditional HTTP API or communication via service bus messages may offer a better solution.

Typical use cases for gRPC include
- Microservices where efficiency is critical (i.e. a service bus won't do)
  - gRPC is high performance and lightweight - messages are sent in binary resulting in reduced network usage
- Large systems where easy cross language communication is required
  - gRPC is an open standard supported by many languages, and use of `.proto` contract files makes it language agnostic
- IoT devices

## Drawbacks

gRPC servers require special hosting. For those in the Microsoft Azure ecosystem, gRPC servers cannot be hosted via IIS or in an Azure App Service. Instead the server must be hosted via Azure Kubernetes Service (AKS). For more information see [this GitHub issue](https://github.com/dotnet/AspNetCore/issues/9020).

gRPC also requires the server and client(s) to have access to the same `.proto` file, and it has to be kept in sync. It is possible to reference `.proto` files via URLs, but you still need to be careful that any updates are [non-breaking](https://docs.microsoft.com/en-us/aspnet/core/grpc/versioning?view=aspnetcore-3.1). See [this article](https://docs.microsoft.com/en-us/aspnet/core/grpc/dotnet-grpc?view=aspnetcore-3.1) for more information.

## Further reading
- [gRPC for .NET Examples](https://github.com/grpc/grpc-dotnet/tree/master/examples) - GitHub examples for gRPC scenarios
- [gRPC UI](https://github.com/fullstorydev/grpcui) - UI for testing gRPC servers (similar to Postman)
- [Intro to gRPC in C# - How To Get Started](https://www.youtube.com/watch?v=QyxCX2GYHxk) - Tim Corey
- [Introduction to gRPC on .NET Core](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-3.1) - Microsoft Docs
- [Create Protobuf messages for .NET apps](https://docs.microsoft.com/en-us/aspnet/core/grpc/protobuf?view=aspnetcore-3.1) - Microsoft Docs
- [Implementing Microservices with gRPC and .NET Core 3.1](https://auth0.com/blog/implementing-microservices-grpc-dotnet-core-3/) - Auth0 Blog