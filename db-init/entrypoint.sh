#!/bin/bash
/opt/mssql/bin/sqlservr &

echo "Waiting for SQL Server to start..."
sleep 15

/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@123 -i /docker-entrypoint-initdb.d/init.sql

wait
