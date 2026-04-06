@echo off
echo ============================
echo 🚀 Setting up CQRS E-Commerce API...
echo ============================
setlocal

:: Step 1: Create Solution & API Project
mkdir EcommerceAPI
cd EcommerceAPI
dotnet new sln -n Ecommerce

:: Create Main API Project
dotnet new webapi -n Ecommerce.API

:: Create Core Application Layers
dotnet new classlib -n Ecommerce.Application
dotnet new classlib -n Ecommerce.Domain
dotnet new classlib -n Ecommerce.Infrastructure
dotnet new xunit -n Ecommerce.Tests

:: Add Projects to Solution
dotnet sln add Ecommerce.API
dotnet sln add Ecommerce.Application
dotnet sln add Ecommerce.Domain
dotnet sln add Ecommerce.Infrastructure
dotnet sln add Ecommerce.Tests

:: Add References
dotnet add Ecommerce.API reference Ecommerce.Application
dotnet add Ecommerce.API reference Ecommerce.Infrastructure
dotnet add Ecommerce.Application reference Ecommerce.Domain
dotnet add Ecommerce.Infrastructure reference Ecommerce.Domain
dotnet add Ecommerce.Tests reference Ecommerce.Application

:: Step 2: Install Required NuGet Packages
echo Installing required NuGet packages...
dotnet tool install --global dotnet-ef
dotnet add Ecommerce.Application package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add Ecommerce.API package MediatR
dotnet add Ecommerce.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add Ecommerce.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Ecommerce.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add Ecommerce.Application package FluentValidation.AspNetCore
dotnet add Ecommerce.Infrastructure package StackExchange.Redis
dotnet add Ecommerce.API package Serilog.AspNetCore
dotnet add Ecommerce.API package Microsoft.ApplicationInsights.AspNetCore

:: Step 3: Create CQRS Folder Structure
echo Creating CQRS folder structure...

:: API Layer
mkdir Ecommerce.API\Controllers
mkdir Ecommerce.API\DependencyInjection
mkdir Ecommerce.API\Middlewares

:: Application Layer (CQRS)
mkdir Ecommerce.Application\Features\Orders\Commands
mkdir Ecommerce.Application\Features\Orders\Handlers
mkdir Ecommerce.Application\Features\Orders\Queries
mkdir Ecommerce.Application\Features\Orders\Models
mkdir Ecommerce.Application\Common

:: Domain Layer
mkdir Ecommerce.Domain\Entities
mkdir Ecommerce.Domain\Enums
mkdir Ecommerce.Domain\Interfaces
mkdir Ecommerce.Domain\Common

:: Infrastructure Layer
mkdir Ecommerce.Infrastructure\Persistence
mkdir Ecommerce.Infrastructure\Persistence\Repositories
mkdir Ecommerce.Infrastructure\Persistence\Configurations
mkdir Ecommerce.Infrastructure\Caching
mkdir Ecommerce.Infrastructure\Security

:: Step 4: Create Placeholder Files for Order Processing
echo Creating Order feature files...

:: Controller
echo. > Ecommerce.API\Controllers\OrderController.cs

:: Command for Order Placement
echo. > Ecommerce.Application\Features\Orders\Commands\CreateOrderCommand.cs
echo. > Ecommerce.Application\Features\Orders\Handlers\CreateOrderHandler.cs

:: Query for Getting Order Details
echo. > Ecommerce.Application\Features\Orders\Queries\GetOrderByIdQuery.cs
echo. > Ecommerce.Application\Features\Orders\Handlers\GetOrderByIdHandler.cs

:: Model for Order
echo. > Ecommerce.Application\Features\Orders\Models\OrderDto.cs

:: Domain - Order Entity & Interface
echo. > Ecommerce.Domain\Entities\Order.cs
echo. > Ecommerce.Domain\Interfaces\IOrderRepository.cs

:: Infrastructure - Repository Implementation
echo. > Ecommerce.Infrastructure\Persistence\Repositories\OrderRepository.cs
echo. > Ecommerce.Infrastructure\Persistence\Configurations\OrderConfiguration.cs
echo. > Ecommerce.Infrastructure\Persistence\AppDbContext.cs

:: Step 5: Build the Solution (No Running API)
echo ============================
echo 🚀 Building Solution...
echo ============================
dotnet build

echo ============================
echo ✅ Build Complete! API is ready for development.
echo ============================

endlocal
