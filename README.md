# Al-Ghani Medical Store

A stock, expiry and billing system for a medical store. It runs on one shop PC and works without internet.

## Tech

- ASP.NET Core MVC, .NET 9
- Entity Framework Core
- SQL Server
- ASP.NET Identity

## Features so far

- Medicine setup with box, strip and tablet units
- Category, company, generic and supplier setup
- Purchase entry with batch number and expiry date
- Stock saved per batch in tablets

## Coming next

- Billing with earliest expiry first
- Expiry and low stock alerts
- Returns, stock adjustment and daily reports

## Run

    dotnet restore
    dotnet ef database update
    dotnet run

Default login for development: owner / Owner@123

## Author

Muhammad Hashim