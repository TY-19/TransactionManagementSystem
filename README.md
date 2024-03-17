## Transaction Management System
### Table of Contents
* [Introduction](#introduction)
* [Launch](#launch)
* [Technology](#technology)
* [External API](#external-api)
* [Functionality](#application-functionality)
* [Impementation Details](#impementation-details)
	* [DST and TimeZoneInfo](#dst-and-timezoneinfo)
	* [Current User Time Zone](#current-user-time-zone)
* [Notes](#notes)
	* [Time Zones and Filtering by Date](#time-zones-and-filtering-by-date)
	* [DST Effect on Time Displaying](#dst-effect-on-time-displaying)

### Introduction
This project was developed as a test task for the position of Junior .NET Developer.
The Transaction Management System is a simple API that provides functionality for importing/exporting transactions.

### Launch
Before starting the application, ensure that the connection string to your instance of MS SQL Server is provided by editing the **appsettings.json** file in the **src/WebAPI folder**.

### Technology
- ASP.NET 8
- EF Core (for database migrations)
- Dapper (for writing SQL queries to the database)
- ClosedXml (for writing .xlsx files)
- MediatR
- FluentValidation
- Serilog

### External API
- [timeapi.io](https://timeapi.io) -Provides information about time zones based on coordinates, IP, or IANA name.
- [freeipapi.com](https://freeipapi.com) - Used to retrieve the IP address of the server if the user sends a request from the local network.

### Functionality
- Import (create/update) transactions from a .csv file.
- Export transactions to a .xlsx file, supporting selection of specific columns, date filtering, and sorting by specified columns.
- Obtain transaction date and time in one of the following time zones:
    - Time zone of the client making the transaction, based on their coordinates (adjusts for daylight saving time rules).
    - Time zone of the current API user (calculated based on user IP), showing the time in the user's time zone when the transaction was completed (adjusts for user time zone DST).
    - Specified time zone: works like the users time zone, but with an explicitly provided time zone [IANA](https://www.iana.org/time-zones) name.
- Get transactions with predefined parameters:
    - Transactions completed in 2023 (clients' local time zones).
    - Transactions completed in 2023 (current API user's time zone).
    - Transactions completed in January 2024 (clients' local time zones).
    - Transactions completed in January 2024 (current API user's time zone).

### Impementation Details
#### DST and TimeZoneInfo
To check if Daylight Saving Time (DST) is applied, the TimeZoneInfo class is used to search for the time zone with the specific IANA name. Depending on the operating system, some IANA time zone names may be missing from the system registry. In such cases, fallback values for the time zone IDs can be specified in the `timezone-aliases.json` file.  
The functionality was tested on the following operating systems:
- Ubuntu 22.04, which contains all IANA names, thus no fallback values were necessary.
- Windows 10 (as of March 15, 2024), is missing 9 time zone IANA names, for which fallback values are provided in the `timezone-aliases.json` file by default. If the system lacks information about a specific time zone IANA and no fallback value is provided, daylight saving time calculation rules will be approximated.

#### Current User Time Zone
The time zone of the current user is determined based on their IP address. If the user accesses the application locally (server and user are in the same network), then the server IP is used.

### Notes
#### Time Zones and Filtering by Date
There is a difference between the client time zone and the current user or arbitrary time zone when filtering transactions by date.
- Example: 
   - Transaction A: 15.03.2024 00:30 UTC+10
   - Transaction B: 15.03.2024 23:30 UTC-10
   - Get transactions that happened on 15.03.2024:
- Using the client time zone while filtering will select all transactions that occur in this date range by **local time of transaction**. So both transactions A and B will be selected despite the fact that transaction B occurred 43 hours after transaction A.
- Using the user or arbitrary time zone while filtering considers what time was in the user's time zone when the transaction occurred and checks if this **local user time** (as opposed to the transaction time) falls within the specified range. For example, if the user time zone is UTC+2, neither transaction A nor B will be selected because according to the user's time zone, neither of them occurred on 15.03.2024 (A occured on 14.03.2024 16:30 UTC+2, and B on 16.03.2024 11:30 UTC+2).

#### DST effect on time displaying
When transactions are displayed in the time of the current API user (or arbitrary time zone) and this zone observes daylight saving time (DST), the DST adjustment is applied to the displayed transaction times. For example, transactions from a time zone without DST that occurred at the same time on different dates may be displayed with different times for a time zone that observes DST. For example, for Europe/Kyiv:  
15.03.2024 17:30 UTC-4 -> 15.03.2024 23:30 UTC+2 (DST is not active on March 15).  
15.06.2023 17:30 UTC-4 -> 16.06.2023 00:30 UTC+3 (DST is active on June 16).