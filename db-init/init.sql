IF DB_ID('Weather_DB') IS NULL
BEGIN
    CREATE DATABASE Weather_DB;
END
GO

USE Weather_DB;
GO

IF NOT EXISTS (
    SELECT
        *
    FROM
        sysobjects
    WHERE
        name = 'WeatherForecasts'
        AND xtype = 'U'
)
BEGIN
    CREATE TABLE WeatherForecasts (
        Id INT PRIMARY KEY IDENTITY (1, 1),
        Date DATE NOT NULL,
        TemperatureC INT NOT NULL,
        Summary NVARCHAR(100) NULL
    );
END
GO
