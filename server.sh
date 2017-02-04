#!/bin/sh
ASPNETCORE_URLS="https://*:14150" nohup dotnet run >run.log 2>&1 &
